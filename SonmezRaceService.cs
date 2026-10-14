using System.Numerics;
using System.Reflection;
using System.Text.Json;
using AssettoServer.Network.ClientMessages;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace SonmezRacePlugin;

public class SonmezRaceService : BackgroundService
{
    private const float Dt = 0.1f;
    private const string ScoreFile = "cfg/sonmez_scores.json";

    private readonly SonmezRaceConfiguration _cfg;
    private readonly EntryCarManager _entryCarManager;
    private readonly Dictionary<byte, CarState> _states = new();
    private readonly Dictionary<(byte, byte), PairState> _pairs = new();
    private readonly List<Race> _races = new();
    private readonly Dictionary<ulong, int> _totals = new();
    private readonly object _lock = new();
    private readonly int[] _yawVotes = new int[8];

    public SonmezRaceService(SonmezRaceConfiguration cfg,
        EntryCarManager entryCarManager,
        CSPServerScriptProvider scriptProvider,
        CSPClientMessageTypeManager cspClientMessageTypeManager)
    {
        _cfg = cfg;
        _entryCarManager = entryCarManager;

        scriptProvider.AddScript(
            Assembly.GetExecutingAssembly().GetManifestResourceStream("sonmezrace.lua")!,
            "sonmezrace.lua");

        cspClientMessageTypeManager.RegisterOnlineEvent<HornPacket>(OnHorn);
        LoadTotals();
    }

    // ------------------------------------------------------------------ main loop
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Dt * 1000));
        double lastSave = Now();
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    lock (_lock) Tick();
                    if (Now() - lastSave > 60) { lastSave = Now(); SaveTotals(); }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "SonmezRacePlugin tick hatasi");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // sunucu kapaniyor
        }
        finally
        {
            SaveTotals();
        }
    }

    private static double Now() => Environment.TickCount64 / 1000.0;

    // ------------------------------------------------------------------ tick
    private void Tick()
    {
        double now = Now();
        var active = new List<(EntryCar Car, CarState St)>();

        foreach (var car in _entryCarManager.EntryCars)
        {
            var client = car.Client;
            if (client == null || !client.HasSentFirstUpdate)
            {
                _states.Remove(car.SessionId);
                continue;
            }

            if (!_states.TryGetValue(car.SessionId, out var st) || !ReferenceEquals(st.Client, client))
            {
                st = new CarState(client) { Total = _totals.GetValueOrDefault(client.Guid) };
                _states[car.SessionId] = st;
            }
            active.Add((car, st));
        }

        foreach (var (car, st) in active)
            UpdateCar(car, st, now);

        UpdateMakas(active, now);
        UpdateRaces(active, now);

        foreach (var (car, st) in active)
            SendHud(car, st, now);
    }

    // ------------------------------------------------------------------ per car: drift / crash / combo
    private void UpdateCar(EntryCar car, CarState st, double now)
    {
        var pos = car.Status.Position;
        var vel = car.Status.Velocity;
        float speedMs = new Vector2(vel.X, vel.Z).Length();
        float speedKmh = speedMs * 3.6f;

        st.Crashed = false;
        st.Speed = speedKmh;
        st.Pos = pos;

        float moved = st.HasLast ? new Vector2(pos.X - st.LastPos.X, pos.Z - st.LastPos.Z).Length() : 0;
        bool teleported = !st.HasLast || moved > 80f;

        if (teleported)
        {
            st.DriftPending = 0; st.DriftTime = 0; st.DriftGrace = 0;
        }
        else
        {
            if (st.LastSpeed - speedKmh > _cfg.CrashSpeedDropKmh)
            {
                st.Crashed = true;
                st.DriftPending = 0; st.DriftTime = 0; st.DriftGrace = 0;
                st.Combo = 0;
            }

            if (st.Race is { State: RaceState.Racing })
                st.Travel += moved;
        }

        st.LastPos = pos;
        st.LastSpeed = speedKmh;
        st.HasLast = true;

        // ---- DRIFT
        bool drifting = false;
        if (!st.Crashed && !teleported && speedKmh >= _cfg.DriftMinSpeedKmh)
        {
            float velHeading = MathF.Atan2(vel.X, vel.Z);
            float yaw = car.Status.Rotation.X;
            if (speedKmh > 60f) VoteYaw(velHeading, yaw);

            float carHeading = YawToHeading(yaw);
            float angDeg = MathF.Abs(NormalizeAngle(velHeading - carHeading)) * 180f / MathF.PI;

            if (angDeg >= _cfg.DriftMinAngleDeg && angDeg <= _cfg.DriftMaxAngleDeg)
            {
                drifting = true;
                st.DriftGrace = 0;
                st.DriftTime += Dt;
                st.DriftPending += Dt * (angDeg / 45f) * (speedKmh / 60f) * _cfg.DriftPointsPerSecond;
                st.ShowKind = 1;
                st.ShowPts = (int)st.DriftPending * Math.Max(1, st.Combo);
                st.ShowUntil = now + 1.5;
            }
        }

        if (!drifting && st.DriftTime > 0)
        {
            st.DriftGrace += Dt;
            if (st.DriftGrace > 0.5f)
            {
                if (st.DriftTime >= _cfg.DriftMinSeconds && st.DriftPending >= 1)
                    Bank(st, 1, (int)st.DriftPending, now);
                st.DriftPending = 0; st.DriftTime = 0; st.DriftGrace = 0;
            }
        }

        if (st.Combo > 0 && now - st.LastBank > _cfg.ComboWindowSeconds)
            st.Combo = 0;
    }

    private void Bank(CarState st, byte kind, int basePts, double now)
    {
        if (st.Combo > 0 && now - st.LastBank <= _cfg.ComboWindowSeconds)
            st.Combo = Math.Min(_cfg.MaxCombo, st.Combo + 1);
        else
            st.Combo = 1;

        int pts = basePts * st.Combo;
        st.Session += pts;
        st.Total += pts;
        st.LastBank = now;
        st.ShowKind = kind;
        st.ShowPts = pts;
        st.ShowUntil = now + 2.0;
        _totals[st.Client.Guid] = st.Total;
    }

    // ------------------------------------------------------------------ makas
    private void UpdateMakas(List<(EntryCar Car, CarState St)> active, double now)
    {
        var seen = new HashSet<(byte, byte)>();

        foreach (var (carA, a) in active)
        {
            foreach (var (carB, b) in active)
            {
                if (carA.SessionId == carB.SessionId) continue;

                var key = (carA.SessionId, carB.SessionId);
                var va = new Vector2(carA.Status.Velocity.X, carA.Status.Velocity.Z);
                var vb = new Vector2(carB.Status.Velocity.X, carB.Status.Velocity.Z);
                var delta = new Vector2(a.Pos.X - b.Pos.X, a.Pos.Z - b.Pos.Z);

                if (delta.Length() > 25f) { _pairs.Remove(key); continue; }
                if (va.Length() < 1f || vb.Length() < 1f) continue;

                var dirA = Vector2.Normalize(va);
                var dirB = Vector2.Normalize(vb);
                if (Vector2.Dot(dirA, dirB) < 0.5f) { _pairs.Remove(key); continue; }

                float forward = Vector2.Dot(delta, dirB);
                float lateral = MathF.Abs(delta.X * dirB.Y - delta.Y * dirB.X);

                seen.Add(key);
                if (!_pairs.TryGetValue(key, out var pair)) { pair = new PairState(); _pairs[key] = pair; }

                if (pair.HasLast && pair.LastForward < 0 && forward >= 0
                    && lateral <= _cfg.MakasMaxLateralMeters
                    && MathF.Abs(pair.LastForward) < 15f
                    && a.Speed >= _cfg.MakasMinSpeedKmh
                    && b.Speed >= _cfg.MakasMinOtherSpeedKmh
                    && !a.Crashed && !b.Crashed
                    && now - pair.LastScore > _cfg.MakasCooldownSeconds)
                {
                    float closeness = 1f + (_cfg.MakasMaxLateralMeters - lateral) / _cfg.MakasMaxLateralMeters;
                    float speedF = a.Speed / _cfg.MakasMinSpeedKmh;
                    int pts = (int)(_cfg.MakasBasePoints * closeness * speedF);
                    Bank(a, 2, Math.Max(1, pts), now);
                    pair.LastScore = now;
                }

                pair.LastForward = forward;
                pair.HasLast = true;
            }
        }

        foreach (var k in _pairs.Keys.Where(k => !seen.Contains(k)).ToList())
            _pairs.Remove(k);
    }

    // ------------------------------------------------------------------ yaw kalibrasyonu
    private float YawToHeading(float yaw)
    {
        int mode = _cfg.YawMode > 0 ? Math.Clamp(_cfg.YawMode, 1, 8) - 1 : BestYawMode();
        float sign = mode < 4 ? 1f : -1f;
        float offset = (mode % 4) * MathF.PI / 2f;
        return sign * yaw + offset;
    }

    private int BestYawMode()
    {
        int best = 0;
        for (int i = 1; i < 8; i++)
            if (_yawVotes[i] > _yawVotes[best]) best = i;
        return best;
    }

    private void VoteYaw(float velHeading, float yaw)
    {
        for (int i = 0; i < 8; i++)
        {
            float sign = i < 4 ? 1f : -1f;
            float h = sign * yaw + (i % 4) * MathF.PI / 2f;
            if (MathF.Abs(NormalizeAngle(velHeading - h)) < 0.26f) // ~15 derece
                _yawVotes[i]++;
        }
        if (_yawVotes.Max() > 1_000_000)
            for (int i = 0; i < 8; i++) _yawVotes[i] /= 2;
    }

    private static float NormalizeAngle(float a)
    {
        while (a > MathF.PI) a -= 2 * MathF.PI;
        while (a < -MathF.PI) a += 2 * MathF.PI;
        return a;
    }

    // ------------------------------------------------------------------ korna / yaris baslatma
    private void OnHorn(ACTcpClient client, HornPacket packet)
    {
        lock (_lock) { TriggerRace(client); }
    }

    /// <summary>Korna sayaci: HornCount kez HornWindowSeconds icinde basilirsa yaris baslar.</summary>
    public void TriggerRace(ACTcpClient client, bool force = false)
    {
        lock (_lock)
        {
            if (!_states.TryGetValue(client.EntryCar.SessionId, out var st)) return;
            double now = Now();

            st.Horns.Enqueue(now);
            while (st.Horns.Count > 0 && now - st.Horns.Peek() > _cfg.HornWindowSeconds)
                st.Horns.Dequeue();

            if (!force && st.Horns.Count < _cfg.HornCount) return;
            st.Horns.Clear();

            if (st.Race != null || now < st.RaceCooldownUntil) return;

            var near = new List<CarState> { st };
            foreach (var other in _states.Values)
            {
                if (ReferenceEquals(other, st) || other.Race != null || now < other.RaceCooldownUntil) continue;
                float d = new Vector2(other.Pos.X - st.Pos.X, other.Pos.Z - st.Pos.Z).Length();
                if (d <= _cfg.RaceRadiusMeters) near.Add(other);
            }

            if (near.Count < 2)
            {
                client.SendChatMessage($"Yakininda yaris icin oyuncu yok ({(int)_cfg.RaceRadiusMeters} m).");
                st.RaceCooldownUntil = now + 3;
                return;
            }

            var race = new Race
            {
                State = RaceState.Countdown,
                CountdownEnd = now + _cfg.RaceCountdownSeconds,
                Deadline = now + _cfg.RaceCountdownSeconds + _cfg.RaceTimeoutSeconds,
                Participants = near
            };
            foreach (var p in near)
            {
                p.Race = race;
                p.Travel = 0;
                p.ResultUntil = 0;
                p.Client.SendChatMessage($"YARIS! {st.Client.Name} yaris baslatti. {(int)(_cfg.RaceLengthMeters / 100f) / 10f:0.0} km, {_cfg.RaceCountdownSeconds} sn sonra basliyor!");
            }
            _races.Add(race);
        }
    }

    // ------------------------------------------------------------------ yarislar
    private void UpdateRaces(List<(EntryCar Car, CarState St)> active, double now)
    {
        foreach (var race in _races.ToList())
        {
            race.Participants.RemoveAll(p => !_states.ContainsValue(p) || p.Race != race);

            if (race.Participants.Count < 2)
            {
                EndRace(race, now, null);
                foreach (var p in race.Participants) p.Client.SendChatMessage("Yaris iptal edildi.");
                continue;
            }

            if (race.State == RaceState.Countdown)
            {
                if (now >= race.CountdownEnd)
                {
                    race.State = RaceState.Racing;
                    race.StartTime = now;
                    foreach (var p in race.Participants) p.Travel = 0;
                }
                continue;
            }

            if (now > race.Deadline)
            {
                foreach (var p in race.Participants) p.Client.SendChatMessage("Yaris zaman asimina ugradi.");
                EndRace(race, now, null);
                continue;
            }

            var winner = race.Participants
                .Where(p => p.Travel >= _cfg.RaceLengthMeters)
                .OrderByDescending(p => p.Travel)
                .FirstOrDefault();

            if (winner != null)
            {
                double time = now - race.StartTime;
                winner.Session += _cfg.RaceWinnerBonus;
                winner.Total += _cfg.RaceWinnerBonus;
                _totals[winner.Client.Guid] = winner.Total;
                _entryCarManager.BroadcastChat(
                    $"YARIS BITTI! Birinci: {winner.Client.Name} - {time:0.0} sn (+{_cfg.RaceWinnerBonus} puan)");
                EndRace(race, now, (winner, (float)time));
            }
        }
    }

    private void EndRace(Race race, double now, (CarState Winner, float Time)? result)
    {
        foreach (var p in race.Participants)
        {
            p.Race = null;
            p.RaceCooldownUntil = now + _cfg.RaceCooldownSeconds;
            if (result != null)
            {
                p.ResultUntil = now + _cfg.ResultShowSeconds;
                p.WinnerName = Trim(result.Value.Winner.Client.Name);
                p.WinnerTime = result.Value.Time;
            }
        }
        _races.Remove(race);
    }

    private static string Trim(string s) => s.Length > 16 ? s[..16] : s;

    // ------------------------------------------------------------------ HUD
    private void SendHud(EntryCar car, CarState st, double now)
    {
        var p = new HudPacket
        {
            Session = st.Session,
            Total = st.Total,
            Combo = (byte)(st.Combo >= 2 ? st.Combo : 0),
            Kind = (byte)(now < st.ShowUntil ? st.ShowKind : 0),
            Pts = now < st.ShowUntil ? st.ShowPts : 0,
            Length = _cfg.RaceLengthMeters,
            Winner = ""
        };

        if (st.Race is { } race)
        {
            p.RaceCount = (byte)race.Participants.Count;
            if (race.State == RaceState.Countdown)
            {
                p.RaceState = 1;
                p.Countdown = (byte)Math.Max(0, Math.Ceiling(race.CountdownEnd - now));
            }
            else
            {
                p.RaceState = 2;
                p.Distance = MathF.Round(st.Travel / 10f) * 10f;
                p.Rank = (byte)(race.Participants.OrderByDescending(x => x.Travel).ToList().IndexOf(st) + 1);
            }
        }
        else if (now < st.ResultUntil)
        {
            p.RaceState = 3;
            p.Winner = st.WinnerName;
            p.WinnerTime = MathF.Round(st.WinnerTime * 10f) / 10f;
        }

        var snap = (p.Session, p.Total, p.Combo, p.Kind, p.Pts, p.RaceState, p.Countdown, p.Rank, p.RaceCount, p.Distance, p.Winner, p.WinnerTime);
        bool changed = !snap.Equals(st.LastSnap);
        if (changed && now - st.LastSend < 0.2) return;   // en fazla 5 Hz
        if (!changed && now - st.LastSend < 2.0) return;  // degisim yoksa 2 sn'de bir

        st.LastSnap = snap;
        st.LastSend = now;
        st.Client.SendPacket(p);
    }

    // ------------------------------------------------------------------ kalici toplam puan
    private void LoadTotals()
    {
        try
        {
            if (!File.Exists(ScoreFile)) return;
            var d = JsonSerializer.Deserialize<Dictionary<ulong, int>>(File.ReadAllText(ScoreFile));
            if (d != null) foreach (var kv in d) _totals[kv.Key] = kv.Value;
        }
        catch (Exception ex) { Log.Warning(ex, "Sonmez puan dosyasi okunamadi"); }
    }

    private void SaveTotals()
    {
        try
        {
            Dictionary<ulong, int> copy;
            lock (_lock) copy = new Dictionary<ulong, int>(_totals);
            Directory.CreateDirectory(Path.GetDirectoryName(ScoreFile)!);
            File.WriteAllText(ScoreFile, JsonSerializer.Serialize(copy));
        }
        catch (Exception ex) { Log.Warning(ex, "Sonmez puan dosyasi yazilamadi"); }
    }

    // ------------------------------------------------------------------ tipler
    private enum RaceState { Countdown, Racing }

    private class Race
    {
        public RaceState State;
        public double CountdownEnd, StartTime, Deadline;
        public List<CarState> Participants = new();
    }

    private class PairState
    {
        public bool HasLast;
        public float LastForward;
        public double LastScore;
    }

    private class CarState
    {
        public CarState(ACTcpClient client) { Client = client; }
        public ACTcpClient Client;
        public Vector3 Pos, LastPos;
        public bool HasLast, Crashed;
        public float Speed, LastSpeed;

        public int Session, Total, Combo;
        public double LastBank;

        public float DriftPending, DriftTime, DriftGrace;
        public byte ShowKind; public int ShowPts; public double ShowUntil;

        public Queue<double> Horns = new();
        public Race? Race;
        public float Travel;
        public double RaceCooldownUntil, ResultUntil;
        public string WinnerName = ""; public float WinnerTime;

        public object? LastSnap; public double LastSend;
    }
}

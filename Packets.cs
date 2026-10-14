using AssettoServer.Network.ClientMessages;

namespace SonmezRacePlugin;

// Alan adlari alfabetik siralanir -> C# ve Lua tarafinda ayni sirada olmali (f01, f02 ...)
[OnlineEvent(Key = "sonmezHud")]
public class HudPacket : OnlineEvent<HudPacket>
{
    [OnlineEventField(Name = "f01_session")] public int Session;
    [OnlineEventField(Name = "f02_total")] public int Total;
    [OnlineEventField(Name = "f03_combo")] public byte Combo;
    [OnlineEventField(Name = "f04_kind")] public byte Kind;       // 0 yok, 1 drift, 2 makas
    [OnlineEventField(Name = "f05_pts")] public int Pts;
    [OnlineEventField(Name = "f06_rstate")] public byte RaceState; // 0 yok, 1 geri sayim, 2 yaris, 3 sonuc
    [OnlineEventField(Name = "f07_count")] public byte Countdown;
    [OnlineEventField(Name = "f08_rank")] public byte Rank;
    [OnlineEventField(Name = "f09_rcount")] public byte RaceCount;
    [OnlineEventField(Name = "f10_dist")] public float Distance;
    [OnlineEventField(Name = "f11_len")] public float Length;
    [OnlineEventField(Name = "f12_winner", Size = 24)] public string Winner = "";
    [OnlineEventField(Name = "f13_wtime")] public float WinnerTime;
}

[OnlineEvent(Key = "sonmezHorn")]
public class HornPacket : OnlineEvent<HornPacket>
{
    [OnlineEventField(Name = "f01_seq")] public byte Seq;
}

-- Sonmez Game World | Makas - Drift - Yaris HUD (CSP online script)

local LOGO_B64 = [==[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]==]

-- ---------------------------------------------------------------- base64 -> binary
local function b64decode(data)
  local b = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'
  local map = {}
  for i = 1, #b do map[b:byte(i)] = i - 1 end
  local out, n, bits, acc = {}, 0, 0, 0
  local chunk = {}
  for i = 1, #data do
    local v = map[data:byte(i)]
    if v then
      acc = acc * 64 + v
      bits = bits + 6
      if bits >= 8 then
        bits = bits - 8
        local div = 2 ^ bits
        local byte = math.floor(acc / div)
        acc = acc - byte * div
        n = n + 1
        chunk[n] = string.char(byte)
        if n >= 4096 then out[#out + 1] = table.concat(chunk); chunk = {}; n = 0 end
      end
    end
  end
  out[#out + 1] = table.concat(chunk)
  return table.concat(out)
end

local logo = nil
pcall(function() logo = ui.decodeImage(b64decode(LOGO_B64)) end)
if not logo then ac.log('SonmezRace: logo yuklenemedi') end

-- ---------------------------------------------------------------- state
local hud = {
  session = 0, total = 0, combo = 0, kind = 0, pts = 0,
  rstate = 0, count = 0, rank = 0, rcount = 0, dist = 0, len = 2000, winner = '', wtime = 0,
  recv = 0
}

local hudEvent = ac.OnlineEvent({
  ac.StructItem.key('sonmezHud'),
  f01_session = ac.StructItem.int32(),
  f02_total = ac.StructItem.int32(),
  f03_combo = ac.StructItem.byte(),
  f04_kind = ac.StructItem.byte(),
  f05_pts = ac.StructItem.int32(),
  f06_rstate = ac.StructItem.byte(),
  f07_count = ac.StructItem.byte(),
  f08_rank = ac.StructItem.byte(),
  f09_rcount = ac.StructItem.byte(),
  f10_dist = ac.StructItem.float(),
  f11_len = ac.StructItem.float(),
  f12_winner = ac.StructItem.string(24),
  f13_wtime = ac.StructItem.float(),
}, function(sender, d)
  hud.session = d.f01_session; hud.total = d.f02_total
  hud.combo = d.f03_combo; hud.kind = d.f04_kind; hud.pts = d.f05_pts
  hud.rstate = d.f06_rstate; hud.count = d.f07_count
  hud.rank = d.f08_rank; hud.rcount = d.f09_rcount
  hud.dist = d.f10_dist; hud.len = d.f11_len
  hud.winner = d.f12_winner; hud.wtime = d.f13_wtime
  hud.recv = os.clock()
end)

local hornEvent = ac.OnlineEvent({
  ac.StructItem.key('sonmezHorn'),
  f01_seq = ac.StructItem.byte(),
}, function() end)

-- ---------------------------------------------------------------- korna algilama
local hornSeq, hornWas = 0, false

local function hornDown()
  local car = ac.getCar(0)
  if not car then return false end
  local ok, v = pcall(function() return car.hornActive end)
  if ok and v ~= nil then return v and true or false end
  ok, v = pcall(function() return car.horn end)
  if ok and v ~= nil then return v and true or false end
  ok, v = pcall(function() return ac.isKeyDown(ac.KeyIndex.H) end)   -- varsayilan korna tusu
  if ok and v ~= nil then return v and true or false end
  return false
end

function script.update(dt)
  local down = hornDown()
  if down and not hornWas then
    hornSeq = (hornSeq + 1) % 256
    hornEvent({ f01_seq = hornSeq })
  end
  hornWas = down
end

-- ---------------------------------------------------------------- cizim
local RED = rgbm(0.92, 0.10, 0.10, 1)
local WHITE = rgbm(1, 1, 1, 1)
local GREY = rgbm(0.75, 0.75, 0.78, 1)

local function text(str, size, pos, col)
  ui.dwriteDrawText(str, size, pos, col or WHITE)
end

local function textRight(str, size, rightX, y, col)
  local w = ui.measureDWriteText(str, size).x
  ui.dwriteDrawText(str, size, vec2(rightX - w, y), col or WHITE)
end

local function fmt(n) return string.format('%d', n) end

function script.drawUI()
  local sw = ac.getUI().windowSize.x
  local W = 340
  local H = 34 + 100 + (hud.rstate ~= 0 and 86 or 0)

  ui.transparentWindow('sonmezHud', vec2(sw - W - 24, 90), vec2(W, H), function()
    local o = ui.windowPos()
    ui.pushDWriteFont('Segoe UI;Weight=Bold')

    -- zemin + header
    ui.drawRectFilled(o, o + vec2(W, H), rgbm(0.05, 0.05, 0.06, 0.90))
    ui.drawRectFilled(o, o + vec2(W, 32), rgbm(0.11, 0.11, 0.12, 0.95))
    ui.drawRectFilled(o + vec2(0, 32), o + vec2(W, 34), RED)
    ui.drawRectFilled(o, o + vec2(4, H), RED)

    -- filigran logo
    if logo then
      ui.drawImage(logo, o + vec2(W / 2 - 40, 38), o + vec2(W / 2 + 40, 118), rgbm(1, 1, 1, 0.30))
      ui.drawImage(logo, o + vec2(10, 4), o + vec2(36, 30), WHITE)
    end

    text('SÖNMEZ GAME WORLD', 15, o + vec2(42, 7), WHITE)
    textRight('MAKAS • DRİFT • YARIŞ', 9, W - 8, 12, GREY)

    -- puanlar
    text('OTURUM PUANI', 10, o + vec2(14, 42), GREY)
    text(fmt(hud.session), 34, o + vec2(14, 54), WHITE)
    textRight('TOPLAM', 10, W - 14, 42, GREY)
    textRight(fmt(hud.total), 20, W - 14, 56, WHITE)

    if hud.kind == 1 then
      text('DRİFT +' .. fmt(hud.pts), 17, o + vec2(14, 100), WHITE)
    elseif hud.kind == 2 then
      text('MAKAS +' .. fmt(hud.pts), 17, o + vec2(14, 100), WHITE)
    end
    if hud.combo >= 2 then
      textRight('KOMBO x' .. fmt(hud.combo), 14, W - 14, 102, RED)
    end

    -- yaris bolumu
    if hud.rstate ~= 0 then
      local y = 134
      ui.drawRectFilled(o + vec2(4, y), o + vec2(W, y + 2), RED)
      if hud.rstate == 1 then
        text('YARIŞ BAŞLIYOR', 12, o + vec2(14, y + 8), WHITE)
        text(fmt(hud.count), 42, o + vec2(14, y + 24), WHITE)
      elseif hud.rstate == 2 then
        text('YARIŞ', 12, o + vec2(14, y + 8), WHITE)
        textRight(fmt(hud.rank) .. ' / ' .. fmt(hud.rcount), 22, W - 14, y + 6, WHITE)
        local frac = math.saturate(hud.dist / math.max(1, hud.len))
        local bx, by, bw = 14, y + 42, W - 28
        ui.drawRectFilled(o + vec2(bx, by), o + vec2(bx + bw, by + 8), rgbm(0.25, 0.25, 0.27, 1))
        ui.drawRectFilled(o + vec2(bx, by), o + vec2(bx + bw * frac, by + 8), RED)
        text(string.format('%.2f / %.2f km', hud.dist / 1000, hud.len / 1000), 11, o + vec2(14, y + 56), GREY)
      elseif hud.rstate == 3 then
        text('BİRİNCİ', 11, o + vec2(14, y + 8), RED)
        text(hud.winner, 24, o + vec2(14, y + 22), WHITE)
        text(string.format('Süre: %.1f sn', hud.wtime), 11, o + vec2(14, y + 58), GREY)
      end
    end

    -- baglanti noktasi
    local alive = (os.clock() - hud.recv) < 6
    ui.drawCircleFilled(o + vec2(W - 12, H - 12), 4, alive and rgbm(0.2, 0.9, 0.25, 1) or rgbm(0.5, 0.5, 0.5, 1), 12)

    ui.popDWriteFont()
  end)
end

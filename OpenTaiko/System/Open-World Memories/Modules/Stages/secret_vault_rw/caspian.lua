---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, inject-field
local Dialogue = require("dialogue")
local Easing = require("Easing")
local NavInput = require("NavInput")
local Tex = require("tex")

local C = {}

local FOOT_X, FOOT_Y = 960, 905
local FRAMES = { neutral = "Neutral", happy = "Happy", laugh = "Laugh", grumpy = "Grumpy" }
local BOX = { x = 300, y = 822, w = 1320, h = 210 }
local CPS = 38
local LAUGH = 1.4
local BLIP_EVERY = 3
local VOICES = 4
-- marks that make no sound: wide (CJK) punctuation and spaces
local SILENT = {}
for _, ch in ipairs({ "、", "。", "，", "．", "！", "？", "：", "；", "…", "‥", "・", "「", "」", "『", "』", "（", "）",
    "～", "—", "–", "　", "¡", "¿", "«", "»", "“", "”", "‘", "’" }) do SILENT[ch] = true end

-- the box in the cabin's dark marble and bronze
local THEME = {
    face = { 46, 54, 66, 255 }, face2 = { 28, 34, 44, 255 }, outline = { 196, 152, 86, 255 },
    namePill = { 210, 166, 96, 255 }, namePill2 = { 152, 108, 54, 255 }, nameText = { 255, 250, 236 },
    text = { 244, 238, 226 }, caret = { 232, 192, 112 }, shadow = { 0, 10, 20, 110 },
}

-- event -> lines { expression, English }; keys VAULT_TALK_<EVENT>_<n>
local LINES = {
    firstmeet = {
        { "happy",   "Well, blow me down! A fresh face in me cabin, after all these long tides!" },
        { "neutral", "The name's Caspian. Mr.Caspian to you. I sailed every sea there is, and now I keep watch over this old hoard." },
        { "happy",   "Every chest here wants its own proper key, matey. Bring the right one and try your luck." },
        { "laugh",   "But mind ye... now and then a key snaps clean in the lock. Har har!" },
    },
    gift = {
        { "happy",   "Here, take this key on the house. Don't go sayin' old Caspian never did right by ye!" },
    },
    greet = {
        { "happy",   "Ahoy there, matey! Come aboard, come aboard!" },
        { "neutral", "Back again, are ye? The chests be waitin' right where ye left 'em." },
        { "happy",   "Ah, a visitor! Old Caspian was gettin' lonely down here in the deep." },
        { "grumpy",  "Who's knockin' on me hatch? ...Oh, it's you, shipmate. Come in, then." },
        { "laugh",   "Har! The tide washed ye back in, did it? Knew ye couldn't stay away!" },
    },
    warn = {
        { "grumpy",  "Oh, it's you again. Ye'll behave this time, won't ye? Keep yer fists off me chests." },
        { "grumpy",  "Back, are ye? Mind yer manners this time, or it's out the hatch again!" },
        { "grumpy",  "Hmph. One more smashed chest and ye'll be swimmin' home. Behave yerself, matey." },
    },
    prompt = {
        { "neutral", "So, which chest will it be today?" },
        { "neutral", "Take your time, matey, and pick one." },
        { "happy",   "Choose well. The sea never gives back what she takes." },
        { "happy",   "Every chest has a tale inside. Which one do ye fancy?" },
    },
    confirm = {
        { "neutral", "That'll cost ye one key. Sure about it?" },
        { "happy",   "Once the key turns, there's no turnin' back. Ready?" },
        { "happy",   "A bold choice! Say the word and we'll crack her open." },
    },
    empty = {
        { "grumpy",  "Nothin' left in that one but coin and bad luck. Still want a go?" },
        { "neutral", "I'll be straight with ye: that chest's been picked near clean. Still turnin' the key?" },
    },
    nokey = {
        { "grumpy",  "Ye can't crack that one without the proper key, matey." },
        { "grumpy",  "No key, no treasure. That's the law o' the sea!" },
        { "neutral", "Your pockets be empty, sailor. Come back with the right key." },
    },
    snap = {
        { "laugh",   "Har har! Snapped clean in two! Ye've a grip like a kraken!" },
        { "laugh",   "Ooh, there goes your key! The lock wins this round, matey!" },
        { "laugh",   "Har! I've seen many a key break, but never one so sorry-lookin'!" },
        { "laugh",   "Hear that snap? That's the sound o' bad luck, that is!" },
        { "laugh",   "Me old parrot turns a key better than that, and he's got no hands!" },
    },
    hey = {
        { "grumpy",  "Hey! Hands off me chests, ye scallywag!" },
        { "grumpy",  "Oi! That chest be older than me grandmother!" },
        { "grumpy",  "Hey! Ye'll be swabbin' me floor for that, matey!" },
    },
    kick = {
        { "grumpy",  "That's three o' me chests! Out, ye bilge rat!" },
        { "grumpy",  "Three chests on me floor! Off me ship, ye barnacle-brained landlubber!" },
        { "grumpy",  "Enough! Out ye go, before I have ye keelhauled!" },
    },
    happy = {
        { "happy",   "Would ye look at that haul! Fortune smiles on ye today!" },
        { "happy",   "Har, the chest was kind to ye! Fine plunder, fine plunder!" },
        { "happy",   "Now that's a proper treasure, matey!" },
    },
    keygot = {
        { "happy",   "A key inside a chest? Luckiest thing I've seen all week!" },
        { "happy",   "Look there! The chest paid ye back with a shiny new key!" },
    },
    bye = {
        { "happy",   "Fair winds, matey! Mind the current on your way up." },
        { "neutral", "Off ye go, then. Old Caspian'll keep the lantern lit for ye." },
        { "happy",   "Calm seas to ye! Don't be a stranger, now." },
    },
}

local ctx = nil
local dlg = nil
local mode = nil          -- "talk", "remark" or nil
local fresh = false       -- the talk started this frame: its first input waits for the next one
local lastPick = {}
local lastVoice = nil
local remarkBlipAt = 0
local clock = 0
C.expr = "neutral"
local exprT, hopT, hopAmp = -10, nil, 0

local function name() return Tex.tr("VAULT_CASPIAN", "Mr.Caspian") end

local function line(event, i)
    return Tex.tr("VAULT_TALK_" .. event:upper() .. "_" .. i, LINES[event][i][2])
end

local function pick(event)
    local n = #LINES[event]
    local i = math.random(1, n)
    if n > 1 and i == lastPick[event] then i = i % n + 1 end
    lastPick[event] = i
    return i
end

function C.setExpr(e)
    if FRAMES[e] == nil then return end
    if e ~= C.expr then
        C.expr = e
        exprT, hopT, hopAmp = clock, clock, 1
    else
        hopT, hopAmp = clock, 0.4
    end
end

function C.hop() hopT, hopAmp = clock, 1.4 end

-- the n-th letter shown of the line being typed
local function shownChar(n)
    if dlg == nil or dlg.glines == nil then return nil end
    for _, l in ipairs(dlg.glines) do
        if n <= l.n then return l.chars[n] end
        n = n - l.n
    end
    return nil
end

local function blip()
    local ch = shownChar(math.floor(dlg.revealed or 0))
    if ch == nil or SILENT[ch] or ch:match("^[%s%p]$") then return end
    local i = math.random(1, VOICES)
    if i == lastVoice then i = i % VOICES + 1 end
    lastVoice = i
    local s = ctx.snd["VoiceBlip" .. i]
    if s then s:Play() end
end

function C.init(c)
    ctx = c
    dlg = Dialogue.new{
        gfont = c.fonts.talk, gfontName = c.fonts.name,
        ui = "popui", theme = THEME, box = BOX, portraitSize = 0, cps = CPS,
        sfx = { click = function() if c.snd.Decide then c.snd.Decide:Play() end end },
        onExpr = function(e) C.setExpr(e) end,
        onBlip = blip, blipEvery = BLIP_EVERY,
        advanceInput = function() return NavInput.cancel() end,
    }
end

-- the font the speaker's name is drawn in (sized to fit the name tag)
function C.setNameFont(f)
    if dlg then dlg.gfontName = f end
end

function C.reset()
    mode, fresh = nil, false
    C.expr = "neutral"
    exprT, hopT, hopAmp = -10, nil, 0
    if dlg then dlg.activeFlag = false end
end

function C.dispose()
    if dlg then dlg:dispose() end
end

-- a line's text has no braces of its own, so a {expr:} tag in front is the only command in it
local function node(event, i, tagged)
    local e = LINES[event][i][1]
    local text = line(event, i):gsub("[{}]", "")
    return { name = name(), text = (tagged and ("{expr:" .. e .. "}") or "") .. text, expr = e }
end

local function start(nodes, how)
    if #nodes == 0 then return end
    dlg:start(nodes)
    dlg.caret = how == "talk"
    mode, fresh = how, how == "talk"
    remarkBlipAt = 0
    C.setExpr(nodes[1].expr)
end

-- the first line's face is set when the talk starts; every later line sets its own as it begins
function C.talkFirst(gifted)
    local nodes = {}
    local n = #LINES.firstmeet
    for i = 1, n do
        if gifted and i == n then nodes[#nodes + 1] = node("gift", 1, true) end
        nodes[#nodes + 1] = node("firstmeet", i, i > 1)
    end
    start(nodes, "talk")
end

function C.talk(event)
    if LINES[event] == nil then return end
    start({ node(event, pick(event), false) }, "talk")
end

function C.remark(event)
    if LINES[event] == nil then return end
    start({ node(event, pick(event), false) }, "remark")
end

function C.talking() return mode == "talk" end

function C.update(dt, now)
    clock = now
    if mode == "talk" then
        if fresh then fresh = false; return nil end
        if dlg:update(dt) == "done" then
            -- the last line stays up as a remark, so the box can fade out with it
            local last = dlg.node
            mode = nil
            if last ~= nil then
                dlg:start({ last })
                dlg.revealed = dlg.total or 0
                dlg.caret = false
                mode = "remark"
            end
            return "done"
        end
    elseif mode == "remark" and dlg.activeFlag then
        local total = dlg.total or 0
        if (dlg.revealed or 0) < total then
            dlg.revealed = math.min(total, (dlg.revealed or 0) + CPS * dt)
            if dlg.revealed >= remarkBlipAt + BLIP_EVERY then
                remarkBlipAt = dlg.revealed
                blip()
            end
        end
    end
    return nil
end

local function hopShape(b, amp)
    if b == nil or b < 0 or b >= 0.62 then return 1, 1, 0 end
    if b < 0.08 then
        local k = Easing.outQuad(b / 0.08) * amp
        return 1 + 0.06 * k, 1 - 0.07 * k, 0
    end
    if b < 0.38 then
        local u = (b - 0.08) / 0.30
        local v = math.abs(math.cos(u * math.pi)) * amp
        return 1 - 0.04 * v, 1 + 0.06 * v, 16 * amp * math.sin(u * math.pi)
    end
    local d = (b - 0.38) / 0.24
    local e = (1 - d) * (1 - d) * math.cos(d * 3 * math.pi) * amp
    return 1 + 0.06 * e, 1 - 0.07 * e, 0
end

function C.drawBody(now)
    clock = now
    local tex = ctx.tex["Cabin/Caspian/" .. FRAMES[C.expr]]
    if not Tex.ok(tex) then tex = ctx.tex["Cabin/Caspian/Neutral"] end
    if not Tex.ok(tex) then return end
    local sx, sy, lift = hopShape(hopT and (now - hopT) or nil, hopAmp)
    local breath = 1 + 0.009 * math.sin(now * 2 * math.pi / 3.6)
    if C.expr == "laugh" then
        local a = now - exprT
        if a >= 0 and a < LAUGH then lift = lift + 7 * math.abs(math.sin(a * 2 * math.pi * 2.6)) * (1 - a / LAUGH) end
    end
    Tex.place(tex, FOOT_X, FOOT_Y - lift, "bottom", sx, sy * breath)
end

function C.drawBox(opacity)
    if dlg == nil or not dlg.activeFlag or opacity <= 0 then return end
    dlg.opacity = opacity
    dlg:draw()
end

return C

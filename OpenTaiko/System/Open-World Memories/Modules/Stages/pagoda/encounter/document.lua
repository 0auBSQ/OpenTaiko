-- A monster drawn in parts over its box (the manifest entry's parts).

local assets = require("encounter/assets")
local sprite = require("encounter/sprite")
local Easing = require("Easing")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local A = Animation.encounter.document
local SPIN, LOOK, BLINK, CROWN = A.spin, A.look, A.blink, A.crown
local SPIN_EASE = Easing[SPIN.ease] or Easing.inOutCubic
local LOOK_EASE = Easing[LOOK.ease] or Easing.inOutSine
local TAU = math.pi * 2

local sin, floor = math.sin, math.floor

local D = {}
D.__index = D

-- textures its parts draw with
function D.keys(meta)
    local keys = {}
    if meta == nil or meta.parts == nil then return keys end
    for _, part in pairs(meta.parts) do
        keys[#keys + 1] = part.key
        if part.off ~= nil then keys[#keys + 1] = part.off end
    end
    return keys
end

-- deterministic numbers in [0, 1) for one instance
local function rnd(self)
    self.n = self.n + 1
    local v = sin(self.seed * 127.1 + self.n * 311.7) * 43758.5453
    return v - floor(v)
end

local function between(self, r)
    return r[1] + rnd(self) * (r[2] - r[1])
end

-- meta = the box's manifest entry (with its parts); seed = the instance's randomness
function D.new(meta, seed)
    local self = setmetatable({ seed = seed or 0, n = 0, t = 0, parts = {} }, D)
    for name, part in pairs(meta.parts) do
        self.parts[name] = {
            tex = (assets.get(part.key)), x = part.x, y = part.y,
            off = part.off ~= nil and (assets.get(part.off)) or nil,
        }
    end
    self.spinNext, self.spinT, self.spinDir = between(self, SPIN.every_s), nil, 1
    self.look, self.lookPhase, self.lookT, self.lookNext, self.lookHold = 0, nil, 0, between(self, LOOK.every_s), 0
    self.blink, self.blinkPhase, self.blinkT, self.blinkNext = 1, nil, 0, between(self, BLINK.every_s)
    self.lit, self.flash = 0, 0
    self.crownPhase, self.crownT, self.crownNext, self.crownSeg = "off", 0, between(self, CROWN.every_s), 1
    self.litUntil, self.flashNext, self.flashT = 0, 0, nil
    return self
end

local function stepSpin(self, dt)
    if self.spinT ~= nil then
        self.spinT = self.spinT + dt
        if self.spinT >= SPIN.turn_s then
            self.spinT = nil
            self.spinNext = self.t + between(self, SPIN.every_s)
        end
    elseif self.t >= self.spinNext then
        self.spinT = 0
        self.spinDir = rnd(self) < 0.5 and -1 or 1
    end
end

local function stepLook(self, dt)
    local ph = self.lookPhase
    if ph == nil then
        if self.t >= self.lookNext then self.lookPhase, self.lookT = "to", 0 end
        return
    end
    self.lookT = self.lookT + dt
    if ph == "to" then
        self.look = LOOK_EASE(self.lookT / LOOK.move_s)
        if self.lookT >= LOOK.move_s then self.lookPhase, self.lookT, self.lookHold = "hold", 0, between(self, LOOK.hold_s) end
    elseif ph == "hold" then
        self.look = 1
        if self.lookT >= self.lookHold then self.lookPhase, self.lookT = "back", 0 end
    else
        self.look = 1 - LOOK_EASE(self.lookT / LOOK.move_s)
        if self.lookT >= LOOK.move_s then
            self.look, self.lookPhase = 0, nil
            self.lookNext = self.t + between(self, LOOK.every_s)
        end
    end
end

local function stepBlink(self, dt)
    local ph = self.blinkPhase
    if ph == nil then
        if self.t >= self.blinkNext then self.blinkPhase, self.blinkT = "close", 0 end
        return
    end
    self.blinkT = self.blinkT + dt
    local low = BLINK.shut_scale
    if ph == "close" then
        self.blink = 1 - (1 - low) * math.min(1, self.blinkT / BLINK.close_s)
        if self.blinkT >= BLINK.close_s then self.blinkPhase, self.blinkT = "shut", 0 end
    elseif ph == "shut" then
        self.blink = low
        if self.blinkT >= BLINK.shut_s then self.blinkPhase, self.blinkT = "open", 0 end
    else
        self.blink = low + (1 - low) * math.min(1, self.blinkT / BLINK.open_s)
        if self.blinkT >= BLINK.open_s then
            self.blink, self.blinkPhase = 1, nil
            local again = not self.blinkTwice and rnd(self) < BLINK.double_chance
            self.blinkTwice = again
            self.blinkNext = self.t + (again and BLINK.double_gap_s or between(self, BLINK.every_s))
        end
    end
end

-- crown: off -> "on" (flicker) -> lit -> "out" (flicker) -> off
local function stepCrown(self, dt)
    local ph = self.crownPhase
    self.crownT = self.crownT + dt
    if ph == "off" then
        self.lit = 0
        if self.t >= self.crownNext then self.crownPhase, self.crownT, self.crownSeg = "on", 0, 1 end
    elseif ph == "on" or ph == "out" then
        local segs = ph == "on" and CROWN.flicker_s or CROWN.out_flicker_s
        while self.crownSeg <= #segs and self.crownT >= segs[self.crownSeg] do
            self.crownT = self.crownT - segs[self.crownSeg]
            self.crownSeg = self.crownSeg + 1
        end
        local litSeg = (self.crownSeg % 2 == 1) == (ph == "on")
        self.lit = litSeg and 1 or 0
        if self.crownSeg > #segs then
            if ph == "on" then
                self.crownPhase, self.crownT = "lit", 0
                self.litUntil = between(self, CROWN.lit_s)
                self.flashT, self.flashNext = 0, between(self, CROWN.flash_every_s)
            else
                self.crownPhase, self.crownT = "off", 0
                self.crownNext = self.t + between(self, CROWN.every_s)
            end
        end
    else
        self.lit = 1 - CROWN.hum * (0.5 + 0.5 * sin(TAU * CROWN.hum_hz * self.t))
        if self.flashT == nil and self.crownT >= self.flashNext then
            self.flashT = 0
            self.flashNext = self.crownT + between(self, CROWN.flash_every_s)
        end
        if self.crownT >= self.litUntil then self.crownPhase, self.crownT, self.crownSeg = "out", 0, 1 end
    end
    if self.flashT ~= nil then
        self.flashT = self.flashT + dt
        self.flash = math.max(0, 1 - self.flashT / CROWN.flash_s)
        if self.flashT >= CROWN.flash_s then self.flashT, self.flash = nil, 0 end
    end
    if ph ~= "lit" and self.crownPhase ~= "lit" then self.flash, self.flashT = 0, nil end
end

function D:update(dt)
    self.t = self.t + dt
    stepSpin(self, dt)
    stepLook(self, dt)
    stepBlink(self, dt)
    stepCrown(self, dt)
end

-- degrees the box turns on top of its idle this frame
function D:spin()
    if self.spinT == nil then return 0 end
    return self.spinDir * 360 * SPIN.turns * SPIN_EASE(self.spinT / SPIN.turn_s)
end

local _q = {}
local function q(sx, sy, rot, alpha, r, g, b, blend)
    _q.sx, _q.sy, _q.rot, _q.alpha, _q.r, _q.g, _q.b, _q.blend = sx, sy, rot, alpha, r, g, b, blend
    return _q
end

-- still picture of the parts at rest over the box at (x, y) with o
function D.drawStill(meta, x, y, o, alpha, r, g, b)
    if meta == nil or meta.parts == nil then return end
    local sx, sy, rot = o.sx or 1, o.sy or 1, o.rot or 0
    for _, name in ipairs({ "crown", "eye", "pupil" }) do
        local part = meta.parts[name]
        if part ~= nil then
            local tx = assets.get(part.off or part.key)
            local px, py = sprite.point(meta, x, y, o, part.x, part.y)
            sprite.drawAt(tx, px, py, q(sx, sy, rot, alpha, r, g, b, "Normal"))
        end
    end
end

-- o = { sx, sy, rot, pivot }; alpha, r, g, b as the box
function D:draw(meta, x, y, o, alpha, r, g, b)
    local P = self.parts
    local sx, sy, rot = o.sx or 1, o.sy or 1, o.rot or 0
    local crown = P.crown
    if crown ~= nil then
        local cx, cy = sprite.point(meta, x, y, o, crown.x, crown.y)
        if crown.off ~= nil then sprite.drawAt(crown.off, cx, cy, q(sx, sy, rot, alpha, r, g, b, "Normal")) end
        if self.lit > 0 then sprite.drawAt(crown.tex, cx, cy, q(sx, sy, rot, alpha * self.lit, r, g, b, "Normal")) end
        if self.flash > 0 then
            sprite.drawAt(crown.tex, cx, cy, q(sx, sy, rot, alpha * self.flash * CROWN.flash_opacity, r, g, b, "Add"))
        end
    end
    -- the player is to the left: a look moves the eye left, the pupil further
    local eye, pupil = P.eye, P.pupil
    if eye ~= nil then
        local ex, ey = sprite.point(meta, x, y, o, eye.x - LOOK.eye_px * self.look, eye.y)
        sprite.drawAt(eye.tex, ex, ey, q(sx, sy * self.blink, rot, alpha, r, g, b, "Normal"))
    end
    if pupil ~= nil then
        local px, py = sprite.point(meta, x, y, o, pupil.x - LOOK.pupil_px * self.look, pupil.y)
        sprite.drawAt(pupil.tex, px, py, q(sx, sy * self.blink, rot, alpha, r, g, b, "Normal"))
    end
end

return D

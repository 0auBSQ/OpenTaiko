---@diagnostic disable: undefined-global, need-check-nil
-- VN.Scene: visual-novel scene player on VN.Runner
-- assets, sounds and saved state all come from the caller

local Dialogue = require("VN.Dialogue")
local Runner   = require("VN.Runner")

local Scene = {}
Scene.__index = Scene

local DEF_FADE_SEC, DEF_SPRITE_FADE, DEF_BG_WAIT = 0.45, 0.35, 2.0
local DEF_BOUNCE, DEF_SQUASH = 0.36, 0.04

local function call(fn, ...) if fn ~= nil then return fn(...) end end

-- ==== creation ====
-- opts:
--   box = VN.Dialogue.new options (onBlip, onShake, onExpr, onSfx filled when unset), or dialogue = a VN.Dialogue
--   cast = { [who] = {
--     body = tex or fn(expr) -> tex (caller owns it), face = fn(expr) -> tex at (faceX, faceY) in body px,
--     name (lines without one), expr (on enter),
--     side = "left" | "center" | "right", x (wins over side), y, anchor = "top" | "bottom",
--     scale = 1 or height = px, fade = 0.35 s, slide = 0 px,
--     bounce = 0.36 s (0 = none), squash = 0.04 (share of height) } }
--   sides = { left, center, right }, default 1/4, 1/2, 3/4 of the width
--   focus = who: member an expr without a cast speaker changes (default: the only one, then the last one)
--   dim = { r, g, b }: colour of non-speaking members (nil: no dimming)
--   backgrounds = { defs = { [key] = def }, path = "Textures/bg/%s.png" (key without a def), or resolve = fn(key) -> def,
--     open = fn(layer) -> tex, own (default TEXTURE:CreateTexture; own = false: borrowed, reopened once unloaded),
--     prepare(def), show(def), update(dt), drawBehind(ox, oy), drawOver(ox, oy), close(), wait = 2.0 s max load }
--   def = a layer or { layers = { ... } }, bottom to top; layer = { path, x, y, scale, scroll }
--     no x/y: stretched over the screen; scroll: px/s, repeated across; def.music: key for music.play
--   music = { play(key), fadeOut(sec), playing(), update(dt), stop() }, all optional
--   sound = fn(name); blip = fn(node) -> name (nil: silent); clickSound = name (when box.sfx is unset)
--   cover = tex or fn() -> tex (fade texture), or drawCover = fn(alpha)
--   drawBox = fn(dlg, scene) instead of dlg:draw(); fadeSec = 0.45
--   tr, speakerName, playerTag, playerName, random, onLine: passed to VN.Runner
--   directions = { [key] = fn(line, scene) -> verdict }, extra or replacing built-ins; order: VN.Runner order
--   replay = { [key] = fn(line, scene) }: re-applies the caller's directions on resume
--   outcome = "done"; onBookmark = fn(mark) per started line, for play(..., resume)
--   hooks = { prose(line, scene), start(line, scene), expr(who, old, new, scene),
--     choose(line, value, scene), update(dt, scene), draw(scene), dispose() }
--   prose: before the face changes; start: typing starts; draw: over everything
function Scene.new(opts)
    opts = opts or {}
    local self = setmetatable({}, Scene)
    self.opts = opts
    self.cast = opts.cast or {}
    self.bgOpts = opts.backgrounds or {}
    self.music = opts.music or {}
    self.hooks = opts.hooks or {}
    self.fadeSec = opts.fadeSec or DEF_FADE_SEC
    local only, n = nil, 0
    for who in pairs(self.cast) do only, n = who, n + 1 end
    self.focus0 = opts.focus or ((n == 1) and only or nil)
    self.focus = self.focus0
    self.phase = "idle"             -- idle | fadein | sprite | line | fade | wait | done
    self.bgs = {}                   -- key -> { key, def, layers }, opened for the running scene
    self.bgKey, self.bgShown, self.bgT = nil, nil, 0
    self.fade, self.fadeTarget, self.fadeCover = 0, 0, true
    self.sprites, self.spriteList = {}, {}
    self.shakeState = { t = 0, dur = 0, amp = 0 }
    self.shown = false
    self.ctx = {}

    if opts.dialogue ~= nil then
        self.dlg = opts.dialogue
    else
        local box = {}
        for k, v in pairs(opts.box or {}) do box[k] = v end
        if box.onBlip == nil and opts.blip ~= nil then
            box.onBlip = function()
                local name = opts.blip(self.dlg ~= nil and self.dlg.node or nil)
                if name ~= nil then self:sound(name) end
            end
        end
        if box.onShake == nil then box.onShake = function(amp, dur) self:shake(amp, dur) end end
        if box.onExpr == nil then box.onExpr = function(expr) self:setExpr(self:_tagMember(), expr) end end
        if box.onSfx == nil then box.onSfx = function(name) self:sound(name) end end
        if box.sfx == nil and opts.clickSound ~= nil then
            box.sfx = { click = function() self:sound(opts.clickSound) end }
        end
        self.dlg = Dialogue.new(box)
    end
    return self
end

function Scene:sound(name)
    if name ~= nil and self.opts.sound ~= nil then self.opts.sound(name) end
end

function Scene:shake(amp, dur)
    local s = self.shakeState
    s.amp, s.dur, s.t = amp or 12, dur or 0.3, 0
end

function Scene:active() return self.phase ~= "idle" end

-- shown background { key, def, layers }, or nil
function Scene:shownBackground() return self.bgs[self.bgShown] end

function Scene:bookmark() return self.runner ~= nil and self.runner:bookmark() or nil end

-- ==== backgrounds ====
function Scene:resolveBackground(key)
    if key == nil then return nil end
    local b = self.bgOpts
    if b.resolve ~= nil then return b.resolve(key) end
    local def = b.defs and b.defs[key]
    if def ~= nil then return def end
    return { path = string.format(b.path or "%s", key) }
end

function Scene:_openLayer(def)
    local tex, own
    if self.bgOpts.open ~= nil then
        tex, own = self.bgOpts.open(def)
    else
        tex = TEXTURE:CreateTexture(def.path)
        if tex ~= nil then tex:SetWrapMode("Edge") end
    end
    return { def = def, tex = tex, own = own ~= false, off = 0 }
end

function Scene:_openBackground(key)
    if key == nil or self.bgs[key] ~= nil then return end
    local def = self:resolveBackground(key)
    if def == nil then return end
    call(self.bgOpts.prepare, def)
    local bg = { key = key, def = def, layers = {} }
    for i, l in ipairs(def.layers or { def }) do bg.layers[i] = self:_openLayer(l) end
    self.bgs[key] = bg
end

function Scene:_releaseBackgrounds()
    for _, bg in pairs(self.bgs) do
        for _, l in ipairs(bg.layers) do
            if l.own and l.tex ~= nil then l.tex:Dispose() end
        end
    end
    self.bgs = {}
end

-- true once every layer can be drawn; a layer that failed to load counts as settled
function Scene:_bgSettled(key)
    local bg = self.bgs[key]
    if bg == nil then return true end
    for _, l in ipairs(bg.layers) do
        if l.tex ~= nil and l.tex.Loaded and not l.tex.Ready then return false end
    end
    return true
end

-- requests a background; the drawn one stays until it has loaded
function Scene:setBackground(key)
    self:_openBackground(key)
    self.bgKey, self.bgT = key, 0
end

-- ==== cast ====
local function tex(v, ...) if type(v) == "function" then return v(...) end return v end

function Scene:_face(who, expr)
    local m = self.cast[who]
    if m == nil or m.face == nil or expr == nil then return nil end
    return m.face(expr)
end

-- member a line's expr changes: its speaker if a cast member, else the focus
function Scene:memberOf(line)
    local w = line.who
    if w ~= nil and self.cast[w] ~= nil then return w end
    return self.focus
end

-- member an {expr:} tag changes: the line's speaker if a cast member, else the focus
function Scene:_tagMember()
    local node = self.dlg ~= nil and self.dlg.node or nil
    local w = node ~= nil and node.speaker or nil
    if w ~= nil and self.cast[w] ~= nil then return w end
    return self.focus
end

function Scene:_enter(who, expr, side)
    local m = self.cast[who]
    local st = { who = who, expr = expr or m.expr, side = side or m.side or "center", a = 0, target = 1 }
    if self.sprites[who] == nil then self.spriteList[#self.spriteList + 1] = who end
    self.sprites[who] = st
    self.focus = who
    return st
end

-- a new face starts the bounce; one before 2/3 of its run carries on (no snap to rest)
function Scene:setExpr(who, expr)
    local st = who ~= nil and self.sprites[who] or nil
    if st == nil or expr == nil or expr == st.expr then return end
    local old = st.expr
    st.expr = expr
    local sec = self.cast[who].bounce or DEF_BOUNCE
    if sec > 0 and (st.bounce == nil or st.bounce >= sec * 2 / 3) then st.bounce = 0 end
    call(self.hooks.expr, who, old, expr, self)
end

function Scene:_spritesSettled()
    for _, who in ipairs(self.spriteList) do
        local st = self.sprites[who]
        if st ~= nil and st.a ~= st.target then return false end
    end
    return true
end

function Scene:_updateSprites(dt)
    for _, who in ipairs(self.spriteList) do
        local st, m = self.sprites[who], self.cast[who]
        if st ~= nil then
            -- fade-in waits for body and face to load (no pop-in at full opacity)
            local body = tex(m.body, st.expr)
            local ready = body ~= nil and body.Ready
            if m.face ~= nil then
                local f = self:_face(who, st.expr)
                ready = ready and f ~= nil and f.Ready
            end
            local fs = m.fade or DEF_SPRITE_FADE
            if st.a < st.target then
                if ready then st.a = math.min(st.target, st.a + dt / fs) end
            elseif st.a > st.target then
                st.a = math.max(st.target, st.a - dt / fs)
            end
            if st.bounce ~= nil then
                st.bounce = st.bounce + dt
                if st.bounce >= (m.bounce or DEF_BOUNCE) then st.bounce = nil end
            end
        end
    end
end

-- starts streaming every face the lines use, branches and picks included
function Scene:_streamFaces(lines)
    for _, l in ipairs(lines or {}) do
        local who = l.who
        if type(l.enter) == "string" and self.cast[l.enter] ~= nil then who = l.enter end
        if who == nil or self.cast[who] == nil then who = self.focus end
        if l.expr ~= nil then self:_face(who, l.expr) end
        if type(l.text) == "string" then
            for e in l.text:gmatch("{expr:([%w_]+)}") do self:_face(who, e) end
        end
        if type(l.branch) == "table" then
            for _, br in pairs(l.branch) do self:_streamFaces(br) end
        end
        if type(l.pick) == "table" then
            for _, o in ipairs(l.pick) do self:_streamFaces(o.lines) end
        end
    end
end

-- ==== line walking ====
function Scene:_startLine(line)
    call(self.hooks.start, line, self)
    local node = self.runner:present(self.dlg, line)
    if line.who ~= nil and self.cast[line.who] ~= nil then self.focus = line.who end
    self.speaker = node.speaker
    self.shown = true
    self.phase = "line"
    if self.opts.onBookmark ~= nil then self.opts.onBookmark(self.runner:bookmark()) end
end

-- sprite enter/leave: the box line stays as read (no caret, no choices) until the next one
function Scene:_spritePhase()
    self.phase = "sprite"
    local node = self.dlg.node
    if not self.shown or node == nil then return end
    node.choices = {}
    self.dlg.revealed, self.dlg.choosing, self.dlg.activeFlag = self.dlg.total, true, true
end

function Scene:finish(outcome)
    self.ctx.outcome = outcome or self.opts.outcome or "done"
    self.fadeTarget, self.fadeCover = 1, true
    self.phase = "fade"
    call(self.music.fadeOut, self.fadeSec)
end

-- true when nothing shows between the current line and the end
function Scene:_endsAfter()
    return self.runner:ahead(function(l)
        if l.outcome ~= nil then return true end
        if l.text ~= nil or l.choices ~= nil or l.enter ~= nil or l.fade ~= nil then return false end
        return nil
    end, true)
end

function Scene:_step()
    local v = self.runner:step()
    if v == "end" then self:finish(self.ctx.outcome) end
end

local BUILTIN = {}

function BUILTIN.outcome(line, s)
    s:finish(line.outcome)
    return "hold"
end

function BUILTIN.fade(line, s)
    s.fadeTarget = (line.fade == "out") and 1 or 0
    -- a fade covers the cast too unless keepSprite
    s.fadeCover = (line.keepSprite ~= true)
    -- the last fade takes the music with it
    if line.fade == "out" and s:_endsAfter() then call(s.music.fadeOut, s.fadeSec) end
    s.shown = false
    s.phase = "fade"
    return "hold"
end

function BUILTIN.bg(line, s)
    s:setBackground(line.bg)
    return false
end

local function castKey(s, v, line)
    if type(v) == "string" and s.cast[v] ~= nil then return v end
    if v == true then return s:memberOf(line) end
    return nil
end

function BUILTIN.enter(line, s)
    local who = castKey(s, line.enter, line)
    if who == nil then return nil end
    local st = s.sprites[who]
    if st ~= nil and st.target > 0 then
        -- already on screen: face change, no fade
        s:setExpr(who, line.expr)
        return nil
    end
    -- held until faded in, so the next line never types over an invisible sprite
    s:_enter(who, line.expr, line.side)
    s:_spritePhase()
    return "hold"
end

function BUILTIN.leave(line, s)
    local who = castKey(s, line.leave, line) or s.focus
    local st = who ~= nil and s.sprites[who] or nil
    if st ~= nil then st.target = 0 end
    s:_spritePhase()
    return "hold"
end

function BUILTIN.shake(line, s)
    local v = line.shake
    if type(v) == "table" then s:shake(v[1], v[2])
    elseif type(v) == "number" then s:shake(v)
    else s:shake() end
    return false
end

function BUILTIN.sfx(line, s)
    s:sound(line.sfx)
    return false
end

function BUILTIN.flag(line, s)
    s.ctx.flags = s.ctx.flags or {}
    if line.value == nil then s.ctx.flags[line.flag] = true else s.ctx.flags[line.flag] = line.value end
    return false
end

function BUILTIN.text(line, s)
    call(s.hooks.prose, line, s)
    s:setExpr(s:memberOf(line), line.expr)
    local who = line.who
    if who ~= nil and s.cast[who] ~= nil and s.sprites[who] == nil then
        -- a speaker not yet on screen fades in first, then its line types
        s:_enter(who, line.expr)
        s.pending = line
        s:_spritePhase()
        return "hold"
    end
    s:_startLine(line)
    return "line"
end

function BUILTIN.expr(line, s)
    s:setExpr(s:memberOf(line), line.expr)
    return nil
end

-- built-in replay on resume (silent)
local REPLAY = {}
function REPLAY.bg(line, s) s:setBackground(line.bg) end
function REPLAY.enter(line, s)
    local who = castKey(s, line.enter, line)
    if who == nil then return end
    local st = s.sprites[who]
    if st ~= nil and st.target > 0 then st.expr = line.expr or st.expr else s:_enter(who, line.expr, line.side) end
end
function REPLAY.leave(line, s)
    local who = castKey(s, line.leave, line) or s.focus
    if who ~= nil and s.sprites[who] ~= nil then s.sprites[who].target, s.sprites[who].a = 0, 0 end
end
-- a cast member's line: it enters with it if not on screen yet
function REPLAY.text(line, s)
    local who = line.who
    if who ~= nil and s.cast[who] ~= nil then
        if s.sprites[who] == nil then s:_enter(who, line.expr) end
        s.focus = who
    end
end
function REPLAY.expr(line, s)
    if line.enter ~= nil then return end
    local who = s:memberOf(line)
    local st = who ~= nil and s.sprites[who] or nil
    if st ~= nil then st.expr = line.expr end
end
REPLAY.flag = BUILTIN.flag

function Scene:_directions()
    local dirs = {}
    local own = self.opts.directions or {}
    for k, fn in pairs(BUILTIN) do
        if own[k] == nil then dirs[k] = function(line) return fn(line, self) end end
    end
    for k, fn in pairs(own) do
        dirs[k] = function(line)
            local v = fn(line, self)
            if v == "hold" then self.phase = "wait" end
            return v
        end
    end
    return dirs
end

-- for a held caller direction: the scene goes on once fn(dt) returns true
function Scene:waitFor(fn) self.waitFn = fn end

-- a held scene goes on to its next line
function Scene:continue()
    if self.phase ~= "wait" then return end
    self.waitFn = nil
    self:_step()
end

function Scene:_replay()
    local out = {}
    local own = self.opts.directions or {}
    for k, fn in pairs(REPLAY) do
        if own[k] == nil then out[k] = function(line) fn(line, self) end end
    end
    for k, fn in pairs(self.opts.replay or {}) do out[k] = function(line) fn(line, self) end end
    return out
end

local function defaultSpeakerName(cast)
    return function(who)
        local m = cast[who]
        if m ~= nil and m.name ~= nil then return m.name end
        return who
    end
end

-- ==== play ====
-- call order: play(scene, ctx, onDone, resume), then update(dt) and draw() every frame
-- scene = { bg = key, music = key (else the background's), lines = { ... } }, VN.Runner lines plus:
--   { enter = who, expr =, side = }   held until faded in; already on screen: face change only
--   { leave = who }                   held until gone
--   { expr = name }                   face of `who`, else the focus member
--   { bg = key }                      shown once its layers have loaded
--   { fade = "out" | "in", keepSprite = true }   keepSprite: the cast stays lit
--   { shake = true | amp | { amp, dur } }, { sfx = name }
--   { flag = name, value = v }        ctx.flags[name] = v (true when absent)
--   { outcome = value }               fade out and report value
-- a line of a cast member not yet on screen fades it in first
-- a caller direction returning "hold" waits for scene:continue() or scene:waitFor(fn)
-- ctx: read by `when` guards, carries choices and outcome; resume: a bookmark to continue from
-- onDone(result): the outcome (opts.outcome, default "done", when the lines run out)
function Scene:play(scene, ctx, onDone, resume)
    local o = self.opts
    self.scene, self.ctx, self.onDone = scene, ctx or {}, onDone
    self.ctx.choices = self.ctx.choices or {}
    self.ctx.outcome = nil
    self.runner = Runner.new{
        lines = scene.lines, ctx = self.ctx, tr = o.tr,
        speakerName = o.speakerName or defaultSpeakerName(self.cast),
        playerTag = o.playerTag, playerName = o.playerName, random = o.random, onLine = o.onLine,
        onChoose = function(line, value) call(self.hooks.choose, line, value, self) end,
        directions = self:_directions(), order = o.order,
    }
    self.pending, self.shown, self.speaker, self.waitFn = nil, false, nil, nil
    self.sprites, self.spriteList = {}, {}
    self.focus = self.focus0
    -- stream every face now, so expression swaps never wait
    self:_streamFaces(self.runner.lines)
    call(self.bgOpts.close)
    self:_releaseBackgrounds()
    self:_openBackground(scene.bg)
    self.bgKey, self.bgShown, self.bgT = scene.bg, nil, 0
    local def = self.bgs[scene.bg] and self.bgs[scene.bg].def or nil
    call(self.music.play, scene.music or (def and def.music))     -- fades in with the scene
    self.fade, self.fadeTarget, self.fadeCover = 1, 0, true   -- scenes always fade in
    self.phase = "fadein"
    if resume ~= nil then
        self.runner:resume(resume, self:_replay())
        for _, who in ipairs(self.spriteList) do
            local st = self.sprites[who]
            if st ~= nil then st.a = st.target end
        end
    end
end

function Scene:reset()
    self.phase, self.scene, self.onDone = "idle", nil, nil
    self.runner = nil
    self.pending = nil
    self.sprites, self.spriteList = {}, {}
    self.fade, self.fadeTarget = 0, 0
    if self.dlg ~= nil then self.dlg.activeFlag = false end
    call(self.bgOpts.close)
    call(self.music.stop)
    self:_releaseBackgrounds()
    self.bgKey, self.bgShown, self.bgT = nil, nil, 0
end

function Scene:dispose()
    call(self.bgOpts.close)
    call(self.music.stop)
    call(self.hooks.dispose)
    self:_releaseBackgrounds()
    if self.dlg ~= nil and self.opts.dialogue == nil then self.dlg:dispose() end
end

function Scene:update(dt)
    if self.phase == "idle" then return "done" end
    call(self.music.update, dt)
    call(self.hooks.update, dt, self)

    local sh = self.shakeState
    if sh.dur > 0 then
        sh.t = sh.t + dt
        if sh.t >= sh.dur then sh.dur = 0 end
    end

    -- a requested background replaces the drawn one once loaded
    if self.bgShown ~= self.bgKey then
        self.bgT = self.bgT + dt
        if self:_bgSettled(self.bgKey) or self.bgT >= (self.bgOpts.wait or DEF_BG_WAIT) then
            self.bgShown = self.bgKey
            local bg = self.bgs[self.bgShown]
            local def = bg and bg.def or nil
            call(self.bgOpts.show, def)
            call(self.music.play, def and def.music)
        end
    end
    call(self.bgOpts.update, dt)
    local bg = self.bgs[self.bgShown]
    if bg ~= nil then
        for i, l in ipairs(bg.layers) do
            -- a borrowed texture released by its owner is replaced by a scene-owned one
            if not l.own and (l.tex == nil or not l.tex.Loaded) then bg.layers[i] = self:_openLayer(l.def) end
        end
        for _, l in ipairs(bg.layers) do
            if l.def.scroll ~= nil then l.off = l.off + l.def.scroll * dt end
        end
    end

    self:_updateSprites(dt)

    if self.phase == "fadein" then
        -- fades in once the background is up
        if self.bgShown == self.bgKey then self.fade = math.max(0, self.fade - dt / self.fadeSec) end
        if self.fade <= 0 then self:_step() end
        return "running"
    end

    -- waits for sprite enter/leave; then a pending line starts typing
    if self.phase == "sprite" then
        if self:_spritesSettled() then
            if self.pending ~= nil then
                local l = self.pending
                self.pending = nil
                self:_startLine(l)
            else
                self:_step()
            end
        end
        return "running"
    end

    if self.phase == "fade" then
        -- a fade-in waits for a requested background
        if self.fade > self.fadeTarget and self.bgShown ~= self.bgKey then return "running" end
        local d = dt / self.fadeSec
        if self.fade < self.fadeTarget then self.fade = math.min(self.fadeTarget, self.fade + d)
        else self.fade = math.max(self.fadeTarget, self.fade - d) end
        if self.fade == self.fadeTarget then
            if self.ctx.outcome ~= nil then
                -- the scene ends once its music has faded too
                if self.music.playing ~= nil and self.music.playing() then return "running" end
                self.phase = "done"
                local cb, outcome = self.onDone, self.ctx.outcome
                self:reset()
                if cb ~= nil then cb(outcome) end
                return "done"
            end
            self:_step()
        end
        return "running"
    end

    if self.phase == "wait" then
        if self.waitFn ~= nil and self.waitFn(dt) == true then
            self.waitFn = nil
            self:_step()
        end
        return "running"
    end

    if self.phase == "line" then
        if self.dlg:update(dt) == "done" then
            self.runner:choose(self.dlg.result)
            self:_step()
        end
        return "running"
    end
    return "running"
end

-- ==== draw ====
function Scene:_drawFade(res)
    if self.fade <= 0 then return end
    if self.opts.drawCover ~= nil then self.opts.drawCover(self.fade) return end
    local b = tex(self.opts.cover)
    if b == nil or not b.Ready then return end
    b:SetScale(res.X / math.max(1, b.Width), res.Y / math.max(1, b.Height))
    b:SetOpacity(self.fade)
    b:Draw(0, 0)
    b:SetOpacity(1) ; b:SetScale(1, 1)
end

-- layer without x/y: stretched over the screen; with scroll: repeated across
local function drawLayer(l, res, ox, oy)
    local tx, d = l.tex, l.def
    if tx == nil or not tx.Ready then return end
    if d.x == nil and d.y == nil then
        tx:SetScale(res.X / tx.Width, res.Y / tx.Height)
        tx:Draw(ox, oy)
    else
        local s = d.scale or 1
        local x, y = d.x or 0, (d.y or 0) + oy
        tx:SetScale(s, s)
        if d.scroll ~= nil then
            local w = tx.Width * s
            x = (x + l.off) % w - w
            while x + ox < res.X do
                tx:Draw(x + ox, y)
                x = x + w
            end
        else
            tx:Draw(x + ox, y)
        end
    end
    tx:SetScale(1, 1)
end

function Scene:_sideX(side, res)
    local sides = self.opts.sides or {}
    if side == "left" then return sides.left or res.X * 0.25 end
    if side == "right" then return sides.right or res.X * 0.75 end
    return sides.center or res.X * 0.5
end

function Scene:_drawSprite(who, res, ox, oy)
    local st, m = self.sprites[who], self.cast[who]
    if st == nil or st.a <= 0 then return end
    local body = tex(m.body, st.expr)
    if body == nil or not body.Ready then return end
    -- body never drawn without a face; while the new face streams in, the last one stays
    local f = nil
    if m.face ~= nil then
        f = self:_face(who, st.expr)
        if f == nil or not f.Ready then f = st.lastFace end
        if f == nil or not f.Ready then return end
        st.lastFace = f
    end
    -- bounce: k = squash * sin(3*pi*u) * (1-u)^2, u = t / bounce
    local k = 0
    if st.bounce ~= nil then
        local u = st.bounce / (m.bounce or DEF_BOUNCE)
        k = (m.squash or DEF_SQUASH) * math.sin(3 * math.pi * u) * (1 - u) * (1 - u)
    end
    local scale = m.scale or ((m.height ~= nil and body.Height > 0) and m.height / body.Height or 1)
    local sx, sy = scale * (1 + 0.5 * k), scale * (1 - k)
    local cx = m.x or self:_sideX(st.side, res)
    local slide = m.slide or 0
    if slide ~= 0 then
        local dir = (st.side == "left") and -1 or 1
        cx = cx + dir * (1 - st.a) * slide
    end
    -- anchored by top (or bottom) centre, squashed about the bottom centre; the face uses the same scale
    local x0 = cx - body.Width * sx * 0.5 + ox
    local y0
    if m.anchor == "bottom" then
        y0 = (m.y or res.Y) + oy - body.Height * sy
    else
        y0 = (m.y or 0) + oy + body.Height * (scale - sy)
    end
    local dim = self.opts.dim
    local dimmed = dim ~= nil and self.speaker ~= nil and self.speaker ~= who
    if dimmed then body:SetColor(dim[1], dim[2], dim[3]) end
    body:SetScale(sx, sy)
    body:SetOpacity(st.a)
    body:Draw(x0, y0)
    body:SetOpacity(1) ; body:SetScale(1, 1)
    if dimmed then body:SetColor(1, 1, 1) end
    if f ~= nil then
        if dimmed then f:SetColor(dim[1], dim[2], dim[3]) end
        f:SetScale(sx, sy)
        f:SetOpacity(st.a)
        f:Draw(x0 + (m.faceX or 0) * sx, y0 + (m.faceY or 0) * sy)
        f:SetOpacity(1) ; f:SetScale(1, 1)
        if dimmed then f:SetColor(1, 1, 1) end
    end
end

function Scene:draw()
    if self.phase == "idle" then return end
    local res = THEME:GetResolution()

    -- screen shake offsets the whole scene
    local ox, oy = 0, 0
    local sh = self.shakeState
    if sh.dur > 0 then
        local k = 1 - (sh.t / sh.dur)
        ox = math.sin(sh.t * 62) * sh.amp * k
        oy = math.cos(sh.t * 47) * sh.amp * k * 0.6
    end

    call(self.bgOpts.drawBehind, ox, oy)
    local bg = self.bgs[self.bgShown]
    if bg ~= nil then
        for _, l in ipairs(bg.layers) do drawLayer(l, res, ox, oy) end
    end
    call(self.bgOpts.drawOver, ox, oy)

    if not self.fadeCover then self:_drawFade(res) end   -- backdrop-only fade (keepSprite)
    -- left side first; right and centre overlap it
    for _, who in ipairs(self.spriteList) do
        local st = self.sprites[who]
        if st ~= nil and st.side == "left" then self:_drawSprite(who, res, ox, oy) end
    end
    for _, who in ipairs(self.spriteList) do
        local st = self.sprites[who]
        if st ~= nil and st.side ~= "left" then self:_drawSprite(who, res, ox, oy) end
    end
    if self.fadeCover then self:_drawFade(res) end

    if self.phase == "line" or (self.phase == "sprite" and self.shown) then
        if self.opts.drawBox ~= nil then self.opts.drawBox(self.dlg, self) else self.dlg:draw() end
    end
    call(self.hooks.draw, self)
end

return Scene

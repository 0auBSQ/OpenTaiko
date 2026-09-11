---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- editmode.lua — the mouse-driven room editor (Tab toggles it from Script.lua), on PopUI.
--
-- Layout: a bottom bar with a TAB ROW per action (Furniture / Wall / Floor / Paint / Door / Eraser)
-- and the tab's inventory as square shop-style buttons — a live ModelIcon render of the ACTUAL 3D
-- model when the entry is GLB-built (staggered by ModelIcon's per-frame budget; drawn art fills in
-- until it lands), name underneath, and a rounded ×N stock pill in the corner. Door/Eraser tabs show
-- helper text instead of a grid. The 3D area keeps the interactions on the OWM3d overlay layers:
--   browse  — PRESS a placed item to select it INSTANTLY: inline buttons (Rotate/Move/Remove)
--             appear right under it in-world (world:project). Rotate applies live to the placed
--             item; the phone offers Move only. Hold + drag the item (ground OR wall) to move it:
--             the original disappears and the bounds-anchored ghost tracks the hovered cell/slot,
--             dropping on release (an illegal drop restores). The door tile drags along the front.
--   place   — an item is selected in the grid: a green/red ghost previews the spot. Furniture:
--             wheel rotates. Wall items: wheel flips the mount (low/high). Floor/paint: swipe.
--   move    — the inline Move button picks the item up (same in-hand flow as dragging; click drops).
--   door    — the Door tab: a ghost doorway tile follows the hovered front-edge tile; click moves.
--   eraser  — the Eraser tab: hover any placed thing (furniture, wall item, flooring, paint) and
--             click/swipe to remove it back to stock. The phone can never be erased.
-- Every mutation goes through Edit:commit() → onChange (rebuild + save + net sync), which also drops
-- the cached ghost model instances (the rebuild deletes their scene objects).

local A         = require("assets")
local Room      = require("room")
local I         = require("icons")
local PopUI     = require("PopUI")
local I18N      = require("i18n")
local T = I18N.texts("editmode")   -- lang/<code>/editmode.json
local ModelIcon = require("OWM3d").ModelIcon

local floor, max, min = math.floor, math.max, math.min

local Edit = {}
Edit.__index = Edit

local SW, SH = 1920, 1080
local BAR_H = 260
local BAR_Y = SH - BAR_H
local FY = Room.FLOOR_Y
local PICK2 = (SW * 0.05) ^ 2         -- squared pick radius for 3D hover

local CATS = {
    { key = "furn",   label = "tab_furniture" },
    { key = "wall",   label = "tab_wall" },
    { key = "floor",  label = "tab_floor" },
    { key = "paint",  label = "tab_paint" },
    { key = "door",   label = "tab_door" },
    { key = "eraser", label = "tab_eraser" },
}

-- bar geometry (tab row on top, item grid underneath)
local TAB_X0, TAB_Y, TAB_W, TAB_H, TAB_STEP = 28, BAR_Y + 18, 168, 52, 178
local GRID_X0, GRID_Y, SLOT_W, SLOT_H, SLOT_STEP = 28, BAR_Y + 84, 118, 150, 130

local THEME = {
    colors = {
        surface  = { 252, 246, 238, 255 }, surface2 = { 240, 226, 208, 255 },
        primary  = { 236, 156, 96, 255 },  primary2 = { 214, 122, 62, 255 },
        outline  = { 92, 62, 46, 255 },    text = { 92, 62, 46, 255 },
    },
    font = { small = 16, label = 19, button = 20, title = 26 },
}

-- ── 3D overlay helpers (ported from isoengine's grid/ghost/hilite, lifted by the platform) ────────
local function gridClear(world) world.scene:ObjBegin(world.gridObj) end
local function ghostClear(world) world.scene:ObjBegin(world.ghostObj); world._boxTarget = world.prop3dObj end
local function hiliteClear(world) world.scene:ObjBegin(world.hiliteObj) end

local function gridBegin(world) world.scene:ObjBegin(world.gridObj) end
local function gridGround(world, c, r)
    local x0, z0 = c, (world.gridH - 1 - r)
    world.scene:ObjAddQuadFlat(world.gridObj, x0 + 0.06, FY + 0.04, z0 + 0.06, x0 + 0.94, FY + 0.04, z0 + 0.06,
        x0 + 0.94, FY + 0.04, z0 + 0.94, x0 + 0.06, FY + 0.04, z0 + 0.94)
end
local function gridWallBack(world, c)
    local z = world.gridH - 1 - 0.012
    world.scene:ObjAddQuadFlat(world.gridObj, c + 0.06, FY + 0.18, z, c + 0.94, FY + 0.18, z, c + 0.94, FY + 2.82, z, c + 0.06, FY + 2.82, z)
end
local function gridWallRight(world, r)
    local x, z0 = world.gridW - 1 - 0.012, (world.gridH - 1 - r)
    world.scene:ObjAddQuadFlat(world.gridObj, x, FY + 0.18, z0 + 0.06, x, FY + 0.18, z0 + 0.94, x, FY + 2.82, z0 + 0.94, x, FY + 2.82, z0 + 0.06)
end

-- y defaults to just above the floor; stacked items pass their surface's top so the highlight
-- visibly wraps the PC/TV instead of hiding under the desk
local function hilite(world, cells, r, g, b, a, y)
    y = y or (FY + 0.05)
    world.scene:ObjSetPass(world.hiliteObj, 1, r, g, b, a)
    world.scene:ObjBegin(world.hiliteObj)
    for _, cell in ipairs(cells) do
        local x0, z0 = cell[1], (world.gridH - 1 - cell[2])
        world.scene:ObjAddQuadFlat(world.hiliteObj, x0 + 0.04, y, z0 + 0.04, x0 + 0.96, y, z0 + 0.04,
            x0 + 0.96, y, z0 + 0.96, x0 + 0.04, y, z0 + 0.96)
    end
end
-- highlight height for a PLACED item (surface top + epsilon when stacked)
local function itemHiliteY(room, it)
    if it and it.on then
        local surf = room:surfaceUnder(it.id, it.c, it.r, it.facing or 0)
        if surf then return room:surfaceTopY(surf) + 0.05 end
    end
    return FY + 0.05
end
local function hiliteTile(world, c, r, valid)
    hilite(world, { { c, r } }, valid and 90 or 235, valid and 225 or 95, valid and 120 or 95, 130)
end
-- a full wall tile (paint picking)
local function hiliteWallTile(world, slot, valid)
    world.scene:ObjSetPass(world.hiliteObj, 1, valid and 90 or 235, valid and 225 or 95, valid and 120 or 95, 150)
    world.scene:ObjBegin(world.hiliteObj)
    if slot.wall == "back" then
        local c, z = slot.idx, world.gridH - 1 - 0.02
        world.scene:ObjAddQuadFlat(world.hiliteObj, c + 0.05, FY + 0.12, z, c + 0.95, FY + 0.12, z, c + 0.95, FY + 2.88, z, c + 0.05, FY + 2.88, z)
    else
        local x, z0 = world.gridW - 1 - 0.02, (world.gridH - 1 - slot.idx)
        world.scene:ObjAddQuadFlat(world.hiliteObj, x, FY + 0.12, z0 + 0.05, x, FY + 0.12, z0 + 0.95, x, FY + 2.88, z0 + 0.95, x, FY + 2.88, z0 + 0.05)
    end
end
-- a thin axis-aligned bar (a small rectangular tube) between two points — the building block of a
-- wireframe box. Faces emitted as flat quads into the hilite object.
local function wbar(world, ax, ay, az, bx2, by2, bz2, t)
    local o = world.hiliteObj
    local x0, x1 = math.min(ax, bx2), math.max(ax, bx2)
    local y0, y1 = math.min(ay, by2), math.max(ay, by2)
    local z0, z1 = math.min(az, bz2), math.max(az, bz2)
    if x1 - x0 < t then local m = (x0 + x1) * 0.5; x0, x1 = m - t * 0.5, m + t * 0.5 end
    if y1 - y0 < t then local m = (y0 + y1) * 0.5; y0, y1 = m - t * 0.5, m + t * 0.5 end
    if z1 - z0 < t then local m = (z0 + z1) * 0.5; z0, z1 = m - t * 0.5, m + t * 0.5 end
    local s = world.scene
    s:ObjAddQuadFlat(o, x0, y1, z0, x1, y1, z0, x1, y1, z1, x0, y1, z1)   -- top
    s:ObjAddQuadFlat(o, x0, y0, z0, x1, y0, z0, x1, y0, z1, x0, y0, z1)   -- bottom
    s:ObjAddQuadFlat(o, x0, y0, z1, x1, y0, z1, x1, y1, z1, x0, y1, z1)   -- +z
    s:ObjAddQuadFlat(o, x0, y0, z0, x1, y0, z0, x1, y1, z0, x0, y1, z0)   -- -z
    s:ObjAddQuadFlat(o, x1, y0, z0, x1, y0, z1, x1, y1, z1, x1, y1, z0)   -- +x
    s:ObjAddQuadFlat(o, x0, y0, z0, x0, y0, z1, x0, y1, z1, x0, y1, z0)   -- -x
end
-- a wireframe box around an AABB (12 edges). *Into = append edges only (caller did ObjSetPass+ObjBegin),
-- so several boxes can share one hilite pass (selected + hovered at once).
local function wireBoxInto(world, x0, y0, z0, x1, y1, z1, t)
    t = t or 0.03
    wbar(world, x0, y0, z0, x1, y0, z0, t); wbar(world, x0, y0, z1, x1, y0, z1, t)   -- bottom ±z
    wbar(world, x0, y0, z0, x0, y0, z1, t); wbar(world, x1, y0, z0, x1, y0, z1, t)   -- bottom ±x
    wbar(world, x0, y1, z0, x1, y1, z0, t); wbar(world, x0, y1, z1, x1, y1, z1, t)   -- top ±z
    wbar(world, x0, y1, z0, x0, y1, z1, t); wbar(world, x1, y1, z0, x1, y1, z1, t)   -- top ±x
    wbar(world, x0, y0, z0, x0, y1, z0, t); wbar(world, x1, y0, z0, x1, y1, z0, t)   -- verticals
    wbar(world, x0, y0, z1, x0, y1, z1, t); wbar(world, x1, y0, z1, x1, y1, z1, t)
end
local function wireBox(world, x0, y0, z0, x1, y1, z1, r, g, b, a, t)
    world.scene:ObjSetPass(world.hiliteObj, 1, r, g, b, a)
    world.scene:ObjBegin(world.hiliteObj)
    wireBoxInto(world, x0, y0, z0, x1, y1, z1, t or 0.03)
end
-- the world AABB of a placed furniture item (its footprint cells × height), sitting on the floor or
-- its host surface top. Used to draw a wireframe box around it on hover/select.
local function itemAABB(world, room, it)
    local cat = Room.CATALOG[it.id]
    local by = FY + 0.02
    if it.on then
        local surf = room:surfaceUnder(it.id, it.c, it.r, it.facing or 0)
        if surf then by = room:surfaceTopY(surf) + 0.02 end
    end
    -- top: the item's real built top (GLB fit) when known, else the surface/collision height — keeps
    -- the box hugging elevated items instead of floating a tall box above them
    local top = (it._topY and it._topY > by + 0.05) and it._topY
                or (by + ((cat and (cat.surface and cat.surfaceH or cat.collH)) or 0.85))
    -- Prefer the ACTUAL rendered GLB bounds (scaled + yaw-rotated, centred on the footprint) so the box
    -- hugs the model, NOT the full grid footprint: a desk whose model doesn't fill its 2 cells (modelFit
    -- keeps aspect, so a non-2:1 desk is scaled narrower) looked "weirdly big". Mirrors buildOneProp's
    -- collision maths. Falls back to the footprint for box-art items / when there is no live instance.
    local inst = world._propInst and world._propInst[it]
    if inst ~= nil and inst.model then
        local got, ax0, az0, ax1, az1 = pcall(function()
            local mnx, _, mnz, mxx, _, mxz = inst.model:GetBounds()
            local s = inst.scale or 1
            local bw, bd = (mxx - mnx) * s, (mxz - mnz) * s
            local yr = (inst.yaw or 0) * math.pi / 180
            local ca, sa = math.abs(math.cos(yr)), math.abs(math.sin(yr))
            local hx = (bw * ca + bd * sa) * 0.5
            local hz = (bw * sa + bd * ca) * 0.5
            -- centre on the FOOTPRINT centre (= where poseFitted anchored the model's bounds-centre),
            -- NOT inst.x/inst.z which for anchor="bounds" GLBs is the model ORIGIN (offset by the rotated
            -- bounds-centre). Same anchor as buildOneProp, so the box hugs the rendered model.
            local w, h = cat.w, cat.h
            if (it.facing or 0) % 2 == 1 then w, h = h, w end
            local cxw, czw = world:footprintCenter(it.c, it.r, w, h)
            return cxw - hx, czw - hz, cxw + hx, czw + hz
        end)
        if got and ax0 then return ax0, by, az0, ax1, top, az1 end
    end
    local cells = room:footprint(it.id, it.c, it.r, it.facing or 0)
    local x0, x1, z0, z1 = math.huge, -math.huge, math.huge, -math.huge
    for _, cell in ipairs(cells) do
        local cx, wz = cell[1], (world.gridH - 1 - cell[2])
        if cx < x0 then x0 = cx end
        if cx + 1 > x1 then x1 = cx + 1 end
        if wz < z0 then z0 = wz end
        if wz + 1 > z1 then z1 = wz + 1 end
    end
    return x0 + 0.05, by, z0 + 0.05, x1 - 0.05, top, z1 - 0.05
end
-- a mount-height band on a wall tile: slot = {wall, c, r, mount} (or a Room:wallItemSlots() entry)
-- *Into = append the band quad only (caller owns ObjSetPass+ObjBegin).
local function wallSlotInto(world, s)
    local y0 = FY + Room.MOUNT_Y[s.mount] - 0.08
    local y1 = y0 + 0.86
    if s.wall == "back" then
        local c, z = s.c, world.gridH - 1 - 0.02
        world.scene:ObjAddQuadFlat(world.hiliteObj, c + 0.05, y0, z, c + 0.95, y0, z, c + 0.95, y1, z, c + 0.05, y1, z)
    else
        local x, z0 = world.gridW - 1 - 0.02, (world.gridH - 1 - s.r)
        world.scene:ObjAddQuadFlat(world.hiliteObj, x, y0, z0 + 0.05, x, y0, z0 + 0.95, x, y1, z0 + 0.95, x, y1, z0 + 0.05)
    end
end
local function hiliteWallSlot(world, s, valid, blue)
    local rC, gC, bC
    if blue then rC, gC, bC = 120, 200, 255
    elseif valid then rC, gC, bC = 90, 225, 120
    else rC, gC, bC = 235, 95, 95 end
    world.scene:ObjSetPass(world.hiliteObj, 1, rC, gC, bC, 150)
    world.scene:ObjBegin(world.hiliteObj)
    wallSlotInto(world, s)
end

local function ghostBegin(world, ok)
    world.scene:ObjSetTint(world.ghostObj, ok and 0.4 or 1.0, ok and 1.0 or 0.4, ok and 0.5 or 0.4)
    world.scene:ObjBegin(world.ghostObj)
    world._boxTarget = world.ghostObj
end
local function ghostEnd(world) world._boxTarget = world.prop3dObj end

-- ── construction ──────────────────────────────────────────────────────────────────────────────────
function Edit.new(room, world, onChange)
    local self = setmetatable({}, Edit)
    self.room, self.world, self.onChange = room, world, onChange or function() end
    self.cat = 1
    self.sel = nil            -- selected CATALOG id (place mode)
    self.facing = 0           -- ground placement facing
    self.mount = "low"        -- wall placement mount height
    self.selected = nil       -- selected PLACED item: { it, kind = "ground"|"wall" }
    self.selUI = nil          -- inline action buttons under the selection
    self.selBtns = nil
    self.hold = nil           -- item in hand: { it, kind, fromC, fromR, fromMount, drag }
    self.pressItem = nil      -- press-armed item (drag detection)
    self.dragExit = false
    self.playerC, self.playerR = -1, -1
    self.ui = nil
    self.barSlots = {}
    self.ghostModels = {}     -- id → model instance | false (ghost previews; wiped on commit)
    self.activeGhost = nil
    I.build()
    return self
end

function Edit:catKey() return CATS[self.cat].key end
function Edit:items() return self.room:categoryItems(self:catKey()) end

function Edit:enter(pc, pr)
    I18N.detect()                                          -- pick up the active game language
    self.playerC, self.playerR = pc, pr
    self.sel, self.hold, self.pressItem = nil, nil, nil
    self.dragExit = false
    self.facing, self.mount = 0, "low"
    self.ghostModels, self.activeGhost = {}, nil
    self.ghostWallModels, self.activeWallGhost = {}, nil
    self:deselect(true)
    self:buildBar()
end

function Edit:leave()
    local world = self.world
    if self.hold then self:restoreHold(true) end               -- never lose an in-hand item (phone!)
    gridClear(world); ghostClear(world); hiliteClear(world)
    self:hideGhostModel()
    self.ghostModels = {}
    self.ghostWallModels = {}
    self:deselect(true)
    -- keep the 3D preview icons ALIVE across open/close: re-rendering ~20 offscreen GLB scenes on
    -- every open was the edit-menu lag. They live for the stage session (freed by ModelIcon.disposeAll
    -- on stage teardown); modelIconFor recreates any that got disposed (e.g. after a deactivate).
    self._rotFlash = nil
    if self._badge then self._badge:Dispose(); self._badge = nil end
    if self.ui then self.ui:disposeWidgets(); self.ui = nil end
end

-- every room mutation funnels through here: the rebuild (onChange → loadMap) deletes ALL model
-- instances including the cached ghost models, so drop those references BEFORE it runs
function Edit:commit()
    self.activeGhost = nil
    self.ghostModels = {}
    self.activeWallGhost = nil
    self.ghostWallModels = {}
    self.onChange()
end

-- ── ghost model instances (the REAL GLB previews placement, bounds-anchored like the placed one) ──
function Edit:ghostModelFor(id)
    local e = self.ghostModels[id]
    if e == false then return nil end
    if e then return e end
    local inst = Room.addGhostModel(self.world, id)
    if inst == nil then self.ghostModels[id] = false; return nil end
    local scene = self.world.scene
    scene:ObjSetPass(inst.obj, 1, 0, 0, 0, 150)              -- transparent ghost pass
    scene:ObjSetLit(inst.obj, false)                          -- match the unlit box-ghost layer
    if scene.ObjSetCastShadow then scene:ObjSetCastShadow(inst.obj, false) end
    inst:setVisible(false)
    self.ghostModels[id] = inst
    return inst
end

function Edit:hideGhostModel()
    if self.activeGhost then self.activeGhost:setVisible(false); self.activeGhost = nil end
    if self.activeWallGhost then self.activeWallGhost:setVisible(false); self.activeWallGhost = nil end
end

-- parked wall-GLB ghost per id — REUSED every hover frame (the old path called
-- buildWallItemVisual per frame, adding a fresh model instance at every hovered slot)
function Edit:ghostWallModelFor(id)
    local g = Room.WALL_GLB and Room.WALL_GLB[id]
    if g == nil then return nil end
    self.ghostWallModels = self.ghostWallModels or {}
    local e = self.ghostWallModels[id]
    if e == false then return nil end
    if e then return e end
    local inst = nil
    pcall(function()
        inst = self.world.models:add{ file = "Models/" .. g.file .. ".glb", x = 0, y = -100, z = 0,
                                      collide = "none", anchor = "bounds" }
    end)
    if inst == nil then self.ghostWallModels[id] = false; return nil end
    local scene = self.world.scene
    for _, o in ipairs(inst.objs or {}) do
        scene:ObjSetPass(o, 1, 0, 0, 0, 150)
        scene:ObjSetLit(o, false)
        if scene.ObjSetCastShadow then scene:ObjSetCastShadow(o, false) end
    end
    inst:setVisible(false)
    self.ghostWallModels[id] = inst
    return inst
end

-- wall-item placement ghost: the parked GLB instance when one exists, else box art on the
-- ghost layer (buildWallItemVisual's ghost flag skips its instance path entirely)
function Edit:ghostWallItem(id, c, r, mount, ok)
    local world = self.world
    local gi = self:ghostWallModelFor(id)
    if gi and self.room:poseWallGlb(gi, id, c, r, mount) then
        world.scene:ObjSetTint(gi.obj, ok and 0.4 or 1.0, ok and 1.0 or 0.4, ok and 0.5 or 0.4)
        gi:setVisible(true)
        self.activeWallGhost = gi
        return
    end
    ghostBegin(world, ok)
    self.room:buildWallItemVisual(world, { id = id, c = c, r = r, mount = mount }, true)
    ghostEnd(world)
end

-- ── 3D model preview icons (ModelIcon: the ACTUAL GLB rendered into a small offscreen scene) ──────
-- Created lazily while edit mode is open (one per catalog id actually shown, keyed by id) and
-- disposed on leave — allocate-before-use / free-after. First renders are budgeted per frame by
-- ModelIcon.newFrame (Script calls it once per stage update); while an icon waits its turn, draw()
-- returns false and the drawn icons.lua art fills in. Box-built entries and the flooring/paint
-- swatches always keep the drawn art. The PC/shop screens should follow the same pattern with
-- res = 512 previews created on open and disposed on close.
-- EVERY shop entry gets a real 3D snapshot: GLB items via ModelIcon.new, box-art ground items /
-- wall items / flooring / paint via ModelIcon.newBuilder (the procedural builders render into a
-- tiny offscreen scene, textures registered through A.registerInto). The drawn icons.lua art only
-- fills the frame or two an icon spends in the ModelIcon.newFrame budget queue.
-- resolve a catalog id → ModelIcon spec: returns (build, opts) where build is "new"/"builder" (the
-- ModelIcon constructor to call) or nil for flat swatches (floor/paint, which use a colour icon, not a
-- 3D render — a swatch render would also sync-load ~9 surface PNGs per icon, the tab-switch stall).
local function iconOptsFor(id)
    local cat = Room.CATALOG[id]
    if not cat then return nil end
    if cat.place ~= "wall" then
        local stem = Room.modelStem(id)   -- variant items (arcade_neon → arcade.glb) share one GLB
        if Room.modelAvailable(id, stem) then
            -- autoFrame: pick the front-3/4 yaw that shows the most of the model + tight projected-AABB fit
            return "new", { file = "Models/" .. stem .. ".glb", res = 128, autoFrame = true }
        elseif cat.build then
            local w, h = cat.w or 1, cat.h or 1
            local ch = cat.collH or 1.0
            local build = cat.build
            return "builder", {
                res = 128, yaw = 25,
                frame = { cx = 0, cy = ch * 0.45, cz = 0, radius = math.sqrt(w * w + h * h + ch * ch) * 0.55 },
                populate = function(wld)
                    A.registerInto(wld.scene)
                    build(wld, 0, 0, 0, 0)
                end,
            }
        end
    else
        local g = Room.WALL_GLB and Room.WALL_GLB[id]
        if g and Room.modelAvailable(id, g.file) then
            -- wall items are FLAT (thin in one axis); faceThin points the camera along that axis so the
            -- FACE is shown head-on, not the thin edge. (If a model shows its back, flip the yaw sign in
            -- modelicon.lua's faceThin block.)
            return "new", { file = "Models/" .. g.file .. ".glb", res = 128, faceThin = true }
        elseif cat.picture and Room.pictureVisual then
            -- framed picture (portrait): render the SAME framed quad as placement, built at y=0 so it's
            -- centred in the icon frame (photo centre ≈ y+0.4). Nearly head-on so it isn't skewed.
            local picTex = cat.picture
            return "builder", {
                res = 128, yaw = 6, pitch = -6,
                frame = { cx = 0, cy = 0.4, cz = -0.03, radius = 0.52 },
                populate = function(wld)
                    A.registerInto(wld.scene)
                    Room.pictureVisual(wld, { ax = 0, az = 0, nx = 0, nz = -1, tx = 1, tz = 0 }, 0, picTex)
                end,
            }
        elseif cat.wallBuild then
            local wallBuild = cat.wallBuild
            return "builder", {
                res = 128, yaw = 16, pitch = -10,
                frame = { cx = 0, cy = 0.32, cz = -0.12, radius = 0.62 },
                populate = function(wld)
                    A.registerInto(wld.scene)
                    -- a synthetic back-wall frame at the origin (inward normal -z)
                    wallBuild(wld, { ax = 0, az = 0, nx = 0, nz = -1, tx = 1, tz = 0 }, 0)
                end,
            }
        end
    end
    return nil
end

local function makeIcon(id, res, cpu)
    local ctor, opts = iconOptsFor(id)
    if not ctor then return nil end
    if res then opts.res = res end
    opts.cpu = cpu or nil
    if ctor == "builder" then return ModelIcon.newBuilder(opts) end
    return ModelIcon.new(opts)
end

function Edit:modelIconFor(id)
    self.modelIcons = self.modelIcons or {}
    local cached = self.modelIcons[id]
    if cached ~= nil then
        if cached == false then return nil end          -- known "no icon"
        if not cached._disposed then return cached end  -- live cache (kept across open/close)
        -- else it was disposed (stage teardown) → fall through and rebuild
    end
    local ic = makeIcon(id, 128, false) or false
    self.modelIcons[id] = ic
    return ic or nil
end

-- Bake shop thumbnails: for each id, render it ONCE on the CPU rasterizer (the only path whose canvas
-- has a readable pixel buffer) and copy the image into a SHARED texture keyed `prefix..id`, which the
-- coin shop then draws — reusing My Room's furniture models/geometry without re-declaring them there.
-- Standalone (no Edit instance) so Script can bake at stage load. Returns how many baked (for logging).
function Edit.bakeThumbnails(ids, prefix, res)
    local n = 0
    for _, id in ipairs(ids) do
        local ic = makeIcon(id, res or 128, true)
        if ic then
            if ic:shareToSharedTexture(prefix .. id) then n = n + 1 end
            ic:dispose()
        end
    end
    return n
end

function Edit:disposeModelIcons()
    for _, ic in pairs(self.modelIcons or {}) do
        if ic then ic:dispose() end
    end
    self.modelIcons = nil
end

-- ── state setters (also what the PopUI callbacks route through — harness-drivable) ────────────────
function Edit:setCategory(i)
    if self.cat ~= i then
        if self.hold then self:restoreHold(true) end
        self.cat = i
        self.sel = nil
        self:deselect(true)
        self._barDirty = true
    end
end
function Edit:setSelection(id)
    self.sel = (self.sel == id) and nil or id
    self.facing, self.mount = 0, "low"
    self:deselect(true)
    self._barDirty = true
end

-- ── selection: inline Rotate / Move / Remove buttons right under the placed item ──────────────────
function Edit:select(it, kind)
    if self.selected and self.selected.it == it then return end
    self:deselect(true)
    self.selected = { it = it, kind = kind }
    local ui = PopUI.new{ theme = THEME, navPlayer = (playerIndex or 0) + 1 }
    self.selUI = ui
    local edit = self
    local defs = {}
    if kind == "ground" then
        defs[#defs + 1] = { T:tr("action_rotate"), function() edit:doRotate(it); return true end }
        defs[#defs + 1] = { T:tr("action_move"), function() edit:beginHoldMove(); return true end }
        defs[#defs + 1] = { T:tr("action_remove"), function() edit:doRemove(it); return true end }
    else
        defs[#defs + 1] = { T:tr("action_move"), function() edit:beginHoldMove(); return true end }
        if it.id ~= "phone" then defs[#defs + 1] = { T:tr("action_remove"), function() edit:doRemove(it); return true end } end
    end
    self.selBtns = {}
    for i, d in ipairs(defs) do
        self.selBtns[i] = ui:button{ text = d[1], x = 0, y = -1000, w = 106, h = 48,
            style = { radius = 16, font = { button = 17 } }, onClick = d[2] }
    end
    self:positionSelButtons()
    SHARED:GetSharedSound("Decide"):Play()
end

function Edit:deselect(silent)
    if self.selUI then self.selUI:disposeWidgets() end
    if not silent and (self.selUI or self.selected or self.selBtns) then
        SHARED:GetSharedSound("Cancel"):Play()
    end
    self.selUI, self.selected, self.selBtns = nil, nil, nil
end

-- lay the inline buttons right under the selected item (projected to screen each frame)
function Edit:positionSelButtons()
    local s = self.selected
    if not (s and self.selBtns) then return end
    local room, world = self.room, self.world
    local it = s.it
    local sx, sy
    if s.kind == "wall" then
        local gh = room:gridH()
        local wx = (it.c == room.iw) and room.iw or (it.c + 0.5)
        local wz = (it.c == room.iw) and ((gh - 1 - it.r) + 0.5) or (gh - 1)
        sx, sy = world:project(wx, FY + Room.MOUNT_Y[it.mount or "low"] - 0.15, wz)
    else
        local cat = Room.CATALOG[it.id]
        local w, h = cat and cat.w or 1, cat and cat.h or 1
        if (it.facing or 0) % 2 == 1 then w, h = h, w end
        local fx, fz = world:footprintCenter(it.c, it.r, w, h)
        sx, sy = world:project(fx, FY, fz)
    end
    if sx == nil then return end
    local n = #self.selBtns
    local total = n * 106 + (n - 1) * 8
    local x0 = min(SW - total - 12, max(12, floor(sx - total / 2)))
    -- sit the buttons well BELOW the item (the floor projection lands mid-item on the tilted camera,
    -- so a small offset covered it); wall items need less drop
    local off = (s.kind == "wall") and 30 or 78
    local by = min(BAR_Y - 60, max(54, floor(sy + off)))
    for i, b in ipairs(self.selBtns) do
        b.x = x0 + (i - 1) * 114
        b.y = by
    end
end

function Edit:mouseOverSelUI(mx, my)
    if not self.selBtns then return false end
    for _, b in ipairs(self.selBtns) do
        if mx >= b.x - 6 and mx < b.x + b.w + 6 and my >= b.y - 6 and my < b.y + b.h + 6 then return true end
    end
    return false
end

-- error feedback for a refused rotation: an SFX + a brief red flash on the item
local function errPlay()
    SHARED:GetSharedSound("Error"):Play()
end
function Edit.disposeSfx()
    -- no things to do for now
end
function Edit:rotateRefused(it)
    self._rotFlash = { it = it, f = 26 }          -- ~0.4s of red flash at 60fps
    errPlay()
end

function Edit:doRotate(it)
    local room = self.room
    local cat = Room.CATALOG[it.id]
    local oldFacing = it.facing or 0
    local newFacing = (oldFacing + 1) % 4
    -- a surface carries its riders through the turn: plan each rider's rotated cell + facing
    local plan = {}
    if cat and cat.surface then
        for _, st in ipairs(room:stackedItemsOn(it)) do
            local nc, nr = room:rotatedRiderCell(it, st)
            plan[#plan + 1] = { it = st, nc = nc, nr = nr, nf = ((st.facing or 0) + 1) % 4 }
        end
    end
    -- detach riders so they don't block the surface's own fit test, then validate as a unit
    for _, p in ipairs(plan) do room:detachFurniture(p.it) end
    it.facing = newFacing
    local ok = room:canPlace(it.id, it.c, it.r, newFacing, it)
    if ok then
        -- every rider must land inside the rotated surface footprint, none overlapping another
        local fp, occ = {}, {}
        for _, cell in ipairs(room:footprint(it.id, it.c, it.r, newFacing)) do fp[cell[1] .. "," .. cell[2]] = true end
        for _, p in ipairs(plan) do
            for _, cell in ipairs(room:footprint(p.it.id, p.nc, p.nr, p.nf)) do
                local k = cell[1] .. "," .. cell[2]
                if not fp[k] or occ[k] then ok = false; break end
                occ[k] = true
            end
            if not ok then break end
        end
    end
    if ok then
        for _, p in ipairs(plan) do
            p.it.c, p.it.r, p.it.facing = p.nc, p.nr, p.nf
            room:attachFurniture(p.it)             -- re-stacks onto the rotated surface
        end
        self:commit()                              -- rebuild: the rotation shows live
        SHARED:GetSharedSound("Skip"):Play()
    else
        it.facing = oldFacing                      -- revert; riders never moved
        for _, p in ipairs(plan) do room:attachFurniture(p.it) end
        self:rotateRefused(it)
    end
end
function Edit:doRemove(it)
    local room = self.room
    local cat = Room.CATALOG[it.id]
    if cat and cat.place == "wall" then
        if room:removeWallItem(it) then room:invAdd(it.id); self:commit(); self._barDirty = true end
    else
        room:removeFurniture(it); room:invAdd(it.id); self:commit(); self._barDirty = true
    end
    self:deselect(true)
    SHARED:GetSharedSound("Cancel"):Play()
end

-- ── the in-hand move flow (Move button OR press-drag; ground + wall items alike) ──────────────────
-- The item is DETACHED from the room while held, so the original visibly disappears and the ghost
-- at the hovered cell/slot is the live candidate. The phone moves through here too (the detach
-- bypasses the no-remove rule; cancel/leave always restores it).
function Edit:pickUp(it, kind, viaDrag)
    local room = self.room
    local riders = nil
    if kind == "wall" then
        for i, w in ipairs(room.wallItems) do
            if w == it then table.remove(room.wallItems, i); break end
        end
    else
        -- a surface travels WITH all its stacked riders: detach them as a unit (offsets kept)
        local cat = Room.CATALOG[it.id]
        if cat and cat.surface then
            for _, st in ipairs(room:stackedItemsOn(it)) do
                riders = riders or {}
                riders[#riders + 1] = { it = st, dc = st.c - it.c, dr = st.r - it.r }
                room:detachFurniture(st)
            end
        end
        room:detachFurniture(it)
    end
    self.hold = { it = it, kind = kind, fromC = it.c, fromR = it.r,
                  fromMount = it.mount, drag = viaDrag and true or false, riders = riders,
                  -- last VALID (placeable) target; the ghost parks here when the cursor goes off-screen
                  -- so the piece never "depops". Seeded to its origin (a valid spot).
                  lastC = it.c, lastR = it.r, lastMount = it.mount,
                  lastSlot = (kind == "wall") and { c = it.c, r = it.r, mount = it.mount or "low" } or nil }
    self:deselect(true)
    self.pressItem = nil
    self:commit()                                  -- rebuild without the original
    SHARED:GetSharedSound(viaDrag and "Move" or "Decide"):Play()
end

function Edit:beginHoldMove()
    local s = self.selected
    if s then self:pickUp(s.it, s.kind, false) end
end

function Edit:restoreHold(silent)
    local h = self.hold
    if h == nil then return end
    local room = self.room
    h.it.c, h.it.r = h.fromC, h.fromR
    if h.kind == "wall" then
        h.it.mount = h.fromMount
        room.wallItems[#room.wallItems + 1] = h.it
    else
        room:attachFurniture(h.it)          -- re-derives the stacked flag at the restore spot
        for _, rd in ipairs(h.riders or {}) do
            rd.it.c, rd.it.r = h.fromC + rd.dc, h.fromR + rd.dr
            room:attachFurniture(rd.it)     -- lands back on the restored surface
        end
    end
    self.hold = nil
    self:commit()
    if not silent then SHARED:GetSharedSound("Cancel"):Play() end
end

-- try to drop the held item at the hovered target; returns true when it landed
function Edit:dropHold()
    local h = self.hold
    if h == nil then return false end
    local room = self.room
    if h.kind == "wall" then
        local s = self.hoverSlot or h.lastSlot                   -- released off-screen → last valid slot
        if s and room:canPlaceWall(h.it.id, s.c, s.r, s.mount) then
            h.it.c, h.it.r, h.it.mount = s.c, s.r, s.mount
            room.wallItems[#room.wallItems + 1] = h.it
            local dropped = h.it
            self.hold = nil
            self:commit()
            self:select(dropped, "wall")
            return true
        end
    else
        local c, r = self.hoverC, self.hoverR
        if not c then c, r = h.lastC, h.lastR end                -- released off-screen → last valid cell
        if c and room:canPlace(h.it.id, c, r, h.it.facing or 0) then
            h.it.c, h.it.r = c, r
            room:attachFurniture(h.it)      -- lands stacked when dropped onto a surface
            -- riders travel with their surface: same relative cells, re-stacked on top
            for _, rd in ipairs(h.riders or {}) do
                rd.it.c, rd.it.r = c + rd.dc, r + rd.dr
                room:attachFurniture(rd.it)
            end
            local dropped = h.it
            self.hold = nil
            self:commit()
            self:select(dropped, "ground")
            return true
        end
    end
    return false
end

-- the door can't move out from under the player's feet
function Edit:playerOnExit()
    return self.playerC == self.room.exitCol and self.playerR == self.room:gridH() - 1
end
function Edit:moveDoorTo(c)
    local room = self.room
    if c == room.exitCol then return true end
    if c == nil or not room:canMoveExitTo(c) or self:playerOnExit() then return false end
    room.exitCol = c
    self:commit()
    SHARED:GetSharedSound("Decide"):Play()
    return true
end

-- ── the PopUI bar: tab row + square item grid ─────────────────────────────────────────────────────
function Edit:buildBar()
    self._barDirty = false
    if self.ui then self.ui:disposeWidgets() end
    local ui = PopUI.new{ theme = THEME, navPlayer = (playerIndex or 0) + 1 }
    self.ui = ui
    self.barSlots = {}
    local edit = self
    ui:panel{ x = 8, y = BAR_Y + 4, w = SW - 16, h = BAR_H - 8 }

    -- tab row
    for i, cdef in ipairs(CATS) do
        local ti = i
        ui:button{
            text = T:tr(cdef.label), x = TAB_X0 + (i - 1) * TAB_STEP, y = TAB_Y, w = TAB_W, h = TAB_H,
            accent = (self.cat == i),
            style = { font = { button = 19 } },
            onClick = function() edit:setCategory(ti) end,
        }
    end

    -- item grid: square buttons — icon + name underneath + a ×N stock pill (drawn in draw())
    local ids = self:items()
    for i, id in ipairs(ids) do
        local iid = id
        local x = GRID_X0 + (i - 1) * SLOT_STEP
        local y = GRID_Y
        if x + SLOT_W < SW - 16 then
            local selOn = (self.sel == iid)
            ui:button{
                text = "", x = x, y = y, w = SLOT_W, h = SLOT_H, accent = selOn,
                onClick = function() edit:setSelection(iid) end,
            }
            local infinite = (iid == Room.DEFAULT_FLOOR or iid == Room.DEFAULT_PAINT)
            self.barSlots[#self.barSlots + 1] = { id = iid, x = x, y = y, w = SLOT_W, h = SLOT_H,
                                                  n = infinite and -1 or (self.room.inventory[iid] or 0), sel = selOn }
        end
    end
    ui:clearPrevFocus()
end

-- the rounded ×N stock pill (baked once with PopUI Shape in the theme's accent colours)
function Edit:badgePill()
    if self._badge then return self._badge end
    local cv = CANVAS:CreateCanvas(46, 26)
    cv:Clear(0, 0, 0, 0)
    PopUI.Shape.fillRoundAA(cv, 0, 0, 46, 26, 12, THEME.colors.primary2)
    PopUI.Shape.fillRoundAA(cv, 2, 2, 42, 22, 10, THEME.colors.primary)
    cv:Upload()
    self._badge = cv
    return cv
end

-- ── 3D picking (screen-projected, like the old editor) ────────────────────────────────────────────
-- nearest floor cell to the cursor. floorOnly=true tests ONLY the floor point (used by hover so a
-- cursor a BLOCK ABOVE a piece maps to the cell BEHIND it, not the piece); the default also tests each
-- cell's item TOP so placement can aim AT an elevated surface.
function Edit:pickGround(mx, my, floorOnly)
    local room, world = self.room, self.world
    local best, bc, br = PICK2, nil, nil
    for r = 0, room:gridH() - 1 do
        for c = 0, room:gridW() - 1 do
            local t = room:cellType(c, r)
            if t == "O" or t == "E" then
                local wx, wz = world:cellToWorld(c, r)
                local sx, sy, depth = world:project(wx, FY, wz)
                if depth and depth > 0 then
                    local dx, dy = sx - mx, sy - my; local d = dx * dx + dy * dy
                    if d < best then best, bc, br = d, c, r end
                end
                if not floorOnly then
                    -- also test the cell's ITEM TOP so an elevated item on a surface (a computer on a
                    -- desk) is picked by aiming AT it, not at the floor behind it (was clunky)
                    local it = room:furnitureAt(c, r)
                    if it and it._topY and it._topY > FY + 0.1 then
                        local sx2, sy2, d2 = world:project(wx, it._topY, wz)
                        if d2 and d2 > 0 then
                            local dx, dy = sx2 - mx, sy2 - my; local dd = dx * dx + dy * dy
                            if dd < best then best, bc, br = dd, c, r end
                        end
                    end
                end
            end
        end
    end
    return bc, br
end
-- nearest per-(tile × mount) wall-item slot
function Edit:pickWallSlot(mx, my)
    local best, bs = PICK2, nil
    for _, s in ipairs(self.room:wallItemSlots()) do
        local sx, sy, depth = self.world:project(s.wx, s.wy, s.wz)
        if depth and depth > 0 then
            local dx, dy = sx - mx, sy - my; local d = dx * dx + dy * dy
            if d < best then best, bs = d, s end
        end
    end
    return bs
end
-- nearest per-tile wall slot (paint)
function Edit:pickWallTile(mx, my)
    local best, bs = PICK2, nil
    for _, s in ipairs(self.room:wallSlots()) do
        local sx, sy, depth = self.world:project(s.wx, s.wy, s.wz)
        if depth and depth > 0 then
            local dx, dy = sx - mx, sy - my; local d = dx * dx + dy * dy
            if d < best then best, bs = d, s end
        end
    end
    return bs
end
-- nearest front-fringe column (for the door)
function Edit:pickFringeCol(mx, my)
    local room, world = self.room, self.world
    local gh = room:gridH(); local best, bc = (SW * 0.06) ^ 2, nil
    for c = 0, room.iw - 1 do
        local wx, wz = world:cellToWorld(c, gh - 1)
        local sx, sy, depth = world:project(wx, FY, wz)
        if depth and depth > 0 then
            local dx, dy = sx - mx, sy - my; local d = dx * dx + dy * dy
            if d < best then best, bc = d, c end
        end
    end
    return bc
end

-- ── input ─────────────────────────────────────────────────────────────────────────────────────────
-- returns "exit" when the editor should close, else nil
function Edit:update(ts, editCameraFuncs)
    if INPUT:KeyboardPressed("Tab") then
        self:leave()
        SHARED:GetSharedSound("Cancel"):Play()
        return "exit"
    end
    if self._barDirty then self:buildBar() end
    if self._rotFlash then
        self._rotFlash.f = self._rotFlash.f - 1
        if self._rotFlash.f <= 0 then self._rotFlash = nil end
    end

    local mx, my = INPUT:GetMouseXY()
    local dmx, dmy = INPUT:GetMouseDelta()
    local _, sdy = INPUT:GetScrollDelta()
    local lpressed  = INPUT:MousePressed("left")
    local lpressing = INPUT:MousePressing("left")
    local lreleased = INPUT:MouseReleased("left")
    local overBar   = my >= BAR_Y
    self.mx, self.my, self.overBar = mx, my, overBar

    self.ui:update(ts)

    editCameraFuncs.pan(dmx, dmy)
    editCameraFuncs.move()

    -- inline selection buttons: repositioned every frame, updated before the 3D interactions
    -- (a click on them must never reach the room). A press-drag that STARTED on the item itself
    -- may cross the buttons while held — don't let them swallow it.
    local overSel = false
    if self.selUI then
        self:positionSelButtons()
        self.selUI:update(ts)
        overSel = self:mouseOverSelUI(mx, my) and not (self.pressItem and lpressing)
    end

    -- Esc backs out of the current sub-mode (right mouse is reserved for camera rotation in edit mode)
    if INPUT:KeyboardPressed("Escape") then
        if self.hold then self:restoreHold()
        elseif self.selected then self:deselect()
        elseif self.sel or self.dragExit then
            self.sel, self.dragExit = nil, false
            self._barDirty = true
            SHARED:GetSharedSound("Cancel"):Play()
        end
    end

    if not overBar and not overSel then
        local key = self:catKey()
        if self.hold then
            self:updateHold(mx, my, sdy, lpressed, lreleased, editCameraFuncs.zoom)
        elseif key == "door" then
            editCameraFuncs.zoom(sdy)
            self:updateDoor(mx, my, lpressed)
        elseif key == "eraser" then
            editCameraFuncs.zoom(sdy)
            self:updateEraser(mx, my, lpressed, lpressing)
        elseif self.sel then
            self:updatePlace(mx, my, sdy, lpressed, lpressing, editCameraFuncs.zoom, dmx, dmy)
        else
            editCameraFuncs.zoom(sdy)
            self:updateBrowse(mx, my, lpressed, lpressing, lreleased)
        end
    else
        editCameraFuncs.zoom(sdy)
        self.hoverC, self.hoverR, self.hoverSlot, self.hoverTile, self.hoverExitC = nil, nil, nil, nil, nil
        self.hoverItem, self.hoverWallItem, self.hoverExit = nil, nil, false
        if lreleased then self.pressItem = nil end
    end

    self:buildOverlays()
    return nil
end

-- place mode: ghost follows the hover; click/swipe to place
function Edit:updatePlace(mx, my, sdy, lpressed, lpressing, zoomCamera, dmx, dmy)
    local room = self.room
    local key = self:catKey()
    if key == "furn" then
        if sdy ~= 0 then  -- wheel rotates
            self.facing = (self.facing + (sdy > 0 and 1 or 3)) % 4
            SHARED:GetSharedSound("Skip"):Play()
        end
        local c, r = self:pickGround(mx, my)
        if c and r and (self.hoverC ~= c or self.hoverR ~= r) then
            SHARED:GetSharedSound("Move"):Play()
        end
        self.hoverC, self.hoverR = c, r
        if lpressed then
            if c and room:canPlace(self.sel, c, r, self.facing) and room:invTake(self.sel) then
                room:addFurniture(self.sel, c, r, self.facing); self:commit()
                SHARED:GetSharedSound("Decide"):Play()
            end
            self.sel = nil; self._barDirty = true                 -- furniture → back to item select
        end
    elseif key == "wall" then
        -- point at the LOW or HIGH band of a tile to place on that layer (both coexist per tile);
        -- the wheel still nudges the preferred mount for when two bands project close together
        local s = (dmx ~= 0 or dmy ~= 0) and self:pickWallSlot(mx, my) or self.hoverSlot
        if sdy ~= 0 and s then
            local mount = (self.mount == "low") and "high" or "low"
            self.mount = mount
            local alt = room:wallItemSlots()
            for _, o in ipairs(alt) do if o.c == s.c and o.r == s.r and o.mount == mount then s = o; break end end
            SHARED:GetSharedSound("Skip"):Play()
        end
        if s and (not self.hoverSlot or self.hoverSlot.key ~= s.key) then
            SHARED:GetSharedSound("Move"):Play()
        end
        self.hoverSlot = s
        if lpressed and s then
            if room:canPlaceWall(self.sel, s.c, s.r, s.mount) and room:invTake(self.sel) then
                room:addWallItem(self.sel, s.c, s.r, s.mount); self:commit()
                SHARED:GetSharedSound("Decide"):Play()
            end
            self.sel = nil; self._barDirty = true
        end
    elseif key == "floor" then
        zoomCamera(sdy)
        local c, r = self:pickGround(mx, my)
        if c and r and (self.hoverC ~= c or self.hoverR ~= r) then
            SHARED:GetSharedSound("Move"):Play()
        end
        self.hoverC, self.hoverR = c, r
        local sel, remain
        if self.sel == Room.DEFAULT_FLOOR then sel, remain = nil, math.huge  -- infinite: revert to wood
        else sel, remain = self.sel, room.inventory[self.sel] or 0
        end
        if lpressing and c and room:cellType(c, r) == "O" and room:floorAt(c, r) ~= sel and remain > 0 then
            if room:floorAt(c, r) then room:invAdd(room:floorAt(c, r)) end   -- refund the covered paint
            if sel ~= nil then room:invTake(sel) end
            room:setFloor(c, r, sel); self:commit(); self._barDirty = true
            SHARED:GetSharedSound("Decide"):Play()
            if remain <= 0 then self.sel = nil end  -- out of stock → back to select
        end
    elseif key == "paint" then
        zoomCamera(sdy)
        local s = self:pickWallTile(mx, my)
        if s and (not self.hoverTile or self.hoverTile.key ~= s.key) then
            SHARED:GetSharedSound("Move"):Play()
        end
        self.hoverTile = s
        local sel, remain
        if self.sel == Room.DEFAULT_PAINT then sel, remain = nil, math.huge  -- infinite: revert to plaster
        else sel, remain = self.sel, room.inventory[self.sel] or 0
        end
        if lpressing and s and room:wallPaintAt(s.key) ~= sel and remain > 0 then
            if room:wallPaintAt(s.key) then room:invAdd(room:wallPaintAt(s.key)) end
            if sel ~= nil then room:invTake(sel) end
            room:setWallPaint(s.key, sel); self:commit(); self._barDirty = true
            SHARED:GetSharedSound("Decide"):Play()
            if remain <= 0 then self.sel = nil end
        end
    end
end

-- the Door tab: the ghost doorway follows the hovered front-fringe tile; click moves the exit there
function Edit:updateDoor(mx, my, lpressed)
    local c = self:pickFringeCol(mx, my)
    if c and self.hoverExitC ~= c then SHARED:GetSharedSound("Move"):Play() end
    self.hoverExitC = c
    if lpressed then self:moveDoorTo(c) end
end

-- in-hand move (Move button or press-drag): the ghost is the live candidate at the hover
function Edit:updateHold(mx, my, sdy, lpressed, lreleased, zoomCamera)
    local h = self.hold
    local room = self.room
    if h.kind == "wall" then
        zoomCamera(sdy)
        local s = self:pickWallSlot(mx, my)
        if s and (not self.hoverSlot or self.hoverSlot.key ~= s.key) then
            SHARED:GetSharedSound("Move"):Play()
        end
        self.hoverSlot = s                       -- the slot's own mount: drag to the height you want
        self.hoverC, self.hoverR = nil, nil
        if s and room:canPlaceWall(h.it.id, s.c, s.r, s.mount) then h.lastSlot = s end   -- remember last valid target
    else
        if sdy ~= 0 then
            h.it.facing = ((h.it.facing or 0) + (sdy > 0 and 1 or 3)) % 4
            SHARED:GetSharedSound("Skip"):Play()
        end
        local c, r = self:pickGround(mx, my)
        if c and r and (self.hoverC ~= c or self.hoverR ~= r) then
            SHARED:GetSharedSound("Move"):Play()
        end
        self.hoverC, self.hoverR = c, r
        self.hoverSlot = nil
        if c and room:canPlace(h.it.id, c, r, h.it.facing or 0) then h.lastC, h.lastR = c, r end   -- remember last valid target
    end
    if h.drag then
        if lreleased then
            if not self:dropHold() then self:restoreHold() end   -- an illegal drag-drop restores
        end
    elseif lpressed then
        self:dropHold()                                          -- an illegal click keeps holding
    end
end

-- the Eraser tab: hover ANY placed thing and click/swipe to remove it (never the phone)
function Edit:updateEraser(mx, my, lpressed, lpressing)
    local room = self.room
    local prevDelTarget = self.defTarget
    self.delTarget, self.delKind = nil, nil
    local c, r = self:pickGround(mx, my); self.hoverC, self.hoverR = c, r
    self.hoverSlot, self.hoverTile = nil, nil
    local g = c and room:furnitureAt(c, r)
    if g then
        self.delTarget, self.delKind = g, "furn"
    else
        local ws = self:pickWallSlot(mx, my)
        local wIt = ws and room:wallItemAt(ws.c, ws.r, ws.mount)
        if wIt and wIt.id == "phone" then wIt = nil end          -- the phone can't be erased
        if wIt then
            self.delTarget, self.delKind, self.hoverSlot = wIt, "wall", ws
        elseif c and room:floorAt(c, r) then
            self.delTarget, self.delKind = room:floorAt(c, r), "floor"
        else
            local wt = self:pickWallTile(mx, my)
            local paint = wt and room:wallPaintAt(wt.key)
            if paint then self.delTarget, self.delKind, self.hoverTile = paint, "paint", wt end
        end
    end
    if self.delTarget and self.defTarget ~= prevDelTarget then
        SHARED:GetSharedSound("Move"):Play()
    end
    if (lpressed or lpressing) and self.delTarget then
        if self.delKind == "furn" then
            room:removeFurniture(self.delTarget); room:invAdd(self.delTarget.id)
            SHARED:GetSharedSound("Cancel"):Play()
        elseif self.delKind == "wall" then
            if not room:removeWallItem(self.delTarget) then return end
            room:invAdd(self.delTarget.id)
            SHARED:GetSharedSound("Cancel"):Play()
        elseif self.delKind == "floor" then
            room:invAdd(self.delTarget); room:setFloor(c, r, nil)
            SHARED:GetSharedSound("Cancel"):Play()
        elseif self.delKind == "paint" then
            room:invAdd(self.delTarget); room:setWallPaint(self.hoverTile.key, nil)
            SHARED:GetSharedSound("Cancel"):Play()
        end
        self:commit(); self._barDirty = true
        self.delTarget, self.delKind = nil, nil
    end
end

-- browse mode: PRESS an item to select it instantly (inline buttons); hold + move to drag it;
-- the door (exit) tile drags along the front fringe
-- at a hovered cell, pick WHICH occupant the cursor aims at BY HEIGHT: the elevated stacked item
-- (a computer on a desk) vs the ground surface under it (the desk). Aim high → the item on top; aim
-- low → the table. Falls back to whichever exists.
function Edit:pickItemAtCell(mx, my, c, r)
    local room, world = self.room, self.world
    local stacked = room:stackedItemAt(c, r)
    local ground = room:groundItemAt(c, r)
    if not stacked then return ground end
    if not ground then return stacked end
    local surfTop = room:surfaceTopY(ground)
    local wx, wz = world:cellToWorld(c, r)
    local function d(y)
        local sx, sy, depth = world:project(wx, y, wz)
        if not depth or depth <= 0 then return math.huge end
        local dx, dy = sx - mx, sy - my; return dx * dx + dy * dy
    end
    local groundMid = (FY + surfTop) * 0.5                                    -- the table's body
    local stackMid  = (surfTop + (stacked._topY or (surfTop + 0.5))) * 0.5    -- the item-on-top's body
    if d(stackMid) <= d(groundMid) then return stacked else return ground end
end

-- pick the ground furniture whose ON-SCREEN bounding box contains the cursor, so hovering a block
-- ABOVE a piece no longer selects it. When boxes overlap (an item stacked on a surface) the SMALLER
-- box wins → aim at the computer to get it, aim at the desk body to get the desk.
function Edit:pickHoverItem(mx, my)
    local room, world = self.room, self.world
    local best, bestArea = nil, math.huge
    for _, it in ipairs(room.furniture) do
        local x0, y0, z0, x1, y1, z1 = itemAABB(world, room, it)
        local minx, miny, maxx, maxy = math.huge, math.huge, -math.huge, -math.huge
        local any = false
        for _, xz in ipairs({ { x0, z0 }, { x1, z0 }, { x0, z1 }, { x1, z1 } }) do
            for _, yy in ipairs({ y0, y1 }) do
                local sx, sy, dp = world:project(xz[1], yy, xz[2])
                if dp and dp > 0 then
                    any = true
                    if sx < minx then minx = sx end
                    if sx > maxx then maxx = sx end
                    if sy < miny then miny = sy end
                    if sy > maxy then maxy = sy end
                end
            end
        end
        if any and mx >= minx and mx <= maxx and my >= miny and my <= maxy then
            local area = (maxx - minx) * (maxy - miny)
            if area < bestArea then best, bestArea = it, area end
        end
    end
    if best then return best end
    -- fallback: the ground item at the cursor's FLOOR cell (floor-only pick, so a cursor a block ABOVE
    -- a piece resolves to the cell behind it, not the piece). Keeps hover robust for thin/short items.
    local c, r = self:pickGround(mx, my, true)
    return c and self:pickItemAtCell(mx, my, c, r) or nil
end

function Edit:updateBrowse(mx, my, lpressed, lpressing, lreleased)
    local room = self.room
    if self.dragExit then                                        -- continue a door drag
        local c = self:pickFringeCol(mx, my)
        if c and self.hoverExitC ~= c then SHARED:GetSharedSound("Move"):Play() end
        self.hoverExitC = c
        if lreleased then
            if not self:moveDoorTo(c) then
                SHARED:GetSharedSound("Cancel"):Play()
            end
            self.dragExit = false
        end
        return
    end

    local gc, gr = self:pickGround(mx, my); self.hoverC, self.hoverR = gc, gr
    local gItem = self:pickHoverItem(mx, my)                   -- precise: cursor must be over the item's box
    local ws = self:pickWallSlot(mx, my)
    local wItem = ws and room:wallItemAt(ws.c, ws.r, ws.mount)
    local onExit = gc and room:cellType(gc, gr) == "E" and not (gc == self.playerC and gr == self.playerR)
    if (gItem and gItem ~= self.hoverItem) or onExit ~= self.hoverExit or (wItem and wItem ~= self.hoverWallItem) then
        SHARED:GetSharedSound("Move"):Play()
    end
    self.hoverItem = gItem
    self.hoverWallItem = wItem
    self.hoverSlot = (not gItem) and wItem and ws or nil
    self.hoverExit = onExit

    -- press-drag: once the pointer leaves the pressed item's cell/slot while held, pick it up
    local pi = self.pressItem
    if pi and lpressing then
        local moved
        if pi.kind == "ground" then
            moved = gc and not (gc == pi.c and gr == pi.r)
        else
            moved = ws and not (ws.c == pi.it.c and ws.r == pi.it.r and ws.mount == (pi.it.mount or "low"))
        end
        if moved then
            self:pickUp(pi.it, pi.kind, true)
            return
        end
    end
    if lreleased then self.pressItem = nil end

    if lpressed then
        if gItem then
            self:select(gItem, "ground")                         -- INSTANT selection on press
            self.pressItem = { it = gItem, kind = "ground", c = gc, r = gr }
        elseif self.hoverExit then
            self.dragExit = true
            SHARED:GetSharedSound("Decide"):Play()
        elseif wItem then
            self:select(wItem, "wall")
            self.pressItem = { it = wItem, kind = "wall" }
        else
            self:deselect()
        end
    end
end

-- ── build the 3D overlays (grid, ghost, hilite) for this frame ────────────────────────────────────
function Edit:buildOverlays()
    local room, world = self.room, self.world
    gridBegin(world)
    for r = 0, room:gridH() - 1 do
        for c = 0, room:gridW() - 1 do
            local t = room:cellType(c, r)
            if t == "O" or t == "E" then gridGround(world, c, r) end
        end
    end
    for _, s in ipairs(room:wallSlots()) do
        if s.wall == "back" then gridWallBack(world, s.idx) else gridWallRight(world, s.idx) end
    end

    ghostClear(world); hiliteClear(world)
    self:hideGhostModel()
    if self.overBar then return end
    local key = self:catKey()

    if self.hold then                                           -- in-hand: the ghost is the candidate
        local h = self.hold
        if h.kind == "wall" then
            local s = self.hoverSlot or h.lastSlot               -- off-screen: keep the ghost at the last valid slot
            if s then
                local ok = room:canPlaceWall(h.it.id, s.c, s.r, s.mount)
                self:ghostWallItem(h.it.id, s.c, s.r, s.mount, ok)
                hiliteWallSlot(world, s, ok, ok)                 -- BLUE while dropping (red only if it can't)
            end
        else
            local c, r = self.hoverC, self.hoverR
            if not c then c, r = h.lastC, h.lastR end            -- off-screen: park the ghost at the last valid cell
            if c then
                local ok = room:canPlace(h.it.id, c, r, h.it.facing or 0)
                self:ghostFurniture(h.it.id, c, r, h.it.facing or 0, ok)
                -- BLUE wireframe box at the drop candidate (red if it can't drop there)
                local cat = Room.CATALOG[h.it.id]
                local tmp = { id = h.it.id, c = c, r = r, facing = h.it.facing or 0, on = cat and cat.stackOn or false }
                local x0, y0, z0, x1, y1, z1 = itemAABB(world, room, tmp)
                if ok then wireBox(world, x0, y0, z0, x1, y1, z1, 120, 200, 255, 220, 0.032)
                else wireBox(world, x0, y0, z0, x1, y1, z1, 235, 90, 90, 220, 0.032) end
            end
        end
    elseif self.dragExit or key == "door" then                  -- door: the ghost doorway follows hover
        hilite(world, { { room.exitCol, room:gridH() - 1 } }, 255, 225, 150, 130)
        local c = self.hoverExitC
        if c and c ~= room.exitCol then
            local ok = room:canMoveExitTo(c) and not self:playerOnExit()
            ghostBegin(world, ok)
            room:buildDoorTileVisual(world, c)
            ghostEnd(world)
        end
    elseif key == "eraser" then                                  -- eraser hover: red on the target
        if self.delKind == "furn" and self.delTarget then
            hilite(world, room:footprint(self.delTarget.id, self.delTarget.c, self.delTarget.r, self.delTarget.facing or 0),
                235, 95, 95, 150, itemHiliteY(room, self.delTarget))
        elseif self.delKind == "wall" and self.hoverSlot then
            hiliteWallSlot(world, self.hoverSlot, false)
        elseif self.delKind == "floor" and self.hoverC then
            hiliteTile(world, self.hoverC, self.hoverR, false)
        elseif self.delKind == "paint" and self.hoverTile then
            hiliteWallTile(world, self.hoverTile, false)
        end
    elseif self.sel then                                        -- place preview
        if key == "furn" then
            local c, r = self.hoverC, self.hoverR
            if c then
                local ok = room:canPlace(self.sel, c, r, self.facing)
                self:ghostFurniture(self.sel, c, r, self.facing, ok)
                -- stackables: draw the placement selector on the HOST SURFACE top (with the ghost),
                -- not on the ground, so placing a TV/PC onto a desk reads correctly
                local hy = nil
                local scat = Room.CATALOG[self.sel]
                if scat and scat.stackOn then
                    local surf = room:surfaceUnder(self.sel, c, r, self.facing)
                    if surf then hy = room:surfaceTopY(surf) + 0.05 end
                end
                hilite(world, room:footprint(self.sel, c, r, self.facing), ok and 90 or 235, ok and 225 or 95, ok and 120 or 95, 120, hy)
            end
        elseif key == "wall" then
            local s = self.hoverSlot
            if s then
                local ok = room:canPlaceWall(self.sel, s.c, s.r, s.mount)
                self:ghostWallItem(self.sel, s.c, s.r, s.mount, ok)
                hiliteWallSlot(world, s, ok)
            end
        elseif key == "floor" then
            local c, r = self.hoverC, self.hoverR
            if c then hiliteTile(world, c, r, room:cellType(c, r) == "O") end
        elseif key == "paint" then
            if self.hoverTile then hiliteWallTile(world, self.hoverTile, true) end
        end
    elseif self.selected then                                    -- selected box + STILL show hovered others
        local it = self.selected.it
        world.scene:ObjSetPass(world.hiliteObj, 1, 120, 200, 255, 220)
        world.scene:ObjBegin(world.hiliteObj)                     -- one pass: selected + hover accumulate
        if self.selected.kind == "wall" then
            wallSlotInto(world, { wall = (it.c == room.iw) and "right" or "back",
                                  c = it.c, r = it.r, mount = it.mount or "low" })
        else
            local x0, y0, z0, x1, y1, z1 = itemAABB(world, room, it)
            wireBoxInto(world, x0, y0, z0, x1, y1, z1, 0.032)     -- selected: thicker box
        end
        if self.hoverItem and self.hoverItem ~= it then          -- hovering a different ground item
            local a0, b0, c0, a1, b1, c1 = itemAABB(world, room, self.hoverItem)
            wireBoxInto(world, a0, b0, c0, a1, b1, c1, 0.024)     -- hover: thinner box
        elseif self.hoverWallItem and self.hoverWallItem ~= it and self.hoverSlot then
            wallSlotInto(world, self.hoverSlot)
        end
    else                                                         -- browse hover
        if self.hoverItem then
            local x0, y0, z0, x1, y1, z1 = itemAABB(world, room, self.hoverItem)
            wireBox(world, x0, y0, z0, x1, y1, z1, 150, 210, 255, 150, 0.026)   -- hover: soft box
        elseif self.hoverWallItem and self.hoverSlot then
            hiliteWallSlot(world, self.hoverSlot, true)
        elseif self.hoverExit then
            hilite(world, { { self.hoverC, self.hoverR } }, 255, 225, 150, 140)
        end
    end
    -- refused-rotation flash: a red footprint over the item (drawn LAST so it wins the hilite obj)
    if self._rotFlash and self._rotFlash.it then
        local it = self._rotFlash.it
        local cat = Room.CATALOG[it.id]
        if cat and cat.place == "wall" then
            hiliteWallSlot(world, { wall = (it.c == room.iw) and "right" or "back",
                                    c = it.c, r = it.r, mount = it.mount or "low" }, false)
        else
            hilite(world, room:footprint(it.id, it.c, it.r, it.facing or 0), 235, 60, 60, 175,
                itemHiliteY(room, it))
        end
    end
end

function Edit:ghostFurniture(id, c, r, facing, ok)
    local world = self.world
    local cat = Room.CATALOG[id]; if cat == nil then return end
    local w, h = cat.w, cat.h; if facing % 2 == 1 then w, h = h, w end
    local cx, cz = world:footprintCenter(c, r, w, h)
    local yaw = (facing or 0) * 90
    -- stackables preview at the hovered surface's top (table/desk), else at floor height
    local baseY = nil
    if cat.stackOn then
        local surf = self.room:surfaceUnder(id, c, r, facing)
        if surf then baseY = self.room:surfaceTopY(surf) end
    end
    -- GLB entries preview with the REAL model instance, bounds-anchored exactly like placement
    -- (Room.poseFitted passes the ANCHOR POINT — the engine's setTransform owns the yaw offset)
    local gi = self:ghostModelFor(id)
    if gi then
        if Room.poseFitted(gi, cx, cz, yaw, cat, baseY) then
            world.scene:ObjSetTint(gi.obj, ok and 0.4 or 1.0, ok and 1.0 or 0.4, ok and 0.5 or 0.4)
            gi:setVisible(true)
            self.activeGhost = gi
            return
        end
    end
    if cat.build then
        ghostBegin(world, ok)
        self.room:buildFurnitureVisual(world, id, cx, cz, yaw, baseY)
        ghostEnd(world)
    end
end

-- icon for an item id: flooring/paint use a colour swatch, furniture the drawn art (fallback)
local function iconFor(id)
    local sw = A.SWATCH[id]
    if sw then return I.swatch(id, sw[1], sw[2], sw[3]) end
    return I.get(id)
end

-- ── draw: the PopUI bar, then icon/name/pill composited over each item button ─────────────────────
function Edit:draw()
    local ui = self.ui
    if not ui then return end
    local key = self:catKey()
    local hint = T:tr("hint_default")
    if self.hold then
        if self.hold.kind == "wall" then
            hint = T:trf("hint_moving_wall",
                Room.displayName(self.hold.it.id), T:tr(self.hold.drag and "drop_release" or "drop_click"))
        else
            hint = T:trf("hint_moving",
                Room.displayName(self.hold.it.id), T:tr(self.hold.drag and "drop_release" or "drop_click"))
        end
    elseif key == "door" then
        hint = T:tr("hint_door_tab")
    elseif key == "eraser" then
        hint = T:tr("hint_eraser")
    elseif self.sel then
        if key == "furn" then hint = T:trf("hint_placing", Room.displayName(self.sel))
        elseif key == "wall" then hint = T:trf("hint_placing_wall", Room.displayName(self.sel))
        else hint = T:trf("hint_painting", Room.displayName(self.sel)) end
    elseif self.selected then
        hint = T:trf("hint_selected", Room.displayName(self.selected.it.id))
    elseif self.dragExit then hint = T:tr("hint_door_sliding")
    elseif self.hoverExit then hint = T:tr("hint_door_hover")
    elseif self.hoverItem or self.hoverWallItem then
        local id = (self.hoverItem or self.hoverWallItem).id
        hint = T:trf("hint_hover_item", Room.displayName(id))
    end
    ui:rect(0, 0, SW, 46, 12, 14, 20, 180)
    ui:drawTextEx(22, hint, 28, 8, { 220, 228, 240 }, { 0, 0, 0, 230 }, 1, 1, 1860)

    -- door marker
    do
        local gh = self.room:gridH()
        local wx, wz = self.world:cellToWorld(self.room.exitCol, gh - 1)
        local sx, sy = self.world:project(wx, FY + 0.05, wz)
        if sx and sx > 0 then ui:drawTextEx(18, T:tr("door_label"), sx - 22, sy - 14, { 255, 232, 150 }, { 0, 0, 0, 230 }) end
    end

    ui:draw()

    -- shop-grid dressing: 3D preview icon (budget-staggered; drawn art until it lands), name, ×N pill
    for _, s in ipairs(self.barSlots) do
        local drawn = false
        local mi = self:modelIconFor(s.id)
        if mi then
            drawn = mi:draw(floor(s.x + (s.w - 66) / 2), s.y + 6, 66, 66) == true
        end
        if not drawn then
            local ic = iconFor(s.id)
            if ic then I.draw(ic, s.x + (s.w - 60) / 2, s.y + 10, 60, 60) end
        end
        local fg = s.sel and { 255, 255, 255 } or { 92, 62, 46 }
        local bg = s.sel and { 120, 60, 20, 220 } or { 255, 255, 255, 160 }
        ui:drawTextEx(15, Room.displayName(s.id), floor(s.x + s.w / 2), s.y + s.h - 42, fg, bg, 1, 1, s.w - 10, "top")
        if s.n >= 0 then                                        -- infinite default swatches show no count
            local pill = self:badgePill()
            pill:SetColor(1, 1, 1); pill:SetScale(1, 1); pill:SetOpacity(1)
            pill:Draw(s.x + s.w - 52, s.y + 6)
            ui:drawTextEx(15, "×" .. s.n, s.x + s.w - 29, s.y + 8, { 255, 255, 255 }, { 60, 30, 10, 200 }, 1, 1, 0, "top")
        end
    end
    if key == "door" then
        ui:drawTextEx(19, T:tr("door_help"),
            GRID_X0 + 6, GRID_Y + 22, { 92, 62, 46 }, { 255, 255, 255, 160 })
    elseif key == "eraser" then
        ui:drawTextEx(19, T:tr("eraser_help"),
            GRID_X0 + 6, GRID_Y + 22, { 92, 62, 46 }, { 255, 255, 255, 160 })
    end

    if self.selUI then self.selUI:draw() end
end

return Edit

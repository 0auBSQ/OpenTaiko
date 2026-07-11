---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- room.lua — the myroom v2 data model + OWM3d geometry/physics builder + (de)serialization.
--
-- Interior IW×IH grid wrapped by a back wall (far row) + right wall (far col), with a front fringe row
-- holding the exit doorway (slidable). Grid coords: col 0..IW-1 interior / IW = right wall ; row 0 =
-- back wall / 1..IH interior / IH+1 = front fringe.  gridW = IW+1, gridH = IH+2.
--
-- v2 additions over the isoengine room:
--   * The floor is a raised plank PLATFORM (FLOOR_Y high) with visible side skirts; flooring deco
--     (carpet) renders as a thin rug slab on top. heightAt() returns the platform height.
--   * wallItems are FIRST-CLASS: {id, c, r, mount="low"|"high"} on any valid wall tile, at one of two
--     mount heights. The PHONE is a movable wall item that always exists exactly once.
--   * Geometry + physics build together: platform tops/skirts, wall boxes, furniture footprint boxes
--     and invisible edge fences all enter the physics soup (world.phys) during the proc-map build.
--   * Ground furniture uses Models/<id>.glb via world.models (pcall-guarded, auto-fitted to the
--     footprint); a missing model renders as a plain placeholder box sized to the footprint.
--
-- Serialization: toTable() keeps every v1 key (tier/iw/ih/exitCol/furniture/floorDeco/wallPaint/
-- inventory) and adds v=2 + wallItems. Legacy-renderable wall items (clock) are ALSO mirrored into the
-- furniture array so a v1 client (old save reader or old P2P peer) still shows them. loadTable()
-- accepts v1 tables (absent v) and migrates wall furniture + the fixed phone into wallItems.

local A = require("assets")
local IsoMap = require("OWM3d.isomap")
local MIG = require("migration")       -- save-table migrations (retired ids, v1→v2, starter seeding)
local cos, sin, pi, floor = math.cos, math.sin, math.pi, math.floor

local Room = {}
Room.__index = Room

Room.FLOOR_Y = 0.35                    -- platform top height (the character walks up here)
Room.WALL_H  = 3.0                     -- wall height above the platform
Room.WIN_Y0, Room.WIN_Y1 = 1.25, 2.35  -- window opening band, relative to the platform top
Room.MOUNT_Y = { low = 1.0, high = 1.9 }  -- wall-item base heights above the platform top
local FY = Room.FLOOR_Y

-- ── box helper (fills world._boxTarget: placeholders, the window frame, picture frames, ghosts) ──
local function bx(world, cx, y0, cz, sx, sy, sz, yaw, tex, sTop, sSide)
    IsoMap.box(world, cx, y0, cz, sx, sy, sz, yaw, tex, sTop, sSide)
end

-- ── missing-model placeholders ────────────────────────────────────────────────────────────────────
-- Every furniture visual comes from Models/<id>.glb. When the file is absent the item renders as a
-- plain neutral box sized to its footprint, so a missing model is obvious without pretending to be
-- the real thing.
local GROUND_BUILDERS = {}   -- per-id override hooks (none by default; the GLB is the visual)

-- ── wall-item builders (a local wall frame keeps them wall-agnostic) ──────────────────────────────
-- frame: anchor (ax,az) at the wall-face centre of the tile, inward normal (nx,nz), tangent (tx,tz).
-- HANDEDNESS: +tangent must read as the VIEWER'S RIGHT when standing in the room facing the wall,
-- on EVERY wall — else anything asymmetric along the tangent (portrait pixels, shelf books, the
-- phone builder's handset) renders mirrored on one wall. Camera basis: facing +z (back wall) the
-- screen-right is +x; facing +x (right wall) the screen-right is -z. The right wall shipped with
-- tz=+1 (viewer's LEFT) for a long time — that was the "every wall furniture is flipped on the
-- right wall" bug.
local function wallFrame(room, c, r)
    local gh = room:gridH()
    if c == room.iw then     -- right wall (plane x = iw), interior is -x; viewer-right = -z
        return { ax = room.iw, az = (gh - 1 - r) + 0.5, nx = -1, nz = 0, tx = 0, tz = -1 }
    end                      -- back wall (plane z = gh-1), interior is -z; viewer-right = +x
    return { ax = c + 0.5, az = gh - 1, nx = 0, nz = -1, tx = 1, tz = 0 }
end
-- a box in the wall frame: `a` along the wall from centre, base `u` above baseY, front face `d` off
-- the wall; extent along×up×depth. Axis-aligned (walls are axis-aligned), so sizes map by orientation.
local function wb(world, f, baseY, a, u, d, alongSz, upSz, depthSz, tex, sTop, sSide)
    local off = d + depthSz * 0.5
    local cx = f.ax + f.tx * a + f.nx * off
    local cz = f.az + f.tz * a + f.nz * off
    local sx = (f.tx ~= 0) and alongSz or depthSz
    local sz = (f.tz ~= 0) and alongSz or depthSz
    bx(world, cx, baseY + u, cz, sx, upSz, sz, 0, tex, sTop, sSide)
end

-- a framed picture (souvenir poster/painting): a gold frame slab + the photo texture flush on its
-- front face, upright and centred on the wall tile. Shared by placement AND the edit preview icon so
-- they always match. `f` = wallFrame, `y` = mount-height world Y, `picTex` = the texture id.
local function pictureVisual(world, f, y, picTex)
    local frameH = 0.8
    wb(world, f, y, 0, 0, 0.0, 0.8, frameH, 0.06, A.COL.gold, 1.05, 0.85)    -- gold frame (bx grows UP from y)
    -- the photo must sit at the frame's VISUAL CENTRE (bx bottom-anchors at y, so centre = y + frameH/2),
    -- else the image sits half a frame too low
    local half, off = 0.33, 0.066                                            -- photo just in front of the frame
    local cy = y + frameH * 0.5
    local cx = f.ax + f.nx * off
    local cz = f.az + f.nz * off
    -- Renderer UV order (fixed per corner index): v0=(0,vMax) v1=(uMax,vMax) v2=(uMax,0) v3=(0,0), with
    -- V=0 = image TOP. Vertical: the bottom world corners (cy-half) take vMax (image bottom), so the photo
    -- is upright. Horizontal: FAITHFUL mapping — image-left (u=0, v0/v3) sits at -tangent = the viewer's
    -- LEFT (wallFrame guarantees +tangent = viewer's right on every wall).
    -- History: this quad was once U-mirrored to "fix" a flipped portrait — the flip was actually the
    -- right wall's inverted tangent (see wallFrame) mirroring EVERY wall item there; the compensation
    -- then broke the back wall and the shop icon. Keep this mapping faithful and fix walls in wallFrame.
    world.scene:ObjAddQuadTex(world._boxTarget,
        cx - f.tx * half, cy - half, cz - f.tz * half,   -- v0 image bottom-left  → viewer bottom-left
        cx + f.tx * half, cy - half, cz + f.tz * half,   -- v1 image bottom-right → viewer bottom-right
        cx + f.tx * half, cy + half, cz + f.tz * half,   -- v2 image top-right    → viewer top-right
        cx - f.tx * half, cy + half, cz - f.tz * half,   -- v3 image top-left     → viewer top-left
        picTex, 1, 1, 1.0)
end
Room.pictureVisual = pictureVisual   -- editmode's preview-icon builder renders the same framed picture

local WALL_BUILDERS = {}   -- per-id override hooks (none by default)


-- ── catalog: data/furniture.json is AUTHORITATIVE ─────────────────────────────────────────────────
-- Every item's data (footprint, collision, surfaces, model/screen/accent/picture refs, wall-GLB
-- metadata, shop flag) lives in data/furniture.json; builders and texture-id resolution stay here.
-- Adding furniture = a furniture.json entry (+ its id in _order) + a GLB in Models/ or a builder above.
-- The table below is an EMERGENCY fallback (base-room items only) for a missing/broken JSON — edit
-- data/furniture.json, not this.
local DEFAULTS = {
    desk     = { w = 2, h = 1, place = "ground", collH = 1.15, surface = true, surfaceH = 0.82 },
    chair    = { w = 1, h = 1, place = "ground", collH = 1.0 },
    computer = { w = 1, h = 1, place = "ground", collH = 0.9, stackOn = true, computer = true,
                 model = "computer", collW = 0.7, collD = 0.55 },
    phone    = { w = 1, h = 1, place = "wall", fixed = true },
    -- clock stays known in fallback mode: saves carry its legacy-mirror copy in the furniture array,
    -- which loadTable must classify as wall-place (else it renders as a phantom ground box)
    clock    = { w = 1, h = 1, place = "wall" },
}
Room.CATALOG = {}
Room.WALL_GLB = {}              -- wall items with a real GLB (metadata parsed from furniture.json)
Room.SHOP_MODEL_IDS = {}        -- shop-sold ids that get a 3D thumbnail baked at onStart
Room.SHOP_SWATCH_TEX = {}       -- shop-sold paint/flooring ids → flat preview PNG (from deco JSONs)
-- infinite "revert to default" swatches shown first in the Floor/Paint tabs (safer than a global eraser)
Room.DEFAULT_FLOOR, Room.DEFAULT_PAINT = "default_floor", "default_paint"
Room.FLOORDECO, Room.WALLPAINT = {}, {}
-- wall ids a v1 client can render (mirrored into the furniture array for old readers/peers)
local LEGACY_WALL_IDS = {}
-- the brand-new room layout + starter inventory (data/starter_room.json; nil = use the hardcoded base)
local STARTER = nil
-- starter stock ALSO seeded into existing saves that are missing the id (new-content migration in
-- loadTable; from starter_room.json `starterStock` — counts the player already changed are untouched)
local NEW_STARTER_STOCK = {}

-- data/*.json access: JsonParseFileAny gives plain dictionaries (arrays 1-indexed), JsonGet is a
-- nil-safe lookup, LANG:FromDict turns a {en,ja,fr} node into a CLocalizationData
local function jget(d, k) return JSONLOADER:JsonGet(d, k) end
local function jnum(v) return tonumber(v) end
local function jflag(node, k) if jget(node, k) == true then return true end return nil end
local function jloc(node)
    if node == nil then return nil end
    local ok, loc = pcall(function()
        local l = LANG:FromDict(node)
        -- CLocalizationData falls back to the "default" key (not "en"): mirror en into default so
        -- languages beyond the file's en/ja/fr read English instead of the raw id
        local en = JSONLOADER:JsonGet(node, "en")
        if en and en ~= "" then l:SetString("default", en) end
        return l
    end)
    if ok then return loc end
    return nil
end

local function buildCatalogFromJson()
    local doc = JSONLOADER:JsonParseFileAny("data/furniture.json")
    local order = doc and jget(doc, "_order")
    local n = order and JSONLOADER:JsonCount(order) or 0
    if n == 0 then return false end
    for i = 1, n do
        local id = jget(order, i)
        local node = id and jget(doc, id)
        if node then
            local e = {
                w = floor(jnum(jget(node, "w")) or 1), h = floor(jnum(jget(node, "h")) or 1),
                place = jget(node, "place") or "ground",
                collH = jnum(jget(node, "collH")), collW = jnum(jget(node, "collW")), collD = jnum(jget(node, "collD")),
                surface = jflag(node, "surface"), surfaceH = jnum(jget(node, "surfaceH")),
                stackOn = jflag(node, "stackOn"), computer = jflag(node, "computer"), fixed = jflag(node, "fixed"),
                interact = jget(node, "interact"), model = jget(node, "model"),
                emissivePart = jget(node, "emissivePart"),   -- GLB material routed to its own part
                                                             -- object (runtime glow, e.g. jukebox screen)
                nameLoc = jloc(jget(node, "name")),
                build = GROUND_BUILDERS[id], wallBuild = WALL_BUILDERS[id],
            }
            local scr = jget(node, "screen")
            if scr then
                local t = jget(scr, "tex")
                e.screen = { tex = (t and A.SCREEN[t]) or A.SCREEN.taiko, emit = jflag(scr, "emit") }
            end
            local acc = jget(node, "accent")
            if acc then e.accent = { jnum(jget(acc, 1)) or 255, jnum(jget(acc, 2)) or 255, jnum(jget(acc, 3)) or 255 } end
            local pic = jget(node, "picture")
            if pic and A.PICTURE[pic] then e.picture = A.PICTURE[pic] end
            local wg = jget(node, "wallGlb")
            if wg and jget(wg, "file") then
                Room.WALL_GLB[id] = { file = jget(wg, "file"), h = jnum(jget(wg, "h")) or 0.5,
                                      axis = jget(wg, "axis") or "x", flip = jflag(wg, "flip") }
            end
            if jflag(node, "legacyMirror") then LEGACY_WALL_IDS[id] = true end
            if jflag(node, "shop") then Room.SHOP_MODEL_IDS[#Room.SHOP_MODEL_IDS + 1] = id end
            Room.CATALOG[id] = e
        end
    end
    return next(Room.CATALOG) ~= nil
end

-- floorings/paints: id registry + localized names (+ shop swatch). NOTE: only `name` and `swatch` are
-- live data — texture binding stays in assets.lua (A.FLOORPAINT/A.WALLPAINT) and the infinite
-- "revert to default" entries are the DEFAULT_FLOOR/DEFAULT_PAINT constants above.
local function buildDecoFromJson(file, target)
    local doc = JSONLOADER:JsonParseFileAny(file)
    local order = doc and jget(doc, "_order")
    local n = order and JSONLOADER:JsonCount(order) or 0
    for i = 1, n do
        local id = jget(order, i)
        local node = id and jget(doc, id)
        if node then
            target[id] = { tex = jget(node, "tex"), nameLoc = jloc(jget(node, "name")) }
            local sw = jget(node, "swatch")
            if sw then Room.SHOP_SWATCH_TEX[id] = sw end
        end
    end
    return next(target) ~= nil
end

local function loadStarterRoom()
    local doc = JSONLOADER:JsonParseFileAny("data/starter_room.json")
    if doc == nil then return end
    local s = { furniture = {}, wallItems = {}, inventory = {}, starterStock = {} }
    local function readList(key, fields)
        local list = jget(doc, key)
        for i = 1, JSONLOADER:JsonCount(list) do
            local node = jget(list, i)
            local it = {}
            for _, fk in ipairs(fields) do it[fk] = jget(node, fk) end
            s[key][#s[key] + 1] = it
        end
    end
    readList("furniture", { "id", "c", "r", "facing", "on" })
    readList("wallItems", { "id", "c", "r", "mount" })
    readList("inventory", { "id", "n" })
    readList("starterStock", { "id", "n" })
    for _, e in ipairs(s.starterStock) do
        if e.id then NEW_STARTER_STOCK[e.id] = floor(jnum(e.n) or 0) end
    end
    if #s.furniture > 0 or #s.wallItems > 0 then STARTER = s end
end

local okCat, gotCat = pcall(buildCatalogFromJson)
if not (okCat and gotCat) then
    -- emergency fallback: base-room items only, so a broken furniture.json still boots the stage
    for id, d in pairs(DEFAULTS) do
        Room.CATALOG[id] = { w = d.w, h = d.h, place = d.place, computer = d.computer,
                             collH = d.collH, collW = d.collW, collD = d.collD, fixed = d.fixed,
                             surface = d.surface, surfaceH = d.surfaceH, stackOn = d.stackOn, model = d.model,
                             build = GROUND_BUILDERS[id], wallBuild = WALL_BUILDERS[id] }
    end
    Room.WALL_GLB = { clock = { file = "wallclock", h = 0.55, axis = "x" },
                      phone = { file = "wallphone", h = 0.62, axis = "z" } }
    LEGACY_WALL_IDS = { clock = true }
end
if not pcall(buildDecoFromJson, "data/floordeco.json", Room.FLOORDECO) or next(Room.FLOORDECO) == nil then
    Room.FLOORDECO = { wood = { tex = "wood", isDefault = true }, carpet = { tex = "carpet" },
                       stone = { tex = "stone" }, grass = { tex = "grass" }, sand = { tex = "sand" },
                       snow = { tex = "snow" }, default_floor = { tex = "wood" } }
end
if not pcall(buildDecoFromJson, "data/wallpaint.json", Room.WALLPAINT) or next(Room.WALLPAINT) == nil then
    Room.WALLPAINT = { plaster = { isDefault = true }, blue = {}, beige = {}, sage = {},
                       brick = {}, rock = {}, stonewall = {}, default_paint = { tex = "wall" } }
end
pcall(loadStarterRoom)

-- ── coin-shop integration ─────────────────────────────────────────────────────────────────────────
-- Furniture is bought in the coin shop (Items.db3 `furn_<id>` rows). Each purchase increments a per-id
-- global counter (".myroom_<id>"); My Room claims the delta into its inventory on load (drainShopGrants,
-- idempotent via self.claimed). SHOP_MODEL_IDS (furniture.json `shop` flags) get a 3D thumbnail baked
-- for the shop (furniture GLBs AND the framed portraits, which render through pictureVisual so the shop
-- icon shares the room's exact, U-corrected quad); SHOP_SWATCH_TEX (deco JSON `swatch` fields) reuse a
-- flat PNG as the shop preview (paints/floorings only).
Room.SHOP_IDS = {}
for _, id in ipairs(Room.SHOP_MODEL_IDS) do Room.SHOP_IDS[#Room.SHOP_IDS + 1] = id end
for id in pairs(Room.SHOP_SWATCH_TEX) do Room.SHOP_IDS[#Room.SHOP_IDS + 1] = id end
function Room.shopCounter(id) return ".myroom_" .. id end

-- localized display name for a furniture / floor / wall-paint id (falls back to the id)
function Room.displayName(id)
    local cat = Room.CATALOG[id] or Room.FLOORDECO[id] or Room.WALLPAINT[id]
    if cat and cat.nameLoc then return cat.nameLoc:GetString(id) end
    return id
end

-- ── construction ──────────────────────────────────────────────────────────────────────────────────
function Room.newDefault()
    local self = setmetatable({}, Room)
    self.tier = 1
    self.iw, self.ih = 5, 5
    self.exitCol = 2
    self.winC0, self.winC1 = 2, 3
    -- the starter layout comes from data/starter_room.json (STARTER); the literals below are the
    -- emergency fallback for a missing/broken file
    if STARTER then
        self.furniture = {}
        for _, f in ipairs(STARTER.furniture) do
            self.furniture[#self.furniture + 1] =
                { id = f.id, c = floor(jnum(f.c) or 0), r = floor(jnum(f.r) or 0),
                  facing = floor(jnum(f.facing) or 0), on = (f.on == true) or nil }
        end
        self.wallItems = {}
        for _, w in ipairs(STARTER.wallItems) do
            local c = floor(jnum(w.c) or 0)
            self.wallItems[#self.wallItems + 1] =
                { id = w.id, c = (c < 0) and self.iw or c, r = floor(jnum(w.r) or 1),   -- c -1 = right wall
                  mount = (w.mount == "high") and "high" or "low" }
        end
        self.inventory = {}
        for _, e in ipairs(STARTER.inventory) do
            if e.id then self.inventory[e.id] = floor(jnum(e.n) or 0) end
        end
    else
        self.furniture = {
            { id = "desk", c = 3, r = 1, facing = 0 },                     -- ground furniture only
            { id = "computer", c = 3, r = 1, facing = 0, on = true },      -- the PC, stacked on the desk
        }
        self.wallItems = { { id = "phone", c = self.iw, r = 3, mount = "low" } }
        self.inventory = { chair = 1 }
    end
    -- starterStock (starter_room.json) seeds brand-new rooms on BOTH paths (loadTable applies the
    -- same map to existing saves that miss an id)
    for id, n in pairs(NEW_STARTER_STOCK) do self.inventory[id] = n end
    self.floorDeco = {}                                       -- ["c,r"] = flooring id (carpet); nil = wood
    self.wallPaint = {}                                       -- ["back:c"]/["right:r"] = paint id; nil = plaster
    self.claimed = {}   -- per-id coin-shop grant ledger (see drainShopGrants)
    return self
end

-- reset an EXISTING room object back to a fresh default (keeps the same table so held references —
-- edit.room, the MO getRoom closure — stay valid when the player-select reloads another save's room)
function Room:reset()
    local d = Room.newDefault()
    for k in pairs(self) do self[k] = nil end
    for k, v in pairs(d) do self[k] = v end
end

function Room:gridW() return self.iw + 1 end
function Room:gridH() return self.ih + 2 end

function Room:cellType(c, r)
    local gw, gh = self:gridW(), self:gridH()
    if c < 0 or r < 0 or c >= gw or r >= gh then return "X" end
    if r == 0 then
        if c >= self.winC0 and c <= self.winC1 then return "F" else return "W" end
    end
    if c == self.iw then return "W" end
    if r == gh - 1 then return (c == self.exitCol) and "E" or "X" end
    return "O"
end

function Room:walkable(c, r)
    local t = self:cellType(c, r)
    if t ~= "O" and t ~= "E" then return false end
    if self:furnitureAt(c, r) then return false end
    return true
end

local function itemCovers(it, cat, c, r)
    local w, h = 1, 1
    if cat then w, h = cat.w, cat.h; if (it.facing or 0) % 2 == 1 then w, h = h, w end end
    return c >= it.c and c < it.c + w and r >= it.r and r < it.r + h
end

-- the TOP occupant of a cell: a stacked item (on = riding a surface) wins over its surface —
-- the eraser removes stacked-first and the PC interact finds the computer, not the desk
function Room:furnitureAt(c, r)
    local it, cat = self:stackedItemAt(c, r)
    if it then return it, cat end
    return self:groundItemAt(c, r)
end

-- the floor-standing occupant of a cell (surfaces live below stacked items)
function Room:groundItemAt(c, r)
    for _, it in ipairs(self.furniture) do
        if not it.on then
            local cat = Room.CATALOG[it.id]
            if (cat == nil or cat.place ~= "wall") and itemCovers(it, cat, c, r) then return it, cat end
        end
    end
    return nil
end

function Room:stackedItemAt(c, r)
    for _, it in ipairs(self.furniture) do
        if it.on then
            local cat = Room.CATALOG[it.id]
            if (cat == nil or cat.place ~= "wall") and itemCovers(it, cat, c, r) then return it, cat end
        end
    end
    return nil
end

-- the ONE surface item fully hosting a stack-candidate footprint (nil → floor placement)
function Room:surfaceUnder(id, c, r, facing)
    local cat = Room.CATALOG[id]
    if not (cat and cat.stackOn) then return nil end
    local surf = nil
    for _, cell in ipairs(self:footprint(id, c, r, facing)) do
        local g, gcat = self:groundItemAt(cell[1], cell[2])
        if g == nil or not (gcat and gcat.surface) then return nil end
        if surf ~= nil and surf ~= g then return nil end
        surf = g
    end
    return surf
end

-- ALL stacked items resting on a surface item (footprint overlap) — a 2×1 surface can carry
-- several 1×1 riders side by side
function Room:stackedItemsOn(surf)
    local out = {}
    local scat = Room.CATALOG[surf.id]
    if not (scat and scat.surface) then return out end
    local w, h = scat.w, scat.h
    if (surf.facing or 0) % 2 == 1 then w, h = h, w end
    for _, it in ipairs(self.furniture) do
        if it.on and it ~= surf then
            local cat = Room.CATALOG[it.id]
            local iw2, ih2 = 1, 1
            if cat then iw2, ih2 = cat.w, cat.h; if (it.facing or 0) % 2 == 1 then iw2, ih2 = ih2, iw2 end end
            if it.c < surf.c + w and it.c + iw2 > surf.c and it.r < surf.r + h and it.r + ih2 > surf.r then
                out[#out + 1] = it
            end
        end
    end
    return out
end
-- the FIRST stacked item on a surface (nil if none) — kept for callers that want just one
function Room:stackedItemOn(surf)
    return self:stackedItemsOn(surf)[1]
end

-- world Y of a surface item's top: the real built top (GLB _fitTop or the catalog surfaceH),
-- recorded by buildProps; the catalog fallback covers queries before the first build
function Room:surfaceTopY(surf)
    if surf._topY then return surf._topY end
    local cat = Room.CATALOG[surf.id]
    return FY + ((cat and cat.surfaceH) or 0.8)
end

-- ── wall items ────────────────────────────────────────────────────────────────────────────────────
function Room:wallTileValid(c, r)
    if r == 0 and c >= 0 and c < self.iw then
        return not (c >= self.winC0 and c <= self.winC1)      -- window columns hold nothing
    end
    return c == self.iw and r >= 1 and r <= self.ih
end

function Room:wallItemAt(c, r, mount)
    for _, it in ipairs(self.wallItems) do
        if it.c == c and it.r == r and (mount == nil or (it.mount or "low") == mount) then return it end
    end
    return nil
end

function Room:canPlaceWall(id, c, r, mount, ignore)
    if not self:wallTileValid(c, r) then return false end
    local occ = nil
    for _, it in ipairs(self.wallItems) do
        if it ~= ignore and it.c == c and it.r == r and (it.mount or "low") == mount then occ = it end
    end
    return occ == nil
end

function Room:addWallItem(id, c, r, mount)
    self.wallItems[#self.wallItems + 1] = { id = id, c = c, r = r, mount = mount or "low" }
end
function Room:removeWallItem(it)
    if it.id == "phone" then return false end                 -- the phone can never be removed
    for i, w in ipairs(self.wallItems) do
        if w == it then table.remove(self.wallItems, i); return true end
    end
    return false
end
function Room:moveWallItem(it, c, r, mount)
    if not self:canPlaceWall(it.id, c, r, mount, it) then return false end
    it.c, it.r, it.mount = c, r, mount
    return true
end

function Room:phoneItem()
    for _, it in ipairs(self.wallItems) do if it.id == "phone" then return it end end
    return nil
end

-- the phone must always exist exactly once, on a valid wall tile
function Room:ensurePhone()
    local phone = nil
    for i = #self.wallItems, 1, -1 do
        local it = self.wallItems[i]
        if it.id == "phone" then
            if phone ~= nil then table.remove(self.wallItems, i) else phone = it end
        end
    end
    if phone == nil then
        phone = { id = "phone", c = self.iw, r = math.min(3, self.ih), mount = "low" }
        self.wallItems[#self.wallItems + 1] = phone
    end
    if not self:wallTileValid(phone.c, phone.r) then
        phone.c, phone.r = self.iw, math.min(3, self.ih)
    end
    phone.mount = (phone.mount == "high") and "high" or "low"
end

-- the interior cell the player must stand next to for the phone interact
function Room:phoneCell()
    local p = self:phoneItem()
    if p == nil then return self.iw - 1, math.min(3, self.ih) end
    if p.c == self.iw then return self.iw - 1, p.r end
    return p.c, 1
end

-- ── edit-mode placement API (ground) ──────────────────────────────────────────────────────────────
function Room:footprint(id, c, r, facing)
    local cat = Room.CATALOG[id]
    local w, h = 1, 1
    if cat then w, h = cat.w, cat.h; if (facing or 0) % 2 == 1 then w, h = h, w end end
    local cells = {}
    for dr = 0, h - 1 do for dc = 0, w - 1 do cells[#cells + 1] = { c + dc, r + dr } end end
    return cells, w, h
end
-- does the candidate footprint (id at c,r,facing) fully contain item `it`'s footprint?
function Room:footprintContains(id, c, r, facing, it)
    local cells = {}
    for _, cell in ipairs(self:footprint(id, c, r, facing)) do cells[cell[1] .. "," .. cell[2]] = true end
    local cat = Room.CATALOG[it.id]
    local w, h = 1, 1
    if cat then w, h = cat.w, cat.h; if (it.facing or 0) % 2 == 1 then w, h = h, w end end
    for dr = 0, h - 1 do
        for dc = 0, w - 1 do
            if not cells[(it.c + dc) .. "," .. (it.r + dr)] then return false end
        end
    end
    return true
end

-- where a rider ends up after its surface turns 90° CW (one doRotate step). The surface keeps its
-- (c,r) origin and swaps its oriented dims; a local cell (x,y) in the W×H footprint maps to
-- (H-1-y, x) in the new H×W footprint. Returns the rider's new top-left cell (its facing rotates +1
-- too, so its own dims swap). Derived to match GltfModel.Pose's CW rotation so the visual follows.
function Room:rotatedRiderCell(surf, rider)
    local scat = Room.CATALOG[surf.id]
    local _, H = scat.w, scat.h
    if (surf.facing or 0) % 2 == 1 then H = scat.w end          -- surface oriented HEIGHT at current facing
    local rcat = Room.CATALOG[rider.id]
    local rh = 1
    if rcat then rh = ((rider.facing or 0) % 2 == 1) and rcat.w or rcat.h end   -- rider oriented height
    local lc, lr = rider.c - surf.c, rider.r - surf.r
    return surf.c + (H - lr - rh), surf.r + lc
end

-- placement: free cells always work; a stackOn item may additionally land on cells that ONE
-- surface item fully hosts, provided that surface carries no other stacked item yet. A surface
-- may rotate/move under its OWN rider (ignore = itself) as long as the rider stays inside the
-- new footprint.
function Room:canPlace(id, c, r, facing, ignore)
    local cat = Room.CATALOG[id]; if cat == nil or cat.place == "wall" then return false end
    -- our own riders (when we're a surface moving under them) never count as obstacles
    local ownRiders = {}
    if cat.surface and ignore ~= nil then
        for _, st in ipairs(self:stackedItemsOn(ignore)) do ownRiders[st] = true end
    end
    local surf = nil
    for _, cell in ipairs(self:footprint(id, c, r, facing)) do
        if self:cellType(cell[1], cell[2]) ~= "O" then return false end
        local st = self:stackedItemAt(cell[1], cell[2])
        if st and st ~= ignore then
            -- a cell held by ANOTHER stacked item blocks us — unless we're that item's surface
            -- moving with it (it stays inside our new footprint). Multiple riders coexist as long
            -- as their cells don't overlap (this per-cell test is what enforces that).
            local riderOk = ownRiders[st] and self:footprintContains(id, c, r, facing, st)
            if not riderOk then return false end
        end
        local g, gcat = self:groundItemAt(cell[1], cell[2])
        if g and g ~= ignore then
            -- landing on a surface: a stackOn item may share the surface with other riders as
            -- long as its own cells are free (checked above), so no whole-surface exclusion here
            if not (cat.stackOn and gcat and gcat.surface) then return false end
            if surf ~= nil and surf ~= g then return false end
            surf = g
        end
    end
    return true
end

-- insert an existing entry, re-deriving its stacked flag from what lies underneath
function Room:attachFurniture(it)
    it.on = (self:surfaceUnder(it.id, it.c, it.r, it.facing or 0) ~= nil) and true or nil
    self.furniture[#self.furniture + 1] = it
end
function Room:addFurniture(id, c, r, facing)
    self:attachFurniture({ id = id, c = c, r = r, facing = facing or 0 })
end
-- plain list removal, NO stacked-rider cascade (the move flow detaches surface + rider as a unit)
function Room:detachFurniture(it)
    for i, f in ipairs(self.furniture) do
        if f == it then table.remove(self.furniture, i); return true end
    end
    return false
end
function Room:removeFurniture(it)
    if self:detachFurniture(it) then
        -- never orphan a floater: a DELETED surface takes ALL its stacked riders back to stock
        local cat = Room.CATALOG[it.id]
        if cat and cat.surface then
            for _, st in ipairs(self:stackedItemsOn(it)) do
                self:removeFurniture(st)
                self:invAdd(st.id)
            end
        end
    end
end
function Room:invAdd(id, n) self.inventory[id] = (self.inventory[id] or 0) + (n or 1) end
function Room:invTake(id) if (self.inventory[id] or 0) > 0 then self.inventory[id] = self.inventory[id] - 1; return true end return false end

-- Claim coin-shop purchases: each shop item bumps a per-id global counter; the delta since our last
-- claim is added to inventory (idempotent via self.claimed). getCounter(name)->number. Returns true if
-- anything was gained (so the caller can persist the ledger). A counter that went DOWN (a fresh save
-- slot whose counter is lower/zero) only resyncs the ledger — it never removes owned stock.
function Room:drainShopGrants(getCounter)
    self.claimed = self.claimed or {}
    local gained = false
    for _, id in ipairs(Room.SHOP_IDS) do
        local total = floor((getCounter(Room.shopCounter(id)) or 0) + 0.5)
        local had = self.claimed[id] or 0
        if total > had then
            self.inventory[id] = (self.inventory[id] or 0) + (total - had)
            self.claimed[id] = total
            gained = true
        elseif total < had then
            self.claimed[id] = total
        end
    end
    return gained
end
function Room:carpetAt(c, r) return self.floorDeco[c .. "," .. r] == "carpet" end
function Room:setCarpet(c, r, on) self.floorDeco[c .. "," .. r] = on and "carpet" or nil end
function Room:canMoveExitTo(c) return c >= 0 and c <= self.iw - 1 end   -- front fringe, not the under-wall corner

-- generic flooring (carpet etc.); nil = wood (the empty default, NOT a paint)
function Room:floorAt(c, r) return self.floorDeco[c .. "," .. r] end
function Room:setFloor(c, r, id) self.floorDeco[c .. "," .. r] = id end  -- id=nil reverts to wood

-- wall paint per tile; key "back:c" / "right:r"; nil = plaster (the empty default, NOT a paint)
function Room:wallPaintAt(key) return self.wallPaint[key] end
function Room:setWallPaint(key, id) self.wallPaint[key] = id end          -- id=nil reverts to plaster

-- per-TILE wall slots (paint picking + the grid overlay). Window columns ARE paintable now (the wall
-- geometry splits into upper/lower quads around the opening and each samples its own paint).
--   {wall="back"|"right", idx, key, wx,wy,wz (tile centre), it={c,r}}
function Room:wallSlots()
    local gh = self:gridH(); local out = {}
    for c = 0, self.iw - 1 do
        out[#out + 1] = { wall = "back", idx = c, key = "back:" .. c,
                          wx = c + 0.5, wy = FY + 1.45, wz = gh - 1, it = { c = c, r = 0 } }
    end
    for r = 1, self.ih do
        out[#out + 1] = { wall = "right", idx = r, key = "right:" .. r,
                          wx = self.iw, wy = FY + 1.45, wz = (gh - 1 - r) + 0.5, it = { c = self.iw, r = r } }
    end
    return out
end

-- per-(tile × mount) wall-ITEM slots (item picking/placement). wy sits at the item's mid height.
--   {wall, idx, c, r, mount, wx, wy, wz}
function Room:wallItemSlots()
    local out = {}
    for _, s in ipairs(self:wallSlots()) do
        for _, m in ipairs({ "low", "high" }) do
            out[#out + 1] = { wall = s.wall, idx = s.idx, c = s.it.c, r = s.it.r, mount = m,
                              wx = s.wx, wy = FY + Room.MOUNT_Y[m] + 0.3, wz = s.wz }
        end
    end
    return out
end

-- the ids available to place for a category ("furn"/"wall"/"floor"/"paint"), in stable order, that the
-- player currently has stock of.
function Room:categoryItems(cat)
    local out = {}
    if cat == "furn" or cat == "wall" then
        local want = (cat == "wall") and "wall" or "ground"
        for id in pairs(self.inventory) do
            local c = Room.CATALOG[id]
            if c and c.place == want and (self.inventory[id] or 0) > 0 then out[#out + 1] = id end
        end
    elseif cat == "floor" then
        for id in pairs(A.FLOORPAINT) do if (self.inventory[id] or 0) > 0 then out[#out + 1] = id end end
        table.sort(out); table.insert(out, 1, Room.DEFAULT_FLOOR)   -- infinite "revert to wood" first
        return out
    elseif cat == "paint" then
        for id in pairs(A.WALLPAINT) do if (self.inventory[id] or 0) > 0 then out[#out + 1] = id end end
        table.sort(out); table.insert(out, 1, Room.DEFAULT_PAINT)   -- infinite "revert to plaster" first
        return out
    end
    table.sort(out)
    return out
end

function Room:spawnCell() return self.exitCol, self.ih end

-- ── tier extension: the room sizes the (greedy) landlord sells, keeping furniture; wall items follow
-- their wall. Loaded from data/tiers.json — TIERS[n] = interior IW×IH + the coin price to reach tier n
-- (tier 1 = the free base) + flavorLoc, the landlord's localized per-tier sales pitch. The table below
-- is the emergency fallback for a missing/broken file. ───────────────────────────────────────────────
Room.TIERS = {
    { iw = 5,  ih = 5,  cost = 0 },
    { iw = 6,  ih = 6,  cost = 100 },
    { iw = 7,  ih = 7,  cost = 1000 },
    { iw = 8,  ih = 8,  cost = 5000 },
    { iw = 9,  ih = 8,  cost = 10000 },
    { iw = 10, ih = 8,  cost = 10000 },
    { iw = 10, ih = 9,  cost = 10000 },
    { iw = 10, ih = 10, cost = 10000 },
}
pcall(function()
    local doc = JSONLOADER:JsonParseFileAny("data/tiers.json")
    local n = doc and JSONLOADER:JsonCount(doc) or 0
    if n < 1 then return end
    local tiers = {}
    for i = 1, n do
        local node = jget(doc, i)
        tiers[i] = { iw = floor(jnum(jget(node, "iw")) or 5), ih = floor(jnum(jget(node, "ih")) or 5),
                     cost = floor(jnum(jget(node, "cost")) or 0), flavorLoc = jloc(jget(node, "flavor")) }
    end
    Room.TIERS = tiers
end)
function Room:canExtend() return self.tier < #Room.TIERS end
function Room:nextTierInfo() return Room.TIERS[self.tier + 1] end   -- the size/price the landlord offers next, or nil
function Room:extendTo(iw, ih)
    local oldIw = self.iw
    self.iw, self.ih = iw, ih
    self.tier = self.tier + 1
    for _, it in ipairs(self.wallItems) do
        if it.c == oldIw and it.r >= 1 then it.c = self.iw end   -- pushed out with the right wall
    end
    self.exitCol = math.min(self.exitCol, self.iw - 1)
end

-- ══ geometry + physics build (the proc-map builder; runs inside the physics static session) ═══════
local FENCE_H = FY + 1.4

function Room:buildMap(world)
    local scene = world.scene
    local phys = world.phys
    world:setGridSize(self:gridW(), self:gridH())
    local gw, gh = self:gridW(), self:gridH()
    scene:ObjBegin(world.floorObj)
    scene:ObjBegin(world.wallObj)
    scene:ObjBegin(world.prop3dObj)
    world._boxTarget = world.prop3dObj
    -- UNLIT walls: the two walls face different directions (back −z, right −x), so directional
    -- sun lighting shaded them DIFFERENT colours; unlit, both read the same baked texture/paint
    -- (the platform skirts on this object carry their own baked shade factors already)
    scene:ObjSetLit(world.wallObj, false)

    local function isPlat(c, r) local t = self:cellType(c, r); return t == "O" or t == "E" end

    -- platform tops (uniform wood; rugs are slabs on top) + physics
    for r = 0, gh - 1 do
        for c = 0, gw - 1 do
            if isPlat(c, r) then
                local x0, z0 = c, gh - 1 - r
                -- flooring = a FLAT RE-TEXTURE of the tile itself (no raised rug slab); default = wood
                local fid = self.floorDeco[c .. "," .. r]
                local ftex = (fid and A.FLOORPAINT[fid]) or A.TEX.wood
                scene:ObjAddQuadTex(world.floorObj, x0, FY, z0, x0, FY, z0 + 1, x0 + 1, FY, z0 + 1, x0 + 1, FY, z0, ftex, 1, 1, 1.0)
                phys:addQuad(x0, FY, z0, x0, FY, z0 + 1, x0 + 1, FY, z0 + 1, x0 + 1, FY, z0)
                -- side skirts where the platform edge is exposed (not backed by a wall cell)
                local edges = {
                    { dc = 1, dr = 0,  q = { x0 + 1, 0, z0, x0 + 1, 0, z0 + 1, x0 + 1, FY, z0 + 1, x0 + 1, FY, z0 }, sh = 0.82 },
                    { dc = -1, dr = 0, q = { x0, 0, z0 + 1, x0, 0, z0, x0, FY, z0, x0, FY, z0 + 1 }, sh = 0.74 },
                    { dc = 0, dr = -1, q = { x0, 0, z0 + 1, x0 + 1, 0, z0 + 1, x0 + 1, FY, z0 + 1, x0, FY, z0 + 1 }, sh = 0.66 },
                    { dc = 0, dr = 1,  q = { x0 + 1, 0, z0, x0, 0, z0, x0, FY, z0, x0 + 1, FY, z0 }, sh = 0.9 },
                }
                for _, e in ipairs(edges) do
                    local nt = self:cellType(c + e.dc, r + e.dr)
                    if nt ~= "O" and nt ~= "E" and nt ~= "W" and nt ~= "F" then
                        local q = e.q
                        scene:ObjAddQuadTex(world.wallObj, q[1], q[2], q[3], q[4], q[5], q[6], q[7], q[8], q[9], q[10], q[11], q[12], A.TEX.skirt, 1, FY, e.sh)
                        phys:addQuad(q[1], q[2], q[3], q[4], q[5], q[6], q[7], q[8], q[9], q[10], q[11], q[12])
                    end
                end
            end
        end
    end

    -- walls: per-tile paintable quads from the ground up (they cover the platform edge behind them)
    local wallTop = FY + Room.WALL_H
    local function wallQuad(x0, z0, x1, z1, y0, y1, tex)
        local len = math.sqrt((x1 - x0) ^ 2 + (z1 - z0) ^ 2)
        scene:ObjAddQuadTex(world.wallObj, x0, y0, z0, x1, y0, z1, x1, y1, z1, x0, y1, z0, tex, len, y1 - y0, 1.0)
    end
    local zb = gh - 1                                        -- back wall plane (interior back edge)
    for c = 0, self.iw - 1 do
        local tex = A.WALLPAINT[self.wallPaint["back:" .. c]] or A.TEX.wall
        if c >= self.winC0 and c <= self.winC1 then          -- window column: split around the opening
            wallQuad(c, zb, c + 1, zb, 0, FY + Room.WIN_Y0, tex)
            wallQuad(c, zb, c + 1, zb, FY + Room.WIN_Y1, wallTop, tex)
        else
            wallQuad(c, zb, c + 1, zb, 0, wallTop, tex)
        end
        phys:addBox(c, 0, zb, c + 1, wallTop, zb + 1)        -- solid even under the window sill
    end
    for r = 1, self.ih do                                    -- right wall, per interior row
        local tex = A.WALLPAINT[self.wallPaint["right:" .. r]] or A.TEX.wall
        local z0 = gh - 1 - r
        wallQuad(self.iw, z0, self.iw, z0 + 1, 0, wallTop, tex)
        phys:addBox(self.iw, 0, z0, self.iw + 1, wallTop, z0 + 1)
    end
    phys:addBox(self.iw, 0, zb, self.iw + 1, wallTop, zb + 1)    -- far corner cell

    -- invisible fences: the platform's open edges (left, interior front except the exit gap, and the
    -- exit cell's three free sides) so the physics character can't walk off the diorama
    local zf = gh - 1 - self.ih                              -- interior front edge (z = 1)
    phys:addBox(-0.06, 0, zf, 0.0, FENCE_H, gh - 1)                                   -- left edge
    if self.exitCol > 0 then phys:addBox(0, 0, zf - 0.06, self.exitCol, FENCE_H, zf) end
    if self.exitCol < self.iw - 1 then phys:addBox(self.exitCol + 1, 0, zf - 0.06, self.iw, FENCE_H, zf) end
    phys:addBox(self.exitCol - 0.06, 0, 0, self.exitCol, FENCE_H, zf)                 -- exit cell left
    phys:addBox(self.exitCol + 1, 0, 0, self.exitCol + 1.06, FENCE_H, zf)             -- exit cell right
    phys:addBox(self.exitCol, 0, -0.06, self.exitCol + 1, FENCE_H, 0)                 -- exit cell front

    self:buildProps(world)
    self:buildWindow(world)

    -- map handle (proc-map contract): heightAt + spawns
    local room = self
    local sc, sr = self:spawnCell()
    return {
        room = room,
        heightAt = function(_, wx, wz)
            local c, r = floor(wx), room:gridH() - 1 - floor(wz)
            local t = room:cellType(c, r)
            return (t == "O" or t == "E") and FY or 0
        end,
        spawns = { default = { x = sc + 0.5, z = (room:gridH() - 1 - sr) + 0.5, yaw = 45 } },
    }
end

-- ── props: ground furniture (GLB when available, shaded boxes otherwise) + wall items ─────────────
local GLB_STATE = {}          -- id → "ok" | "missing" (per-session file availability)

-- Shared GLB fitting: pick the scale that fills the footprint (and the resulting top Y).
-- ANCHORING is the engine's job now: instances are created with anchor="bounds", so setTransform
-- treats incoming coords as the ANCHOR POINT (bounds XZ centre on the ground) and re-applies the
-- yaw-rotated origin offset itself, with the same handedness pose() uses. The old Room.modelAnchor
-- duplicated that offset math locally — after the engine's setTransform re-anchoring change the two
-- conventions collided and rotated furniture (the sofa) drifted off its tile.
-- Returns scale, scaledHeight — or nil when bounds are unavailable/degenerate.
function Room.modelFit(model, cat)
    if model == nil or model.GetBounds == nil then return nil end
    local ok, s, hs = pcall(function()
        local mnx, mny, mnz, mxx, mxy, mxz = model:GetBounds()
        local sx, sy, sz = mxx - mnx, mxy - mny, mxz - mnz
        if sx < 0.01 or sz < 0.01 then return nil end
        local sc = math.min(0.92 * cat.w / sx, 0.92 * cat.h / sz)
        if sy * sc > 2.4 then sc = 2.4 / sy end
        if sc > 2.5 then sc = 2.5 end
        return sc, sy * sc
    end)
    if not ok or s == nil then return nil end
    return s, hs
end

-- pose a bounds-anchored instance at a footprint centre: placement, edit-mode ghosts AND rotation
-- all go through this one function (single convention — the anchor point, never origin coords).
-- baseY defaults to the floor; stacked items pass the surface top.
function Room.poseFitted(inst, cx, cz, yaw, cat, baseY)
    if inst == nil then return false end
    baseY = baseY or FY
    local s, hs = Room.modelFit(inst.model, cat)
    if s == nil then return false end
    inst:setTransform(cx, baseY, cz, yaw, s)
    inst._fitTop = baseY + hs
    return true
end

-- the GLB file stem for an item: catalog `model` override (several variants can share one GLB,
-- e.g. arcade cabs) else the id itself
local function modelStem(id)
    local cat = Room.CATALOG[id]
    return (cat and cat.model) or id
end
Room.modelStem = modelStem     -- editmode's icon path needs the GLB stem for variant items

local function tryModel(world, id, cx, cz, yaw, cat, baseY)
    local stem = modelStem(id)
    if GLB_STATE[stem] == "missing" then return nil end
    -- per-variant ACCENT: recolour ONLY the model's "Blue" material part to the accent colour (flat
    -- albedo override on a routed part) so arcade cabs differ (neon = red) without tinting the whole
    -- cab. accent = {r,g,b} in 0-255. (A whole-object tint multiplied blue toward black — the old bug.)
    local parts = nil
    if cat and cat.accent then
        local a = cat.accent
        parts = { { material = "Blue", color = { (a[1] or 255) / 255, (a[2] or 255) / 255, (a[3] or 255) / 255 } } }
    end
    -- catalog emissivePart: route that material to its own part object at a dim idle glow; runtime
    -- code (the jukebox screen) finds it via world._propInst[it].parts and pulses ObjSetEmissive
    if cat and cat.emissivePart then
        parts = parts or {}
        parts[#parts + 1] = { material = cat.emissivePart, emissive = { 1, 0.95, 0.8, 0.12 } }
    end
    local inst = nil
    local ok = pcall(function()
        inst = world.models:add{ file = "Models/" .. stem .. ".glb", x = cx, y = baseY or FY, z = cz,
                                 yaw = yaw, collide = "none", anchor = "bounds", parts = parts }
    end)
    if not ok or inst == nil then GLB_STATE[stem] = "missing"; return nil end
    GLB_STATE[stem] = "ok"
    Room.poseFitted(inst, cx, cz, yaw, cat, baseY)
    return inst
end

-- does Models/<stem>.glb load? Cached per session; lets the editor pick real-3D previews
-- (ModelIcon) over the drawn fallback icons. `file` overrides the stem (wall-item GLBs).
function Room.modelAvailable(id, file)
    local stem = file or id
    local st = GLB_STATE[stem]
    if st == "ok" then return true end
    if st == "missing" then return false end
    local m = nil
    local ok = pcall(function() m = MODEL:Load("Models/" .. stem .. ".glb") end)
    if ok and m ~= nil then GLB_STATE[stem] = "ok"; return true end
    GLB_STATE[stem] = "missing"
    return false
end

-- a GLB instance for the edit-mode ghost (parked off-world, invisible until posed by the editor);
-- nil when the id has no model file — the caller then falls back to the box-builder ghost
function Room.addGhostModel(world, id)
    local stem = modelStem(id)
    if GLB_STATE[stem] == "missing" then return nil end
    local inst = nil
    local ok = pcall(function()
        inst = world.models:add{ file = "Models/" .. stem .. ".glb", x = 0, y = -100, z = 0,
                                 collide = "none", anchor = "bounds" }
    end)
    if not ok or inst == nil then GLB_STATE[stem] = "missing"; return nil end
    GLB_STATE[stem] = "ok"
    return inst
end

-- one furniture entry's visuals + collision. baseY = the floor, or the hosting surface's top for
-- stacked entries (which add NO extra collision — the surface's own box already blocks the cell).
function Room:buildOneProp(world, phys, gh, it, baseY, stacked)
    local cat = Room.CATALOG[it.id]
    if cat == nil then
        -- unknown item (e.g. a host placed furniture this client doesn't have) → a red placeholder
        local cx, cz = world:footprintCenter(it.c, it.r, 1, 1)
        bx(world, cx, baseY, cz, 0.8, 0.8, 0.8, (it.facing or 0) * 90, A.COL.red, 1.0, 0.7)
        if not stacked then phys:addBox(it.c, FY, gh - it.r - 1, it.c + 1, FY + 0.8, gh - it.r) end
        return
    end
    if cat.place == "wall" then return end
    local w, h = cat.w, cat.h
    if (it.facing or 0) % 2 == 1 then w, h = h, w end
    local cx, cz = world:footprintCenter(it.c, it.r, w, h)
    local yaw = (it.facing or 0) * 90
    local inst = tryModel(world, it.id, cx, cz, yaw, cat, baseY)
    if inst == nil then
        if cat.build then cat.build(world, cx, cz, yaw, baseY)
        else   -- model file absent: a plain box the size of the footprint marks the spot
            bx(world, cx, baseY, cz, w * 0.8, cat.collH or 0.8, h * 0.8, yaw, A.COL.gray, 0.95, 0.7)
        end
    end
    if inst then world._propInst = world._propInst or {}; world._propInst[it] = inst end   -- for interact glow
    -- the built top feeds pass-2 stacking + edit ghosts (GLB → real top; placeholder → surfaceH)
    it._topY = (inst and inst._fitTop) or (baseY + (cat.surfaceH or (cat.collH or 0.85)))
    -- floor lamp on/off: a lit lamp glows (emissive shade + a point light added in rebuild); an off
    -- lamp is dark. lit defaults ON (nil ~= false), so old saves keep their lamps lit.
    if it.id == "floorlamp" and inst then
        local isLit = it.lit ~= false
        if world.scene.ObjSetEmissive then
            if isLit then world.scene:ObjSetEmissive(inst.obj, 0.9, 0.72, 0.36)
            else world.scene:ObjSetEmissive(inst.obj, 0, 0, 0) end
        end
        if not isLit and world.scene.ObjSetTint then world.scene:ObjSetTint(inst.obj, 0.6, 0.6, 0.66) end
    end
    -- (per-variant ACCENT recolour is applied inside tryModel via the routed "Blue" part.)
    -- per-variant SCREEN: the cab's screen is a SEPARATE UV-mapped plane in arcade.glb; map the art
    -- onto its EXACT local rectangle transformed by the instance's real world transform (perfect fit at
    -- any yaw/scale — world = inst.xyz + Rot(scale·local), matching GltfModel.Pose). Nudged along the
    -- screen normal (+x local) to sit in front of the GLB's dark screen. world._screenObj = unlit + emissive.
    if cat.screen and inst and world._screenObj then
        local yr = (inst.yaw or 0) * pi / 180
        local cc, ss, sc = cos(yr), sin(yr), inst.scale or 1
        local function w(lx, ly, lz)
            return inst.x + sc * (cc * lx + ss * lz), inst.y + sc * ly, inst.z + sc * (-ss * lx + cc * lz)
        end
        local dxn = 0.03                                    -- forward nudge along +x (the screen normal)
        local x0, y0, z0 = w(0.338 + dxn, 1.499,  0.385)   -- v0 bottom +z
        local x1, y1, z1 = w(0.338 + dxn, 1.499, -0.395)   -- v1 bottom -z
        local x2, y2, z2 = w(0.303 + dxn, 2.278, -0.395)   -- v2 top -z
        local x3, y3, z3 = w(0.303 + dxn, 2.278,  0.385)   -- v3 top +z
        world.scene:ObjAddQuadTex(world._screenObj, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3,
            cat.screen.tex, 1, 1, 1.0)
    end
    if stacked then return end
    -- collision: GLB instances collide as their scaled, yaw-rotated bounds FOOTPRINT (rotated-AABB,
    -- the same maths models.lua emitCollision uses) clamped to the cells + a 0.05 inflate — a thin
    -- floorlamp pole no longer blocks its whole tile. Box-fallback items honour per-catalog
    -- collW/collD overrides, else keep the full cell footprint. collH is unchanged.
    local hx, hz = nil, nil
    if inst ~= nil then
        pcall(function()
            local mnx, _, mnz, mxx, _, mxz = inst.model:GetBounds()
            local bw = (mxx - mnx) * inst.scale
            local bd = (mxz - mnz) * inst.scale
            local yr = yaw * pi / 180
            local ca, sa = math.abs(cos(yr)), math.abs(sin(yr))
            hx = math.min(w, bw * ca + bd * sa + 0.1) * 0.5
            hz = math.min(h, bw * sa + bd * ca + 0.1) * 0.5
        end)
    elseif cat.collW or cat.collD then
        local ow, od = cat.collW or cat.w, cat.collD or cat.h
        if (it.facing or 0) % 2 == 1 then ow, od = od, ow end
        hx, hz = math.min(w, ow) * 0.5, math.min(h, od) * 0.5
    end
    if hx then
        phys:addBox(cx - hx, FY, cz - hz, cx + hx, FY + (cat.collH or 0.85), cz + hz)
    else
        local z0 = gh - it.r - h
        phys:addBox(it.c, FY, z0, it.c + w, FY + (cat.collH or 0.85), z0 + h)
    end
end

-- point lights for every LIT floor lamp (Script.rebuild appends these to the room's light list,
-- so a lamp actually illuminates when on and goes dark when off)
function Room:lampLights(world)
    local out = {}
    for _, it in ipairs(self.furniture) do
        if it.id == "floorlamp" and it.lit ~= false and not it.on then
            local cat = Room.CATALOG[it.id]
            local w, h = cat.w, cat.h
            if (it.facing or 0) % 2 == 1 then w, h = h, w end
            local cx, cz = world:footprintCenter(it.c, it.r, w, h)
            out[#out + 1] = { x = cx, y = FY + 1.5, z = cz, r = 1.0, g = 0.82, b = 0.5, intensity = 0.6, range = 4.5 }
        end
    end
    return out
end

function Room:buildProps(world)
    local phys = world.phys
    local gh = self:gridH()
    world._boxTarget = world.prop3dObj
    self:invalidateWallGlb()          -- full rebuild: the old wall-GLB instances died with models:clear()
    world._propInst = {}              -- furniture entry → its GLB instance (play-mode interact glow)
    -- the arcade screen layer: one persistent, slightly-emissive object reused every rebuild (so it
    -- never accumulates); buildOneProp adds each cab's screen quad into it
    if world._screenObj == nil then
        world._screenObj = world.scene:NewObject()
        world.scene:ObjSetLit(world._screenObj, false)
        if world.scene.ObjSetEmissive then world.scene:ObjSetEmissive(world._screenObj, 0.45, 0.45, 0.45) end
    end
    world.scene:ObjBegin(world._screenObj)
    -- pass 1: floor-standing items (records every surface's real top Y)
    for _, it in ipairs(self.furniture) do
        if not it.on then self:buildOneProp(world, phys, gh, it, FY, false) end
    end
    -- pass 2: stacked items ride their hosting surface's top
    for _, it in ipairs(self.furniture) do
        if it.on then
            local surf = self:surfaceUnder(it.id, it.c, it.r, it.facing or 0)
            local baseY = surf and self:surfaceTopY(surf) or FY
            self:buildOneProp(world, phys, gh, it, baseY, true)
        end
    end
    for _, it in ipairs(self.wallItems) do
        self:buildWallItemVisual(world, it)
    end
end

-- ghost-able visuals (edit ghosts and previews when the GLB ghost is unavailable);
-- baseY defaults to the floor, stacked ghosts pass the hovered surface's top
function Room:buildFurnitureVisual(world, id, cx, cz, yaw, baseY)
    local cat = Room.CATALOG[id]
    if cat == nil then return end
    if cat.build then cat.build(world, cx, cz, yaw, baseY or FY); return end
    local w, h = cat.w or 1, cat.h or 1
    bx(world, cx, baseY or FY, cz, w * 0.8, cat.collH or 0.8, h * 0.8, yaw, A.COL.gray, 0.95, 0.7)
end

-- a ghost doorway tile (platform-slab shape) at front-fringe column c — the edit-mode door preview
function Room:buildDoorTileVisual(world, c)
    bx(world, c + 0.5, 0, 0.5, 0.98, FY, 0.98, 0, A.TEX.wood, 1.0, 0.8)
end

-- wall items with a real GLB: file stem, mounted world height, and the model's FACE axis —
--   axis "x" (default): the thin/face axis is X — +x yaws onto the wall's inward normal
--   axis "z":           the face is on ±Z (payphone/painting/poster) — +z yaws onto the normal,
--                       and the wall-depth offset reads the Z bounds extent instead
-- Yaw formulas use the TRUE engine handedness (GltfModel.Pose: x' = c·px + s·pz, z' = −s·px + c·pz):
--   +x → normal: yaw = deg(atan(−nz, nx))        +z → normal: yaw = deg(atan(nx, nz))
-- `flip = true` adds 180° — each model's facing is a coin flip until a visual check confirms it.
-- entries come from furniture.json `wallGlb` nodes (parsed into Room.WALL_GLB by the catalog loader;
-- editmode's ghost path shares them). e.g. clock = wallclock.glb face +x; phone = the Poly "Payphone"
-- (pole+base trimmed to a wall unit), open front on +z.
local WALL_GLB = Room.WALL_GLB

-- pose an existing wall-GLB instance onto a wall tile/mount (shared by placed items + ghosts)
function Room:poseWallGlb(inst, id, c, r, mount)
    local g = WALL_GLB[id]
    if inst == nil or g == nil then return false end
    local f = wallFrame(self, c, r)
    local y = FY + Room.MOUNT_Y[(mount == "high") and "high" or "low"]
    local sc, halfD = nil, nil
    local okB = pcall(function()
        local mnx, mny, mnz, mxx, mxy, mxz = inst.model:GetBounds()
        local sy = mxy - mny
        if sy >= 0.01 then
            sc = g.h / sy
            local depth = (g.axis == "z") and (mxz - mnz) or (mxx - mnx)
            halfD = math.max(depth, 0.02) * sc * 0.5
        end
    end)
    if not okB or sc == nil then return false end
    local yaw
    if g.axis == "z" then
        yaw = math.deg(math.atan(f.nx, f.nz))      -- +z onto the inward normal
    else
        yaw = math.deg(math.atan(-f.nz, f.nx))     -- +x onto the inward normal
    end
    if g.flip then yaw = yaw + 180 end
    inst:setTransform(f.ax + f.nx * (halfD + 0.02), y, f.az + f.nz * (halfD + 0.02), yaw, sc)
    return true
end

-- wall-GLB instance registry: ONE instance per placed wall item, REUSED (setTransform) across
-- redraws and moves — the old code added a fresh instance on every call, piling up duplicates
-- at every previous position. Invalidated on full rebuild (models:clear() killed them all).
function Room:wallGlbInstanceFor(world, it)
    local g = WALL_GLB[it.id]
    if g == nil or GLB_STATE[g.file] == "missing" then return nil end
    self._wallGlb = self._wallGlb or {}
    local inst = self._wallGlb[it]
    if inst == nil then
        local ok = pcall(function()
            inst = world.models:add{ file = "Models/" .. g.file .. ".glb", x = 0, y = -100, z = 0,
                                     collide = "none", anchor = "bounds" }
        end)
        if not ok or inst == nil then GLB_STATE[g.file] = "missing"; return nil end
        GLB_STATE[g.file] = "ok"
        -- UNLIT: the back wall's inward normal faces away from the sun — lit wall GLBs rendered
        -- BLACK there; unlit they read consistently on both (equally unlit) walls
        for _, o in ipairs(inst.objs or {}) do world.scene:ObjSetLit(o, false) end
        self._wallGlb[it] = inst
    end
    return inst
end

function Room:removeWallGlbInstance(world, it)
    local inst = self._wallGlb and self._wallGlb[it]
    if inst then
        pcall(function() world.models:remove(inst) end)
        self._wallGlb[it] = nil
    end
end

-- the full rebuild (models:clear inside loadMap) destroyed every instance: drop stale handles
function Room:invalidateWallGlb()
    self._wallGlb = {}
end

-- ghost=true skips the GLB path entirely (placeholder into the current _boxTarget only): ghost GLB
-- previews go through editmode's PARKED instance registry, never through per-call adds
function Room:buildWallItemVisual(world, it, ghost)
    local cat = Room.CATALOG[it.id]
    if not ghost then
        local inst = self:wallGlbInstanceFor(world, it)
        if inst and self:poseWallGlb(inst, it.id, it.c, it.r, it.mount) then
            inst:setVisible(true)
            return
        end
    end
    local f = wallFrame(self, it.c, it.r)
    local y = FY + Room.MOUNT_Y[(it.mount == "high") and "high" or "low"]
    -- picture wall items (souvenir posters/paintings): a gold frame box + a textured quad standing
    -- slightly off the wall. Story mode adds more variants by defining picture = <texture id>.
    if cat and cat.picture then
        pictureVisual(world, f, y, cat.picture)
        return
    end
    if cat and cat.wallBuild then
        cat.wallBuild(world, f, y)
    elseif cat then
        wb(world, f, y, 0, 0, 0.01, 0.6, 0.6, 0.08, A.COL.gray, 0.95, 0.8)   -- model file absent
    else
        wb(world, f, y, 0, 0, 0.01, 0.6, 0.6, 0.08, A.COL.red, 1.0, 0.8)     -- unknown wall item
    end
end

-- a framed window model set into the back-wall opening (frame border + cross mullions; the gap behind
-- it shows the day/night sky). Built as thin boxes on the back-wall plane.
function Room:buildWindow(world)
    local zbw = self:gridH() - 1
    local x0, x1 = self.winC0, self.winC1 + 1                 -- opening x-extent (world)
    local cw, ow = (x0 + x1) * 0.5, (x1 - x0)
    local y0, y1 = FY + Room.WIN_Y0, FY + Room.WIN_Y1
    local h = y1 - y0
    local fc, dz = A.COL.white, 0.16
    world._boxTarget = world.prop3dObj
    bx(world, cw, y1, zbw, ow + 0.16, 0.1, dz, 0, fc, 1.0, 0.85)                 -- top bar
    bx(world, cw, y0 - 0.1, zbw, ow + 0.16, 0.1, dz, 0, fc, 1.0, 0.85)           -- bottom bar
    bx(world, x0, y0 - 0.1, zbw, 0.12, h + 0.2, dz, 0, fc, 1.0, 0.8)             -- left bar
    bx(world, x1, y0 - 0.1, zbw, 0.12, h + 0.2, dz, 0, fc, 1.0, 0.8)             -- right bar
    bx(world, cw, y0, zbw, 0.05, h, 0.1, 0, fc, 0.95, 0.8)                       -- vertical mullion
    bx(world, cw, (y0 + y1) * 0.5, zbw, ow, 0.05, 0.1, 0, fc, 0.95, 0.8)         -- horizontal mullion
end

function Room:centerWorld(world)
    return world:footprintCenter(0, 1, self.iw, self.ih)
end

-- ── (de)serialization ─────────────────────────────────────────────────────────────────────────────
function Room:toTable()
    local furn = {}
    for _, it in ipairs(self.furniture) do furn[#furn + 1] = it end
    for _, it in ipairs(self.wallItems) do
        if LEGACY_WALL_IDS[it.id] then
            furn[#furn + 1] = { id = it.id, c = it.c, r = it.r, facing = 0 }   -- v1-readable mirror
        end
    end
    local wi = {}
    for _, it in ipairs(self.wallItems) do
        wi[#wi + 1] = { id = it.id, c = it.c, r = it.r, mount = it.mount or "low" }
    end
    return { v = 2, tier = self.tier, iw = self.iw, ih = self.ih, exitCol = self.exitCol,
             furniture = furn, floorDeco = self.floorDeco, wallPaint = self.wallPaint,
             inventory = self.inventory, wallItems = wi, claimed = self.claimed }
end

function Room:loadTable(t)
    if type(t) ~= "table" then return end
    -- all version upgrades + retired-id cleanups live in migration.lua (REMAP/DROPPED registries,
    -- the v1→v2 wall-item split, the computer split-out, starter-stock seeding)
    MIG.remapIds(t)
    self.tier = t.tier or self.tier
    self.iw, self.ih = t.iw or self.iw, t.ih or self.ih
    self.exitCol = t.exitCol or self.exitCol
    self.floorDeco = t.floorDeco or self.floorDeco
    self.wallPaint = t.wallPaint or self.wallPaint
    MIG.applyItems(self, t, Room.CATALOG, NEW_STARTER_STOCK)
    self.claimed = t.claimed or self.claimed or {}   -- coin-shop grant ledger (see drainShopGrants)
    self:ensurePhone()
end

return Room

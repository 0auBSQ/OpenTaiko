---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/actors.lua — billboard sprite layers (player / NPCs / props / talk bubbles), draped cast
-- shadows, floating ground markers, and animated water surfaces. Ported from isoengine's sprite +
-- shadow + marker + water builders onto the World's layer objects.

local sin, cos, pi = math.sin, math.cos, math.pi

local Actors = {}
Actors.__index = Actors

function Actors.new(world)
    local self = setmetatable({}, Actors)
    self.world = world
    return self
end

-- ── static prop sprites (rebuild on map change) ───────────────────────────────────────────────────
function Actors:propBegin() self.world.scene:ObjBegin(self.world.propObj) end
function Actors:propSprite(wx, wy, wz, w, h, spriteId)
    self.world.scene:ObjAddSprite(self.world.propObj, wx, wy or 0, wz, w, h, spriteId, 1, 1)
end

-- ── dynamic actors (player + remotes; rebuilt every frame) ────────────────────────────────────────
function Actors:actorsBegin() self.world.scene:ObjBegin(self.world.actorObj) end
function Actors:actorSprite(wx, wy, wz, w, h, spriteId)
    self.world.scene:ObjAddSprite(self.world.actorObj, wx, wy or 0, wz, w, h, spriteId, 1, 1)
end

function Actors:npcsBegin() self.world.scene:ObjBegin(self.world.npcObj) end
function Actors:npcSprite(wx, wy, wz, w, h, spriteId)
    self.world.scene:ObjAddSprite(self.world.npcObj, wx, wy or 0, wz, w, h, spriteId, 1, 1)
end
function Actors:npcTint(r, g, b) self.world.scene:ObjSetTint(self.world.npcObj, r, g, b) end

-- ── talk bubbles ──────────────────────────────────────────────────────────────────────────────────
function Actors:bubblesBegin() self.world.scene:ObjBegin(self.world.bubbleObj) end
function Actors:bubble(wx, wy, wz, w, h, spriteId)
    self.world.scene:ObjAddSprite(self.world.bubbleObj, wx, wy, wz, w, h, spriteId, 1, 1)
end

-- ── draped cast shadows (silhouette sprite projected along the light onto the ground) ─────────────
function Actors:shadowsBegin() self.world.scene:ObjBegin(self.world.shadowObj) end
function Actors:shadowSprite(wx, wz, w, h, spriteId, groundFn)
    local world = self.world
    local ldx, ldz = world.lightDx or 0.7071, world.lightDz or 0.7071
    local px, pz = -ldz, ldx
    local len = h * math.min(world.lightInvTan or 1.2, 1.3)
    local hw = w * 0.5
    local flx, flz = wx - px * hw, wz - pz * hw
    local frx, frz = wx + px * hw, wz + pz * hw
    local hlx, hlz = flx + ldx * len, flz + ldz * len
    local hrx, hrz = frx + ldx * len, frz + ldz * len
    local function gy(x, z) return (groundFn and groundFn(x, z) or 0) + 0.04 end
    world.scene:ObjAddSpriteQuad(world.shadowObj,
        flx, gy(flx, flz), flz,  frx, gy(frx, frz), frz,
        hrx, gy(hrx, hrz), hrz,  hlx, gy(hlx, hlz), hlz, spriteId, 0)
end

-- soft ellipse shadow (props without a directional silhouette)
function Actors:shadow(wx, wz, rad, height, groundFn)
    local world = self.world
    rad = rad or 0.34; height = height or 1.4
    local len = height * (world.lightInvTan or 1.6); if len > 4.5 then len = 4.5 end
    local ldx, ldz = world.lightDx or 0.7071, world.lightDz or 0.7071
    local pxd, pzd = -ldz, ldx
    local ccx, ccz = wx + ldx * len * 0.5, wz + ldz * len * 0.5
    local semiA, semiB = len * 0.5 + rad, rad
    local function gy(x, z) return (groundFn and groundFn(x, z) or 0) + 0.05 end
    local cy = gy(ccx, ccz)
    local n = 16
    local px, pz, py
    for i = 0, n do
        local a = i / n * 2 * pi
        local ox, oz = cos(a) * semiA, sin(a) * semiB
        local gx = ccx + ldx * ox + pxd * oz
        local gz = ccz + ldz * ox + pzd * oz
        local gyy = gy(gx, gz)
        if i > 0 then
            world.scene:ObjAddQuadFlat(world.shadowObj, ccx, cy, ccz, px, py, pz, gx, gyy, gz, gx, gyy, gz)
        end
        px, pz, py = gx, gz, gyy
    end
end

-- ── ground ring markers (missions / portals) ──────────────────────────────────────────────────────
function Actors:markersBegin() self.world.scene:ObjBegin(self.world.markerObj) end
function Actors:markerRing(wx, wy, wz, rIn, rOut)
    local world = self.world
    local y = wy + 0.05
    local n = 24
    local pix, piz, pox, poz
    for i = 0, n do
        local a = i / n * 2 * pi
        local ca, sa = cos(a), sin(a)
        local ix, iz = wx + ca * rIn, wz + sa * rIn
        local ox, oz = wx + ca * rOut, wz + sa * rOut
        if i > 0 then world.scene:ObjAddQuadFlat(world.markerObj, pix, y, piz, pox, y, poz, ox, y, oz, ix, y, iz) end
        pix, piz, pox, poz = ix, iz, ox, oz
    end
end

-- ── animated water surfaces (rebuilt per frame; water.lua adds reflections in the visuals phase) ──
function Actors:waterBegin() self.world.scene:ObjBegin(self.world.waterObj) end
function Actors:waterTile(x0, z0, x1, z1, baseY, t, amp, texId)
    local world = self.world
    local SUB = 4
    local function wy(x, z)
        return baseY + amp * (0.55 * sin(x * 0.7 + t) + 0.45 * cos(z * 0.6 + t * 0.85)
                            + 0.30 * sin((x + z) * 0.95 - t * 1.3) + 0.20 * cos((x - z) * 1.45 + t * 1.7))
    end
    local dx, dz = (x1 - x0) / SUB, (z1 - z0) / SUB
    for i = 0, SUB - 1 do
        local ax = x0 + dx * i; local bx = ax + dx
        for j = 0, SUB - 1 do
            local az = z0 + dz * j; local bz = az + dz
            world.scene:ObjAddQuadTex(world.waterObj, ax, wy(ax, az), az, bx, wy(bx, az), az,
                bx, wy(bx, bz), bz, ax, wy(ax, bz), bz, texId, 1, 1, 1.0)
        end
    end
end
function Actors:waterDisc(cx, cz, r, baseY, t, amp, texId, segs, rings)
    local world = self.world
    segs = segs or 18; rings = rings or 4
    local function wy(rad) return baseY + amp * sin(rad * 2.2 - t * 3.2) end
    for ri = 0, rings - 1 do
        local r0 = r * ri / rings; local r1 = r * (ri + 1) / rings
        local y0, y1 = wy(r0), wy(r1)
        for s = 0, segs - 1 do
            local a0 = s / segs * 6.28318; local a1 = (s + 1) / segs * 6.28318
            local c0, s0 = cos(a0), sin(a0); local c1, s1 = cos(a1), sin(a1)
            world.scene:ObjAddQuadTex(world.waterObj,
                cx + c0 * r0, y0, cz + s0 * r0, cx + c0 * r1, y1, cz + s0 * r1,
                cx + c1 * r1, y1, cz + s1 * r1, cx + c1 * r0, y0, cz + s1 * r0, texId, 1, 1, 1.0)
        end
    end
end

return Actors

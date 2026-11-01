---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/isomap.lua — iso grid maps: builds the visible geometry (floors / ramps / side faces / wall
-- cells / buildings / door panels — ported from isoengine's builders) AND emits the matching physics
-- triangles, so what you see is exactly what you collide with. Also provides heightAt() (bilinear,
-- identical maths to iso_demo2/maps.lua) for sprite shadows and ground snapping.
--
-- Cell format (from json.lua or procedural builders):
--   cells[r][c] = { h = 0.0, tex = "grass", ramp = {dir="x+",lo=,hi=}, wall = true|nil, liquid = "water"|nil }

local sin, cos, pi, sqrt = math.sin, math.cos, math.pi, math.sqrt

local IsoMap = {}
IsoMap.__index = IsoMap

local WALL_H = 2.2          -- visible + collision height of wall cells

-- ── height queries (parity with iso_demo2/maps.lua) ──────────────────────────────────────────────
local function cellAt(def, wx, wz)
    local c = math.floor(wx)
    local r = def.gridH - 1 - math.floor(wz)
    if c < 0 or c >= def.gridW or r < 0 or r >= def.gridH then return nil end
    return def.cells[r] and def.cells[r][c] or nil, c, r
end

function IsoMap.heightAt(def, wx, wz)
    local cell = cellAt(def, wx, wz)
    if cell == nil then return 0 end
    if cell.ramp then
        local fx, fz = wx - math.floor(wx), wz - math.floor(wz)
        local rp = cell.ramp
        local t
        if rp.dir == "x+" then t = fx
        elseif rp.dir == "x-" then t = 1 - fx
        elseif rp.dir == "z+" then t = fz
        else t = 1 - fz end
        return rp.lo + (rp.hi - rp.lo) * t
    end
    return cell.h or 0
end

function IsoMap.isWall(def, wx, wz)
    local cell = cellAt(def, wx, wz)
    return cell ~= nil and cell.wall == true
end

-- ── build: geometry + collision ───────────────────────────────────────────────────────────────────
-- corner heights of cell (c,r): at (x0,z0),(x0+1,z0),(x0+1,z0+1),(x0,z0+1) — matches terrainTopRamp
local function cornerHeights(cell)
    local h = cell.h or 0
    if not cell.ramp then return h, h, h, h end
    local rp = cell.ramp
    if rp.dir == "x+" then return rp.lo, rp.hi, rp.hi, rp.lo
    elseif rp.dir == "x-" then return rp.hi, rp.lo, rp.lo, rp.hi
    elseif rp.dir == "z+" then return rp.lo, rp.lo, rp.hi, rp.hi
    else return rp.hi, rp.hi, rp.lo, rp.lo end
end

-- build the whole iso map into the world's layers + physics. texId(name) resolves texture names.
-- Returns a map handle with heightAt / doors / dispose.
function IsoMap.build(world, def, texId)
    local scene = world.scene
    local phys = world.phys
    world:setGridSize(def.gridW, def.gridH)

    scene:ObjBegin(world.floorObj)
    scene:ObjBegin(world.wallObj)
    scene:ObjBegin(world.roofObj)
    scene:ObjBegin(world.prop3dObj)
    world._boxTarget = world.prop3dObj

    local function addTop(c, r, cell)
        local x0, z0 = c, (def.gridH - 1 - r)
        local a, b, cc, d = cornerHeights(cell)
        local tex = texId(cell.tex)
        -- visible top (wound so the normal faces up — isoengine convention)
        scene:ObjAddQuadTex(world.floorObj, x0, a, z0, x0, d, z0 + 1, x0 + 1, cc, z0 + 1, x0 + 1, b, z0, tex, 1, 1, 1.0)
        -- collision top (same corners)
        phys:addQuad(x0, a, z0, x0, d, z0 + 1, x0 + 1, cc, z0 + 1, x0 + 1, b, z0)
    end

    -- vertical side face between a cell edge and a lower neighbour (visible + collision)
    local function addSide(c, r, edge, y0, ya, yb, texName)
        if ya - y0 <= 0.001 and yb - y0 <= 0.001 then return end
        local x0, z0 = c, (def.gridH - 1 - r)
        local v = math.max(ya, yb) - y0
        local tex = texId(texName)
        local q
        if edge == "e" then     q = { x0 + 1, y0, z0, x0 + 1, y0, z0 + 1, x0 + 1, yb, z0 + 1, x0 + 1, ya, z0, 0.82 }
        elseif edge == "w" then q = { x0, y0, z0 + 1, x0, y0, z0, x0, ya, z0, x0, yb, z0 + 1, 0.74 }
        elseif edge == "s" then q = { x0, y0, z0 + 1, x0 + 1, y0, z0 + 1, x0 + 1, yb, z0 + 1, x0, ya, z0 + 1, 0.66 }
        else                    q = { x0 + 1, y0, z0, x0, y0, z0, x0, ya, z0, x0 + 1, yb, z0, 0.9 } end
        scene:ObjAddQuadTex(world.wallObj, q[1], q[2], q[3], q[4], q[5], q[6], q[7], q[8], q[9], q[10], q[11], q[12], tex, 1, v, q[13])
        phys:addQuad(q[1], q[2], q[3], q[4], q[5], q[6], q[7], q[8], q[9], q[10], q[11], q[12])
    end

    -- neighbour corner heights on an edge of (c,r): returns the TWO top heights of that edge
    local function edgeTops(cell, edge)
        local a, b, cc, d = cornerHeights(cell)   -- (x0,z0),(x1,z0),(x1,z1),(x0,z1)
        if edge == "e" then return b, cc          -- x = x0+1: (x1,z0), (x1,z1)
        elseif edge == "w" then return a, d       -- x = x0:   (x0,z0), (x0,z1)
        elseif edge == "s" then return d, cc      -- z = z0+1: (x0,z1), (x1,z1)
        else return a, b end                      -- z = z0:   (x0,z0), (x1,z0)
        end

    local function neighbour(c, r, edge)
        -- e/w step along x; s = larger z = SMALLER r (row 0 far); n = larger r
        if edge == "e" then return c + 1, r
        elseif edge == "w" then return c - 1, r
        elseif edge == "s" then return c, r - 1
        else return c, r + 1 end
    end

    local function cellTopMax(cell)
        if cell.wall then return (cell.h or 0) + WALL_H end
        local a, b, cc, d = cornerHeights(cell)
        return math.max(a, b, cc, d)
    end
    local function cellTopMin(cell)
        if cell.wall then return (cell.h or 0) + WALL_H end
        local a, b, cc, d = cornerHeights(cell)
        return math.min(a, b, cc, d)
    end

    for r = 0, def.gridH - 1 do
        for c = 0, def.gridW - 1 do
            local cell = def.cells[r] and def.cells[r][c]
            if cell then
                if cell.wall then
                    -- a wall cell: a full box (top + 4 sides down to the lowest neighbour floor)
                    local base = cell.h or 0
                    local top = base + WALL_H
                    local x0, z0 = c, (def.gridH - 1 - r)
                    local tex = texId(cell.tex)
                    scene:ObjAddQuadTex(world.floorObj, x0, top, z0, x0, top, z0 + 1, x0 + 1, top, z0 + 1, x0 + 1, top, z0, tex, 1, 1, 1.0)
                    for _, e in ipairs({ "e", "w", "s", "n" }) do
                        local nc, nr = neighbour(c, r, e)
                        local ncell = (nc >= 0 and nc < def.gridW and nr >= 0 and nr < def.gridH)
                            and def.cells[nr] and def.cells[nr][nc] or nil
                        local ny = ncell and (ncell.wall and ((ncell.h or 0) + WALL_H) or cellTopMin(ncell)) or 0
                        if ny < top - 0.001 then addSide(c, r, e, ny, top, top, cell.tex) end
                    end
                    phys:addBox(x0, base, z0, x0 + 1, top, z0 + 1)
                else
                    addTop(c, r, cell)
                    -- side faces down to lower neighbours (visible + collision, no gaps under ramps)
                    for _, e in ipairs({ "e", "w", "s", "n" }) do
                        local ya, yb = edgeTops(cell, e)
                        local nc, nr = neighbour(c, r, e)
                        local ncell = (nc >= 0 and nc < def.gridW and nr >= 0 and nr < def.gridH)
                            and def.cells[nr] and def.cells[nr][nc] or nil
                        local ny
                        if ncell == nil then ny = math.min(0, cellTopMin(cell))       -- map border: drop to 0
                        elseif ncell.wall then ny = nil                                -- wall neighbour covers it
                        else
                            -- neighbour's matching edge tops
                            local mirror = { e = "w", w = "e", s = "n", n = "s" }
                            local na, nb = edgeTops(ncell, mirror[e])
                            ny = math.min(na, nb)
                        end
                        if ny ~= nil and (ya > ny + 0.001 or yb > ny + 0.001) then
                            addSide(c, r, e, ny, ya, yb, cell.tex)
                        end
                    end
                end
            end
        end
        if world.buildYield and r % 6 == 5 then world.buildYield(r / def.gridH) end
    end

    -- invisible border walls: the map edge is a hard boundary (no falling off the world)
    do
        local W, H = def.gridW, def.gridH
        phys:addQuad(0, -2, 0, W, -2, 0, W, 9, 0, 0, 9, 0)
        phys:addQuad(0, -2, H, W, -2, H, W, 9, H, 0, 9, H)
        phys:addQuad(0, -2, 0, 0, -2, H, 0, 9, H, 0, 9, 0)
        phys:addQuad(W, -2, 0, W, -2, H, W, 9, H, W, 9, 0)
    end

    -- ── buildings (ported from isoengine; walls also become collision boxes) ─────────────
    local doors = {}
    local tex = setmetatable({}, { __index = function(_, k) return texId(k) end })
    for bi, b in ipairs(def.buildings or {}) do
        local groundY = IsoMap.heightAt(def, b.c + 0.5, def.gridH - b.r - 0.5)
        local hinge = IsoMap.buildBuilding(world, def, b, groundY, tex, phys)
        if hinge and b.door then
            local group = "door_" .. bi
            hinge.group = group
            hinge.open = 0
            hinge.y0 = groundY
            hinge.tex = texId("doorwood")
            doors[#doors + 1] = hinge
            -- the closed door panel's collision (own group). Half-thickness must EXCEED the
            -- character radius (0.32): the old ±0.05 slab let the solver's push-out step across it
            -- in one substep — walking into a closed door could squeeze through the frame (the
            -- "softlocked inside a house" report).
            phys:setGroup(group)
            local ex, ez = hinge.hx + hinge.dx * hinge.w, hinge.hz + hinge.dz * hinge.w
            local x0, x1 = math.min(hinge.hx, ex), math.max(hinge.hx, ex)
            local z0, z1 = math.min(hinge.hz, ez), math.max(hinge.hz, ez)
            phys:addBox(x0 - 0.16, groundY, z0 - 0.16, x1 + 0.16, groundY + hinge.h, z1 + 0.16)
            phys:setGroup("default")
        end
    end

    local map = setmetatable({
        def = def,
        doors = doors,
        heightAt = function(_, wx, wz) return IsoMap.heightAt(def, wx, wz) end,
        isWall = function(_, wx, wz) return IsoMap.isWall(def, wx, wz) end,
    }, { __index = {
        -- auto-open doors near the player + rebuild panels (rebuilt per frame like isoengine)
        updateDoors = function(m, dt, px, pz)
            if #m.doors == 0 then return end
            local scene2 = world.scene
            scene2:ObjBegin(world.doorObj)
            for _, d in ipairs(m.doors) do
                local mx, mz = d.hx + d.dx * d.w * 0.5, d.hz + d.dz * d.w * 0.5
                local want = 0
                if px then
                    local dx2, dz2 = px - mx, pz - mz
                    if dx2 * dx2 + dz2 * dz2 < 2.6 * 2.6 then want = 1 end
                end
                local k = 1 - math.exp(-6 * (dt or 0.016))
                d.open = d.open + (want - d.open) * k
                if d.open < 0.001 then d.open = 0 elseif d.open > 0.999 then d.open = 1 end
                -- door collision: solid when mostly closed
                local solid = d.open < 0.4
                if solid ~= d._solid then
                    d._solid = solid
                    world.phys:setGroupSolid(d.group, solid)
                end
                -- swing panel (~105° inward)
                local ang = d.open * 1.83
                local ca, sa = cos(ang), sin(ang)
                local pxd, pzd = d.dx * ca - d.dz * sa, d.dx * sa + d.dz * ca
                local ex, ez = d.hx + pxd * d.w, d.hz + pzd * d.w
                scene2:ObjAddQuadTex(world.doorObj, d.hx, d.y0, d.hz, ex, d.y0, ez,
                    ex, d.y0 + d.h, ez, d.hx, d.y0 + d.h, d.hz, d.tex, 1, 1, 0.95)
            end
        end,
    } })
    return map
end

-- one building: walls/windows/roof into the lit layers + wall collision boxes. Ported from
-- isoengine:building() with the addition of the physics quads. Returns the door hinge (or nil).
function IsoMap.buildBuilding(world, def, b, groundY, tex, phys)
    local scene = world.scene
    local x0, z1 = b.c, (def.gridH - 1 - b.r) + 1
    local x1, z0 = b.c + b.w, z1 - b.d
    local y0 = groundY or 0
    local wallH = b.wallH or 2.6
    local y1 = y0 + wallH
    local wallTex, roofTex = tex.plaster, (b.roof == "slate" and tex.roofSlate or tex.roofRed)
    local ds = b.door and b.door.side or nil
    local dOff = b.door and b.door.off or math.floor(b.w / 2)
    local DW = 1.0

    local function wallRun(ax, az, bx, bz, holeA, holeB)
        local len = sqrt((bx - ax) ^ 2 + (bz - az) ^ 2)
        local ux, uz = (bx - ax) / len, (bz - az) / len
        -- collision top: the wall blocks all the way past the roof line, so nothing can be climbed over
        local roofTop = y1 + (b.roofH or 1.3) + 0.4
        local function seg(s0, s1, yb, yt)
            if s1 - s0 < 0.01 then return end
            scene:ObjAddQuadTex(world.wallObj, ax + ux * s0, yb, az + uz * s0, ax + ux * s1, yb, az + uz * s1,
                ax + ux * s1, yt, az + uz * s1, ax + ux * s0, yt, az + uz * s0, wallTex, s1 - s0, yt - yb, 1.0)
            -- THICK axis-aligned collision box (a thin quad tunnels under a fast collide-and-slide);
            -- building walls run along an axis, so the inflated box hugs the visual wall
            local p0x, p0z = ax + ux * s0, az + uz * s0
            local p1x, p1z = ax + ux * s1, az + uz * s1
            local x0b, x1b = math.min(p0x, p1x) - 0.09, math.max(p0x, p1x) + 0.09
            local z0b, z1b = math.min(p0z, p1z) - 0.09, math.max(p0z, p1z) + 0.09
            phys:addBox(x0b, yb, z0b, x1b, math.max(yt, roofTop), z1b)
        end
        if holeA then
            seg(0, holeA, y0, y1); seg(holeB, len, y0, y1)
            seg(holeA, holeB, y0 + 2.0, y1)
        else
            seg(0, len, y0, y1)
        end
        local mx, mz = (ax + bx) / 2, (az + bz) / 2
        local yawD = math.deg(math.atan(ux, uz)) + 90
        world._boxTarget = world.prop3dObj
        IsoMap.box(world, mx, y0, mz, len + 0.08, 0.30, 0.16, yawD, tex.stoneTrim or wallTex, 0.8, 0.6)
        IsoMap.box(world, mx, y1 - 0.16, mz, len + 0.08, 0.14, 0.14, yawD, tex.timber or wallTex, 0.62, 0.45)
    end
    if ds == "s" then wallRun(x0, z0, x1, z0, dOff, dOff + DW) else wallRun(x0, z0, x1, z0) end
    if ds == "n" then wallRun(x1, z1, x0, z1, b.w - dOff - DW, b.w - dOff) else wallRun(x1, z1, x0, z1) end
    if ds == "w" then wallRun(x0, z1, x0, z0, b.d - dOff - DW, b.d - dOff) else wallRun(x0, z1, x0, z0) end
    if ds == "e" then wallRun(x1, z0, x1, z1, dOff, dOff + DW) else wallRun(x1, z0, x1, z1) end

    -- windows (south wall, skipping the door bay)
    local wy = y0 + 1.25
    for i = 0, b.w - 1 do
        if not (ds == "s" and i >= dOff and i < dOff + DW) and (i % 2 == (b.winPhase or 0) % 2) then
            IsoMap.box(world, x0 + i + 0.5, wy, z0 + 0.02, 0.56, 0.7, 0.07, 0, tex.window, 1.45, 1.3)
            IsoMap.box(world, x0 + i + 0.5, wy - 0.42, z0 + 0.04, 0.7, 0.08, 0.10, 0, tex.timber, 0.7, 0.5)
        end
    end

    -- gabled roof (visible only; roofs don't need collision — characters can't reach them)
    local ridgeY = y1 + (b.roofH or 1.3)
    local zm = (z0 + z1) / 2
    local ov = 0.35
    scene:ObjAddQuadTex(world.roofObj, x0 - ov, y1, z0 - ov, x1 + ov, y1, z0 - ov, x1 + ov, ridgeY, zm, x0 - ov, ridgeY, zm, roofTex, b.w + 1, 2, 1.0)
    scene:ObjAddQuadTex(world.roofObj, x1 + ov, y1, z1 + ov, x0 - ov, y1, z1 + ov, x0 - ov, ridgeY, zm, x1 + ov, ridgeY, zm, roofTex, b.w + 1, 2, 0.86)
    scene:ObjAddQuadTex(world.roofObj, x0, y1, z0, x0, y1, z1, x0, ridgeY, zm, x0, ridgeY, zm, wallTex, b.d, 1.4, 0.92)
    scene:ObjAddQuadTex(world.roofObj, x1, y1, z1, x1, y1, z0, x1, ridgeY, zm, x1, ridgeY, zm, wallTex, b.d, 1.4, 0.86)
    IsoMap.box(world, x0 + 0.8, ridgeY - 0.5, zm + 0.2, 0.4, 1.1, 0.4, 0, tex.stoneTrim or wallTex, 0.9, 0.7)

    -- dark backing inside the doorway (unless this building streams a real interior group)
    if ds == "s" and not b.interiorGroup then
        scene:ObjAddQuadTex(world.wallObj, x0 + dOff, y0, z0 + 0.35, x0 + dOff + DW, y0, z0 + 0.35,
            x0 + dOff + DW, y0 + 2.0, z0 + 0.35, x0 + dOff, y0 + 2.0, z0 + 0.35, wallTex, 1, 1, 0.02)
    end

    if ds == nil then return nil end
    if ds == "s" then return { hx = x0 + dOff, hz = z0, dx = 1, dz = 0, w = DW, h = 2.0 }
    elseif ds == "n" then return { hx = x1 - dOff, hz = z1, dx = -1, dz = 0, w = DW, h = 2.0 }
    elseif ds == "w" then return { hx = x0, hz = z1 - dOff, dx = 0, dz = -1, w = DW, h = 2.0 }
    else return { hx = x1, hz = z0 + dOff, dx = 0, dz = 1, w = DW, h = 2.0 } end
end

-- shaded box into world._boxTarget (ported from isoengine:box; used for trims/windows/furniture)
function IsoMap.box(world, cx, y0, cz, sx, sy, sz, yaw, texId, sTop, sSide)
    sTop = sTop or 1.0; sSide = sSide or 0.72
    local hx, hz = sx * 0.5, sz * 0.5
    local yr = (yaw or 0) * pi / 180; local cw, sw = cos(yr), sin(yr)
    local function rot(dx, dz) return cx + dx * cw - dz * sw, cz + dx * sw + dz * cw end
    local x0, z0 = rot(-hx, -hz); local x1, z1 = rot(hx, -hz)
    local x2, z2 = rot(hx, hz);   local x3, z3 = rot(-hx, hz)
    local yt = y0 + sy
    local o = world._boxTarget
    local scene = world.scene
    scene:ObjAddQuadTex(o, x0, yt, z0, x1, yt, z1, x2, yt, z2, x3, yt, z3, texId, 1, 1, sTop)
    scene:ObjAddQuadTex(o, x0, y0, z0, x1, y0, z1, x1, yt, z1, x0, yt, z0, texId, 1, 1, sSide)
    scene:ObjAddQuadTex(o, x1, y0, z1, x2, y0, z2, x2, yt, z2, x1, yt, z1, texId, 1, 1, sSide * 0.9)
    scene:ObjAddQuadTex(o, x2, y0, z2, x3, y0, z3, x3, yt, z3, x2, yt, z2, texId, 1, 1, sSide * 0.8)
    scene:ObjAddQuadTex(o, x3, y0, z3, x0, y0, z0, x0, yt, z0, x3, yt, z3, texId, 1, 1, sSide)
end

return IsoMap

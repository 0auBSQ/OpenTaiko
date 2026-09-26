---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- SpaceSky: the Online Lobby's night sky, shared with the space_voyage transition.
-- One module drives it at a time; the others draw its last frame.

local Sky = {}

local sin, cos, sqrt, pi, floor, random = math.sin, math.cos, math.sqrt, math.pi, math.floor, math.random

local KEY, STATE_KEY, DRIVER_KEY = "online_sky", "online_sky_state", "online_sky_driver"

local RW, RH = 1280, 720                     -- render size, scaled up to the screen
local SCREEN_W, SCREEN_H = 1920, 1080
local FOV = 60                               -- vertical, in degrees
local FAR_R = 100                            -- star sphere radius

-- sky shader colours (0-1)
local ZENITH, HORIZON = { 0.012, 0.018, 0.070 }, { 0.070, 0.050, 0.160 }
local NEBULA_A, NEBULA_B = { 0.30, 0.14, 0.50 }, { 0.06, 0.30, 0.40 }
local BAND_AXIS = { 0.50, 0.70, -0.51 }      -- normal of the milky way's plane

-- count: alive at once; life: seconds; size: world units
local STARS = { count = 2400, life = { 14, 30 }, size = { 0.9, 2.5 } }
local BAND = { count = 1100, life = { 16, 32 }, size = { 0.6, 1.6 }, width = 0.10 }
local TWINKLES = { count = 70, life = { 2.2, 4.2 }, size = { 3.0, 6.5 } }
local DUST = { count = 160, life = { 10, 20 }, near = { 10, 28 } }
local STAR_COLS = { { 236, 240, 255 }, { 236, 240, 255 }, { 190, 210, 255 }, { 255, 238, 205 }, { 255, 212, 170 } }
local LAYERS = { "stars", "band", "twinkles", "dust", "meteor" }

local SKY = [[
vec3 h33(vec3 p){
    p = vec3(dot(p, vec3(127.1, 311.7, 74.7)), dot(p, vec3(269.5, 183.3, 246.1)), dot(p, vec3(113.5, 271.9, 124.6)));
    return fract(sin(p) * 43758.5453);
}
float vnoise(vec3 p){
    vec3 i = floor(p); vec3 f = fract(p);
    vec3 u = f * f * (3.0 - 2.0 * f);
    float a = h33(i).x, b = h33(i + vec3(1, 0, 0)).x, c = h33(i + vec3(0, 1, 0)).x, d = h33(i + vec3(1, 1, 0)).x;
    float e = h33(i + vec3(0, 0, 1)).x, f1 = h33(i + vec3(1, 0, 1)).x, g = h33(i + vec3(0, 1, 1)).x, h = h33(i + vec3(1, 1, 1)).x;
    return mix(mix(mix(a, b, u.x), mix(c, d, u.x), u.y), mix(mix(e, f1, u.x), mix(g, h, u.x), u.y), u.z);
}
float fbm(vec3 p, int octaves){
    float v = 0.0; float amp = 0.55;
    for (int i = 0; i < 4; i++){
        if (i >= octaves) break;
        v += vnoise(p) * amp; p = p * 2.07 + vec3(17.3, 9.1, 4.7); amp *= 0.5;
    }
    return v;
}
float region(vec3 dir, vec3 c, float w){
    float a = acos(clamp(dot(dir, normalize(c)), -1.0, 1.0));
    return exp(-(a / w) * (a / w));
}
void main(){
    vec2 uv = gl_FragCoord.xy / uRes;
    vec3 F = uUser[0].xyz; float tanF = uUser[0].w;
    vec3 R = uUser[1].xyz; float aspect = uUser[1].w;
    vec3 U = uUser[2].xyz;
    float nx = uv.x * 2.0 - 1.0, ny = 1.0 - uv.y * 2.0;
    vec3 dir = normalize(F + R * (nx * tanF * aspect) + U * (ny * tanF));

    vec3 col = mix(uUser[4].xyz, uUser[3].xyz, smoothstep(-0.55, 0.85, dir.y));
    float n = fbm(dir * 2.3 + vec3(0.0, uTime * 0.004, uTime * 0.002), 3);   // few octaves to stay cheap
    float m = fbm(dir * 5.1 - vec3(uTime * 0.006, 0.0, 0.0), 2);
    float cloud = smoothstep(0.42, 0.80, n) * (0.55 + 0.6 * m);
    col += uUser[5].xyz * cloud * (region(dir, vec3(0.62, 0.30, -0.72), 0.95) + 0.6 * region(dir, vec3(-0.45, 0.55, 0.70), 0.7)) * 0.8;
    col += uUser[6].xyz * cloud * (region(dir, vec3(-0.80, -0.05, -0.55), 0.85) + 0.5 * region(dir, vec3(0.35, -0.10, 0.93), 0.6)) * 0.7;
    float q = dot(dir, normalize(uUser[7].xyz)) / max(uUser[7].w, 0.05);
    float band = exp(-q * q);
    col += vec3(0.15, 0.16, 0.27) * band * (0.55 + 0.6 * m);
    col *= 1.0 - 0.28 * band * smoothstep(0.52, 0.72, m) * smoothstep(0.35, 0.6, n);
    col *= 1.0 - 0.22 * (nx * nx * 0.6 + ny * ny);
    col += vec3((h33(vec3(gl_FragCoord.xy, 0.0)).x - 0.5) * (1.5 / 255.0));
    frag = vec4(col, 1.0);
}
]]

local scene = nil
local ps = {}
local acc = { stars = 0, band = 0, twinkles = 0, dust = 0 }
local t = 0
local camX, camY, camZ = 0, 0, 0
local meteor = { wait = 3, on = false }
local lift = { y = 0, pitch = 0 }           -- extra camera height and tilt for transitions
local me = nil                              -- our name while we drive the sky

local function rnd(a, b) return a + (b - a) * random() end
local function norm(x, y, z) local l = sqrt(x * x + y * y + z * z); return x / l, y / l, z / l end

-- random sky direction between two latitudes (degrees)
local function skyDir(latLo, latHi)
    local y = rnd(sin(latLo * pi / 180), sin(latHi * pi / 180))
    local a = rnd(0, 2 * pi)
    local r = sqrt(1 - y * y)
    return r * cos(a), y, r * sin(a)
end

-- two axes in the milky way's plane
local bx, by, bz = norm(BAND_AXIS[1], BAND_AXIS[2], BAND_AXIS[3])
local e1x, e1y, e1z = norm(by, -bx, 0)
local e2x, e2y, e2z = by * e1z - bz * e1y, bz * e1x - bx * e1z, bx * e1y - by * e1x

local function starCol() return STAR_COLS[floor(rnd(1, #STAR_COLS + 1))] end

local function emitStar()
    local x, y, z = skyDir(-35, 65)
    local c = starCol()
    local s = rnd(0, 1) ^ 3
    local size = STARS.size[1] + (STARS.size[2] - STARS.size[1]) * s
    scene:PsEmit(ps.stars, x * FAR_R, y * FAR_R, z * FAR_R, 0, 0, 0, c[1], c[2], c[3], rnd(0.55, 1.0), size, size,
        rnd(STARS.life[1], STARS.life[2]), 0, 0, 1)
end

local function emitBandStar()
    local a = rnd(0, 2 * pi)
    local off = (rnd(-1, 1) + rnd(-1, 1) + rnd(-1, 1)) / 3 * BAND.width
    local x, y, z = norm(e1x * cos(a) + e2x * sin(a) + bx * off, e1y * cos(a) + e2y * sin(a) + by * off, e1z * cos(a) + e2z * sin(a) + bz * off)
    local size = rnd(BAND.size[1], BAND.size[2])
    scene:PsEmit(ps.band, x * FAR_R, y * FAR_R, z * FAR_R, 0, 0, 0, 225, 228, 255, rnd(0.35, 0.85), size, size,
        rnd(BAND.life[1], BAND.life[2]), 0, 0, 1)
end

local function emitTwinkle()
    local x, y, z = skyDir(-30, 60)
    local c = starCol()
    local size = rnd(TWINKLES.size[1], TWINKLES.size[2])
    scene:PsEmit(ps.twinkles, x * FAR_R, y * FAR_R, z * FAR_R, 0, 0, 0, c[1], c[2], c[3], rnd(0.7, 1.0), size, size * 0.4,
        rnd(TWINKLES.life[1], TWINKLES.life[2]), 0, 0, 1)
end

local function emitDust()
    local x, y, z = skyDir(-40, 60)
    local d = rnd(DUST.near[1], DUST.near[2])
    scene:PsEmit(ps.dust, camX + x * d, camY + y * d, camZ + z * d, rnd(-0.15, 0.15), rnd(-0.06, 0.06), rnd(-0.15, 0.15),
        200, 196, 255, rnd(0.15, 0.35), rnd(0.12, 0.3), rnd(0.12, 0.3), rnd(DUST.life[1], DUST.life[2]), 0, 0, 1)
end

-- keep each layer at its live count
local function emitLayers(dt)
    local function feed(key, cfg, fn)
        acc[key] = acc[key] + cfg.count / ((cfg.life[1] + cfg.life[2]) / 2) * dt
        while acc[key] >= 1 do fn(); acc[key] = acc[key] - 1 end
    end
    feed("stars", STARS, emitStar)
    feed("band", BAND, emitBandStar)
    feed("twinkles", TWINKLES, emitTwinkle)
    feed("dust", DUST, emitDust)
end

local function stepLayers(dt)
    for _, key in ipairs(LAYERS) do scene:PsUpdate(ps[key], dt) end
end

-- shooting star
local function startMeteor()
    local fx, fy, fz = scene:GetCameraForward()
    local rx, ry, rz = scene:GetCameraRight()
    local ux, uy, uz = scene:GetCameraUp()
    local tanV = math.tan(FOV * pi / 360)
    local sx, sy = rnd(-0.8, 0.8) * tanV * RW / RH, rnd(0.25, 0.85) * tanV
    meteor.dx, meteor.dy, meteor.dz = norm(fx + rx * sx + ux * sy, fy + ry * sx + uy * sy, fz + rz * sx + uz * sy)
    local side = (sx > 0) and -1 or 1                      -- head for the middle of the screen
    meteor.tx, meteor.ty, meteor.tz = norm(rx * side + ux * -0.5, ry * side + uy * -0.5, rz * side + uz * -0.5)
    meteor.t, meteor.dur, meteor.speed = 0, rnd(0.7, 1.1), rnd(0.45, 0.7)
    meteor.on, meteor.lx = true, nil
end

local function meteorHead()
    local k = meteor.t * meteor.speed
    local x, y, z = norm(meteor.dx + meteor.tx * k, meteor.dy + meteor.ty * k, meteor.dz + meteor.tz * k)
    return camX + x * 90, camY + y * 90, camZ + z * 90
end

local function tickMeteor(dt)
    if not meteor.on then
        meteor.wait = meteor.wait - dt
        if meteor.wait <= 0 then startMeteor() end
        return
    end
    meteor.t = meteor.t + dt
    if meteor.t >= meteor.dur then meteor.on = false; meteor.wait = rnd(5, 11); return end
    local hx, hy, hz = meteorHead()
    local fade = math.min(1, meteor.t / 0.15) * math.min(1, (meteor.dur - meteor.t) / 0.3)
    if meteor.lx then
        local dx, dy, dz = hx - meteor.lx, hy - meteor.ly, hz - meteor.lz
        local n = math.max(1, floor(sqrt(dx * dx + dy * dy + dz * dz) / 0.35))
        for i = 1, n do
            local f = i / n
            scene:PsEmit(ps.meteor, meteor.lx + dx * f, meteor.ly + dy * f, meteor.lz + dz * f, 0, 0, 0,
                205, 222, 255, 0.85 * fade, 1.5, 0.15, 0.45, 0, 0, 1)
        end
    end
    scene:PsEmit(ps.meteor, hx, hy, hz, 0, 0, 0, 255, 255, 255, fade, 3.2, 3.2, 0.05, 0, 0, 1)
    meteor.lx, meteor.ly, meteor.lz = hx, hy, hz
end

local function placeCamera()
    local yaw = 200 + t * 1.1
    local pitch = 10 + 3 * sin(t * 2 * pi / 90) + lift.pitch
    camX, camY, camZ = 3 * sin(t * 0.05), 0.8 * sin(t * 0.07) + lift.y, 3 * cos(t * 0.05) - 3
    scene:SetCameraPosition(camX, camY, camZ)
    scene:SetCameraAngles(yaw, pitch)
end

local function skyUniforms()
    local fx, fy, fz = scene:GetCameraForward()
    local rx, ry, rz = scene:GetCameraRight()
    local ux, uy, uz = scene:GetCameraUp()
    scene:SetSkyUniform(0, fx, fy, fz, math.tan(FOV * pi / 360))
    scene:SetSkyUniform(1, rx, ry, rz, RW / RH)
    scene:SetSkyUniform(2, ux, uy, uz, 0)
    scene:SetSkyUniform(3, ZENITH[1], ZENITH[2], ZENITH[3], 0)
    scene:SetSkyUniform(4, HORIZON[1], HORIZON[2], HORIZON[3], 0)
    scene:SetSkyUniform(5, NEBULA_A[1], NEBULA_A[2], NEBULA_A[3], 0)
    scene:SetSkyUniform(6, NEBULA_B[1], NEBULA_B[2], NEBULA_B[3], 0)
    scene:SetSkyUniform(7, bx, by, bz, 0.22)
    scene:SetTime(t)
end

-- plain gradient when the sky shader is not available
local function fallbackSky()
    local bands = 48
    local bh = RH / bands
    for i = 0, bands - 1 do
        local f = i / (bands - 1)
        local r = ZENITH[1] + (HORIZON[1] - ZENITH[1]) * f
        local g = ZENITH[2] + (HORIZON[2] - ZENITH[2]) * f
        local b = ZENITH[3] + (HORIZON[3] - ZENITH[3]) * f
        scene:FillRect(0, floor(i * bh), RW, floor(bh) + 2, floor(r * 255), floor(g * 255), floor(b * 255), 255)
    end
end

-- the driver's state, passed on as a shared string
local function saveState()
    local m = meteor
    SHARED:SetSharedString(STATE_KEY, table.concat({
        t, acc.stars, acc.band, acc.twinkles, acc.dust,
        m.on and 1 or 0, m.wait or 0, m.dx or 0, m.dy or 0, m.dz or 0, m.tx or 0, m.ty or 0, m.tz or 0,
        m.t or 0, m.dur or 0, m.speed or 0, m.lx or "n", m.ly or "n", m.lz or "n",
        ps.stars, ps.band, ps.twinkles, ps.dust, ps.meteor }, ";"))
end

local function loadState()
    local v, n = {}, 0
    for s in (SHARED:GetSharedString(STATE_KEY) .. ";"):gmatch("([^;]*);") do n = n + 1; v[n] = tonumber(s) end
    if n < 24 or v[1] == nil or v[20] == nil then return false end
    t = v[1]
    acc.stars, acc.band, acc.twinkles, acc.dust = v[2], v[3], v[4], v[5]
    meteor.on, meteor.wait = v[6] == 1, v[7]
    meteor.dx, meteor.dy, meteor.dz, meteor.tx, meteor.ty, meteor.tz = v[8], v[9], v[10], v[11], v[12], v[13]
    meteor.t, meteor.dur, meteor.speed = v[14], v[15], v[16]
    meteor.lx, meteor.ly, meteor.lz = v[17], v[18], v[19]
    ps.stars, ps.band, ps.twinkles, ps.dust, ps.meteor = v[20], v[21], v[22], v[23], v[24]
    return true
end

local function registerSprite(id, path)
    local tex = TEXTURE:CreateTextureSync(path)
    if tex then scene:RegisterSpriteFromTexture(id, tex); scene:SetSpriteFilter(id, "linear"); tex:Dispose() end
end

-- the shared sky, or nil
local function find()
    scene = SHARED:GetSharedScene(KEY)
    if scene == nil then me = nil end
    return scene
end

-- build the sky unless one is shared already; sprites = { dot, sparkle } image paths
function Sky.create(sprites)
    if find() ~= nil or SCENE3D == nil then return end
    scene = SCENE3D:CreateScene(RW, RH)
    scene:SetMode("raster")
    scene:SetLighting(false)
    scene:SetCameraFov(FOV)
    scene:SetCameraNear(0.05)
    scene:SetFog(false, 0, 0, 0, 0, 0)
    registerSprite(1, sprites.dot)
    registerSprite(2, sprites.sparkle)
    for _, key in ipairs(LAYERS) do
        ps[key] = scene:NewParticleSystem()
        scene:PsSetCap(ps[key], 3000)
        scene:PsSetSprite(ps[key], key == "twinkles" and 2 or 1)
        scene:PsSetNextRotation(ps[key], 0, 0)
    end
    scene:PsSetNextCurves(ps.stars, 0, 1)                   -- fade in and out
    scene:PsSetNextCurves(ps.band, 0, 1)
    scene:PsSetNextCurves(ps.twinkles, 2, 1)                -- swell, then shrink
    scene:PsSetNextCurves(ps.dust, 0, 1)
    scene:PsSetNextCurves(ps.meteor, 0, 0)
    scene:SetSkyShader(SKY)
    -- fill the sky before the first frame
    t = rnd(0, 300)
    acc = { stars = 0, band = 0, twinkles = 0, dust = 0 }
    lift.y, lift.pitch = 0, 0
    placeCamera()
    for _ = 1, 48 do emitLayers(0.5); stepLayers(0.5) end
    meteor.on, meteor.wait, meteor.lx = false, rnd(2, 5), nil
    -- render once now so the shader compiles before the first visible frame
    if scene:SkyShaderActive() then skyUniforms() else fallbackSky() end
    pcall(function() scene:Render(); scene:Upload() end)
    SHARED:SetSharedScene(KEY, scene)
    saveState()
    SHARED:SetSharedString(DRIVER_KEY, "")
end

-- drive the sky as `name`, from where the last driver left it
function Sky.claim(name)
    if find() == nil or not loadState() then return false end
    me = name
    lift.y, lift.pitch = 0, 0
    SHARED:SetSharedString(DRIVER_KEY, name)
    return true
end

-- whether we still drive the sky (another module may have taken it)
function Sky.driving()
    return me ~= nil and scene ~= nil and not scene.IsDisposed and SHARED:GetSharedString(DRIVER_KEY) == me
end

-- stop driving; the sky waits for the next driver
function Sky.release()
    if Sky.driving() then
        saveState()
        SHARED:SetSharedString(DRIVER_KEY, "")
    end
    me = nil
end

-- extra camera height (world units) and tilt (degrees); 0, 0 is the lobby view
function Sky.setLift(y, pitch) lift.y, lift.pitch = y, pitch end

function Sky.update(dt)
    if not Sky.driving() then return end
    t = t + dt
    placeCamera()
    emitLayers(dt)
    tickMeteor(dt)
    stepLayers(dt)
    saveState()
end

-- driver only
function Sky.render()
    if not Sky.driving() then return end
    if scene:SkyShaderActive() then skyUniforms() else fallbackSky() end
    scene:Render()
    scene:Upload()
end

-- draw the last frame with its top at y; false when there is no sky
function Sky.draw(y, opacity)
    if scene == nil or scene.IsDisposed then find() end
    if scene == nil then return false end
    scene:SetColor(1, 1, 1); scene:SetOpacity(opacity or 1)
    scene:SetScale(SCREEN_W / RW, SCREEN_H / RH)
    scene:Draw(0, math.floor((y or 0) + 0.5))
    scene:SetOpacity(1)
    return true
end

-- call before the module goes away
function Sky.forget()
    Sky.release()
    scene = nil
end

return Sky

-- Encounter texture lifetime per visit; never called from a draw function.

local M = {}

M.root = ""     -- prefix of every texture path

local function load_manifest(name)
    local ok, m = pcall(require, name)
    if not ok or type(m) ~= "table" then return {} end
    return m
end

local MANIFESTS = {
    load_manifest("encounter/manifest"), load_manifest("encounter/manifest_result"),
    load_manifest("encounter/manifest_cut10"), load_manifest("encounter/manifest_cut20"),
}

-- entries of every manifest; the first manifest wins on a shared key
local ENTRIES = {}
for i = #MANIFESTS, 1, -1 do
    for k, e in pairs(MANIFESTS[i].tex or {}) do ENTRIES[k] = e end
end

local _handles = {}     -- key -> texture created this visit
local _metas = {}       -- key -> normalised manifest entry

-- puts a manifest first: it wins on a shared key
function M.add(manifest)
    if type(manifest) ~= "table" then return end
    table.insert(MANIFESTS, 1, manifest)
    for k, e in pairs(manifest.tex or {}) do
        ENTRIES[k] = e
        _metas[k] = nil
    end
end

-- the manifest entry with named fields (path, w, h, x, y, fx, fy, mirror, jump, parts), or nil
function M.meta(key)
    local m = _metas[key]
    if m ~= nil then return m end
    local e = ENTRIES[key]
    if type(e) ~= "table" then return nil end
    m = {
        key = key,
        path = e.path or e[1],
        w = e.w or e[2] or 0,
        h = e.h or e[3] or 0,
        x = e.x, y = e.y,
        fx = e.fx, fy = e.fy,
        mirror = e.mirror,
        jump = e.jump,
        parts = e.parts,
    }
    _metas[key] = m
    return m
end

-- scene data stored beside the textures, by name
function M.data(name)
    if name == "tex" then return nil end
    for _, m in ipairs(MANIFESTS) do
        if m[name] ~= nil then return m[name] end
    end
    return nil
end

-- accepts a list of keys or a set (key = true); unknown keys are skipped
local function each_key(keys, fn)
    if keys == nil then return end
    for k, v in pairs(keys) do
        if type(k) == "number" then
            if type(v) == "string" then fn(v) end
        elseif type(k) == "string" and v then
            fn(k)
        end
    end
end

-- creates the listed textures not created yet; sync = true loads them at once instead of streaming
function M.open(keys, sync)
    each_key(keys, function(key)
        if _handles[key] ~= nil then return end
        local m = M.meta(key)
        if m == nil or m.path == nil then return end
        local tx
        if sync then tx = TEXTURE:CreateTextureSync(M.root .. m.path)
        else tx = TEXTURE:CreateTexture(M.root .. m.path) end
        if tx ~= nil then
            tx:SetWrapMode("Edge")
            _handles[key] = tx
        end
    end)
end

-- returns handle, entry; the handle is nil for a key never opened
function M.get(key)
    return _handles[key], M.meta(key)
end

-- true once every listed handle (all opened ones when keys is nil) is Ready; a failed load counts as settled
function M.ready(keys)
    local all = true
    local function check(key)
        local tx = _handles[key]
        if tx == nil then
            if M.meta(key) ~= nil then all = false end
        elseif tx.Loaded and not tx.Ready then
            all = false
        end
    end
    if keys == nil then
        for key in pairs(_handles) do check(key) end
    else
        each_key(keys, check)
    end
    return all
end

-- disposes every texture of the visit; safe to call again
function M.release()
    for _, tx in pairs(_handles) do
        pcall(tx.Dispose, tx)
    end
    _handles = {}
end

-- disposes the listed textures only (a list or a set)
function M.drop(keys)
    each_key(keys, function(key)
        local tx = _handles[key]
        if tx ~= nil then
            pcall(tx.Dispose, tx)
            _handles[key] = nil
        end
    end)
end

return M

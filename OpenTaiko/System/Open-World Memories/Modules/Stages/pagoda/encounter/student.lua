-- The story student chosen in My Room (Lib/Student) on the encounter and result screens.

local Student = require("Student")

local M = {}

M.FRAMES = { "normal", "run", "run_m", "clear", "fail" }

-- Ume's sprite -> the student's frame in the same state; the student has no brave frame
local FOR_UME = { ume_normal = "normal", ume_brave = "normal", ume_clear = "clear", ume_run = "run",
                  ume_run_m = "run_m", ume_fail = "fail" }

local _gm = nil

local function save()
    local ok, sf = pcall(GetSaveFile, 0)
    if ok then return sf end
    return nil
end

-- "a" or "b"
function M.id()
    return Student.current(save())
end

-- the student's manifest keys by frame name, plus forUme[Ume's key] = the student's key
function M.frames(id)
    id = Student.valid(id or M.id())
    local f, forUme = {}, {}
    for _, n in ipairs(M.FRAMES) do f[n] = "student_" .. id .. "_" .. n end
    for u, n in pairs(FOR_UME) do forUme[u] = f[n] end
    f.forUme = forUme
    return f
end

function M.keys(id)
    local f, list = M.frames(id), {}
    for i, n in ipairs(M.FRAMES) do list[i] = f[n] end
    return list
end

-- reads the student's palette; called when a screen opens, never from a draw function
function M.prepare()
    local sf = save()
    local id = Student.current(sf)
    _gm = Student.gradient(id, Student.index(sf, id))
end

-- the palette as a GRADIENT map for encounter/sprite's grad option, nil for the default colours
function M.palette()
    return _gm
end

return M

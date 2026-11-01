-- Levels whose entrance and result screens play a scripted cutscene.
-- Cutscene module (encounter/cutscenes/<name>.lua), drawn under the shared scrim, dialog and menu.
-- encounter/scene.lua calls:
--   keys(level, ctx, phase) -> encounter/assets keys (phase "enter" | "clear" | "fail")
--   open(level, ctx, phase)  textures exist (not Ready yet), the sky view is reset
--   begin()                  optional: the fade-in starts
--   update(dt)               every frame from the fade-in on
--   draw(ox, oy)             the layers under the scrim; ox, oy = the screen shake offset
--   drawOver(ox, oy)         optional: over the scrim, under the cover and the dialog
--   event(name, line)        an { event = name } script line was reached (line carries any arguments)
--   busy() -> bool           true while an animation a `wait` line waits for still runs
--   scrimAlpha() -> 0..1     optional (default scene.scrimAlpha)
--   sky() -> grey, visible   optional, as a band's sky() (default 0, false)
--   close()                  drop per-visit state (encounter/assets frees the textures)
--
-- Its script (story/cutscenes/<name>.lua) is data:
--   enter  = lines or variants   absent: the floor's story/entrance.lua variant plays
--   clear  = lines or variants   after the clear banner
--   fail   = lines or variants   after the fail banner
--   flight = lines               optional: the fail flight's lines (else story/fail_lines.lua)
--   ending = { clear = "transition" | "credits", fail = "flight" | "title" }
--   music  = "late"              optional: the floor loop waits for a { music = true } line or the menu
-- Lines use the story/entrance.lua format plus who = "you" (player name as speaker), blip = "<sound name>",
-- event = "<name>", wait = true, cover = 0..1 with sec (default 0.6; held like a wait line), music = true;
-- variants are { { when = fn(ctx), lines = { ... } }, ... } as in story/entrance.lua.
--
-- Credits module (credits.lua), run after a clear whose ending is "credits": setup(tex, fonts) (optional),
-- keys(), open(), update(dt, nav) -> nil | "done", draw(), sky() -> grey, visible (optional), close().

local M = {}

local LEVELS = {
    [10] = { module = "encounter/cutscenes/level10", script = "story/cutscenes/level10" },
    [20] = { module = "encounter/cutscenes/level20", script = "story/cutscenes/level20" },
}

local CREDITS = "credits"

-- the module's table, or nil when its file does not exist; an error inside the file is raised
local function load(name)
    local ok, m = pcall(require, name)
    if ok then return (type(m) == "table") and m or nil end
    if tostring(m):find("module '" .. name .. "' not found", 1, true) then return nil end
    error(m, 0)
end

-- the cutscene module and its script data for a level, or nil
function M.forLevel(level)
    local e = LEVELS[level]
    if e == nil then return nil end
    local cut, script = load(e.module), load(e.script)
    if cut == nil or script == nil then return nil end
    return cut, script
end

-- the credits module, or nil when its file does not exist
function M.credits()
    return load(CREDITS)
end

return M

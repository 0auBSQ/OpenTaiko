---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- nav_input.lua — handles common keybinds for directional navigation

local NavInput = {}

-- ─── Player input sets ────────────────────────────────────────────────────────

local inputSets = {
    -- per-player
    { right = "RBlue",       left = "LBlue",      decide1 = "RRed",    decide2 = "LRed",    cancel = nil },
    { right = "RBlue2P",     left = "LBlue2P",    decide1 = "RRed2P",  decide2 = "LRed2P",  cancel = nil },
    { right = "RBlue3P",     left = "LBlue3P",    decide1 = "RRed3P",  decide2 = "LRed3P",  cancel = nil },
    { right = "RBlue4P",     left = "LBlue4P",    decide1 = "RRed4P",  decide2 = "LRed4P",  cancel = nil },
    { right = "RBlue5P",     left = "LBlue5P",    decide1 = "RRed5P",  decide2 = "LRed5P",  cancel = nil },
    -- universal
    pad = {
      right = "RightChange", left = "LeftChange", decide1 = "Decide",  decide2 = "Decide",  cancel = "Cancel" },
    keyboard = {
      right = "RightArrow",  left = "LeftArrow",  decide1 = "Return",  decide2 = "Return",  cancel = "Escape",
      down  = "DownArrow",   up   = "UpArrow" },
}

local binding_player_defs = {
    right = { { nav = "right" } },
    rightOtherPlayer = { { navKbd = "right", navPadOther = "right" } },
    rightKeyboard = { { navKbd = "right" } },
    left = { { nav = "left" } },
    leftOtherPlayer = { { navKbd = "left", navPadOther = "left" } },
    leftKeyboard = { { navKbd = "left" } },
    up = { { navKbd = "up" } },
    upOrPadLeft = { { navKbd = "up", navPad = "left" } },
    down = { { navKbd = "down" } },
    downOrPadRight = { { navKbd = "down", navPad = "right" } },
    decide = { { nav = "decide1" }, { nav = "decide2" } },
    cancel = { { nav = "cancel" } },
}

local binding_union_defs = {
    right = { { nav = "right", navPadOther = "right" } },
    rightKeyboard = { { navKbd = "right" } },
    left = { { nav = "left", navPadOther = "left" } },
    leftKeyboard = { { navKbd = "left" } },
    up = { { navKbd = "up" } },
    upOrPadLeft = { { navKbd = "up", navPad = "left", navPadOther = "left" } },
    down = { { navKbd = "down" } },
    downOrPadRight = { { navKbd = "down", navPad = "right", navPadOther = "right" } },
    decide = { { nav = "decide1", navPadOther = "decide1" }, { nav = "decide2", navPadOther = "decide2" } },
    cancel = { { nav = "cancel", navPadOther = "cancel" } },
}

local function tryInputPad(event, pad)
    return pad ~= nil and INPUT[event](INPUT, pad)
end
local function tryInputKeyboard(event, key)
    return key ~= nil and INPUT["Keyboard" .. event](INPUT, key)
end

local function inputHandler(event, player, def)
    if event == nil or event == "" then event = "Pressed" end
    local inputSetP = inputSets[player] or {}

    local handlers = {}
    for i, v in ipairs(def) do
        local navKbd = v.navKbd or v.nav
        local navPad = v.navPad or v.nav
        local navPadOther = v.navPadOther
        local tryPadOther = (navPadOther == nil) and function(event) return false end or function (event)
            for p = 1, 5, 1 do
                if p ~= player and tryInputPad(event, inputSets[p][navPadOther]) then
                    return true
                end
            end
            return false
        end
        handlers[i] = function (useUniversal)
            if useUniversal == nil then useUniversal = true end
            return tryInputPad(event, inputSetP[navPad]) or tryPadOther(event)
                or (useUniversal and (tryInputPad(event, inputSets.pad[navPad]) or tryInputKeyboard(event, inputSets.keyboard[navKbd])))
        end
    end

    local chainedHandler = nil
    for i = #handlers, 1, -1 do
        local chainedHandlerI = chainedHandler
        local handlerI = handlers[i]
        chainedHandler = (chainedHandlerI == nil) and handlerI or function (useUniversal)
            return handlerI(useUniversal) or chainedHandlerI(useUniversal)
        end
    end
    return chainedHandler or function (useUniversal) return false end
end

-- 1 for P1, 2 for P2, and so on; 0 for using only universal navigation keys; "" or omitted for using the union of all players' keys
function NavInput.getPn(player)
    if player == nil then player = "" end
    if NavInput.p[player] ~= nil then
        return NavInput.p[player]
    end
    local binding_defs = (player == "") and binding_union_defs or binding_player_defs
    local navPn = {}
    -- navPn.right() aka. navPn.rightPressed(), navPn.rightPressing(), navPn.rightReleased(), and so on.
    for i, event in ipairs{ "", "Pressed", "Pressing", "Released" } do
        for binding, def in pairs(binding_defs) do
            navPn[binding .. event] = inputHandler(event, player, def)
        end
    end
    return navPn
end

-- add universal and per-player input functions
NavInput.p = {}
for p = 0, 5, 1 do
    NavInput.p[p] = NavInput.getPn(p)
end

-- add shorthand for universal input functions
NavInput.p[""] = NavInput.getPn()
for k, v in pairs(NavInput.p[""]) do
    NavInput[k] = v
end

-- ─── What a binding answers to, for hints ─────────────────────────────────────

-- SlimDX key names that read better than their enum name; letters, digits and function keys pass through
local KEY_NAMES = {
    LeftControl = "L-Ctrl", RightControl = "R-Ctrl", LeftShift = "L-Shift", RightShift = "R-Shift",
    LeftAlt = "L-Alt", RightAlt = "R-Alt", Return = "Enter", Escape = "Esc", Back = "Backspace",
    UpArrow = "Up", DownArrow = "Down", LeftArrow = "Left", RightArrow = "Right",
    PageUp = "PgUp", PageDown = "PgDn", Insert = "Ins", Delete = "Del", CapsLock = "Caps",
    Grave = "`", Minus = "-", Equals = "=", LeftBracket = "[", RightBracket = "]", Semicolon = ";",
    Apostrophe = "'", Backslash = "\\", Comma = ",", Period = ".", Slash = "/",
    NumberPadEnter = "Num Enter", NumberPadPlus = "Num +", NumberPadMinus = "Num -",
    NumberPadStar = "Num *", NumberPadSlash = "Num /", NumberPadPeriod = "Num .",
}
local DEVICE_PREFIX = { Gamepad = "Pad ", Joystick = "Joy ", MidiIn = "MIDI ", Mouse = "Mouse " }

-- a keyboard key's display name ("Return" -> "Enter", "D1" -> "1"); nil for none
function NavInput.keyName(key)
    if key == nil or key == "" then return nil end
    if KEY_NAMES[key] then return KEY_NAMES[key] end
    local d = key:match("^D(%d)$")
    if d then return d end
    local np = key:match("^NumberPad(%d)$")
    if np then return "Num " .. np end
    return key
end

local function addName(out, seen, name)
    if name ~= nil and name ~= "" and not seen[name] then
        seen[name] = true
        out[#out + 1] = name
    end
end

-- every binding of a key-config input ("Decide", "RRed", ...): keys by name, other devices prefixed
local function addInput(out, seen, input)
    if input == nil then return end
    local ok, n = pcall(function() return INPUT:GetBindingCount(input) end)
    if not ok or type(n) ~= "number" then return end
    for i = 0, n - 1 do
        local dev, name = INPUT:GetBindingDevice(input, i), INPUT:GetBindingName(input, i)
        if dev == "Keyboard" then
            addName(out, seen, NavInput.keyName(name))
        elseif dev == "MidiIn" then
            addName(out, seen, DEVICE_PREFIX.MidiIn .. (name:gsub("%[%d+%]$", "")))
        elseif dev == "Mouse" then
            addName(out, seen, DEVICE_PREFIX.Mouse .. name:sub(1, 1):upper() .. name:sub(2))
        elseif DEVICE_PREFIX[dev] ~= nil then
            addName(out, seen, DEVICE_PREFIX[dev] .. name)
        end
    end
end

-- the display names of everything a binding ("decide", "cancel", "right", ...) answers to for a player (as
-- getPn takes it): its keyboard key first, then the key config's universal inputs, the player's drum
-- inputs and the other players' ones, without repeats
function NavInput.bindingNames(binding, player)
    if player == nil then player = "" end
    local def = ((player == "") and binding_union_defs or binding_player_defs)[binding]
    local out, seen = {}, {}
    if def == nil then return out end
    local inputSetP = inputSets[player] or {}
    for _, v in ipairs(def) do addName(out, seen, NavInput.keyName(inputSets.keyboard[v.navKbd or v.nav])) end
    for _, v in ipairs(def) do addInput(out, seen, inputSets.pad[v.navPad or v.nav]) end
    for _, v in ipairs(def) do addInput(out, seen, inputSetP[v.navPad or v.nav]) end
    for _, v in ipairs(def) do
        if v.navPadOther ~= nil then
            for p = 1, 5 do
                if p ~= player then addInput(out, seen, inputSets[p][v.navPadOther]) end
            end
        end
    end
    return out
end

return NavInput

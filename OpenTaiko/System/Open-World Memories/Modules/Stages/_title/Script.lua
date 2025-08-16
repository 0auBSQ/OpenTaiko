local text = nil
local ttkArr = {}
local background = nil
local sounds = {}
local currentMenu = 1
local defaultMenus = {"gamestart", "aibattlemode", "dangamestart", "taikotowerstart", "heya", "onlinelounge", "config", "exit"}
local localizedMenus = {"TITLE_MODE_TAIKO", "TITLE_MODE_AI", "TITLE_MODE_DAN", "TITLE_MODE_TOWER", "TITLE_MODE_HEYA", "TITLE_MODE_ONLINE", "TITLE_MODE_SETTINGS", "TITLE_MODE_EXIT"}
local luaMenus = {}

local function JsonNum(num)
	return JSONLOADER:ExtractNumber(num)
end

local function JsonText(txt)
	return JSONLOADER:ExtractText(txt)
end

local function GenerateTTK()
    ttkArr = {}
    local menuCount = #defaultMenus + JsonNum(luaMenus["count"])
    for i = 1, menuCount do
        local color = COLOR:CreateColorFromARGB(255, 255, 255, 255)
        if i == currentMenu then color = COLOR:CreateColorFromARGB(255, 242, 207, 1) end
        if i <= #defaultMenus then
			local ls = I18N:GetInternalTranslatedString(localizedMenus[i])
            ttkArr[i] = text:GetText(ls, false, 99999, color)
        else
			local stnb = string.format("%.0f", i - #defaultMenus)
            local sn = luaMenus[stnb]["StageName"]
            local ldt = I18N:AsLocalizationData(sn)
            ttkArr[i] = text:GetText(ldt:GetString(""), false, 99999, color)
        end
    end
end

local function MoveMenu(offset) -- Offset is only 1/-1, will not work as it should with bigger values
    local menuCount = #defaultMenus + JsonNum(luaMenus["count"])
    currentMenu = currentMenu + offset
    if currentMenu < 1 then currentMenu = menuCount end
    if currentMenu > menuCount then currentMenu = 1 end
    GenerateTTK()
end

local function CurrentMenuToStage()
    if currentMenu <= #defaultMenus then
        return "legacy", defaultMenus[currentMenu]
    else
		local stnb = string.format("%.0f", currentMenu - #defaultMenus)
        return "stage", JsonText(luaMenus[stnb]["LuaStageName"])
    end
end

function draw()
    if background ~= nil then background:Draw(0, 0) end
    local menuCount = #defaultMenus + JsonNum(luaMenus["count"])
    for i = 1, menuCount do
        local ypos = 600 - (currentMenu * 100) + 100*i
		if ttkArr[i] ~= nil then 
			ttkArr[i]:DrawAtAnchor(900, ypos, "Center")
		end
    end

    NAMEPLATE:DrawPlayerNameplate(100, 860, 255, 0)
end

function update()
    if INPUT:Pressed("Cancel") == true or INPUT:KeyboardPressed("Escape") == true then
        sounds.Cancel:Play()
        return Exit("stage", "_boot")
    end

    if (INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") or INPUT:KeyboardPressed("DownArrow")) then
        sounds.Move:Play()
        MoveMenu(1)
    end

    if (INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") or INPUT:KeyboardPressed("UpArrow")) then
        sounds.Move:Play()
        MoveMenu(-1)
    end

    if INPUT:Pressed("Decide") == true or INPUT:KeyboardPressed("Return") == true then
        local transition, stn = CurrentMenuToStage()
        sounds.Decide:Play()
        return Exit(transition, stn)
    end
end

function activate()
    luaMenus = JSONLOADER:LoadJson("Config/MainMenuSettings.json")
    text = TEXT:Create(16)
    background = TEXTURE:CreateTexture("Textures/Background.png")
    sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
    sounds.BGM:SetLoop(true)
    sounds.Decide = SOUND:CreateSFX("Sounds/Decide.ogg")
    sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
    sounds.Move = SOUND:CreateSFX("Sounds/Move.ogg")
    sounds.BGM:Play()
    GenerateTTK()
end

function deactivate()
    if text ~= nil then text:Dispose() end
    if background ~= nil then background:Dispose() end
    for _, sound in pairs(sounds) do
        sound:Dispose()
    end
end

function onStart()
end

function afterSongEnum()
end

function onDestroy()
end
-- Modal ROActivity
-- Ported from Modules/Modal/Script.lua to the new Lua API.
-- Textures and sounds are loaded from the original Modules/Modal/ location.

local TEXTURES_DIR = "Textures/"
local SOUNDS_DIR   = "Sounds/"

-- Current modal state
local modal_current_type   = 0
local modal_current_rarity = 1
local modal_current_player = 1
local modal_current_info   = nil
local modal_current_visual = nil

-- Graphics
local icon_players   = {}
local modal_tx       = {}
local modal_tx_coin  = nil

-- Sounds
local modal_sfx      = {}
local modal_sfx_coin = nil

-- Fonts
local font_modal_header = nil
local font_modal_body   = nil
local font_modal_plate  = nil

-- Text strings set when a new modal is registered
local modal_header_text = ""
local modal_body_text   = ""
local modal_body_fg     = nil
local modal_body_bg     = nil

-- Animation counters
local modal_duration          = 500
local modal_counter           = 0
local script_busy             = false
local modal_loopanim_duration = 1000
local modal_loopanim_counter  = 0

-- Song modal cache
local modal_preimage_ref = nil

-- Tmp (rarity index, 1-based)
local modal_asset_id = 0

-- LangInt rarity for star display (matches HRarity.RarityToLangInt)
-- Used only for DrawTitlePlate calls that expect the lang-int scale.
local modal_rarity_lang_int = 0
local rarity_lang_int_map = {
	["Poor"]      = 0,
	["Common"]    = 1,
	["Uncommon"]  = 2,
	["Rare"]      = 3,
	["Epic"]      = 4,
	["Legendary"] = 5,
	["Mythical"]  = 6,
}

-- ────────────────────────────────────────────────────────────────────────────
-- ROActivity lifecycle
-- ────────────────────────────────────────────────────────────────────────────

function onStart()
	for i = 1, 5 do
		icon_players[i] = TEXTURE:CreateTexture(TEXTURES_DIR .. tostring(i) .. "P.png")
	end

	for i = 0, 4 do
		modal_tx[i + 1]   = TEXTURE:CreateTexture(TEXTURES_DIR .. tostring(i) .. ".png")
		modal_sfx[i + 1]  = SOUND:CreateSFX(SOUNDS_DIR .. tostring(i) .. ".ogg")
	end

	modal_tx_coin  = TEXTURE:CreateTexture(TEXTURES_DIR .. "Coin.png")
	modal_sfx_coin = SOUND:CreateSFX(SOUNDS_DIR .. "Coin.ogg")

	font_modal_header = TEXT:Create(84, "regular")
	font_modal_body   = TEXT:Create(84, "regular")
	font_modal_plate  = TEXT:Create(16, "regular")

	modal_body_fg = COLOR:CreateColorFromRGBA(0, 0, 0, 255)
	modal_body_bg = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

function activate() end
function deactivate() end

-- ────────────────────────────────────────────────────────────────────────────
-- Exported functions (called from C# via Call())
-- ────────────────────────────────────────────────────────────────────────────

function isAnimationFinished()
	return not script_busy
end

-- modal_asset_informations: Character object, Coin count, NameplateUnlockable, etc.
-- modal_asset_visual_references: Character texture, Song preimage, CLuaNamePlateScript ref, etc.
function registerNewModal(player, rarity, modal_type, modal_asset_informations, modal_asset_visual_references)
	local header_str = ""
	local body_str   = ""

	modal_current_type   = modal_type
	modal_current_rarity = rarity
	modal_current_player = player
	modal_current_info   = modal_asset_informations
	modal_current_visual = modal_asset_visual_references

	modal_counter          = 0
	modal_loopanim_counter = 0
	script_busy            = true

	if modal_type == 0 then
		-- Coin
		modal_current_rarity = 1
		header_str = LANG:GetString("MODAL_TITLE_COIN")
		body_str   = LANG:GetString("MODAL_MESSAGE_COIN",
			tostring(modal_asset_informations), tostring(modal_asset_visual_references))
		modal_sfx_coin:Play()

	elseif modal_type == 1 then
		-- Character
		header_str = LANG:GetString("MODAL_TITLE_CHARA")
		body_str   = modal_current_info.metadata:tGetName()

	elseif modal_type == 2 then
		-- Puchichara
		header_str = LANG:GetString("MODAL_TITLE_PUCHI")
		body_str   = modal_current_info.metadata:tGetName()

	elseif modal_type == 3 then
		-- Title / Nameplate
		header_str = LANG:GetString("MODAL_TITLE_NAMEPLATE")
		body_str   = modal_asset_informations.Value.nameplateInfo.cld:GetString("")
		modal_rarity_lang_int = rarity_lang_int_map[modal_asset_informations.Value.rarity] or 0

	elseif modal_type == 4 then
		-- Song
		header_str = LANG:GetString("MODAL_TITLE_SONG")
		body_str   = (modal_current_info ~= nil) and modal_current_info.ldTitle:GetString("") or "??? (Not found)"
		if modal_current_info ~= nil then
			modal_preimage_ref = modal_current_visual(modal_current_info)
		else
			modal_preimage_ref = nil
		end
	end

	modal_header_text = header_str
	modal_body_text   = body_str

	modal_asset_id = math.max(1, math.min(5, modal_current_rarity + 1))

	if modal_type ~= 0 then
		modal_sfx[modal_asset_id]:Play()
	end
end

function update()
	if modal_counter <= modal_duration then
		script_busy    = true
		modal_counter  = modal_counter + (1000 * fps.deltaTime)
	else
		script_busy            = false
		modal_loopanim_counter = modal_loopanim_counter + (1000 * fps.deltaTime)
		if modal_loopanim_counter >= modal_loopanim_duration then
			modal_loopanim_counter = 0
		end
	end
end

function draw()
	if icon_players[modal_current_player] ~= nil then
		icon_players[modal_current_player]:Draw(0, 0)
	end

	local tx_header = font_modal_header:GetText(modal_header_text, false, 99999)

	if modal_current_type == 0 then
		-- Coin
		if modal_tx_coin ~= nil then modal_tx_coin:Draw(0, 0) end
		tx_header:DrawAtAnchor(960, 180, "center")
		local tx_body = font_modal_body:GetText(modal_body_text, false, 99999)
		tx_body:DrawAtAnchor(960, 490, "center")
	else
		-- Others
		if modal_tx[modal_asset_id] ~= nil then modal_tx[modal_asset_id]:Draw(0, 0) end
		tx_header:DrawAtAnchor(960, 180, "center")

		if modal_current_type == 1 then
			-- Character
			if modal_current_visual ~= nil then
				modal_current_visual:Draw(0, 260)
			end
			local tx_body = font_modal_body:GetText(modal_body_text, false, 99999)
			tx_body:DrawAtAnchor(960, 390, "center")

		elseif modal_current_type == 2 then
			-- Puchichara
			if modal_current_info ~= nil and modal_current_info.tx ~= nil then
				modal_current_info.tx:DrawAtAnchor(960, 490, "center")
			end
			local tx_body = font_modal_body:GetText(modal_body_text, false, 99999)
			tx_body:DrawAtAnchor(960, 790, "center")

		elseif modal_current_type == 3 then
			-- Nameplate title
			local tx_plate = font_modal_plate:GetText(modal_body_text, false, 99999, modal_body_fg, modal_body_bg)
			-- modal_current_visual is a CNamePlate instance that exposes DrawTitlePlate
			if modal_current_visual ~= nil then
				modal_current_visual:DrawTitlePlate(
					960, 490, 255,
					modal_current_info.Value.nameplateInfo.iType,
					tx_plate,
					modal_rarity_lang_int,
					modal_current_info.Key)
			end

		elseif modal_current_type == 4 then
			-- Song
			if modal_preimage_ref ~= nil then
				modal_preimage_ref:DrawAtAnchor(960, 490, "center")
			end
			local tx_body = font_modal_body:GetText(modal_body_text, false, 99999)
			tx_body:DrawAtAnchor(960, 790, "center")

		else
			local tx_body = font_modal_body:GetText(modal_body_text, false, 99999)
			tx_body:DrawAtAnchor(960, 490, "center")
		end
	end
end

function afterSongEnum() end
function onDestroy() end

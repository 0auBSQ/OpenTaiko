-- Modal ROActivity
-- Ported from Modules/Modal/Script.lua to the new Lua API.
-- Textures and sounds are loaded from the original Modules/Modal/ location.

local TEXTURES_DIR = "Textures/"
local SOUNDS_DIR   = "Sounds/"

-- Target size for song preimages (both the per-song loaded image and the default fallback)
local PREIMAGE_W = 400
local PREIMAGE_H = 400

local function scalePreimage(tex)
	if tex == nil then return end
	local w = tex.Width
	local h = tex.Height
	if w <= 0 or h <= 0 then return end
	tex:SetScale(math.min(PREIMAGE_W / w, PREIMAGE_H / h), math.min(PREIMAGE_W / w, PREIMAGE_H / h))
end

-- Current modal state
local modal_current_type   = 0
local modal_current_rarity = 1
local modal_current_player = 1
local modal_current_info   = nil

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
local modal_loopanim_duration = 1000
local modal_loopanim_counter  = 0

-- Song modal: per-modal preimage (disposed on deactivate) and persistent default fallback
local modal_preimage_ref     = nil
local modal_preimage_default = nil

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

	modal_tx_coin        = TEXTURE:CreateTexture(TEXTURES_DIR .. "Coin.png")
	modal_sfx_coin       = SOUND:CreateSFX(SOUNDS_DIR .. "Coin.ogg")
	modal_preimage_default = TEXTURE:CreateTexture(TEXTURES_DIR .. "preimage.png")
	scalePreimage(modal_preimage_default)

	-- glyph-composed (bounded per-character cache): the header/body strings change per modal, so per-string
	-- textures at 84px were a large leak. The plate keeps GetText: DrawTitlePlate consumes a texture object.
	font_modal_header = TEXT:CreateGlyphCached(84, "regular")
	font_modal_body   = TEXT:CreateGlyphCached(84, "regular")
	font_modal_plate  = TEXT:Create(16, "regular")

	modal_body_fg = COLOR:CreateColorFromRGBA(0, 0, 0, 255)
	modal_body_bg = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

-- modal_asset_informations by type:
--   0 (Coin)       : coin amount (number)
--   1 (Character)  : LuaCharacter
--   2 (Puchichara) : CPuchichara (legacy)
--   3 (Nameplate)  : LuaNameplateInfo
--   4 (Song)       : LuaSongNode
function activate(player, rarity, modal_type, modal_asset_informations, modal_asset_secondary)
	local header_str = ""
	local body_str   = ""

	modal_current_type   = modal_type
	modal_current_rarity = rarity
	modal_current_player = player
	modal_current_info   = modal_asset_informations

	modal_counter          = 0
	modal_loopanim_counter = 0

	if modal_type == 0 then
		-- Coin
		modal_current_rarity = 1
		header_str = LANG:GetString("MODAL_TITLE_COIN")
		body_str   = LANG:GetString("MODAL_MESSAGE_COIN", tostring(modal_asset_informations), tostring(modal_asset_secondary))
		modal_sfx_coin:Play()

	elseif modal_type == 1 then
		-- Character (LuaCharacter)
		header_str = LANG:GetString("MODAL_TITLE_CHARA")
		body_str   = modal_current_info.DisplayName
		modal_current_info:LoadAnimation(CHARACTER.ANIM_RENDER)

	elseif modal_type == 2 then
		-- Puchichara (LuaPuchichara)
		header_str = LANG:GetString("MODAL_TITLE_PUCHI")
		body_str   = modal_current_info.Name

	elseif modal_type == 3 then
		-- Nameplate (LuaNameplateInfo)
		header_str = LANG:GetString("MODAL_TITLE_NAMEPLATE")
		body_str   = modal_current_info.Title
		modal_rarity_lang_int = rarity_lang_int_map[modal_current_info.Rarity] or 0

	elseif modal_type == 4 then
		-- Song (LuaSongNode)
		header_str     = LANG:GetString("MODAL_TITLE_SONG")
		body_str       = modal_current_info.Title or "??? (Not found)"
		modal_preimage_ref = modal_current_info:GetPreimage()
		scalePreimage(modal_preimage_ref)
	end

	modal_header_text = header_str
	modal_body_text   = body_str

	modal_asset_id = math.max(1, math.min(5, modal_current_rarity + 1))

	if modal_type ~= 0 then
		modal_sfx[modal_asset_id]:Play()
	end
end

function deactivate()
	-- Dispose the owned LuaCharacter when the character modal closes
	if modal_current_type == 1 and modal_current_info ~= nil then
		modal_current_info:DisposeAnimation(CHARACTER.ANIM_RENDER)
		modal_current_info:Dispose()
	end
	-- Dispose the per-song preimage texture to avoid leaking
	if modal_preimage_ref ~= nil then
		modal_preimage_ref:Dispose()
		modal_preimage_ref = nil
	end
end

function update()
	if modal_counter <= modal_duration then
		modal_counter = modal_counter + (1000 * fps.deltaTime)
	else
		modal_loopanim_counter = modal_loopanim_counter + (1000 * fps.deltaTime)
		if modal_loopanim_counter >= modal_loopanim_duration then
			modal_loopanim_counter = 0
		end

		if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
			DEACTIVATE()
		end
	end
end

function draw()
	if icon_players[modal_current_player] ~= nil then
		icon_players[modal_current_player]:Draw(0, 0)
	end

	if modal_current_type == 0 then
		-- Coin
		if modal_tx_coin ~= nil then modal_tx_coin:Draw(0, 0) end
		font_modal_header:Draw(modal_header_text, 960, 180, nil, nil, 1, 1, 0, "center")
		font_modal_body:Draw(modal_body_text, 960, 490, nil, nil, 1, 1, 0, "center")
	else
		-- Others
		if modal_tx[modal_asset_id] ~= nil then modal_tx[modal_asset_id]:Draw(0, 0) end
		font_modal_header:Draw(modal_header_text, 960, 180, nil, nil, 1, 1, 0, "center")

		if modal_current_type == 1 then
			-- Character (LuaCharacter)
			if modal_current_info ~= nil then
				modal_current_info:Update(CHARACTER.ANIM_RENDER, true)
				modal_current_info:DrawAtAnchor(960, 390, CHARACTER.ANIM_RENDER, "center")
			end
			font_modal_body:Draw(modal_body_text, 960, 490, nil, nil, 1, 1, 0, "center")

		elseif modal_current_type == 2 then
			-- Puchichara (legacy)
			if modal_current_info ~= nil and modal_current_info.tx ~= nil then
				modal_current_info.tx:DrawAtAnchor(960, 490, "center")
			end
			font_modal_body:Draw(modal_body_text, 960, 790, nil, nil, 1, 1, 0, "center")

		elseif modal_current_type == 3 then
			-- Nameplate (LuaNameplateInfo)
			local tx_plate = font_modal_plate:GetText(modal_body_text, false, 99999, modal_body_fg, modal_body_bg)
			NAMEPLATE:DrawTitlePlate(
				960, 490, 255,
				modal_current_info.Type,
				tx_plate,
				modal_rarity_lang_int,
				modal_current_info.Id)

		elseif modal_current_type == 4 then
			-- Song
			local preimage = modal_preimage_ref or modal_preimage_default
			if preimage ~= nil then
				preimage:DrawAtAnchor(960, 490, "center")
			end
			font_modal_body:Draw(modal_body_text, 960, 790, nil, nil, 1, 1, 0, "center")

		else
			font_modal_body:Draw(modal_body_text, 960, 490, nil, nil, 1, 1, 0, "center")
		end
	end
end

function afterSongEnum() end

function onDestroy()
	if modal_preimage_default ~= nil then
		modal_preimage_default:Dispose()
		modal_preimage_default = nil
	end
end

import ('System.Drawing')

-- Modal info
local modal_current_type = 0
local modal_current_rarity = 1
local modal_current_player = 1
local modal_current_info = nil
local modal_current_visual = nil

-- Modal graphics
local icon_players = { }
local modal_tx = { }
local modal_tx_coin = nil
local ttk_modal_header = nil
local ttk_modal_body = nil

-- Modal sounds
local modal_sfx = { }
local modal_sfx_coin = nil

-- Fonts
local font_modal_header = nil
local font_modal_body = nil
local font_modal_plate = nil

-- Modal counter
local modal_duration = 2000
local modal_counter = 0
local script_busy = false

-- After the item is revealed, a circle glow or smth like that?
local modal_loopanim_duration = 1000
local modal_loopanim_counter = 0

-- Tmp (until new format)
local modal_asset_id = 0

function isAnimationFinished()
	return not script_busy
end

-- modal_asset_informations: Character object, Coin count, Nameplate unlockable whole object, etc... having all the necessary information
-- modal_asset_visual_references: Character textures table, Song preimage (?) or supporting visuals, might be null for some modal types
function registerNewModal(player, rarity, modal_type, modal_asset_informations, modal_asset_visual_references)
	local _modal_header = ""
	local _modal_body = ""
	modal_current_type = modal_type
	modal_current_rarity = rarity
	modal_current_player = player
	modal_current_info = modal_asset_informations
	modal_current_visual = modal_asset_visual_references
	modal_counter = 0
	modal_loopanim_counter = 0
	script_busy = true

	if modal_type == 0 then
	-- Coin
	modal_current_rarity = 1
	_modal_header = getLocalizedString("MODAL_TITLE_COIN")
	_modal_body = getLocalizedString("MODAL_MESSAGE_COIN", tostring(modal_asset_informations), tostring(modal_asset_visual_references)) -- 0: Delta coin, 1: Total coin
	debugLog(_modal_body)

	modal_sfx_coin:PlayStart()

	elseif modal_type == 1 then
	-- Character
	-- > modal_asset_informations: CCharacter
	-- > modal_asset_visual_references: CTexture?
	_modal_header = getLocalizedString("MODAL_TITLE_CHARA")
	_modal_body = modal_current_info.metadata:tGetName()

	elseif modal_type == 2 then
	-- Puchichara
	-- > modal_asset_informations: CPuchichara
	-- > modal_asset_visual_references: 
	_modal_header = getLocalizedString("MODAL_TITLE_PUCHI")
	_modal_body = modal_current_info.metadata:tGetName()

	elseif modal_type == 3 then
	-- Title
	-- > modal_asset_informations: NameplateUnlockable
	-- > modal_asset_visual_references: CLuaNamePlateScript
	_modal_header = getLocalizedString("MODAL_TITLE_NAMEPLATE")
	_modal_body = modal_asset_informations.Value.nameplateInfo.cld:GetString("")
	ttk_modal_body = createTitleTextureKey(_modal_body, font_modal_plate, 99999, Color.FromArgb(0,0,0,1), Color.FromArgb(0,0,0,0))

	elseif modal_type == 4 then
	-- Song
	-- > modal_asset_informations: CSongListNode
	-- > modal_asset_visual_references: CTexture (Preimage)
	_modal_header = getLocalizedString("MODAL_TITLE_SONG")
	_modal_body = modal_current_info.ldTitle:GetString("")

	end 

	ttk_modal_header = createTitleTextureKey(_modal_header, font_modal_header, 99999)
	if modal_type ~= 3 then
		ttk_modal_body = createTitleTextureKey(_modal_body, font_modal_body, 99999)
	end

	-- Tmp
	modal_asset_id = math.max(1, math.min(5, modal_current_rarity))

	if modal_type ~= 0 then
		modal_sfx[modal_asset_id]:PlayStart()
	end

end

function loadAssets()
    config = loadConfig("Config.json")

	for i = 1, 5 do 
		icon_players[i] = loadTexture(tostring(i).."P.png")
    end

	-- Tmp, to change with the new structure later
	for i = 0, 4 do
		modal_tx[i + 1] = loadTexture(tostring(i)..".png")
		modal_sfx[i + 1] = loadSound(tostring(i)..".ogg", "soundeffect")
	end
	modal_tx_coin = loadTexture("Coin.png")
	modal_sfx_coin = loadSound("Coin.ogg", "soundeffect")

	font_modal_header = loadFontRenderer(84, "regular")
    font_modal_body = loadFontRenderer(84, "regular")
	font_modal_plate = loadFontRenderer(16, "regular")

end

function update()
	if modal_counter <= modal_duration then
		script_busy = true
		modal_counter = modal_counter + (1000 * fps.deltaTime)
	else
		script_busy = false
		modal_loopanim_counter = modal_loopanim_counter + (1000 * fps.deltaTime)
		if modal_loopanim_counter >= modal_loopanim_duration then
			modal_loopanim_counter = 0
		end
	end




	-- Idea: If button press and not finished, directly set modal_counter to modal_duration and cut the appearing animation?
end

function draw()
	icon_players[modal_current_player]:t2D_DisplayImage(0, 0)

	tx_header = getTextTex(ttk_modal_header, false, false)

	if modal_current_type == 0 then
		-- Coin
		modal_tx_coin:t2D_DisplayImage(0, 0)
		
		tx_header:t2D_DisplayImage_AnchorCenter(960,180)
		tx_body = getTextTex(ttk_modal_body, false, false)
		tx_body:t2D_DisplayImage_AnchorCenter(960,490)
	else
		-- Others
		modal_tx[modal_asset_id]:t2D_DisplayImage(0, 0)

		tx_header:t2D_DisplayImage_AnchorCenter(960,180)

		if modal_current_type == 1 then
			-- Character
			if modal_current_visual ~= nil then
				modal_current_visual:t2D_DisplayImage(0,260)
			end
			tx_body = getTextTex(ttk_modal_body, false, false)
			tx_body:t2D_DisplayImage_AnchorCenter(960,390)
		elseif modal_current_type == 2 then
			-- Puchichara
			if modal_current_info.tx ~= nil then
				modal_current_info.tx:t2D_DisplayImage_AnchorCenter(960,490)
			end
			tx_body = getTextTex(ttk_modal_body, false, false)
			tx_body:t2D_DisplayImage_AnchorCenter(960,790)
		elseif modal_current_type == 3 then
			-- Nameplate Title
			tx_title = getTextTex(ttk_modal_body, false, false)
			modal_current_visual:DrawTitlePlate(960, 490, 255, modal_current_info.Value.nameplateInfo.iType, tx_title, modal_current_rarity, modal_current_info.Key)
		elseif modal_current_type == 4 then
			-- Song
			if modal_current_visual ~= nil then
				modal_current_visual:t2D_DisplayImage_AnchorCenter(960,490)
			end
			tx_body = getTextTex(ttk_modal_body, false, false)
			tx_body:t2D_DisplayImage_AnchorCenter(960,790)
		else
			-- Custom modals for custom unlockables in the future??
			tx_body = getTextTex(ttk_modal_body, false, false)
			tx_body:t2D_DisplayImage_AnchorCenter(960,490)
		end 
	end

end

---@diagnostic disable: undefined-global, undefined-field, need-check-nil, lowercase-global
-- Modal ROActivity: unlock and reward pop-ups, from the engine after a result screen and from Lua stages.

local NavInput = require("NavInput")
local Fx = require("modal_fx")
local Coins = require("modal_coins")
local Chest = require("modal_chest")
local Card = require("modal_card")

-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local FONTS = Layout.fonts
local COLORS = Layout.colors

local TEX, SND = "Textures/", "Sounds/"
local ANIM_SOUNDS = { "slide", "coin", "coin_alt", "jingle", "pay", "puff", "cushion", "chest", "latch", "sparkle", "flash", "reveal", "close",
                      "box_land", "box_rustle", "box_open" }
local ANIM_TEXTURES = { "piggy", "cushion", "smoke", "dust", "wheel", "glow", "star", "spark", "web", "web2",
                        "case_front", "case_side", "case_top", "rainbow" }

-- the asset bag every flow draws from
local A = { tex = {}, sfx = {}, fanfare = {}, icons = {} }

function A:play(name)
    local s = self.sfx[name]
    if s ~= nil then pcall(function() s:Play() end) end
end

-- key: one of Fx.RARITIES (one fanfare per rarity)
function A:playFanfare(key)
    local s = self.fanfare[key] or self.fanfare.common
    if s ~= nil then pcall(function() s:Play() end) end
end

local coins, chest, card
local state = nil            -- "coins" | "chest" | "card"
local player = 1
local pending = nil          -- the item waiting behind the chest reveal
local nav = NavInput.p[""]
local age = 0                -- seconds since activate: the press that opened the modal must not also skip it
local INPUT_GRACE = Animation.input_grace_s

function onStart()
    for i = 1, 5 do A.icons[i] = TEXTURE:CreateTexture(TEX .. i .. "P.png") end
    A.tex.panel = TEXTURE:CreateTexture(TEX .. "0.png")          -- the one panel; the card tints it per rarity
    for _, n in ipairs(ANIM_TEXTURES) do A.tex[n] = TEXTURE:CreateTexture(TEX .. "Anim/" .. n .. ".png") end
    for _, r in ipairs(Fx.RARITIES) do
        A.tex["chest_" .. r] = TEXTURE:CreateTexture(TEX .. "Anim/chest_" .. r .. ".png")
        A.fanfare[r] = SOUND:CreateSFX(SND .. "Anim/fanfare_" .. r .. ".ogg")
        if r ~= "poor" then A.sfx["open_" .. r] = SOUND:CreateSFX(SND .. "Anim/open_" .. r .. ".ogg") end
    end
    for _, n in ipairs(ANIM_SOUNDS) do A.sfx[n] = SOUND:CreateSFX(SND .. "Anim/" .. n .. ".ogg") end
    A.tex.preimage = TEXTURE:CreateTexture(TEX .. "preimage.png")
    A.fillCv = Fx.makeFill()

    -- glyph fonts, all without a style token: CoinBox creates its own the same way and one script
    -- must not mix the two forms (the NLua params cache)
    A.fontHeader = TEXT:CreateGlyphCached(FONTS.header_size)
    A.fontName = TEXT:CreateGlyphCached(FONTS.name_size)
    A.fontSmall = TEXT:CreateGlyphCached(FONTS.small_size)
    A.fontPlate = TEXT:Create(FONTS.plate_size, "regular")
    A.colPlateFg = Config.color(COLORS.plate_text_color)
    A.colPlateBg = Config.color(COLORS.plate_back_color)
    A.colInk = Config.color(COLORS.ink_color)
    A.colInkOutline = Config.color(COLORS.ink_outline_color)
    A.colPillText = Config.color(COLORS.pill_text_color)
    A.colPillOutline = Config.color(COLORS.pill_outline_color)

    coins, chest, card = Coins.new(A), Chest.new(A), Card.new(A)
end

-- the item's rarity string: its own field, or the character database entry for a LuaCharacter
local function itemRarity(modal_type, info)
    local r
    pcall(function()
        if modal_type == 1 then
            local entry = CHARACTERLIST ~= nil and CHARACTERLIST:GetByName(info.FolderName) or nil
            r = entry ~= nil and entry.Rarity or nil
        else
            r = info.Rarity
        end
    end)
    return r
end

-- type 0 coins: info = amount (negative: a payment), secondary = the balance after it
-- type 1 character: info = LuaCharacter (owned: disposed when the modal closes)
-- type 2 puchichara: LuaPuchichara; 3 nameplate: LuaNameplateInfo; 4 song: LuaSongNode
-- type 5 custom: info = shown name, secondary = rarity name, picture = shared texture key, header = card title
-- rarity: modal int 0..4; the item's own Rarity string (poor to mythical) wins when known
-- (characters via CHARACTERLIST); for types 1..4 a rarity name in secondary overrides it
function activate(player1to, rarity, modal_type, info, secondary, picture, header)
    player = math.max(0, math.min(5, math.floor(tonumber(player1to) or 1)))
    nav = NavInput.p[player] or NavInput.p[""]
    rarity = math.floor(tonumber(rarity) or 0)
    age = 0
    if modal_type == 0 then
        coins:start(info, secondary)
        state = "coins"
    else
        local key = Fx.rarityKey(rarity, itemRarity(modal_type, info))
        if type(secondary) == "string" and Fx.RARITY_COLOR[secondary:lower()] then key = secondary:lower() end
        pending = { kind = modal_type, rarity = rarity, key = key, info = info, picture = picture, header = header }
        -- character menu animation loads now (behind the reveal, not on the card's pop-in frame)
        if modal_type == 1 and info ~= nil then pcall(function() info:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end) end
        chest:start(key)
        state = "chest"
    end
end

local function close()
    if state == "coins" then coins:stop()
    elseif state == "chest" then chest:stop()
    elseif state == "card" then card:stop() end
    state, pending = nil, nil
    DEACTIVATE()
end

function deactivate()
    -- a stage may drop the modal mid-sequence: release what the card still owns
    if state == "card" then card:stop() end
    if state == "chest" and pending ~= nil and pending.kind == 1 and pending.info ~= nil then
        pcall(function() pending.info:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end)
        pcall(function() pending.info:Dispose() end)
    end
    state, pending = nil, nil
end

function update()
    if state == nil then return end
    local dt = math.min(fps.deltaTime, 0.1)
    age = age + dt
    local decide = age > INPUT_GRACE and nav.decide()
    if state == "coins" then
        if coins:update(dt, decide) then close() end
    elseif state == "chest" then
        if chest:update(dt, decide) then
            chest:stop()
            card:start(pending.kind, pending.rarity, pending.key, pending.info, pending.picture, pending.header)
            state = "card"
        end
    elseif state == "card" then
        if card:update(dt, decide) then close() end
    end
end

function draw()
    if state == nil then return end
    if state == "coins" then coins:draw()
    elseif state == "chest" then chest:draw()
    elseif state == "card" then card:draw() end
    local icon = A.icons[player]
    if icon ~= nil then icon:Draw(0, 0) end
end

function afterSongEnum() end

function onDestroy()
    if coins then coins:dispose() end
    if card then card:dispose() end
    if A.fillCv then pcall(function() A.fillCv:Dispose() end) end
end

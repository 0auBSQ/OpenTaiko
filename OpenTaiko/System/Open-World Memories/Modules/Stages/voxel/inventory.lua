---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- voxel/inventory.lua — CREATIVE inventory for the voxel stage.
--
--   * a 9-slot HOTBAR of blocks — blocks are infinite, there are no item counts;
--   * pressing E opens a PALETTE of every placeable block. Left-click a block to pick it up,
--     then left-click a hotbar slot to drop it there (right-click a slot clears it);
--   * blocks place from the selected hotbar slot; mining removes blocks instantly (see Script.lua).
--
-- No crafting, smelting, tools or ingots: every world block is simply an item you can put on the
-- bar and place. Script.lua wires world breaking/placing and calls Inv.selected() for the held block.

local floor, min, max = math.floor, math.min, math.max

local Inv = {}

local NHOT = 9
local ctx                        -- injected: { blocks, BLOCK_NAMES, texPix, iconTexId }

-- ── state ──────────────────────────────────────────────────────────────────────────────────────
Inv.hotbar = {}                  -- [1..9] = blockId or nil
Inv.heldIdx = 1                  -- selected hotbar slot (1..9)
Inv.openMode = nil               -- nil | "palette"
Inv.cursor = nil                 -- a blockId picked up from the palette (follows the mouse)

local icons = {}                 -- [blockId] = LuaCanvas
local palette = {}               -- ordered list of every placeable blockId
Inv.items = {}                   -- [blockId] = { block = blockId, name } — Script reads item.block

-- ── icons ──────────────────────────────────────────────────────────────────────────────────────
local function iconFromPixels(t)
    local c = CANVAS:CreateCanvas(16, 16)
    c:BlitPacked(t, 256)
    return c
end
local function drawIcon(id, x, y, sc)
    local ic = icons[id]
    if ic then ic:SetColor(1, 1, 1); ic:SetOpacity(1); ic:SetScale(sc, sc); ic:Draw(x, y) end
end

-- ── setup ──────────────────────────────────────────────────────────────────────────────────────
function Inv.init(c)
    ctx = c
    local B = ctx.blocks
    -- every named block (bar Air) is placeable in creative; icon = the block's face texture
    local ids = {}
    for id in pairs(ctx.BLOCK_NAMES) do if id ~= 0 then ids[#ids + 1] = id end end
    table.sort(ids)
    for _, id in ipairs(ids) do
        Inv.items[id] = { block = id, name = ctx.BLOCK_NAMES[id] }
        local px = ctx.texPix[ctx.iconTexId(id)]
        if px then icons[id] = iconFromPixels(px) end
        palette[#palette + 1] = id
    end
    -- a handy default bar
    local defaults = { B.GRASS, B.DIRT, B.STONE, B.WOOD, B.PLANKS, B.GLASS, B.SAND, B.TORCH, B.FURNACE }
    for i = 1, NHOT do Inv.hotbar[i] = defaults[i] end
end

-- ── queries (used by Script.lua) ────────────────────────────────────────────────────────────────
-- Returns (id, count, item) of the selected hotbar block. Count is a constant 1 (blocks are
-- infinite); item.block is the placeable block id.
function Inv.selected()
    local id = Inv.hotbar[Inv.heldIdx]
    if not id then return nil, 0, nil end
    return id, 1, Inv.items[id]
end

-- hotbar selection: wheel + number keys (call when the palette is closed)
function Inv.hotbarInput()
    local _, sdy = INPUT:GetScrollDelta()
    if sdy ~= 0 then
        if sdy > 0 then Inv.heldIdx = Inv.heldIdx % NHOT + 1 else Inv.heldIdx = (Inv.heldIdx - 2) % NHOT + 1 end
    end
    for i = 1, NHOT do
        if INPUT:KeyboardPressed("D" .. i) then Inv.heldIdx = i end
    end
end

function Inv.openInv() Inv.openMode = "palette" end
function Inv.close() Inv.openMode = nil; Inv.cursor = nil end

-- ── palette / hotbar layout (fixed 1920×1080 logical canvas) ────────────────────────────────────
local SLOT, GAP, COLS = 72, 12, 8
local STEP = SLOT + GAP
local GRID_X = floor((1920 - (COLS * STEP - GAP)) / 2)          -- centred block grid
local GRID_Y = 214
local HOT_X = floor((1920 - (NHOT * STEP - GAP)) / 2)           -- centred hotbar row

local function paletteRows() return math.ceil(#palette / COLS) end
local function hotbarPanelY() return GRID_Y + paletteRows() * STEP + 46 end

local function inRect(mx, my, x, y, s) return mx >= x and mx < x + s and my >= y and my < y + s end
local function paletteXY(i) local k = i - 1; return GRID_X + (k % COLS) * STEP, GRID_Y + floor(k / COLS) * STEP end
local function hotXY(i) return HOT_X + (i - 1) * STEP, hotbarPanelY() end

-- ── palette interaction ─────────────────────────────────────────────────────────────────────────
function Inv.updateUI()
    if Inv.openMode ~= "palette" then return end
    local mx, my = INPUT:GetMouseXY()
    local left, right = INPUT:MousePressed("Left"), INPUT:MousePressed("Right")
    if not (left or right) then return end
    -- pick a block from the grid onto the cursor
    for i = 1, #palette do
        local x, y = paletteXY(i)
        if inRect(mx, my, x, y, SLOT) then
            if left then Inv.cursor = palette[i]
            elseif right then Inv.cursor = nil end
            return
        end
    end
    -- drop the cursor into (or clear) a hotbar slot
    for i = 1, NHOT do
        local x, y = hotXY(i)
        if inRect(mx, my, x, y, SLOT) then
            if left then
                if Inv.cursor then Inv.hotbar[i] = Inv.cursor; Inv.cursor = nil end
                Inv.heldIdx = i
            elseif right then
                Inv.hotbar[i] = nil
            end
            return
        end
    end
    -- click in empty space cancels the held block
    if left then Inv.cursor = nil end
end

-- ── drawing ───────────────────────────────────────────────────────────────────────────────────
local function txt(font, str, r, g, b)
    return font:GetText(str, false, 1800,
        COLOR:CreateColorFromRGBA(r or 255, g or 255, b or 255, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255))
end
local function slotBg(dim, x, y, hot)
    dim:SetColor(0.22, 0.225, 0.27); dim:SetOpacity(1.0); dim:SetScale(SLOT / 2, SLOT / 2); dim:Draw(x, y)
    dim:SetColor(hot and 0.20 or 0.13, hot and 0.19 or 0.135, hot and 0.10 or 0.17)
    dim:SetScale((SLOT - 6) / 2, (SLOT - 6) / 2); dim:Draw(x + 3, y + 3)
end

function Inv.draw(dim, fonts, SCREEN_W, SCREEN_H)
    if Inv.openMode ~= "palette" then return end
    dim:SetColor(0, 0, 0); dim:SetOpacity(0.55); dim:SetScale(SCREEN_W / 2, SCREEN_H / 2); dim:Draw(0, 0)
    local rows = paletteRows()
    local panelW = COLS * STEP - GAP + 80
    local panelX = floor((SCREEN_W - panelW) / 2)
    local panelTop = GRID_Y - 70
    local panelH = (hotbarPanelY() + SLOT + 40) - panelTop
    dim:SetColor(0.10, 0.105, 0.13); dim:SetOpacity(0.94); dim:SetScale(panelW / 2, panelH / 2); dim:Draw(panelX, panelTop)
    txt(fonts.mid, "BLOCKS", 245, 240, 230):Draw(GRID_X, GRID_Y - 52)
    -- block grid
    local iconSc = (SLOT - 8) / 16
    for i = 1, #palette do
        local x, y = paletteXY(i)
        slotBg(dim, x, y, false)
        drawIcon(palette[i], x + 4, y + 4, iconSc)
    end
    -- hotbar row (drop target)
    txt(fonts.small, "Your bar — drag blocks here", 205, 210, 220):Draw(HOT_X, hotbarPanelY() - 32)
    for i = 1, NHOT do
        local x, y = hotXY(i)
        slotBg(dim, x, y, i == Inv.heldIdx)
        if Inv.hotbar[i] then drawIcon(Inv.hotbar[i], x + 4, y + 4, iconSc) end
        if i == Inv.heldIdx then
            dim:SetColor(0.95, 0.85, 0.4); dim:SetOpacity(0.9); dim:SetScale(SLOT / 2, 3 / 2); dim:Draw(x, y + SLOT - 2)
        end
    end
    -- held cursor follows the mouse
    local mx, my = INPUT:GetMouseXY()
    if Inv.cursor then drawIcon(Inv.cursor, mx - SLOT / 2 + 4, my - SLOT / 2 + 4, iconSc) end
    -- hovered block name
    for i = 1, #palette do
        local x, y = paletteXY(i)
        if inRect(mx, my, x, y, SLOT) then
            local nm = Inv.items[palette[i]] and Inv.items[palette[i]].name
            if nm then txt(fonts.small, nm, 255, 245, 200):Draw(mx + 18, my + 14) end
            break
        end
    end
    txt(fonts.small, "left-click a block, then left-click a bar slot   right-click a slot: clear   E/Esc: close",
        205, 210, 220):DrawAtAnchor(SCREEN_W / 2, hotbarPanelY() + SLOT + 22, "center")
end

-- the in-game hotbar (always visible): 9 slots with block icons + selection ring
function Inv.drawHotbar(dim, fonts, SCREEN_W, SCREEN_H)
    local x0 = SCREEN_W / 2 - (NHOT * (SLOT + 6)) / 2
    local y0 = SCREEN_H - SLOT - 24
    local iconSc = (SLOT - 8) / 16
    for i = 1, NHOT do
        local x = x0 + (i - 1) * (SLOT + 6)
        local sel = (i == Inv.heldIdx)
        dim:SetColor(sel and 0.95 or 0.25, sel and 0.85 or 0.26, sel and 0.40 or 0.30); dim:SetOpacity(sel and 0.95 or 0.75)
        dim:SetScale(SLOT / 2, SLOT / 2); dim:Draw(x, y0)
        dim:SetColor(0.10, 0.105, 0.13); dim:SetOpacity(0.85)
        dim:SetScale((SLOT - 6) / 2, (SLOT - 6) / 2); dim:Draw(x + 3, y0 + 3)
        if Inv.hotbar[i] then drawIcon(Inv.hotbar[i], x + 4, y0 + 4, iconSc) end
    end
    local id = Inv.hotbar[Inv.heldIdx]
    if id and Inv.items[id] then
        txt(fonts.small, Inv.items[id].name, 240, 240, 245):DrawAtAnchor(SCREEN_W / 2, y0 - 24, "center")
    end
end

return Inv

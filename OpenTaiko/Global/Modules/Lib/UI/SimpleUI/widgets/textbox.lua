---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
-- SimpleUI textbox: a rounded field you click (or focus + Decide) to type into. Wraps the engine text input.
--   ui:textbox{ x=, y=, w=, value="", placeholder="name...", maxLen=24, onChange=fn, onSubmit=fn }

local Widget = require("UI.SimpleUI.widget")
local U      = require("UI.SimpleUI.util")

local TextBox = setmetatable({}, { __index = Widget })
TextBox.__index = TextBox

function TextBox.new(o)
    o = o or {}
    o.w = o.w or 440; o.h = o.h or 76
    local self = Widget.new(o)
    setmetatable(self, TextBox)
    self.value = o.value or ""
    self.placeholder = o.placeholder or ""
    self.maxLen = o.maxLen or 64
    self.padX = o.padX or 24
    self.noPop = true   -- the field stays still while typing: the text is drawn unscaled
    return self
end

function TextBox:restyle()
    self:resolveStyle()
    local c = self.eff.colors
    -- a slightly sunken field: face uses surface2 -> surface (inverted gives a soft inset feel)
    self:bakeBody(c.surface2, c.surface)
    self:bakeRing()
end

function TextBox:onCapturing(silence)
    if not silence then self:playSfx("click") end
    self.mgr:releaseKeys(self)
    self._ti = nil
end
function TextBox:onEndCapturing(silence)
    if not silence then self:playSfx("cancel") end
    self.mgr:releaseKeys(self)
    self._ti = nil
end

function TextBox:onActivate()
    if self.capturing then return end
    self:setCapturing(true)
    self._ti = INPUT:CreateTextInput(self.value or "", self.maxLen)
    self._lastText = self.value or ""
    self.mgr:captureKeys(self)
end

function TextBox:update(ctx)
    Widget.update(self, ctx)   -- tick scale/highlight tweens
    if self.capturing then
        local submitted = self._ti:Update()
        local t = self._ti.Text or ""
        if t ~= self._lastText then
            self._lastText = t
            if self.onChange then self.onChange(t, self) end
        end
        if submitted then
            self.value = t; self:setCapturing(false, true)
            if not (self.onSubmit and self.onSubmit(self.value, self)) then
                self:playSfx("decide")
            end
        elseif INPUT:KeyboardPressed("Escape") then
            self.value = t; self:setCapturing(false)
        elseif ctx.mPressed and not self:hitTest(ctx.mx, ctx.my) then
            self.value = t; self:setCapturing(false)
        end
    end
end

function TextBox:drawContent(cx, cy, s)
    local c = self.eff.colors
    local sz = self.eff.font.label
    local x = math.floor(self.x + self.padX)
    local y = math.floor(self.y + self.h * 0.5 - self.mgr:textHeight(sz) * 0.5 + self.mgr:textNudge(sz))
    -- glyph-atlas draw (bounded glyph cache + correct advances) — typing can't flood VRAM, spacing is right
    local textColor = self._hiCapCur > 0.01
        and U.lerpColor(c.text, c.primary2, self._hiCapCur)
        or c.text
    local str, col
    if self.capturing then str, col = (self._ti.DisplayText or ""), textColor
    elseif self.value ~= nil and self.value ~= "" then str, col = self.value, textColor
    else str, col = self.placeholder, c.textDisabled end
    if str == "" then return end
    -- left-clip: if the text is wider than the field, show the longest trailing suffix that fits
    local avail = self.w - 2 * self.padX
    local w = self.mgr:measureText(sz, str)
    if w > avail then
        -- clip whole UTF-8 characters from the left, never splitting a multi-byte sequence
        local chars = self.mgr:utf8chars(str)
        local i = 1
        while i < #chars and w > avail do w = w - self.mgr:measureText(sz, chars[i]); i = i + 1 end
        str = table.concat(chars, "", i)
    end
    self.mgr:drawText(sz, str, x, y, col)
end

return TextBox

-- Line handling shared by the floor entrance dialogs and the VN (VN.Runner); stage directions stay with the caller.

local I18N   = require("i18n")
local Runner = require("VN.Runner")

local M = {}

function M.tr(s) return (s ~= nil) and I18N.tr(s) or nil end

-- a line plays unless its guard says otherwise; an erroring guard counts as false
function M.passes(line, ctx) return Runner.passes(line, ctx) end

-- starts line on the box: name resolved (defaultName covers `who`), text and choice labels localized,
-- <player name> substituted; choice values are not localized. carry = the previous prose line, kept on
-- screen under a bare-choice line.
function M.startLine(dlg, line, ctx, defaultName, carry)
    Runner.start(dlg, line, ctx, { tr = M.tr, speakerName = defaultName }, carry)
end

-- after the box reports "done": records the picked value on ctx and splices that branch's lines after the current one
function M.resolveChoice(dlg, lines, idx, ctx)
    Runner.resolveChoice(lines, idx, ctx, dlg.result)
end

-- { pick = { { lines = {...}, weight = n }, ... } }: one option by weight (default 1), never the same twice
-- in a row; its lines are spliced in after the pick line
function M.resolvePick(lines, idx)
    Runner.resolvePick(lines, idx)
end

return M

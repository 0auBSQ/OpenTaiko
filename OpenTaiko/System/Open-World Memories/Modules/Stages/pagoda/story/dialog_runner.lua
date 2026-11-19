-- line handling for entrance dialogs and the VN (VN.Runner); stage directions stay with the caller

local I18N   = require("i18n")
local Runner = require("VN.Runner")

local M = {}

function M.tr(s) return (s ~= nil) and I18N.tr(s) or nil end

-- no guard: true; guard error: false
function M.passes(line, ctx) return Runner.passes(line, ctx) end

-- localizes text and choice labels (not values); defaultName resolves `who`
-- carry: previous prose line, kept on screen under a bare-choice line
function M.startLine(dlg, line, ctx, defaultName, carry)
    Runner.start(dlg, line, ctx, { tr = M.tr, speakerName = defaultName }, carry)
end

-- after "done": picked value -> ctx, branch lines spliced after idx
function M.resolveChoice(dlg, lines, idx, ctx)
    Runner.resolveChoice(lines, idx, ctx, dlg.result)
end

-- { pick = { { lines, weight = 1 }, ... } }: weighted, no repeat in a row; lines spliced after idx
function M.resolvePick(lines, idx)
    Runner.resolvePick(lines, idx)
end

return M

-- German pagoda text, keyed by the exact English the code translates.

local merged = {}
for _, name in ipairs({ "ui", "story", "entrance", "result", "cutscenes", "items", "credits", "small_talk", "bestiary" }) do
    local ok, part = pcall(require, "lang/de/" .. name)
    if ok and type(part) == "table" then
        for k, v in pairs(part) do
            if v ~= nil and v ~= "" then merged[k] = v end
        end
    end
end
return merged

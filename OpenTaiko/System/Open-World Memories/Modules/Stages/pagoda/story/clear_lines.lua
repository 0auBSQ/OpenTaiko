-- floor clear lines (floor_result.lua); line format: story/entrance.lua

-- rare line weight (others: 1)
local RARE = 0.25

local CLEAR = {
    { lines = { { who = "ume", text = "Hurray! Let’s keep on going!" } } },
    { lines = { { who = "ume", text = "That was rough… You’re so strong!" } } },
    { lines = {
        { who = "ume", text = "… Dinner would be nice, right about now…" },
        { text = "You nod in response. Nevertheless, the two of you keep walking." },
    } },
    { lines = { { who = "ume", text = "Phew… I’m tired, but we can’t stop now!" } } },
    { lines = { { text = "Ume is humming and skipping along happily." } } },
    { lines = { { who = "ume", text = "Alright! That’s great!" } } },
    { lines = { { who = "ume", text = "You’re incredible!" } } },
    { lines = { { text = "Ume stretches, before skipping along." } } },
    { lines = { { who = "ume", text = "… It’s hard not to be scared… Some of them are so creepy…" } } },
    { weight = RARE, lines = {
        { who = "ume", text = "Hoo- Ray! Let’s keep on going!" },
        { who = "you", text = "…???" },
    } },
    { weight = RARE, lines = {
        { who = "ume", text = "dadadada- da daaan!" },
        { text = "That jingle sounds familiar." },
    } },
    { weight = RARE, lines = { { who = "ume", text = "Big victory!!" } } },
    { weight = RARE, lines = {
        { who = "ume", text = "ka BOOM! Boomchikaboom boom boboom booom! Powpowpow boBOOOOM- A… Ah…" },
        { who = "ume", text = "Let’s keep going!" },
    } },
}

-- spoken choice: the picked label is the said line
local function say(...)
    local choices = {}
    for i, label in ipairs({ ... }) do
        choices[i] = { label = label, value = tostring(i) }
    end
    return { text = "", choices = choices }
end

-- [level] entry, else roof (21+), else default; {} skips the dialog
-- entry: line list or variant list (story/entrance.lua)
return {
    default = {
        { pick = CLEAR },
    },

    roof = {},

    -- after a clear with a reward (floors 1-9, 11-19), before the pop-ups
    rewards = {
        { who = "ume", sprite = "normal", react = "surprise", bubble = "exclaim", text = "Oh! The monster left something behind!" },
    },

    -- first clear: light_up event (wait = true holds the line until lit)
    [15] = {
        {
            when = function(ctx) return (ctx.tower or ctx.highest or 0) < 15 end,
            lines = {
                { name = "Together", blip = "blip", text = "WE DID IT!" },
                { event = "light_up", wait = true, text = "As the monster vanishes, the room is shrouded in sunlight, revealing the fully open architecture." },
                say("How pretty. This fresh air is great!", "Interesting.", "i should not have cleared that"),
                { who = "ume", text = "…The scary guys don’t come outside, so maybe they don’t like the light." },
                { who = "ume", text = "If that’s the case, they won’t come back to all the floors we’ve gone through so far with a room full of light in their way, right?" },
                say("If that’s true, that would be great. It’s like a checkpoint."),
                { who = "ume", cheer = true, text = "Checkpoint! Checky checky point!" },
                { text = "Ume starts dancing, singing “Checky-Checky-point”. You don’t really get it, but that’s fine." },
                say("Come on Ume, let’s keep climbing."),
            },
        },
        { lines = { { pick = CLEAR } } },
    },
}

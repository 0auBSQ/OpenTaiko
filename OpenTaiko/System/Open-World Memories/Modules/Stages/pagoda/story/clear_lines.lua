-- Lines said on the encounter screen after a floor clear (floor_result.lua).
-- Line format (story/entrance.lua):
--   { who = "ume", sprite = "clear", text = "..." }   (sprite: normal, scared, brave, clear, run, lost, fail)
--   { name = "Needle", text = "..." }                  (anybody else)
--   { who = "you", text = "..." }                      (the player)
--   { pick = pool }                                    (one option of the pool, drawn at random)
-- A floor plays its own entry, else `roof` (21+) or `default`; an entry may be a variant list as in
-- story/entrance.lua; an empty list skips the dialog.
-- Extra line fields: event = "light_up" (with wait = true) lights the level-15 room and holds the line
-- until it is lit; cheer = true makes Ume cheer; bubble = "exclaim" shows the "!" bubble.
-- `rewards`: Ume's line after a clear that left a reward (floors 1-9 and 11-19), before the pop-ups.

-- a rare line's weight, against 1 for every other line
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

-- a spoken choice: picking it says it, so it is not repeated as a line
local function say(...)
    local choices = {}
    for i, label in ipairs({ ... }) do
        choices[i] = { label = label, value = tostring(i) }
    end
    return { text = "", choices = choices }
end

return {
    default = {
        { pick = CLEAR },
    },

    roof = {},

    rewards = {
        { who = "ume", sprite = "normal", react = "surprise", bubble = "exclaim", text = "Oh! The monster left something behind!" },
    },

    -- shadow room: light_up on the first clear; once lit, the usual lines
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

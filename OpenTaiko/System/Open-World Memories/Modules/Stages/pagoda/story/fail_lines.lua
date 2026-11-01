-- Lines said after a floor is lost (floor_result.lua and fail_flight.lua).
-- `encounter`: on the encounter screen before the monster charges; `flight`: during the fail flight.
-- Line format (story/entrance.lua):
--   { who = "ume", sprite = "fail", text = "..." }   (sprite: normal, scared, brave, clear, run, lost, fail)
--   { name = "Needle", text = "..." }                 (anybody else)
--   { pick = pool }                                   (one option of the pool, drawn at random)
-- Each part plays the floor's own entry, else `roof` (21+) or `default`; an empty list skips the dialog.

local FLIGHT = {
    { lines = { { who = "ume", sprite = "fail", text = "Auwaugwhauwh… M-My head…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "We… w-We can’t… back d-down…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "I’m so weak… I dragged you down again…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "That’s so unfair… What were we supposed to do…?" } } },
    { lines = { { who = "ume", sprite = "fail", text = "I try, and I try… B-But I don’t know why…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "We’re blasting off again!!" } } },
}

return {
    encounter = {
        default = {
            { who = "ume", sprite = "fail", text = "oops..." },
        },

        roof = {},

        -- [level] = { lines } for a floor's own
    },

    flight = {
        default = {
            { pick = FLIGHT },
        },

        roof = {},

        -- [level] = { lines } for a floor's own
    },
}

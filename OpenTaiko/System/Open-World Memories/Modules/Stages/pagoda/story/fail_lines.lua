-- floor fail lines (floor_result.lua, fail_flight.lua); line format: story/entrance.lua

local FLIGHT = {
    { lines = { { who = "ume", sprite = "fail", text = "Auwaugwhauwh… M-My head…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "We… w-We can’t… back d-down…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "I’m so weak… I dragged you down again…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "That’s so unfair… What were we supposed to do…?" } } },
    { lines = { { who = "ume", sprite = "fail", text = "I try, and I try… B-But I don’t know why…" } } },
    { lines = { { who = "ume", sprite = "fail", text = "We’re blasting off again!!" } } },
}

-- encounter: before the monster charges; flight: during the fail flight
-- per part: [level] entry, else roof (21+), else default; {} skips the dialog
return {
    encounter = {
        default = {
            { who = "ume", sprite = "fail", text = "oops..." },
        },

        roof = {},

        -- [level] = { lines } per floor
    },

    flight = {
        default = {
            { pick = FLIGHT },
        },

        roof = {},

        -- [level] = { lines } per floor
    },
}

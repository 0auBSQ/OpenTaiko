-- Opening story once level 20 is beaten (start below the level-21 checkpoint).

return {
    bg = "forest",
    lines = {
        { text = "The once-deserted pagoda, now reaching to the edge of the sky shines in the middle of the forest, glowing in sunlight." },

        { who = "ume", text = "Heeeey, Boss!!!!!" },
        { enter = "ume", expr = "neu2" },

        { text = "Ume is energetically running towards you." },

        { who = "ume", expr = "neu2", text = "I'm so happy to run into you here!" },

        {
            text = "",
            choices = {
                { label = "I am too!", value = "1" },
                { label = "Ok bud.", value = "2" },
            },
            branch = {
                ["1"] = {
                    { text = "The two of you stand there, taking in the scenery of nature." },
                },
                ["2"] = {
                    { text = "The two of you stand there, taking in the scenery of nature. {pause:0.4}\nHe didn't understand that you were trying to dismiss him." },
                },
            },
        },

        { who = "ume", expr = "sad0", text = "It's still so strange isn't it? {pause:0.4}\nI wonder why Tobiraph did everything that it did." },

        {
            text = "",
            choices = {
                { label = "I've been thinking about it too. I sorta want to know more.", value = "1" },
                { label = "...", value = "2" },
            },
        },

        { who = "ume", expr = "gentle0", text = "Hey, why don't we play pretend that it's still.. weird? {pause:0.4}\nI had so much fun with you, and I kind of wish we could do it again." },

        {
            text = "",
            choices = {
                { label = "I'd be happy to!", value = "1" },
                { label = "That's why I'm here.", value = "2" },
                { label = "You nod.", value = "3" },
                { label = "Maybe another time.", value = "4" },
            },
            branch = {
                ["4"] = {
                    { who = "ume", expr = "gentle1", text = "Understood! Don't hesitate to come anytime..." },
                    { fade = "out", keepSprite = false },
                    { text = "You leave the fox village...\nWhy did you came here already?" },
                    { outcome = "abort" },
                },
            },
        },

        {
            fade = "out",
            keepSprite = false,
            when = function(ctx) return not ctx.choices["4"] end,
        },
        {
            text = "The two of you enter the forest behind the dojo, with eyes filled with imagination.",
            when = function(ctx) return ctx.startLevel == 1 end,
        },
        {
            text = "The two of you reach the stairs leading to the pagoda, with eyes filled with imagination.",
            when = function(ctx) return ctx.startLevel == 6 end,
        },
        {
            text = "The two of you enter the pagoda, with eyes filled with imagination.",
            when = function(ctx) return ctx.startLevel ~= 1 and ctx.startLevel ~= 6 end,
        },
        { outcome = "proceed" }
    }
}

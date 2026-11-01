-- Opening story for a run from the level-21 checkpoint.
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
                { label = "Ok bud.", value = "2" }
            },
            branch = {
                ["1"] = {
                    { text = "The two of you stand there, taking in the scenery of nature." }
                },
                ["2"] = {
                    { text = "The two of you stand there, taking in the scenery of nature. {pause:0.4}\nHe didn't understand that you were trying to dismiss him." }
                }
            }
        },

        { who = "ume", expr = "sad0", text = "It's still so strange isn't it? {pause:0.4}\nI wonder why Tobiraph did everything that it did." },

        { who = "ume", expr = "gentle0", text = "... {pause:0.4}\nThat look on your face is telling me that you want to climb higher." },

        {
            text = "",
            choices = {
                { label = "I am!", value = "1" },
                { label = "I just wanna check it out.", value = "2" },
                { label = "Neva neva neva!", value = "3" }
            },
            branch = {
                ["3"] = {
                    { who = "ume", expr = "gentle1", text = "Understood! Don't hesitate to come anytime..." },
                    { fade = "out", keepSprite = false },
                    { text = "You leave the fox village...\nWhy did you came here already?" }
                }
            }
        },

        {
            who = "ume", expr = "neu2",
            text = "Then I'll go up to the roof with you, too!",
            when = function(ctx) return not ctx.choices["3"] end
        },

        {
            who = "ume", expr = "neu2",
            text = "... {pause:0.4}\nThough I probably won't climb that thing, there's some stuff I need to check on inside the pagoda.",
            when = function(ctx) return not ctx.choices["3"] end
        },

        {
            who = "ume", expr = "neu2",
            text = "I'll cheer you on the best I can, though!",
            when = function(ctx) return not ctx.choices["3"] end
        },

        {
            fade = "out", keepSprite = false,
            when = function(ctx) return not ctx.choices["3"] end
        },

        {
            text = "You start climbing the beanstalk, without knowing what to expect...",
            when = function(ctx) return not ctx.choices["3"] end
        },

        { outcome = "abort", when = function(ctx) return ctx.choices["3"] end },
        { outcome = "proceed", when = function(ctx) return not ctx.choices["3"] end }
    }
}

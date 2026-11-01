-- Opening story while level 20 is unbeaten.

return {
    bg = "dojo",
    lines = {
        { who = "ume", expr = "neu2", text = "Hey Boss! {pause:0.4}\nAre you ready to take on the pagoda again?" },

        {
            text = "",
            choices = {
                { label = "Sure am!", value = "1" },
                { label = "Boss???", value = "2" },
                { label = "Pago... Pagowhat?", value = "3" },
                { label = "I literally don't want to, tiny coward", value = "4" }
            },
            branch = {
                ["1"] = {
                    { who = "ume", expr = "smile1", text = "Thank you! {pause:0.4}\nI'm happy to hear that!" }
                },
                ["2"] = {
                    { who = "ume", expr = "neu1", text = "You're the bigger, stronger person! {pause:0.4}\nThat's what a boss is, right? {pause:0.4}\n...Right?" },
                    {
                        text = "",
                        choices = {
                            { label = "I suppose so.", value = "2-1" },
                            { label = "I'm not sure that's how it works...", value = "2-2" },
                            { label = "Yes, my minion.", value = "2-3" }
                        },
                        branch = {
                            ["2-3"] = {
                                { who = "ume", expr = "sad0", text = "Minion...?? {pause:0.4}\n... *Ahem.*" }
                            }
                        }
                    }
                },
                ["3"] = {
                    { text = "Ume blinks at you, concerned." },
                    { who = "ume", expr = "silly0", text = "Uh... {pause:0.4}Y'know... {pause:0.4}\nthe... {pause:0.4}the thing... {pause:0.4}\nthe thing we did..." },
                    { who = "ume", expr = "neu1", text = "... {pause:0.4}\nUgh... {pause:0.4}\nokay..." },
                    { fade = "out", keepSprite = false },
                    { text = "Ume sighs, and explains it all to you again." },
                    { fade = "in" }
                },
                ["4"] = {
                    { text = "You start walking away from Ume." },
                    { who = "ume", expr = "fear3", text = "{shake:14,0.35}{sfx:shock}NOO WAIT!! {pause:0.4}WAAIT!!!" },
                    { text = "Ume drags you back, before trying to convince you to help." }
                }
            }
        },

        { who = "ume", expr = "neu2", text = "Remember, any progress is good progress. {pause:0.4}\nAs long as we get in there and give them even a smidge of a scare, I think the village would be safe, but..." },
        { who = "ume", expr = "sad0", text = "I don't want the pagoda to stay like this forever... {pause:0.4}\nI'm sure, someday, we'll make it to the very top and restore it!" },

        {
            text = "",
            choices = {
                { label = "That's the spirit, Ume!", value = "1" },
                { label = "That's optimistic.", value = "2" },
                { label = "I don't want to play with you anymore.", value = "3" },
                { label = "...bye HAHAHAAHAAAHA", value = "4" }
            }
        },

        { who = "ume", expr = "shock1", text = "Wait! Pleas-", when = function(ctx) return ctx.choice == "3" or ctx.choice == "4" end },
        { fade = "out", when = function(ctx) return ctx.choice == "3" or ctx.choice == "4" end },
        { text = "You leave the fox village, leaving it to its terrible fate...", when = function(ctx) return ctx.choice == "3" or ctx.choice == "4" end },
        { text = "You're hungry, stopping by the local convenience store before going back home.", when = function(ctx) return ctx.choice == "3" or ctx.choice == "4" end },
        { outcome = "abort", when = function(ctx) return ctx.choice == "3" or ctx.choice == "4" end },

        { fade = "out", when = function(ctx) return ctx.choice ~= "3" and ctx.choice ~= "4" end },
        { text = "The two of you enter the forest behind the dojo.", when = function(ctx) return ctx.choice ~= "3" and ctx.choice ~= "4" and ctx.startLevel == 1 end },
        { text = "The two of you reach the stairs leading to the pagoda.", when = function(ctx) return ctx.choice ~= "3" and ctx.choice ~= "4" and ctx.startLevel == 6 end },
        { text = "The two of you enter the pagoda.", when = function(ctx) return ctx.choice ~= "3" and ctx.choice ~= "4" and ctx.startLevel ~= 1 and ctx.startLevel ~= 6 end },
        { outcome = "proceed", when = function(ctx) return ctx.choice ~= "3" and ctx.choice ~= "4" end }
    }
}

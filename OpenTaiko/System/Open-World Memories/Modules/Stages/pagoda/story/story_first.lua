-- Opening story of the first challenge run.

return {
    bg = "fox_village",
    lines = {
        { text = "You find yourself walking through the quaint Fox Village in the forest." },
        { text = "It's a cloudy day, painting the town in a tone of gray." },
        { text = "Suddenly, you hear footsteps coming your way, and an impact against you blows you to the ground!" },
        { text = "You open your eyes back up, and find a familiar looking young fox boy crying." },

        { enter = "ume", expr = "fear2" },

        {
            text = "",
            choices = {
                { label = "Are you okay? What's wrong?", value = "1" },
                { label = "Ow...", value = "2" },
                { label = "HEY! I'M WALKING HERE!", value = "3" }
            }
        },

        { who = "ume", name = "???", text = "Ah, I'm so sorry! I..." },

        { text = "The boy pauses for a second, looking at you carefully." },

        { who = "ume", name = "???", text = "You... {pause:0.4}\nYou're the one that's been visiting our dojo lately..." },

        {
            text = "",
            choices = {
                { label = "Ume?", value = "1" },
                { label = "You're... Tsutaki! Right?", value = "2" },
                { label = "Who's this brat", value = "3" }
            },
            branch = {
                ["1"] = {
                    { who = "ume", expr = "neu2", text = "Phew... I'm glad to run into someone I already know..." }
                },
                ["2"] = {
                    { expr = "neu1", text = "The boy makes a disappointed expression." },
                    { who = "ume", name = "???", text = "I... {pause:0.4}\nI suppose I never introduced myself properly." },
                    { who = "ume", expr = "sad0", text = "I'm Ume... {pause:0.4}\nI've seen you at our dojo, playing taiko drums before..." }
                },
                ["3"] = {
                    { expr = "neu1", text = "The boy makes a disappointed expression." },
                    { who = "ume", name = "???", text = "I... {pause:0.4}\nI suppose I never introduced myself properly." },
                    { who = "ume", expr = "sad0", text = "I'm Ume... {pause:0.4}\nI've seen you at our dojo, playing taiko drums before..." }
                }
            }
        },

        {
            text = "",
            choices = {
                { label = "Good to see you, but what's the matter?", value = "1" },
                { label = "How come?", value = "2" },
                { label = "That's a weird way to say \"nice to see you.\"", value = "3" }
            },
            branch = {
                ["3"] = {
                    { who = "ume", expr = "fear3", text = "{shake:14,0.35}{sfx:shock}I'm sorry... {pause:0.4}\nI don't mean to be rude... {pause:0.4}\nBut..." },
                    { text = "Ume seems pretty scared, so you pause to listen to what he has to say." }
                }
            }
        },

        { who = "ume", expr = "sad0", text = "Well, you see... {pause:0.4}\nThere's this quiet pagoda holding our ancestors relics deep in the woods and... {pause:0.4}\nRecently, the aura around it turned all... weird, and stuff." },

        { who = "ume", expr = "neu1", text = "It's normally empty and serene, but it started to have a strange dark energy enveloping it, more and more with each day I visit." },

        { who = "ume", expr = "neu0", text = "The stream around it has become as big as a lake, growing just like the weird darky stuff..." },

        { who = "ume", expr = "neu0", text = "And... when I stepped inside, even just one step... {pause:0.4}\nsome... {pause:0.4}{expr:fear2}some creature tried to attack me! {pause:0.4}\nIt was scary... I'm scared...!!" },

        {
            text = "",
            choices = {
                { label = "Have you asked one of your grown-ups about it?", value = "1" },
                { label = "That's called a squirrel. It's just a squirrel.", value = "2" }
            },
            branch = {
                ["1"] = {
                    { who = "ume", expr = "sad0", text = "Well... Tsubaki thinks I'm lying, or that it's not important. {pause:0.4}\nShe says it's so deep in the woods that it wouldn't matter, but it's CLEARLY dangerous! {pause:0.4}\nSUPER DUPER dangerous!" },
                    { who = "ume", expr = "sad0", text = "Asagao didn't say anything or do anything about it, so maybe they think I'm making it up too, but..." },
                    { who = "ume", expr = "sad0", text = "I feel like if we don't do something soon, our village could be in some really bad danger..." },
                    { who = "ume", expr = "sad0", text = "... Do you think you could help me, maybe? {pause:0.4}\nYou're the only other super strong person I can think of..." },
                    {
                        text = "",
                        choices = {
                            { label = "Of course, Ume. Take me there.", value = "1-1" }
                        }
                    }
                },
                ["2"] = {
                    { text = "Ume looks down, with an upset expression. {pause:0.4}\nHe took that in a not-great way." },
                    { who = "ume", expr = "shock1", text = "{shake:14,0.35}{sfx:shock}It... {pause:0.4}It wasn't a squirrel... {pause:0.4}I promise..." },
                    { who = "ume", expr = "shock1", text = "I really need your help... {pause:0.4}\nNo one else believes me either, but it really could put the village in some bad danger..." },
                    { who = "ume", expr = "fear3", text = "Can I at least show you the scary thing...? {pause:0.4}\nYou're really my last hope..." },
                    {
                        text = "",
                        choices = {
                            { label = "I'm sorry, I didn't mean to hurt your feelings. Show me, I'll help.", value = "2-1" },
                            { label = "... Uh, okay...", value = "2-2" },
                            { label = "The squirrel? Yeah, I guess.", value = "2-3" },
                            { label = "No.", value = "2-4" }
                        },
                        branch = {
                            ["2-3"] = {
                                { who = "ume", expr = "shock1", text = "It's not a squirrel!!" }
                            }
                        }
                    }
                }
            }
        },

        {
            when = function(ctx) return not ctx.choices["2-4"] and ctx.startLevel == 1 end,
            text = "The two of you reach the forest from the dojo backdoor, and are ready for a long journey."
        },
        {
            when = function(ctx) return not ctx.choices["2-4"] and ctx.startLevel == 6 end,
            text = "The two of you run through the forest, and reach the entrance gate to the sacred place.\nA long climb waits before reaching the deserted pagoda."
        },
        {
            when = function(ctx) return not ctx.choices["2-4"] and ctx.startLevel ~= 1 and ctx.startLevel ~= 6 end,
            text = "The two of you run through the forest, and reach the deserted pagoda, which was indeed distorted with purple hues.\nYou feel like something bad is brewing inside."
        },
        {
            when = function(ctx) return not ctx.choices["2-4"] and ctx.startLevel ~= 1 and ctx.startLevel ~= 6 end,
            text = "The two of you enter the pagoda."
        },
        {
            when = function(ctx) return not ctx.choices["2-4"] end,
            fade = "out",
            keepSprite = false
        },
        {
            when = function(ctx) return not ctx.choices["2-4"] end,
            outcome = "proceed"
        },

        {
            when = function(ctx) return not not ctx.choices["2-4"] end,
            who = "ume",
            expr = "shock1",
            text = "Wait! Pleas-"
        },
        {
            when = function(ctx) return not not ctx.choices["2-4"] end,
            text = "You leave the fox village, leaving it to its terrible fate...\nYou're hungry, stopping by the local convenience store before going back home."
        },
        {
            when = function(ctx) return not not ctx.choices["2-4"] end,
            fade = "out",
            keepSprite = false
        },
        {
            when = function(ctx) return not not ctx.choices["2-4"] end,
            outcome = "abort"
        }
    }
}

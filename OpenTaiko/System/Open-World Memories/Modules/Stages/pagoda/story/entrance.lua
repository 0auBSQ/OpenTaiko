-- Floor entrance dialogs.
-- level -> variants; the first variant whose `when(ctx)` is true plays, so the fallback (no `when`) comes last.
-- Line fields: revealMonster = sec (monster fade-in), pick = pool (one random option), react = "surprise" | "shock".
-- A variant's `sprite` is Ume's pose at the start of the visit.
-- ctx: runStart (the run's start floor), highest (the save's), tower (the tower's shown cleared floor),
-- firstTime (never cleared), lostBefore (a run was lost on it).

local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local Reveal = Animation.story.entrance.reveal_monster

-- levels 1-5
local TRAIL = {
    { lines = { { who = "ume", text = "Here comes another one of those guys… We just have to do the same thing as before…!" } } },
    { lines = { { who = "ume", text = "Why are they acting so strange…? I’ve never seen something like this before…" } } },
    { lines = { { who = "ume", text = "Let’s keep on going strong. After all, only the two of us will take care of something like this." } } },
}

-- levels 6-9
local STAIRS = {
    { lines = { { who = "ume", text = "I always get so lightheaded walking up these stairs… We can’t lose focus, though!" } } },
    { lines = { { text = "Ume’s eyes are wandering around the scene. He stays behind you, with saddened eyes telling you something’s got him distressed." } } },
    { lines = { { who = "ume", text = "…It’s so much colder up here… I can’t stop now, though…" } } },
}

-- after the monster shows (11, 12, 14 and the lit 15)
local REACTIONS = {
    { lines = { { who = "ume", text = "H-Here it comes…!" } } },
    { lines = { { who = "ume", text = "This one looks a little strong… Stay focused…!" } } },
    { lines = { { who = "ume", text = "Are you ready…? I’ll help however I can…" } } },
    { lines = { { who = "ume", text = "Don’t let your guard down…!" } } },
    { lines = { { who = "ume", text = "… I’m scared…" } } },
}

-- true when the run started on this floor (its tutorial plays)
local function startedAt(level)
    return function(ctx) return ctx.runStart == level end
end

-- the long meeting plays while the monster was never met, and on every visit once a checkpoint above
-- is unlocked (16 after clearing 15, 21 after clearing 20)
local function fullAt(level)
    return function(ctx)
        if ctx.firstTime == true and not ctx.lostBefore then return true end
        local h = ctx.highest or 0
        return (level <= 15 and h >= 15) or (level <= 20 and h >= 20)
    end
end

return {
    [1] = {
        {
            when = startedAt(1),
            lines = {
                { text = "Ume leads you through a narrow forest path, drowned in greenery that makes it especially difficult to navigate." },
                { text = "The two of you eventually arrive at a wider, overgrown trail that looks like quite the lengthy walk." },
                { revealMonster = Reveal.floor_1_s, react = "surprise", text = "Suddenly, a bush begins to shake, and a critter jumps out in front of the two of you!" },
                { who = "ume", react = "shock", text = "AH!- …T-This is what I was talking about earlier… Wildlife is patrolling the trail like they’re puppets…" },
                { who = "ume", text = "We don’t have much of a choice but to snap them out of it, and shoo them away…" },
                { who = "ume", text = "That’s not an impossible thing to do, though. Maybe you’ve heard in some local tales that in cases like these, music is quite a powerful thing." },
                { who = "ume", text = "It’s possible that with that force, we may be able to direct its attention away from us, and shoo them off!" },
            },
        },
        { lines = { { pick = TRAIL } } },
    },

    [2] = { { lines = { { pick = TRAIL } } } },
    [3] = { { lines = { { pick = TRAIL } } } },
    [4] = { { lines = { { pick = TRAIL } } } },

    [5] = {
        {
            lines = {
                { who = "ume", text = "Weird... This gate should protect the pagoda but...{pause:0.4}\nIts aura seems weaker than usual..." },
                { who = "ume", text = "Let's hurry!" },
            },
        },
    },

    [6] = {
        {
            when = startedAt(6),
            lines = {
                { text = "Ume leads you through a narrow forest path, drowned in greenery that makes it especially difficult to navigate." },
                { text = "The two of you eventually pass the entrance gate reaching the stairs, climbing towards the pagoda." },
                { text = "Entities start to get stronger from here, but Ume seemed to get braver through the trail." },
                { who = "ume", text = "The wildlife will be stronger from here!" },
                { who = "ume", text = "There seem to be something fishy up there, but we'll need to climb to see it with our own eyes..." },
                { who = "ume", text = "We don’t have much of a choice but to snap them out of it, and shoo them away…" },
                { who = "ume", text = "You might know this already, but maybe you’ve heard in some local tales that in cases like these, music is quite a powerful thing." },
                { who = "ume", text = "It’s possible that with that force, we may be able to direct its attention away from us, and shoo them off!" },
            },
        },
        { lines = { { pick = STAIRS } } },
    },

    [7] = { { lines = { { pick = STAIRS } } } },
    [8] = { { lines = { { pick = STAIRS } } } },
    [9] = { { lines = { { pick = STAIRS } } } },

    [10] = {
        {
            lines = {
                { who = "ume", text = "Oh no... The pagoda is completely engulfed in darkness!" },
                { who = "ume", text = "It should be the last one, let's hurry...{pause:0.4}\nPlease..." },
            },
        },
    },

    [11] = {
        {
            when = startedAt(11),
            sprite = "normal",
            lines = {
                { text = "You enter the pagoda to find a dark room, one that seems normal-looking at first, until.\nA monster in the distance begins to glow, guarding a large door!" },
                { name = "Needle", text = "{shake:16,0.4}{sfx:roar}Uuuuuurrrrrghhhhh" },
                { text = "Ume looks scared like before, but he seems like he's standing a little braver now that you're with him." },
                { who = "ume", text = "We don't have a choice but to get rid of it before it gets rid of us!" },
                { who = "ume", text = "Don't worry though.{pause:0.4}\nMaybe you've heard in some local tales that in cases like these, music is quite a powerful thing." },
                { who = "ume", text = "Endure and reflect it's attacks and rhythms, and together, we can blow it outta the water!" },
            },
        },
        {
            sprite = "normal",
            lines = {
                { name = "Needle", text = "{shake:16,0.4}{sfx:roar}Uuuuuurrrrrghhhhh" },
                { pick = REACTIONS },
            },
        },
    },

    [12] = {
        {
            sprite = "normal",
            lines = {
                { name = "Thorncushion", text = "{shake:16,0.4}{sfx:roar}Arrrrrrgghghghhhh" },
                { pick = REACTIONS },
            },
        },
    },

    [13] = {
        {
            sprite = "normal",
            lines = {
                { name = "Counterplate", text = "Intruder... Exterminate!" },
                { who = "ume", sprite = "scared", text = "It...{pause:0.4}\nIt talks!?" },
                { who = "ume", sprite = "normal", text = "Let's...{pause:0.4}\nLet's stay focused! We can do it!" },
            },
        },
    },

    [14] = {
        {
            sprite = "normal",
            lines = {
                { name = "Pincerblaze", text = "{shake:16,0.4}{sfx:roar}Rooaoaoaooororrrr\nPinch! Cut!" },
                { pick = REACTIONS },
            },
        },
    },

    [15] = {
        {
            -- dark until the tower shows 15 cleared
            when = function(ctx) return (ctx.tower or ctx.highest) < 15 end,
            sprite = "normal",
            lines = {
                { who = "ume", sprite = "scared", text = "This room... Is really dark..." },
                {
                    text = "",
                    choices = {
                        { label = "You hold his hand", value = "1" },
                        { label = "You do nothing", value = "2" },
                    },
                    branch = {
                        ["1"] = {
                            { text = "Ume looks up at you, before making a slightly braver expression." },
                        },
                        ["2"] = {},
                    },
                },
                { revealMonster = Reveal.floor_15_s, text = "For some reason, the air in this room feels especially chilly.\nIt gets you to tense up a little.\nA pair of eyes glow in the distance, revealing a monster!" },
                { name = "Startle", text = "Bzzzzttt Bzzzzztt\nExplosion!" },
                { who = "ume", text = "I don't think this one will be easy...{pause:0.4}\nLet's do our very best!" },
            },
        },
        {
            sprite = "normal",
            lines = {
                { name = "Startle", text = "Bzzzzttt Bzzzzztt\nExplosion!" },
                { pick = REACTIONS },
            },
        },
    },

    [16] = {
        {
            lines = {
                { name = "Fray", text = "You who entered this place...\nShall be punished..." },
                { who = "ume", text = "But... It's you who..." },
                { name = "Fray", text = "{shake:16,0.4}{sfx:roar}Arrrrrrgghghghhhh" },
                { sprite = "scared" },
                { who = "ume", sprite = "brave", text = "Ah...\nLet's do it, <player name>!" },
            },
        },
    },

    [17] = {
        {
            when = fullAt(17),
            lines = {
                { name = "Dragoon", text = "{shake:16,0.4}{sfx:roar}GGOOOOOOOOOOOOOOOOOOOOooooooooggggh!!" },
                { text = "Ume jumps, startled from the incredibly deafening roar. He seems to hold his ground though, and tries to keep his head up." },
                { name = "Dragoon", text = "You two most certainly don’t belong somewhere like this! This floor is mine, and mine ALONE." },
                { who = "ume", text = "… That’s not true, Mr. Dragon, sir…" },
                { name = "Dragoon", text = "THAT’S NOT MY NAME! WHAT A DISRESPECTFUL YOUNG MORSEL!" },
                { sprite = "scared", text = "Ume makes a sad face, looking at you, unable to speak." },
                {
                    text = "",
                    choices = {
                        { label = "“It’s alright Ume, this guy just wants you to give up!”", value = "1" },
                        { label = "“Don’t let him get to your feelings.”", value = "2" },
                        { label = "“Yeah, that’s not his name!!”", value = "3" },
                    },
                    branch = {
                        ["1"] = {
                            { sprite = "brave", text = "Ume pauses for a moment, before nodding sharply, and standing readily by your side." },
                        },
                        ["2"] = {
                            { sprite = "brave", text = "Ume pauses for a moment, before nodding sharply, and standing readily by your side." },
                        },
                        ["3"] = {
                            { sprite = "brave", text = "Ume starts looking overwhelmed. He, discomforted, begins to stand wider, to battle." },
                        },
                    },
                },
            },
        },
        {
            lines = {
                { text = "As the two of you enter the door, the huge dragon begins to grumble furiously, erupting with anger." },
                { name = "Dragoon", text = "{shake:16,0.4}{sfx:roar}gggggggGGOOOOOOOOOOOOOOOOOOGh!" },
                { name = "Dragoon", text = "When will you feeble insects learn that this is MY turf!?" },
                { text = "Ume shakes his head in response." },
                { who = "ume", sprite = "brave", text = "We won’t go down that easy, Mr. Dragon!!" },
                { text = "The huge dragon, fuming red, opens all the eyes along his rough scales to look at Ume." },
                { name = "Dragoon", text = "{shake:16,0.4}{sfx:roar}If you little morsels dare to come back and taunt me like this… then… THEN I WILL MAKE YOU MY FEAST! GOOOOOONARGH!!" },
                { sprite = "scared", react = "shock", text = "Ume, startled by his remark, does his best to bravely stand by your side, to challenge the dragon once more." },
            },
        },
    },

    [18] = {
        {
            when = fullAt(18),
            lines = {
                { text = "The two of you enter the room, hit by an incredibly sharp wave of coldness." },
                { name = "Frangel", text = "Oh! Hello, unexpected darlings." },
                { who = "ume", sprite = "scared", text = "… Hello, ma’am…" },
                { name = "Frangel", text = "Why not stay for a while, love? It’s nice and cold in here, is it not?" },
                { who = "ume", text = "Um… Um, no. I don’t think…" },
                { text = "Her kindness is almost sickening. You’re tempted to stay, but you’ll absolutely freeze, to an unhealthy degree." },
                { text = "She chuckles in a menacing way." },
                { name = "Frangel", text = "Hmm? I can’t hear you, when you’re so quiet, my dear." },
                { name = "Frangel", text = "{shake:16,0.4}SPEAK. UP." },
                { text = "Ume quivers, losing a bit of braveness he had before. He pauses for a moment, looking down at the icy floor." },
                { who = "ume", text = "(Remember… Ume… They’re just like this… To scare you away…)" },
                { sprite = "brave", text = "He breathes heavily for a moment, before looking back up at her." },
                { who = "ume", text = "Ma’am… Do I know you…?" },
                { name = "Frangel", text = "Well darling, I certainly do not believe such." },
                { who = "ume", text = "If you’re not my sister, then don’t talk to me that way!" },
                { text = "Frangel seems quite amused.\nUme’s right. She does kind of talk like Tsubaki." },
                { name = "Frangel", text = "I see how it is. Then, why don’t I freeze you in the loveliest of ice?!" },
                { name = "Frangel", text = "BRING IT ON, LITTLE ICICLE! OHOHOHO!!" },
            },
        },
        {
            lines = {
                { name = "Frangel", text = "Oh, foolish little snowflake, have I put you in your place yet?" },
                { name = "Frangel", text = "…Or do you need a REAL adult to teach you how it is!?" },
            },
        },
    },

    [19] = {
        {
            when = fullAt(19),
            lines = {
                { name = "Zirr", text = "Help..." },
                { who = "ume", text = "Are you... okay?" },
                { name = "Zirr", text = "Help me...\nI... It's cold...\nI'm so tired..." },
                { who = "ume", text = "..." },
                { name = "Zirr", text = "Help...\nI just wanted... To be liked...\nIt hurts..." },
                { who = "ume", sprite = "scared", text = "It's starting to run at us, be careful!!" },
            },
        },
        {
            lines = {
                { name = "Zirr", text = "S… S-Summer… Sweet, sweet summer, is that… Is that you…?" },
                { text = "Like an arachnid, Zirr rushes towards you instantly!" },
                { who = "ume", sprite = "scared", text = "UWAAH! DON’T LET YOUR GUARD DOWN!" },
            },
        },
    },

    [20] = {
        {
            when = fullAt(20),
            lines = {
                { text = "You walk up the set of stairs, and a blinding light begins to fill your vision.\nAs you and Ume's eyes adjust, you find that the two of you are on the roof of the pagoda, and a huge, mysterious figure towering before you." },
                { name = "???", text = "... ?" },
                { text = "It looks down at you, inspecting closely." },
                { who = "ume", sprite = "scared", text = "... What... is that...?" },
                { name = "???", text = "...{pause:0.4}\nThe strange human, and the small fox boy.{pause:0.4}\nI've been watching closely." },
                { name = "???", text = "Congratulations on reaching the summit." },
                { text = "Ume stands bravely by your side.\nHis growth puts a smile on your face.\nYou look up at the being to confront it." },
                {
                    text = "",
                    choices = {
                        { label = "Who are you?", value = "1" },
                        { label = "So, are you the one behind all this?", value = "2" },
                        { label = "\"Summit?\" That's the top of a mountain. This is not a mountain.", value = "3" },
                        { label = "What's the deal creep???", value = "4" },
                    },
                    branch = {
                        ["1"] = {
                            { name = "Tobiraph", text = "I am Tobiraph.{pause:0.4}" },
                            { name = "Tobiraph", text = "I am here, as it is the most optimal location to host my priorities." },
                        },
                        ["2"] = {
                            { name = "???", text = "Conceivably so.{pause:0.4}\nIt is solely what is most optimal for my needs." },
                            { name = "Tobiraph", text = "Title me Tobiraph." },
                        },
                        ["3"] = {
                            { name = "???", text = "Peculiar.{pause:0.4}\nStruggle was very often seen from you, as if you were climbing a mountain." },
                            { name = "Tobiraph", text = "Regardless, it is I who make the rules here.{pause:0.4}\nTitle me Tobiraph." },
                        },
                        ["4"] = {
                            { name = "???", text = "Bold.{pause:0.4}\nThe \"deal\" is that this pagoda rules under my order." },
                            { name = "Tobiraph", text = "Respect those above you,{pause:0.4} and title me as Tobiraph." },
                        },
                    },
                },
                { text = "Ume starts to back down a little, fear crawling up his skin.\nHe still holds a powerful expression." },
                { who = "ume", text = "...{pause:0.4}\nWhy would you do this to the pagoda?{pause:0.4}\nTo the town...?" },
                { name = "Tobiraph", text = "Comprehending my motives is not relevant to you, child." },
                { text = "Ume's face grows sour." },
                { who = "ume", text = "But...{pause:0.4}\nBut that's just not fair...!{pause:0.4}\nYou can't just take over this place, it's a memorial." },
                { name = "Tobiraph", text = "The history of your people is useless." },
                { text = "Though you thought that Tobiraph is not any kind of human, saying something like that blatantly confirms it." },
                { text = "Ume looks at you, trying to hold in his emotions.\nIt looks like tears are going to fall, but Ume stays strong, and holds them to his eyelashes." },
                { text = "You look at the huge bright being before you, and stance yourself to challenge it head on.\nYou look back at Ume, and say..." },
                {
                    text = "",
                    choices = {
                        { label = "We can do this. Remember? Just like you always said.", value = "1" },
                        { label = "You're strong kid, okay? Hold your ground.", value = "2" },
                        { label = "lock in homie", value = "3" },
                    },
                },
                { text = "Ume's expression softens, before beginning to tighten back into a brave, confident truth.\nHe smiles at your remark, and nods in response." },
                { name = "Tobiraph", text = "Incredible.{pause:0.4}\nYou still consider objecting to my ordinances." },
                { name = "Tobiraph", text = "It is unjust to delete my beloved children and assume I will fall the same." },
                { name = "Tobiraph", text = "As soon as you fall once more, they will be reborn as if no change was ever made." },
                { name = "Tobiraph", text = "Isn't that lovely?" },
                { text = "... The air blows briskly. It tightens your form, as you and Ume prepare for your final obstacle." },
            },
        },
        {
            lines = {
                { text = "You return to the roof, this time fully prepared for the blinding lights and powerful force before you." },
                { name = "Tobiraph", text = "Let us continue, until you learn." },
                { who = "ume", text = "Let's stay strong, okay?" },
                { text = "You nod, and your final challenge begins once more." },
            },
        },
    },
}

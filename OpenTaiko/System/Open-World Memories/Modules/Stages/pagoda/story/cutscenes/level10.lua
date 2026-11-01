-- Level-10 cutscene lines; format: story/entrance.lua plus the extras in encounter/cutscenes/init.lua.

local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local FailCover = Animation.story.cutscenes.level10.fail_cover

local SNAKE = "blip_monster3"       -- voice blip for every form
local UME = "blip"

return {
    music = "late",
    ending = { clear = "transition", fail = "title" },

    enter = {
        { text = "The two of you, exhausted, stand in front of a huge pool of water surrounding the pagoda." },
        { who = "ume", text = "Phew… We finally… Finally made it to the top…" },
        { text = "Ume catches his breath and begins to look at the water, then at you." },
        { who = "ume", text = "Do you… have any clue how we’ll get across this…?" },
        {
            text = "",
            choices = {
                { label = "Let’s get big rocks to skip on.", value = "1" },
                { label = "A raft would be ideal", value = "2" },
                { label = "Let’s just walk through it!", value = "3" },
                { label = "Why ask me??", value = "4" },
            },
            branch = {
                ["1"] = {
                    { who = "ume", text = "Um… O-Okay…" },
                    { text = "Ume begins to walk off a little to look for some, but a sudden loud sound freezes him in his place." },
                },
                ["2"] = {
                    { who = "ume", text = "Yeah… It would be nice to get across in a dry way… I’ve had to walk through it last time…" },
                    { text = "Ume looked pretty comfortable talking to you, but was suddenly caught off guard by a loud sound." },
                },
                ["3"] = {
                    { who = "ume", text = "…I guess it would be the fastest way. It wouldn’t exactly be walking though… It looks pretty deep now…" },
                    { text = "Ume’s gaze was lost in the ripples of the water, when suddenly, a loud sound came from it, shocking him!" },
                },
                ["4"] = {
                    { who = "ume", text = "Uh… I dunno..! Um… I was wondering if you had a better idea than… swimming…" },
                    { text = "Suddenly, a loud sound coming from the water shakes the ground and startles the two of you!" },
                },
            },
        },
        { event = "emerge", wait = true },
        { text = "A huge snake covered in forest-made armor and a golden helmet emerges from the pool of water." },
        { name = "???", blip = SNAKE, text = "What bringssss you irrelevant beingssss to my domain?" },
        { who = "ume", text = "Yours…? I don’t think this is yours-" },
        { name = "???", blip = SNAKE, text = "{shake:16,0.4}SSSSILENCE!" },
        { text = "Ume jumps, clearly afraid, and scurries behind you." },
        { name = "Guardian Snake", blip = SNAKE, text = "I am the guard of thissss pagoda. I am on patrol to keep it and thissss foresssst ssssafe from potential threatssss. It’ssss not good to loiter in placessss where you don’t belong." },
        { name = "Guardian Snake", blip = SNAKE, text = "Ussss monssssterssss live harmonioussssly, with thissss pagoda assss our prized center piece. I’m ssssurprissssed the otherssss didn’t sssstop you." },
        { text = "(Ume whispers to you.)" },
        { blip = UME, text = "(“The forest wildlife is DEFINITELY being controlled by this thing. That’s not normal here…”)" },
        { name = "Guardian Snake", blip = SNAKE, text = "If you truly wissssh to passss, you musssst take me down.{pause:0.5}\nOtherwisssse… Perhapssss you’ll be my lunch." },
        { name = "Together", blip = UME, text = "What?!" },
        { music = true },
    },

    fail = {
        { event = "loom", text = "The snake, towering over you, begins to open its mouth…" },
        { cover = FailCover.opacity, sec = FailCover.fade_s },
        { text = "You and Ume run and stumble to the bottom of the stairs faster than you could ever imagine." },
        { text = "The two of you pant, and decide to rest for a while." },
    },

    clear = {
        { name = "Together", blip = UME, text = "WE DID IT!!" },
        { event = "darken", wait = true, text = "The large guardian snake begins to fade into what appears to be black smoke." },
        { name = "Guardian Snake", blip = SNAKE, text = "Perhapssss you may have bessssted me… But don’t ssssay I did not warn you." },
        { name = "Together", blip = UME, text = "?" },
        { event = "reveal", name = "Pagoda Monster Snake", blip = SNAKE, text = "Didn’t I ssssay… You sssshouldn’t be where you don’t belong!?" },
        { event = "dissolve", wait = true },
        { event = "trail", wait = true, text = "The monster, now dust, flies in a trail into the entrance of the pagoda." },
    },
}

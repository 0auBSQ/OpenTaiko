-- Small talk scene after a cleared floor; roll(level) -> scene or nil.

local M = {}

local CHANCE = 0.3
local TOPIC_COUNT = 3

-- true when a small talk may follow this cleared floor
function M.eligible(level)
    return level ~= nil and level >= 1 and level < 20 and level ~= 10 and level ~= 15
end

-- the encounter's background and the story loop
local function setting(level)
    if level <= 5 then return "forest", "forest" end
    if level <= 9 then return "stairs", "forest" end
    return "pagoda_interior", "dojo"
end

local function closing(level)
    if level <= 4 then return "You two continue your walk into the trail." end
    if level <= 9 then return "You two continue your climb towards the pagoda." end
    return "You two continue your climb within the pagoda going for the next floor."
end

local function ume(expr, text) return { who = "ume", expr = expr, text = text } end
local function say(text) return { text = text } end
local function face(expr) return { expr = expr } end

-- a choice; `skips` lists the values that end the topic, every other value plays `rest`
local function ask(labels, skips, rest, extra)
    local choices, branch, skip = {}, {}, {}
    for _, v in ipairs(skips or {}) do skip[v] = true end
    for i, label in ipairs(labels) do
        local v = tostring(i)
        choices[i] = { label = label, value = v }
        if not skip[v] then branch[v] = (extra and extra[v]) or rest end
    end
    return { text = "", choices = choices, branch = branch }
end

local TOPICS = {
    {
        label = "About a dish you like",
        lines = {
            ume("smile0", "You know, it’s sort of been a while since Asagao made Inari-age…"),
            ume("neu1", "They’ve seemed a little too busy to do cooking at home, and even if Sister is a good cook, I really miss that Inari-age. It’s SO good…"),
            ume("sad0", "Maybe things have just been complicated at home, in general… It’s almost felt as weird as the pagoda…"),
            ume("smile1", "...At least I’ve got you, though! You’re a lot of fun!"),
        },
    },
    {
        label = "About Tsubaki",
        lines = {
            ume("neu1", "I wonder what Sister’s up to."),
            ask({ "Even you don’t know?", "Working at the dojo, right?", "(You ignore Ume.)" }, { "3" }, {
                ume("neu2", "Well, yeah, she usually is examining people and their musical skills at our dojo, sometimes combat skills in some cases."),
                ume("neu1", "But lately, she hasn’t been there quite as often… She might be working somewhere else, so the three of us have more money."),
                ume("sad0", "She seems a little stressed too, which is why I think that. She’s always a little stern, but especially now, she’s hard to talk to…"),
                ume("sad0", "I hope Sister and Asagao get everything sorted out. I try to help, but I sort of just get put aside…"),
                ume("gentle0", "Maybe if I can get to the top of this pagoda with you, they’ll… change their minds."),
            }),
        },
    },
    {
        label = "About Asagao",
        lines = {
            ume("smile0", "You know, someday, I want to be strong like Asagao is."),
            ume("smile1", "They may not look like it, but they’re super good at all that they do. A great musician, a great magic user, a great fighter even, believe it or not."),
            ume("neu1", "Most people probably think they just don’t do much, since no one can see their face or hear them talk, and since they usually stand pretty still."),
            ask({ "Have you ever seen under their mask?", "Interesting." }, { "2" }, {
                ume("neu2", "Well, maybe only once. I was really little, and there was some kind of big fight going on in the village. They wanted to try and calm things down."),
                ume("fear0", "Caught in the crossfire of the fight, their mask broke and a chunk came off. Everyone was pretty shocked, and they rushed back home pretty quickly."),
                ume("silly0", "I saw only for a moment, and I remember it quite vividly. From that point on though, they wore a much stronger mask, and even a mask underneath, because I wanted to take off their mask after I saw for the first time."),
            }),
        },
    },
    {
        label = "About Ume",
        lines = {
            face("sad0"),
            say("Ume looks kind of down, walking by your side."),
            ask({ "You ask “what’s the matter?”", "You ignore Ume." }, { "2" }, {
                ume("neu1", "Oh, um… I dunno… Just thinking about some stuff."),
                ume("sad0", "There’s some mean kids at school. They make fun of me for “looking girly”, and for my pink hair. They say it’s “super uncool compared to my family”..."),
                ume("sad0", "They also call me “stupid” and names like that, because I’m not as good at sports, and things like that."),
                ume("sad0", "I try not to listen to them, like Sister and Asagao say I should. I don’t think I’m that weird… but… y’know… It still makes me sad."),
                ask({ "Y’know what? I LOVE your hair, Ume.", "Don’t worry about that. Let’s focus on the Pagoda.", "…" }, { "3" }, nil, {
                    ["1"] = {
                        ume("shock0", "...Really…!? You do…?"),
                        ume("gentle0", "(Wow… Someone so cool… thinks I’m cool…?!)"),
                        ume("smile0", "U-Um… Thanks…! A lot!"),
                        ume("smile1", "I really like thinking of outfits, and sometimes Sister will ask me for my opinion on an outfit she’s thinking of wearing out. I think clothes are fun!"),
                        say("You’ve put a big ol’ smile on Ume’s face. He doesn’t seem so down anymore."),
                    },
                    ["2"] = {
                        face("neu2"),
                        say("Ume looks at you, and tightens his expression."),
                        ume("neu2", "... You’re right. There’s bigger problems to take care of now."),
                        say("He fixes his posture, and runs ahead."),
                        { leave = "ume" },
                    },
                }),
            }),
        },
    },
    {
        label = "About the Pagoda",
        lines = {
            ume("smile0", "Walking through this place always feels nice. The others seem to ignore it because the pagoda’s in bad shape, but maybe that’s a good thing!"),
            ume("sad0", "I like having the place to myself, nice and quiet. I used to pick wildberries along the trail with a kind gramma from the village, but she passed away a couple years ago…"),
            ume("smile1", "I’m not sad, though! I visit her in the pagoda, and pick wildberries just like before. Only the yummy ones, of course!"),
            ume("smile0", "Asagao and Sister used to tell me all kinds of stories about how our village was made, so I spend lots of time with my ancestors too. They were such amazing foxes!"),
            ume("gentle1", "Even though sometimes hard things happen, I need to make sure they know I’m happy in this village!"),
        },
    },
    {
        label = "About the Fox People",
        lines = {
            ume("neu1", "I wish I could transform better."),
            ask({ "…What?", "... Transform into what?", "...Sure, I guess????", "..." }, { "4" }, {
                ume("shock0", "Have you never seen us do that before? I’ll explain."),
                ume("neu2", "Basically, the people with the fox ears in the village can ACTUALLY transform into foxes."),
                ume("silly0", "Some people can shift between the two easily, like Asagao can. Some people… don’t know how to yet. Like me!"),
                ume("neu2", "It’s pretty rare to see though, since being in this human form is how we normally are, and it’s kinda better for everything we like to do."),
                ume("silly0", "Sometimes, I can get my arm a little fluffy, but that’s all… So, I want to be better at it someday!"),
                ask({ "I’m sure you can, Ume!", "(Hard to believe, but sure, I guess??)", "… Huh." }, { "2", "3" }, {
                    face("smile1"),
                    say("Ume giggles and smiles at your response, before looking ahead again."),
                }),
            }),
        },
    },
}

-- every text the scenes can show, for the translation files
function M.texts()
    local out, seen = {}, {}
    local function add(s) if s ~= nil and s ~= "" and not seen[s] then seen[s] = true ; out[#out + 1] = s end end
    local function walk(lines)
        for _, l in ipairs(lines or {}) do
            add(l.text)
            for _, c in ipairs(l.choices or {}) do add(c.label) end
            for _, b in pairs(l.branch or {}) do walk(b) end
        end
    end
    add("It seems that we have a bit of free time to talk a little.")
    add("Tell me...")
    for _, t in ipairs(TOPICS) do add(t.label) ; walk(t.lines) end
    add("Ume looks confused for a split second, before looking ahead again.")
    for _, l in ipairs({ 1, 5, 11 }) do add(closing(l)) end
    return out
end

-- the scene after clearing `level`, TOPIC_COUNT topics drawn at random; random = fn() -> [0, 1)
function M.scene(level, random)
    random = random or math.random
    local order = {}
    for i = 1, #TOPICS do order[i] = i end
    for i = 1, TOPIC_COUNT do
        local j = i + math.floor(random() * (#order - i + 1))
        order[i], order[j] = order[j], order[i]
    end
    local choices, branch = {}, {}
    for i = 1, TOPIC_COUNT do
        local t = TOPICS[order[i]]
        choices[i] = { label = t.label, value = tostring(i) }
        branch[tostring(i)] = t.lines
    end
    choices[TOPIC_COUNT + 1] = { label = "...", value = "skip" }
    branch["skip"] = {
        face("shock0"),
        say("Ume looks confused for a split second, before looking ahead again."),
        face("neu0"),
    }
    local bg, music = setting(level)
    return {
        bg = bg, music = music,
        lines = {
            { enter = "ume", expr = "neu0" },
            say("It seems that we have a bit of free time to talk a little."),
            { who = "you", text = "Tell me..." },
            { text = "", choices = choices, branch = branch },
            { fade = "out" },
            say(closing(level)),
            { outcome = "proceed" },
        },
    }
end

-- the scene to play after clearing `level`, or nil
function M.roll(level, random)
    random = random or math.random
    if not M.eligible(level) or random() >= CHANCE then return nil end
    return M.scene(level, random)
end

return M

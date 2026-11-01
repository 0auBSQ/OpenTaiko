-- Item texts: Ume's presenting lines (story/entrance.lua format), the popup and the focused description.

return {
    charm = {
        present = {
            { who = "ume", sprite = "normal", text = "Oh, um, earlier today, I found an old thing that may help us…" },
            { who = "ume", sprite = "normal", text = "It’s a charm that Asagao gave me. I was told that in times where I am lost with no way out, that this might help." },
            { who = "ume", sprite = "scared", text = "It glows with some strange power… I’m kind of scared of it." },
            { who = "ume", sprite = "brave", text = "Let me know when you think I should use it." },
        },
        name = "Asagao’s Gift",
        subtitle = "The Lineage's Foresight",
        explain = {
            "Ume’s spell can raise your awareness, and alter the way your songs will be played.",
            "Simply put, this charm will reveal the names of any hidden songs on any floor. It can let you prepare more properly.",
            "It can’t be used often, so use it only when you really need it!",
        },
        hint = "Reveal the hidden songs and the existence (or not) of a purple pick, reusable after each checkpoint.",
    },
    tambourine = {
        present = {
            { who = "ume", sprite = "normal", text = "Oh, um… I had an idea that might help…" },
            { who = "ume", sprite = "normal", text = "I’ve been practicing a little trick with my tambourine for the last while, and it’s started glowing when I perform that trick." },
            { who = "ume", sprite = "scared", text = "I asked my older sister about it, and she said I made a spell out of this trick. I dunno how, honestly." },
            { who = "ume", sprite = "brave", text = "When I finally pulled it off in front of her, she believed it can “alter a commotion”, and, in a way, we’re kinda in a commotion, so it may help." },
            { who = "ume", sprite = "normal", text = "I only see its power when it’s not done a lot, so just tell me when you think it would be good… or something…" },
        },
        name = "Ume’s Tambourine",
        subtitle = "Altering a commotion",
        explain = {
            "Ume’s spell can raise your awareness, and alter the way your songs will be played.",
            "In this case, the spell means shuffling the songs you’re about to face with different ones. As he mentioned, it can’t be done often (Once at each major section), so use it only when you really need it!",
        },
        hint = "Shuffles the songs you’re about to face with different ones, reusable after each checkpoint.",
    },
}

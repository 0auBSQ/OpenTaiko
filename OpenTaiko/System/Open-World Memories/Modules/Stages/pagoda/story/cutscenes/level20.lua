-- Level-20 cutscene lines; event = "..." starts an animation of encounter/cutscenes/level20.lua, a wait line holds until it ends.

return {
    ending = { clear = "credits", fail = "flight" },

    clear = {
        { event = "corrupt", wait = true,
          text = "The ground shakes, and the intimidating figure before you distorts. You have achieved what was unthinkable." },
        { text = "Ume’s eyes widened, with those teardrops he was holding finally rolling down to his smile." },
        { who = "ume", text = "It’s… It’s done…?" },
        { text = "You look back at him, breathing heavily, letting your smile grow." },
        { text = "Tobiraph’s voice, weak, began to call out to you once more." },
        { name = "Tobiraph", text = "Beautiful. I cannot deny." },
        { name = "Tobiraph", text = "Your strength, growth, and perseverance is simply beyond my comprehension." },
        { name = "Tobiraph", text = "It is truly admirable." },
        { event = "tobi_fade", text = "Tobiraph begins to slightly fade from your view." },
        { name = "Tobiraph", text = "Perhaps this experience has been more informative and worthwhile than I initially foretold." },
        { name = "Tobiraph", text = "I may not be removed from consciousness, but I will leave your beloved setting in memorial of your achievement." },
        { event = "ascend", text = "Tobiraph begins to ascend into the sky." },
        { name = "Tobiraph", text = "If perhaps, you would ever wish to acknowledge me once more, and continue your hardships, I will instead pollute your sky here with the beauty you have shown me." },
        { name = "Tobiraph", text = "Worry not, as it will only remain within the coil of life that will be born here. Capturing the energy of this land is no longer efficient." },
        { name = "Tobiraph", text = "Thank you, and farewell." },
        { event = "star", wait = true },
        { event = "gleam", text = "Tobiraph fades away into the sky, dissipating the overcast into a bright blue, gleaming hue." },
        { event = "finial", text = "Your eyes are guided to the finial of the pagoda, glowing with bright warmth." },
        { event = "bean", wait = true },
        { text = "Soon enough, a beanstalk begins to emerge?!" },
        { who = "ume", event = "hop", text = "… WHAT!! That’s so cool!! Isn’t that cool!!" },
        {
            text = "",
            choices = {
                { label = "YES!! IT IS!!", value = "1" },
                { label = "It’s pretty.", value = "2" },
                { label = "Ew. It’s gross.", value = "3" },
            },
        },
        { text = "(This must mean that Tobiraph still has minions awaiting on this beanstalk. I can’t see the end of it, so it really would be just to “continue my hardships”.)" },
        { text = "Ume runs towards you and gives you a big hug." },
        { event = "final", wait = true },
        { who = "ume", text = "Thank you! For everything! You’ve taught me so much, and even though I was scared, I had so much fun! I’ll never forget it!" },
        {
            text = "",
            choices = {
                { label = "You pat Ume on the head and accept his hug.", value = "1" },
                { label = "You stand there, and look into the blue sky.", value = "2" },
                { label = "You pull Ume off of you.", value = "3" },
            },
            branch = {
                ["1"] = {
                    { who = "you", text = "I had fun too. Tons of it! It wasn’t easy, but I’m so happy I got to know you better. I’m so proud of you, Ume." },
                },
                ["2"] = {
                    { text = "The vibrant sky swallows your mind. It’s finally over. A smile is painted on the faces of you two, so warm and vibrant, that it will never be forgotten." },
                },
                ["3"] = {
                    { who = "you", text = "Gimme a break, you miniature buffoon!" },
                    { text = "Ume doesn’t seem to care, and keeps hugging you." },
                    { text = "You give up, and just take in the scenery." },
                },
            },
        },
        { text = "The air smells so fresh." },
    },

    fail = {
        { name = "Tobiraph", text = "This is fate. Farewell once more, youthful ones." },
        { event = "wind", wait = true },
    },
}

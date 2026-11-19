-- Shop.Seasonal: featured items pinned into the big slot while their date pattern matches
-- highest priority wins (ties: first listed); pattern syntax in Calendar/almanac.lua
--   code      Items.db3 code (ownership = its OneTime "<RefText>_owned" trigger)
--   when      pattern, token, or a list of either (JST)
--   priority  higher wins
--   sticky    once matched for a save file, stays active for that file

return {
    { code = "enc:cy75f47r444xjr5n", priority = 100, sticky = true, when = "enc:ij5jfkudu5rd2cbvwyjkgpeyxa" },
    -- { code = "furn_cake", priority = 50, when = "*-*-01" },        -- the 1st of every month
    -- { code = "furn_tree", priority = 80, when = "*-12-20+12" },    -- Dec 20 .. Dec 31, yearly
}

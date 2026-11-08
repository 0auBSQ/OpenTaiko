-- Vault.TalkLines: the small talks' lines in order (lang/<code>/talk.json holds their text by key).
-- node = { g = { group, ... } }, group = { t = title, alts = { items, ... } } (one alternative is picked)
-- item: { "l", who (C Mr.Caspian, G Gris, U Gris unnamed, O Mr.Caspian unnamed, N narration), sprite, key [, "fade"] } | { "c", options } | { "a", action }
-- option: { k = label key, items = the lines it leads to (or alts = { items, ... }, one picked),
--   again = asks the same choice again without it }
return {
    OFFER = { g = {
        { t = "One of", alts = {
            {
                { "l", "C", "idle", "OFFER_01" },
            },
            {
                { "l", "C", "laugh", "OFFER_02" },
            },
            {
                { "l", "C", "idle", "OFFER_03" },
            },
            {
                { "l", "C", "idle", "OFFER_04" },
            },
        } },
        { t = "The answer", alts = {
            {
                { "c", {
                    { k = "OFFER_o01", items = {} },
                    { k = "OFFER_o02", alts = {
                        {
                            { "l", "C", "idle", "OFFER_05" },
                        },
                        {
                            { "l", "C", "laugh", "OFFER_06" },
                        },
                    } },
                } },
            },
        } },
    } },
    OPEN = { g = {
        { t = "First talk ever", alts = {
            {
                { "l", "N", "", "OPEN_01" },
                { "l", "C", "idle", "OPEN_02" },
            },
        } },
        { t = "After 1 to 4 talks, one of", alts = {
            {
                { "l", "C", "idle", "OPEN_03" },
            },
            {
                { "l", "C", "laugh", "OPEN_04" },
            },
            {
                { "l", "C", "idle", "OPEN_05" },
            },
        } },
        { t = "After 5 to 11 talks, one of", alts = {
            {
                { "l", "C", "idle", "OPEN_06" },
            },
            {
                { "l", "C", "laugh", "OPEN_07" },
            },
            {
                { "l", "C", "idle", "OPEN_08" },
            },
        } },
        { t = "After 12 talks or more, one of", alts = {
            {
                { "l", "C", "idle", "OPEN_09" },
            },
            {
                { "l", "C", "laugh", "OPEN_10" },
            },
            {
                { "l", "C", "idle", "OPEN_11" },
            },
        } },
    } },
    CLOSE = { g = {
        { t = "After a topic, one of", alts = {
            {
                { "l", "C", "idle", "CLOSE_01" },
            },
            {
                { "l", "C", "laugh", "CLOSE_02" },
            },
            {
                { "l", "C", "idle", "CLOSE_03" },
            },
            {
                { "l", "C", "idle", "CLOSE_04" },
            },
        } },
        { t = "\"That's all for today.\", one of", alts = {
            {
                { "l", "C", "idle", "CLOSE_05" },
            },
            {
                { "l", "C", "laugh", "CLOSE_06" },
            },
        } },
    } },
    S1 = { g = {
        { t = "", alts = {
            {
                { "l", "N", "", "S1_01" },
                { "l", "C", "idle", "S1_02" },
                { "l", "C", "idle", "S1_03" },
                { "l", "C", "laugh", "S1_04" },
                { "l", "C", "idle", "S1_05" },
                { "l", "C", "idle", "S1_06" },
                { "l", "C", "idle", "S1_07" },
                { "l", "C", "idle", "S1_08" },
                { "l", "C", "laugh", "S1_09" },
                { "c", {
                    { k = "S1_o01", items = {
                        { "l", "C", "laugh", "S1_10" },
                        { "l", "C", "idle", "S1_11" },
                    } },
                    { k = "S1_o02", items = {
                        { "l", "C", "idle", "S1_12" },
                        { "l", "C", "laugh", "S1_13" },
                    } },
                    { k = "S1_o03", items = {
                        { "l", "C", "idle", "S1_14" },
                        { "l", "C", "laugh", "S1_15" },
                    } },
                } },
            },
        } },
    } },
    S2 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S2_01" },
                { "l", "N", "", "S2_02" },
                { "l", "C", "laugh", "S2_03" },
                { "l", "C", "idle", "S2_04" },
                { "l", "C", "idle", "S2_05" },
                { "l", "C", "idle", "S2_06" },
                { "l", "C", "idle", "S2_07" },
                { "l", "C", "laugh", "S2_08" },
                { "c", {
                    { k = "S2_o01", items = {
                        { "l", "C", "laugh", "S2_09" },
                        { "l", "C", "idle", "S2_10" },
                    } },
                    { k = "S2_o02", items = {
                        { "l", "C", "idle", "S2_11" },
                        { "l", "C", "stern", "S2_12" },
                    } },
                } },
            },
        } },
    } },
    S4 = { g = {
        { t = "", alts = {
            {
                { "l", "N", "", "S4_01" },
                { "l", "C", "laugh", "S4_02" },
                { "l", "C", "idle", "S4_03" },
                { "l", "C", "idle", "S4_04" },
                { "l", "C", "idle", "S4_05" },
                { "l", "C", "laugh", "S4_06" },
                { "l", "C", "idle", "S4_07" },
                { "l", "C", "idle", "S4_08" },
                { "c", {
                    { k = "S4_o01", items = {
                        { "l", "C", "laugh", "S4_09" },
                    } },
                    { k = "S4_o02", items = {
                        { "l", "C", "stern", "S4_10" },
                    } },
                } },
            },
        } },
    } },
    S3 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S3_01" },
                { "l", "N", "", "S3_02" },
                { "l", "C", "idle", "S3_03" },
                { "l", "C", "laugh", "S3_04" },
                { "l", "C", "idle", "S3_05" },
                { "l", "C", "idle", "S3_06" },
                { "l", "C", "laugh", "S3_07" },
                { "c", {
                    { k = "S3_o01", items = {
                        { "l", "C", "laugh", "S3_08" },
                        { "l", "C", "idle", "S3_09" },
                    } },
                    { k = "S3_o02", items = {
                        { "l", "C", "idle", "S3_10" },
                        { "l", "C", "laugh", "S3_11" },
                    } },
                } },
            },
        } },
    } },
    S5 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S5_01" },
                { "l", "C", "laugh", "S5_02" },
                { "l", "C", "idle", "S5_03" },
                { "l", "C", "idle", "S5_04" },
                { "l", "C", "idle", "S5_05" },
                { "l", "C", "stern", "S5_06" },
                { "c", {
                    { k = "S5_o01", items = {
                        { "l", "C", "laugh", "S5_07" },
                        { "l", "C", "idle", "S5_08" },
                    } },
                    { k = "S5_o02", items = {
                        { "l", "C", "idle", "S5_09" },
                        { "l", "C", "laugh", "S5_10" },
                    } },
                } },
            },
        } },
    } },
    S8 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S8_01" },
                { "l", "N", "", "S8_02" },
                { "l", "C", "idle", "S8_03" },
                { "l", "C", "idle", "S8_04" },
                { "l", "C", "laugh", "S8_05" },
                { "l", "C", "idle", "S8_06" },
                { "l", "C", "idle", "S8_07" },
                { "l", "C", "laugh", "S8_08" },
                { "l", "C", "idle", "S8_09" },
                { "c", {
                    { k = "S8_o01", items = {
                        { "l", "C", "idle", "S8_10" },
                        { "l", "C", "laugh", "S8_11" },
                    } },
                    { k = "S8_o02", items = {
                        { "l", "C", "laugh", "S8_12" },
                        { "l", "C", "idle", "S8_13" },
                    } },
                } },
            },
        } },
    } },
    S19 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S19_01" },
                { "l", "N", "", "S19_02" },
                { "l", "C", "idle", "S19_03" },
                { "l", "C", "idle", "S19_04" },
                { "l", "C", "idle", "S19_05" },
                { "l", "C", "laugh", "S19_06" },
                { "c", {
                    { k = "S19_o01", items = {
                        { "l", "C", "stern", "S19_07" },
                        { "l", "C", "idle", "S19_08" },
                    } },
                    { k = "S19_o02", items = {
                        { "l", "C", "laugh", "S19_09" },
                        { "l", "C", "idle", "S19_10" },
                    } },
                } },
            },
        } },
    } },
    S28 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S28_01" },
                { "l", "C", "idle", "S28_02" },
                { "l", "C", "idle", "S28_03" },
                { "l", "C", "laugh", "S28_04" },
                { "l", "N", "", "S28_05" },
                { "l", "C", "idle", "S28_06" },
                { "l", "C", "laugh", "S28_07" },
                { "l", "C", "stern", "S28_08" },
                { "c", {
                    { k = "S28_o01", items = {
                        { "l", "C", "laugh", "S28_09" },
                    } },
                    { k = "S28_o02", items = {
                        { "l", "C", "idle", "S28_10" },
                    } },
                } },
            },
        } },
    } },
    S6a = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S6a_01" },
                { "l", "C", "idle", "S6a_02" },
                { "l", "C", "idle", "S6a_03" },
                { "l", "C", "idle", "S6a_04" },
                { "l", "C", "idle", "S6a_05" },
                { "l", "N", "", "S6a_06" },
                { "l", "C", "laugh", "S6a_07" },
                { "l", "C", "idle", "S6a_08" },
                { "l", "C", "idle", "S6a_09" },
                { "c", {
                    { k = "S6a_o01", items = {
                        { "l", "C", "laugh", "S6a_10" },
                        { "l", "C", "idle", "S6a_11" },
                    } },
                    { k = "S6a_o02", items = {
                        { "l", "C", "idle", "S6a_12" },
                        { "l", "C", "laugh", "S6a_13" },
                    } },
                } },
            },
        } },
    } },
    S7 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S7_01" },
                { "l", "C", "idle", "S7_02" },
                { "l", "C", "laugh", "S7_03" },
                { "l", "C", "idle", "S7_04" },
                { "l", "C", "idle", "S7_05" },
                { "l", "C", "laugh", "S7_06" },
                { "l", "C", "idle", "S7_07" },
                { "l", "C", "stern", "S7_08" },
                { "c", {
                    { k = "S7_o01", items = {
                        { "l", "C", "idle", "S7_09" },
                        { "l", "C", "laugh", "S7_10" },
                    } },
                    { k = "S7_o02", items = {
                        { "l", "C", "idle", "S7_11" },
                        { "l", "C", "laugh", "S7_12" },
                    } },
                } },
            },
        } },
    } },
    S9 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S9_01" },
                { "l", "C", "idle", "S9_02" },
                { "l", "N", "", "S9_03" },
                { "l", "C", "idle", "S9_04" },
                { "l", "C", "idle", "S9_05" },
                { "l", "C", "laugh", "S9_06" },
                { "c", {
                    { k = "S9_o01", items = {
                        { "l", "C", "laugh", "S9_07" },
                        { "l", "C", "idle", "S9_08" },
                    } },
                    { k = "S9_o02", items = {
                        { "l", "C", "idle", "S9_09" },
                        { "l", "C", "idle", "S9_10" },
                    } },
                } },
            },
        } },
    } },
    S10 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S10_01" },
                { "l", "C", "idle", "S10_02" },
                { "l", "C", "idle", "S10_03" },
                { "l", "C", "idle", "S10_04" },
                { "l", "N", "", "S10_05" },
                { "l", "C", "laugh", "S10_06" },
                { "c", {
                    { k = "S10_o01", items = {
                        { "l", "C", "idle", "S10_07" },
                        { "l", "C", "laugh", "S10_08" },
                    } },
                    { k = "S10_o02", items = {
                        { "l", "C", "laugh", "S10_09" },
                        { "l", "C", "idle", "S10_10" },
                    } },
                } },
            },
        } },
    } },
    X = { g = {
        { t = "", alts = {
            {
                { "l", "N", "", "X_01" },
                { "l", "C", "idle", "X_02" },
                { "l", "C", "stern", "X_03" },
                { "l", "C", "idle", "X_04" },
                { "l", "C", "idle", "X_05" },
                { "l", "C", "idle", "X_06" },
                { "l", "C", "idle", "X_07" },
                { "c", {
                    { k = "X_o01", items = {
                        { "l", "C", "idle", "X_08" },
                    } },
                    { k = "X_o02", items = {
                        { "l", "C", "stern", "X_09" },
                        { "l", "C", "idle", "X_10" },
                    } },
                } },
            },
        } },
    } },
    S27 = { g = {
        { t = "", alts = {
            {
                { "l", "N", "", "S27_01" },
                { "l", "C", "stern", "S27_02" },
                { "l", "C", "idle", "S27_03" },
                { "l", "C", "stern", "S27_04" },
                { "l", "C", "idle", "S27_05" },
                { "l", "C", "laugh", "S27_06" },
                { "l", "C", "idle", "S27_07" },
                { "l", "C", "idle", "S27_08" },
                { "l", "C", "stern", "S27_09" },
                { "c", {
                    { k = "S27_o01", items = {
                        { "l", "C", "stern", "S27_10" },
                        { "l", "C", "idle", "S27_11" },
                    } },
                    { k = "S27_o02", items = {
                        { "l", "C", "laugh", "S27_12" },
                    } },
                } },
            },
        } },
    } },
    S6b = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S6b_01" },
                { "l", "C", "idle", "S6b_02" },
                { "l", "N", "", "S6b_03" },
                { "l", "C", "idle", "S6b_04" },
                { "l", "C", "idle", "S6b_05" },
                { "l", "C", "stern", "S6b_06" },
                { "c", {
                    { k = "S6b_o01", items = {
                        { "l", "C", "laugh", "S6b_07" },
                        { "l", "C", "idle", "S6b_08" },
                    } },
                    { k = "S6b_o02", items = {
                        { "l", "C", "idle", "S6b_09" },
                        { "l", "C", "laugh", "S6b_10" },
                    } },
                } },
            },
        } },
    } },
    S11 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S11_01" },
                { "l", "N", "", "S11_02" },
                { "l", "C", "idle", "S11_03" },
                { "l", "C", "idle", "S11_04" },
                { "l", "C", "idle", "S11_05" },
                { "l", "C", "stern", "S11_06" },
                { "l", "C", "laugh", "S11_07" },
                { "l", "C", "idle", "S11_08" },
                { "c", {
                    { k = "S11_o01", items = {
                        { "l", "C", "idle", "S11_09" },
                        { "l", "C", "laugh", "S11_10" },
                    } },
                    { k = "S11_o02", items = {
                        { "l", "C", "laugh", "S11_11" },
                        { "l", "C", "idle", "S11_12" },
                    } },
                } },
            },
        } },
    } },
    S12 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S12_01" },
                { "l", "C", "idle", "S12_02" },
                { "l", "C", "idle", "S12_03" },
                { "l", "C", "idle", "S12_04" },
                { "l", "C", "idle", "S12_05" },
                { "l", "C", "laugh", "S12_06" },
                { "l", "C", "idle", "S12_07" },
                { "c", {
                    { k = "S12_o01", items = {
                        { "l", "C", "laugh", "S12_08" },
                        { "l", "C", "idle", "S12_09" },
                    } },
                    { k = "S12_o02", items = {
                        { "l", "C", "stern", "S12_10" },
                        { "l", "C", "laugh", "S12_11" },
                    } },
                } },
            },
        } },
    } },
    S17 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S17_01" },
                { "l", "C", "idle", "S17_02" },
                { "l", "C", "idle", "S17_03" },
                { "l", "C", "laugh", "S17_04" },
                { "l", "C", "idle", "S17_05" },
                { "c", {
                    { k = "S17_o01", items = {
                        { "l", "C", "laugh", "S17_06" },
                        { "l", "C", "idle", "S17_07" },
                    } },
                    { k = "S17_o02", items = {
                        { "l", "C", "idle", "S17_08" },
                        { "l", "C", "laugh", "S17_09" },
                    } },
                } },
            },
        } },
    } },
    S18 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "stern", "S18_01" },
                { "l", "C", "laugh", "S18_02" },
                { "l", "C", "idle", "S18_03" },
                { "l", "C", "idle", "S18_04" },
                { "l", "C", "idle", "S18_05" },
                { "l", "C", "idle", "S18_06" },
                { "c", {
                    { k = "S18_o01", items = {
                        { "l", "C", "idle", "S18_07" },
                        { "l", "C", "idle", "S18_08" },
                    } },
                    { k = "S18_o02", items = {
                        { "l", "C", "laugh", "S18_09" },
                        { "l", "C", "stern", "S18_10" },
                    } },
                } },
            },
        } },
    } },
    S13 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S13_01" },
                { "l", "C", "idle", "S13_02" },
                { "l", "C", "idle", "S13_03" },
                { "l", "C", "laugh", "S13_04" },
                { "l", "C", "laugh", "S13_05" },
                { "l", "C", "idle", "S13_06" },
            },
        } },
    } },
    S6c = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S6c_01" },
                { "l", "C", "idle", "S6c_02" },
                { "l", "C", "idle", "S6c_03" },
                { "l", "C", "idle", "S6c_04" },
                { "l", "N", "", "S6c_05" },
                { "l", "C", "stern", "S6c_06" },
                { "l", "C", "idle", "S6c_07" },
                { "l", "C", "laugh", "S6c_08" },
            },
        } },
    } },
    S16 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S16_01" },
                { "l", "C", "idle", "S16_02" },
                { "l", "C", "laugh", "S16_03" },
                { "l", "C", "stern", "S16_04" },
                { "l", "C", "laugh", "S16_05" },
                { "l", "C", "idle", "S16_06" },
                { "l", "C", "idle", "S16_07" },
                { "l", "C", "idle", "S16_08" },
                { "c", {
                    { k = "S16_o01", items = {
                        { "l", "C", "laugh", "S16_09" },
                        { "l", "C", "idle", "S16_10" },
                    } },
                    { k = "S16_o02", items = {
                        { "l", "C", "idle", "S16_11" },
                    } },
                } },
            },
        } },
    } },
    S14 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S14_01" },
                { "l", "N", "", "S14_02" },
                { "l", "C", "idle", "S14_03" },
                { "l", "C", "idle", "S14_04" },
                { "l", "C", "laugh", "S14_05" },
                { "l", "C", "idle", "S14_06" },
                { "l", "C", "laugh", "S14_07" },
                { "l", "C", "idle", "S14_08" },
                { "l", "C", "idle", "S14_09" },
                { "l", "C", "laugh", "S14_10" },
                { "l", "C", "idle", "S14_11" },
                { "c", {
                    { k = "S14_o01", items = {
                        { "l", "C", "laugh", "S14_12" },
                    } },
                    { k = "S14_o02", items = {
                        { "l", "C", "stern", "S14_13" },
                        { "l", "C", "laugh", "S14_14" },
                    } },
                } },
            },
        } },
    } },
    S26 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S26_01" },
                { "l", "N", "", "S26_02" },
                { "l", "C", "idle", "S26_03" },
                { "l", "C", "idle", "S26_04" },
                { "l", "C", "idle", "S26_05" },
                { "l", "C", "idle", "S26_06" },
                { "c", {
                    { k = "S26_o01", items = {
                        { "l", "C", "laugh", "S26_07" },
                    } },
                    { k = "S26_o02", items = {
                        { "l", "C", "stern", "S26_08" },
                        { "l", "C", "laugh", "S26_09" },
                    } },
                } },
            },
        } },
    } },
    S29 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S29_01" },
                { "l", "C", "idle", "S29_02" },
                { "l", "C", "idle", "S29_03" },
                { "l", "C", "stern", "S29_04" },
                { "l", "N", "", "S29_05" },
                { "l", "C", "idle", "S29_06" },
                { "l", "C", "idle", "S29_07" },
                { "l", "C", "laugh", "S29_08" },
                { "l", "C", "idle", "S29_09" },
                { "l", "C", "idle", "S29_10" },
                { "l", "C", "laugh", "S29_11" },
                { "c", {
                    { k = "S29_o01", items = {
                        { "l", "C", "stern", "S29_12" },
                        { "l", "C", "laugh", "S29_13" },
                    } },
                    { k = "S29_o02", items = {
                        { "l", "C", "idle", "S29_14" },
                        { "l", "C", "idle", "S29_15" },
                    } },
                } },
            },
        } },
    } },
    S30 = { g = {
        { t = "", alts = {
            {
                { "l", "N", "", "S30_01" },
                { "l", "C", "laugh", "S30_02" },
                { "l", "N", "", "S30_03" },
                { "l", "C", "idle", "S30_04" },
                { "l", "C", "idle", "S30_05" },
                { "l", "C", "laugh", "S30_06" },
                { "l", "C", "idle", "S30_07" },
                { "l", "C", "idle", "S30_08" },
                { "l", "C", "idle", "S30_09" },
                { "c", {
                    { k = "S30_o01", items = {
                        { "l", "C", "idle", "S30_10" },
                    } },
                    { k = "S30_o02", items = {
                        { "l", "C", "laugh", "S30_11" },
                    } },
                } },
            },
        } },
    } },
    S15 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "S15_01" },
                { "l", "C", "idle", "S15_02" },
                { "l", "C", "laugh", "S15_03" },
                { "l", "C", "idle", "S15_04" },
                { "l", "C", "idle", "S15_05" },
                { "l", "C", "idle", "S15_06" },
                { "l", "C", "laugh", "S15_07" },
            },
        } },
    } },
    S25 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "S25_01" },
                { "l", "C", "idle", "S25_02" },
                { "l", "C", "idle", "S25_03" },
                { "l", "C", "laugh", "S25_04" },
                { "l", "C", "idle", "S25_05" },
                { "l", "C", "idle", "S25_06" },
                { "l", "C", "laugh", "S25_07" },
            },
        } },
    } },
    R1 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "idle", "R1_01" },
                { "l", "N", "", "R1_02" },
                { "l", "C", "idle", "R1_03" },
                { "l", "C", "laugh", "R1_04" },
                { "l", "C", "idle", "R1_05" },
                { "l", "C", "stern", "R1_06" },
                { "l", "C", "idle", "R1_07" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "laugh", "R1_08" },
                { "l", "C", "idle", "R1_09" },
                { "l", "C", "stern", "R1_10" },
                { "l", "C", "idle", "R1_11" },
                { "l", "C", "idle", "R1_12" },
                { "l", "C", "laugh", "R1_13" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "R1_14" },
                { "l", "N", "", "R1_15" },
                { "l", "C", "idle", "R1_16" },
                { "l", "C", "laugh", "R1_17" },
                { "l", "C", "idle", "R1_18" },
                { "l", "C", "idle", "R1_19" },
            },
        } },
        { t = "Variant 4", alts = {
            {
                { "l", "C", "idle", "R1_20" },
                { "l", "C", "idle", "R1_21" },
                { "l", "C", "idle", "R1_22" },
                { "l", "C", "laugh", "R1_23" },
                { "l", "N", "", "R1_24" },
                { "l", "C", "stern", "R1_25" },
                { "l", "C", "laugh", "R1_26" },
            },
        } },
    } },
    R6 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "laugh", "R6_01" },
                { "l", "C", "idle", "R6_02" },
                { "l", "C", "stern", "R6_03" },
                { "l", "C", "idle", "R6_04" },
                { "l", "C", "laugh", "R6_05" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "idle", "R6_06" },
                { "l", "C", "idle", "R6_07" },
                { "l", "C", "idle", "R6_08" },
                { "l", "C", "laugh", "R6_09" },
                { "l", "C", "stern", "R6_10" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "stern", "R6_11" },
                { "l", "C", "idle", "R6_12" },
                { "l", "C", "laugh", "R6_13" },
                { "l", "C", "idle", "R6_14" },
                { "l", "C", "laugh", "R6_15" },
            },
        } },
        { t = "Variant 4", alts = {
            {
                { "l", "C", "idle", "R6_16" },
                { "l", "C", "idle", "R6_17" },
                { "l", "C", "laugh", "R6_18" },
                { "l", "C", "idle", "R6_19" },
                { "l", "C", "stern", "R6_20" },
            },
        } },
    } },
    R10 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "idle", "R10_01" },
                { "l", "C", "idle", "R10_02" },
                { "l", "C", "laugh", "R10_03" },
                { "l", "C", "idle", "R10_04" },
                { "l", "C", "laugh", "R10_05" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "laugh", "R10_06" },
                { "l", "C", "idle", "R10_07" },
                { "l", "C", "idle", "R10_08" },
                { "l", "N", "", "R10_09" },
                { "l", "C", "laugh", "R10_10" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "R10_11" },
                { "l", "N", "", "R10_12" },
                { "l", "C", "laugh", "R10_13" },
                { "l", "C", "idle", "R10_14" },
                { "l", "C", "idle", "R10_15" },
            },
        } },
        { t = "Variant 4 (after S11)", alts = {
            {
                { "l", "C", "laugh", "R10_16" },
                { "l", "C", "idle", "R10_17" },
                { "l", "C", "idle", "R10_18" },
                { "l", "C", "laugh", "R10_19" },
                { "l", "C", "idle", "R10_20" },
            },
        } },
    } },
    R4 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "laugh", "R4_01" },
                { "l", "N", "", "R4_02" },
                { "l", "C", "idle", "R4_03" },
                { "l", "C", "stern", "R4_04" },
                { "l", "C", "laugh", "R4_05" },
                { "l", "C", "idle", "R4_06" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "stern", "R4_07" },
                { "l", "C", "idle", "R4_08" },
                { "l", "C", "idle", "R4_09" },
                { "l", "C", "laugh", "R4_10" },
                { "l", "C", "laugh", "R4_11" },
                { "l", "C", "idle", "R4_12" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "R4_13" },
                { "l", "C", "idle", "R4_14" },
                { "l", "C", "idle", "R4_15" },
                { "l", "C", "idle", "R4_16" },
                { "l", "C", "stern", "R4_17" },
            },
        } },
        { t = "Variant 4", alts = {
            {
                { "l", "C", "laugh", "R4_18" },
                { "l", "C", "idle", "R4_19" },
                { "l", "C", "stern", "R4_20" },
                { "l", "C", "idle", "R4_21" },
                { "l", "C", "laugh", "R4_22" },
            },
        } },
        { t = "Variant 5", alts = {
            {
                { "l", "C", "idle", "R4_23" },
                { "l", "C", "idle", "R4_24" },
                { "l", "C", "stern", "R4_25" },
                { "l", "C", "laugh", "R4_26" },
                { "l", "C", "idle", "R4_27" },
            },
        } },
    } },
    R5 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "idle", "R5_01" },
                { "l", "C", "idle", "R5_02" },
                { "l", "C", "stern", "R5_03" },
                { "l", "N", "", "R5_04" },
                { "l", "C", "laugh", "R5_05" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "idle", "R5_06" },
                { "l", "C", "idle", "R5_07" },
                { "l", "C", "laugh", "R5_08" },
                { "l", "C", "idle", "R5_09" },
                { "l", "C", "laugh", "R5_10" },
            },
        } },
        { t = "Variant 3 (after S10)", alts = {
            {
                { "l", "C", "laugh", "R5_11" },
                { "l", "C", "idle", "R5_12" },
                { "l", "C", "idle", "R5_13" },
                { "l", "C", "idle", "R5_14" },
                { "l", "C", "stern", "R5_15" },
            },
        } },
    } },
    R3 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "idle", "R3_01" },
                { "l", "C", "idle", "R3_02" },
                { "l", "C", "idle", "R3_03" },
                { "l", "C", "laugh", "R3_04" },
                { "l", "C", "idle", "R3_05" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "laugh", "R3_06" },
                { "l", "N", "", "R3_07" },
                { "l", "C", "idle", "R3_08" },
                { "l", "C", "stern", "R3_09" },
                { "l", "C", "idle", "R3_10" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "R3_11" },
                { "l", "C", "idle", "R3_12" },
                { "l", "C", "laugh", "R3_13" },
                { "l", "C", "idle", "R3_14" },
                { "l", "C", "idle", "R3_15" },
            },
        } },
        { t = "Variant 4 (after X)", alts = {
            {
                { "l", "C", "idle", "R3_16" },
                { "l", "C", "idle", "R3_17" },
                { "l", "N", "", "R3_18" },
                { "l", "C", "idle", "R3_19" },
            },
        } },
    } },
    R8 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "laugh", "R8_01" },
                { "l", "C", "idle", "R8_02" },
                { "l", "C", "stern", "R8_03" },
                { "l", "C", "laugh", "R8_04" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "idle", "R8_05" },
                { "l", "C", "idle", "R8_06" },
                { "l", "C", "laugh", "R8_07" },
                { "l", "C", "idle", "R8_08" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "R8_09" },
                { "l", "C", "idle", "R8_10" },
                { "l", "C", "laugh", "R8_11" },
                { "l", "N", "", "R8_12" },
            },
        } },
    } },
    R2 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "idle", "R2_01" },
                { "l", "C", "idle", "R2_02" },
                { "l", "C", "laugh", "R2_03" },
                { "l", "C", "idle", "R2_04" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "laugh", "R2_05" },
                { "l", "C", "idle", "R2_06" },
                { "l", "C", "idle", "R2_07" },
                { "l", "C", "stern", "R2_08" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "R2_09" },
                { "l", "C", "idle", "R2_10" },
                { "l", "C", "idle", "R2_11" },
                { "l", "C", "laugh", "R2_12" },
            },
        } },
        { t = "Variant 4 (after S11)", alts = {
            {
                { "l", "C", "idle", "R2_13" },
                { "l", "C", "laugh", "R2_14" },
                { "l", "N", "", "R2_15" },
                { "l", "C", "stern", "R2_16" },
            },
        } },
    } },
    R7 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "idle", "R7_01" },
                { "l", "C", "idle", "R7_02" },
                { "l", "C", "laugh", "R7_03" },
                { "l", "C", "stern", "R7_04" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "laugh", "R7_05" },
                { "l", "C", "idle", "R7_06" },
                { "l", "C", "laugh", "R7_07" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "R7_08" },
                { "l", "C", "idle", "R7_09" },
                { "l", "C", "idle", "R7_10" },
            },
        } },
        { t = "Variant 4", alts = {
            {
                { "l", "C", "stern", "R7_11" },
                { "l", "C", "idle", "R7_12" },
                { "l", "C", "laugh", "R7_13" },
                { "l", "C", "idle", "R7_14" },
            },
        } },
    } },
    R9 = { g = {
        { t = "Before E2, one of", alts = {
            {
                { "l", "C", "idle", "R9_01" },
                { "l", "C", "laugh", "R9_02" },
            },
            {
                { "l", "C", "stern", "R9_03" },
                { "l", "C", "idle", "R9_04" },
                { "l", "C", "idle", "R9_05" },
            },
            {
                { "l", "C", "idle", "R9_06" },
                { "l", "C", "idle", "R9_07" },
            },
        } },
        { t = "After E2, before calling her", alts = {
            {
                { "l", "C", "laugh", "R9_08" },
                { "l", "C", "idle", "R9_09" },
                { "l", "C", "idle", "R9_10" },
            },
        } },
        { t = "After a call that didn't go all the way (declined, missing or failed)", alts = {
            {
                { "l", "C", "idle", "R9_11" },
                { "l", "C", "laugh", "R9_12" },
                { "l", "C", "idle", "R9_13" },
            },
        } },
        { t = "After her song is beaten, before GP1", alts = {
            {
                { "l", "C", "laugh", "R9_14" },
                { "l", "C", "idle", "R9_15" },
                { "l", "C", "idle", "R9_16" },
                { "l", "C", "idle", "R9_17" },
            },
        } },
        { t = "After GP1", alts = {
            {
                { "l", "C", "idle", "R9_18" },
                { "l", "C", "laugh", "R9_19" },
            },
        } },
        { t = "After GP3", alts = {
            {
                { "l", "C", "laugh", "R9_20" },
                { "l", "C", "idle", "R9_21" },
            },
        } },
        { t = "After GP6", alts = {
            {
                { "l", "C", "idle", "R9_22" },
                { "l", "C", "laugh", "R9_23" },
                { "l", "C", "idle", "R9_24" },
            },
        } },
    } },
    E1 = { g = {
        { t = "", alts = {
            {
                { "l", "N", "", "E1_01" },
                { "l", "C", "idle", "E1_02" },
                { "l", "C", "idle", "E1_03" },
                { "l", "N", "", "E1_04" },
                { "l", "C", "laugh", "E1_05" },
                { "l", "C", "idle", "E1_06" },
                { "l", "C", "laugh", "E1_07" },
                { "c", {
                    { k = "E1_o01", items = {
                        { "l", "C", "laugh", "E1_08" },
                    } },
                    { k = "E1_o02", items = {
                        { "l", "C", "idle", "E1_09" },
                    } },
                } },
            },
        } },
    } },
    E2 = { g = {
        { t = "You never called her", alts = {
            {
                { "l", "N", "", "E2_01" },
                { "l", "C", "stern", "E2_02" },
                { "l", "C", "idle", "E2_03" },
                { "l", "N", "", "E2_04" },
                { "l", "C", "idle", "E2_05" },
                { "l", "C", "laugh", "E2_06" },
                { "l", "C", "laugh", "E2_07" },
            },
        } },
        { t = "You already called her, her song isn't beaten", alts = {
            {
                { "l", "C", "stern", "E2_08" },
                { "l", "C", "laugh", "E2_09" },
                { "l", "N", "", "E2_10" },
                { "l", "C", "idle", "E2_11" },
            },
        } },
        { t = "Her song is already beaten", alts = {
            {
                { "l", "C", "stern", "E2_12" },
                { "l", "C", "laugh", "E2_13" },
                { "l", "C", "idle", "E2_14" },
                { "l", "N", "", "E2_15" },
                { "l", "C", "idle", "E2_16" },
            },
        } },
    } },
    CALL = { g = {
        { t = "Picks up, one of", alts = {
            {
                { "l", "C", "idle", "CALL_01" },
            },
            {
                { "l", "C", "laugh", "CALL_02" },
            },
            {
                { "l", "C", "idle", "CALL_03" },
            },
        } },
        { t = "\"Bye for now.\"", alts = {
            {
                { "l", "C", "idle", "CALL_04" },
                { "l", "N", "", "CALL_05" },
            },
        } },
        { t = "Second call on the same day", alts = {
            {
                { "l", "N", "", "CALL_06" },
            },
        } },
        { t = "After the topic, one of", alts = {
            {
                { "l", "C", "laugh", "CALL_07" },
            },
            {
                { "l", "C", "idle", "CALL_08" },
            },
            {
                { "l", "C", "stern", "CALL_09" },
            },
        } },
        { t = "Then", alts = {
            {
                { "l", "N", "", "CALL_10" },
            },
        } },
    } },
    CALLEARLY = { g = {
        { t = "You never met him", alts = {
            {
                { "l", "C", "idle", "CALLEARLY_01" },
                { "c", {
                    { k = "CALLEARLY_o01", items = {
                        { "l", "C", "laugh", "CALLEARLY_02" },
                    } },
                    { k = "CALLEARLY_o02", items = {
                        { "l", "C", "idle", "CALLEARLY_03" },
                    } },
                } },
                { "l", "C", "idle", "CALLEARLY_04" },
                { "l", "N", "", "CALLEARLY_05" },
            },
        } },
        { t = "You already met him", alts = {
            {
                { "l", "C", "idle", "CALLEARLY_06" },
                { "l", "C", "stern", "CALLEARLY_07" },
                { "l", "C", "laugh", "CALLEARLY_08" },
                { "l", "C", "idle", "CALLEARLY_09" },
                { "l", "N", "", "CALLEARLY_10" },
            },
        } },
    } },
    CP1 = { g = {
        { t = "Variant 1", alts = {
            {
                { "l", "C", "idle", "CP1_01" },
                { "l", "C", "laugh", "CP1_02" },
                { "l", "C", "idle", "CP1_03" },
                { "l", "N", "", "CP1_04" },
                { "l", "C", "laugh", "CP1_05" },
            },
        } },
        { t = "Variant 2", alts = {
            {
                { "l", "C", "laugh", "CP1_06" },
                { "l", "C", "idle", "CP1_07" },
                { "l", "C", "idle", "CP1_08" },
            },
        } },
        { t = "Variant 3", alts = {
            {
                { "l", "C", "idle", "CP1_09" },
                { "l", "C", "laugh", "CP1_10" },
                { "l", "C", "idle", "CP1_11" },
            },
        } },
        { t = "Variant 4", alts = {
            {
                { "l", "C", "stern", "CP1_12" },
                { "l", "N", "", "CP1_13" },
                { "l", "C", "laugh", "CP1_14" },
                { "l", "C", "idle", "CP1_15" },
            },
        } },
    } },
    CP2 = { g = {
        { t = "First time", alts = {
            {
                { "l", "C", "idle", "CP2_01" },
                { "l", "N", "", "CP2_02" },
                { "l", "C", "laugh", "CP2_03" },
                { "l", "C", "idle", "CP2_04" },
            },
        } },
        { t = "Later, one of", alts = {
            {
                { "l", "C", "idle", "CP2_05" },
                { "l", "C", "laugh", "CP2_06" },
                { "l", "C", "idle", "CP2_07" },
            },
            {
                { "l", "C", "idle", "CP2_08" },
                { "l", "C", "laugh", "CP2_09" },
                { "l", "C", "idle", "CP2_10" },
            },
            {
                { "l", "C", "laugh", "CP2_11" },
                { "l", "C", "idle", "CP2_12" },
                { "l", "C", "idle", "CP2_13" },
            },
        } },
    } },
    CP3 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "CP3_01" },
                { "l", "N", "", "CP3_02" },
                { "l", "C", "stern", "CP3_03" },
                { "l", "C", "stern", "CP3_04" },
                { "l", "C", "idle", "CP3_05" },
                { "l", "C", "stern", "CP3_06" },
                { "l", "C", "idle", "CP3_07" },
                { "l", "C", "idle", "CP3_08" },
                { "l", "C", "idle", "CP3_09" },
                { "l", "C", "stern", "CP3_10" },
                { "c", {
                    { k = "CP3_o01", items = {
                        { "l", "C", "idle", "CP3_11" },
                    } },
                    { k = "CP3_o02", items = {
                        { "l", "C", "idle", "CP3_12" },
                    } },
                } },
            },
        } },
    } },
    CP4 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "stern", "CP4_01" },
                { "l", "C", "idle", "CP4_02" },
                { "l", "C", "idle", "CP4_03" },
                { "l", "C", "idle", "CP4_04" },
                { "l", "C", "stern", "CP4_05" },
                { "l", "C", "idle", "CP4_06" },
                { "l", "C", "idle", "CP4_07" },
            },
        } },
    } },
    CP5 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "CP5_01" },
                { "l", "C", "laugh", "CP5_02" },
                { "l", "C", "idle", "CP5_03" },
                { "l", "C", "stern", "CP5_04" },
                { "l", "C", "idle", "CP5_05" },
            },
        } },
    } },
    CP6 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "idle", "CP6_01" },
                { "l", "C", "stern", "CP6_02" },
                { "l", "C", "idle", "CP6_03" },
                { "l", "C", "laugh", "CP6_04" },
                { "l", "C", "idle", "CP6_05" },
            },
        } },
    } },
    GPhA = { g = {
        { t = "", alts = {
            {
                { "l", "U", "talkworry", "GPhA_01" },
                { "l", "U", "talkworry", "GPhA_02" },
                { "l", "U", "talk", "GPhA_03" },
                { "l", "U", "talkworry", "GPhA_04" },
                { "l", "U", "talkdiscomfort", "GPhA_05" },
            },
        } },
        { t = "Her song is installed", alts = {
            {
                { "c", {
                    { k = "GPhA_o01", items = {
                        { "l", "U", "talkworry", "GPhA_06" },
                        { "l", "U", "talk", "GPhA_07" },
                        { "a", "play" },
                    } },
                } },
            },
        } },
        { t = "Her song isn't installed", alts = {
            {
                { "c", {
                    { k = "GPhA_o02", items = {
                        { "l", "U", "talkworry", "GPhA_08" },
                        { "l", "U", "talkworry", "GPhA_09" },
                        { "l", "N", "", "GPhA_10" },
                        { "l", "N", "", "GPhA_11" },
                    } },
                } },
            },
        } },
    } },
    GPhA2 = { g = {
        { t = "After the song was missing", alts = {
            {
                { "l", "U", "talkworry", "GPhA2_01" },
                { "l", "U", "talk", "GPhA2_02" },
            },
        } },
        { t = "After a failed play", alts = {
            {
                { "l", "U", "talkdiscomfort", "GPhA2_03" },
                { "l", "U", "talkworry", "GPhA2_04" },
            },
        } },
        { t = "Her song is installed", alts = {
            {
                { "c", {
                    { k = "GPhA2_o01", items = {
                        { "l", "U", "talk", "GPhA2_05" },
                        { "a", "play" },
                    } },
                } },
            },
        } },
        { t = "Her song isn't installed", alts = {
            {
                { "c", {
                    { k = "GPhA2_o02", items = {
                        { "l", "U", "talkworry", "GPhA2_06" },
                        { "l", "U", "talkworry", "GPhA2_07" },
                        { "l", "N", "", "GPhA2_08" },
                    } },
                } },
            },
        } },
    } },
    GPhC = { g = {
        { t = "", alts = {
            {
                { "l", "G", "talkworry", "GPhC_01" },
                { "c", {
                    { k = "GPhC_o01", items = {
                        { "l", "G", "talkworry", "GPhC_02" },
                        { "l", "G", "talkworry", "GPhC_03" },
                        { "l", "G", "talk", "GPhC_04" },
                        { "l", "G", "talk", "GPhC_05" },
                        { "l", "G", "talkworry", "GPhC_06" },
                    } },
                } },
            },
        } },
        { t = "Her song is installed", alts = {
            {
                { "c", {
                    { k = "GPhC_o02", items = {
                        { "l", "G", "talkworry", "GPhC_07" },
                        { "l", "G", "talk", "GPhC_08" },
                        { "a", "play" },
                    } },
                } },
            },
        } },
        { t = "Her song isn't installed", alts = {
            {
                { "c", {
                    { k = "GPhC_o03", items = {
                        { "l", "G", "talkworry", "GPhC_09" },
                        { "l", "G", "talkworry", "GPhC_10" },
                        { "l", "N", "", "GPhC_11" },
                        { "l", "N", "", "GPhC_12" },
                    } },
                } },
            },
        } },
        { t = "Cleared", alts = {
            {
                { "l", "G", "talk", "GPhC_13" },
                { "l", "G", "talkworry", "GPhC_14" },
                { "l", "G", "talk", "GPhC_15" },
                { "l", "N", "", "GPhC_16" },
                { "l", "N", "", "GPhC_17" },
                { "l", "N", "", "GPhC_18" },
                { "l", "N", "", "GPhC_19" },
                { "a", "unlock_song" },
            },
        } },
        { t = "Failed", alts = {
            {
                { "l", "G", "talkworry", "GPhC_20" },
                { "l", "G", "talkworry", "GPhC_21" },
                { "l", "N", "", "GPhC_22" },
                { "l", "C", "idle", "GPhC_23" },
                { "l", "G", "talkdiscomfort", "GPhC_24" },
                { "l", "C", "idle", "GPhC_25" },
                { "l", "C", "idle", "GPhC_26" },
                { "l", "G", "talkworry", "GPhC_27" },
                { "l", "N", "", "GPhC_28" },
            },
        } },
    } },
    GPhOffer = { g = {
        { t = "", alts = {
            {
                { "c", {
                    { k = "GPhOffer_o01", items = {
                        { "l", "G", "talk", "GPhOffer_01" },
                        { "a", "play" },
                    } },
                    { k = "GPhOffer_o02", items = {
                        { "l", "G", "talkworry", "GPhOffer_02" },
                        { "l", "N", "", "GPhOffer_03" },
                    } },
                } },
            },
        } },
    } },
    GPhMissing = { g = {
        { t = "", alts = {
            {
                { "l", "G", "talkworry", "GPhMissing_01" },
                { "l", "G", "talkdiscomfort", "GPhMissing_02" },
                { "l", "G", "talk", "GPhMissing_03" },
                { "l", "N", "", "GPhMissing_04" },
            },
        } },
    } },
    GPhAgain = { g = {
        { t = "", alts = {
            {
                { "l", "G", "talk", "GPhAgain_01" },
            },
        } },
        { t = "After \"Not right now\", or the song was missing", alts = {
            {
                { "l", "G", "talk", "GPhAgain_02" },
            },
        } },
        { t = "After a failed play", alts = {
            {
                { "l", "G", "talkworry", "GPhAgain_03" },
            },
        } },
    } },
    GPhAfter = { g = {
        { t = "Cleared", alts = {
            {
                { "l", "G", "talk", "GPhAfter_01" },
                { "l", "G", "talkworry", "GPhAfter_02" },
                { "l", "G", "talk", "GPhAfter_03" },
                { "l", "N", "", "GPhAfter_04" },
                { "l", "N", "", "GPhAfter_05" },
                { "l", "N", "", "GPhAfter_06" },
                { "a", "unlock_song" },
            },
        } },
        { t = "Failed", alts = {
            {
                { "l", "G", "talkworry", "GPhAfter_07" },
                { "l", "G", "talkworry", "GPhAfter_08" },
                { "l", "N", "", "GPhAfter_09" },
                { "l", "O", "idle", "GPhAfter_10" },
                { "l", "G", "talkdiscomfort", "GPhAfter_11" },
                { "l", "O", "idle", "GPhAfter_12" },
                { "l", "O", "idle", "GPhAfter_13" },
                { "l", "G", "talkworry", "GPhAfter_14" },
                { "l", "N", "", "GPhAfter_15" },
            },
        } },
    } },
    GP1 = { g = {
        { t = "", alts = {
            {
                { "l", "C", "laugh", "GP1_01" },
                { "l", "G", "idle", "GP1_02" },
                { "l", "C", "idle", "GP1_03" },
                { "l", "N", "", "GP1_04" },
                { "a", "come" },
                { "l", "N", "", "GP1_05" },
                { "l", "G", "talkworry", "GP1_06" },
                { "c", {
                    { k = "GP1_o01", items = {
                        { "l", "N", "", "GP1_07" },
                        { "l", "G", "talk", "GP1_08" },
                        { "l", "G", "talkworry", "GP1_09" },
                        { "l", "N", "", "GP1_10" },
                        { "l", "G", "talkworry", "GP1_11" },
                    } },
                    { k = "GP1_o02", items = {
                        { "l", "G", "talk", "GP1_12" },
                        { "l", "N", "", "GP1_13" },
                        { "l", "G", "talk", "GP1_14" },
                        { "l", "N", "", "GP1_15" },
                        { "l", "G", "talkworry", "GP1_16" },
                    } },
                    { k = "GP1_o03", items = {
                        { "l", "G", "talk", "GP1_17" },
                        { "l", "G", "talk", "GP1_18" },
                        { "l", "G", "talkworry", "GP1_19" },
                    } },
                    { k = "GP1_o04", items = {
                        { "l", "N", "", "GP1_20" },
                        { "l", "C", "stern", "GP1_21" },
                        { "l", "G", "talkworry", "GP1_22" },
                        { "l", "G", "talkworry", "GP1_23" },
                    } },
                } },
                { "c", {
                    { k = "GP1_o05", items = {
                        { "l", "G", "talk", "GP1_24" },
                        { "l", "N", "", "GP1_25" },
                    } },
                    { k = "GP1_o06", items = {
                        { "l", "G", "talk", "GP1_24" },
                        { "l", "N", "", "GP1_25" },
                    } },
                    { k = "GP1_o07", items = {
                        { "l", "G", "talkworry", "GP1_26" },
                        { "l", "G", "talk", "GP1_27" },
                        { "l", "N", "", "GP1_28", "fade" },
                    } },
                    { k = "GP1_o08", again = true, items = {
                        { "l", "N", "", "GP1_29" },
                        { "l", "C", "laugh", "GP1_30" },
                    } },
                } },
                { "l", "G", "talk", "GP1_31" },
                { "c", {
                    { k = "GP1_o09", items = {
                        { "l", "G", "talk", "GP1_32" },
                    } },
                    { k = "GP1_o10", items = {
                        { "l", "G", "talk", "GP1_32" },
                    } },
                    { k = "GP1_o11", items = {
                        { "l", "G", "talk", "GP1_32" },
                    } },
                    { k = "GP1_o12", items = {
                        { "l", "G", "talk", "GP1_32" },
                    } },
                } },
                { "l", "G", "talkworry", "GP1_33" },
                { "l", "N", "", "GP1_34" },
                { "a", "leave" },
                { "l", "C", "idle", "GP1_35" },
            },
        } },
    } },
    GP2 = { g = {
        { t = "", alts = {
            {
                { "l", "G", "talk", "GP2_01" },
                { "l", "G", "talk", "GP2_02" },
                { "l", "C", "laugh", "GP2_03" },
                { "l", "G", "talkworry", "GP2_04" },
                { "l", "G", "talk", "GP2_05" },
                { "l", "C", "idle", "GP2_06" },
                { "l", "G", "talk", "GP2_07" },
                { "c", {
                    { k = "GP2_o01", items = {
                        { "l", "G", "talk", "GP2_08" },
                    } },
                    { k = "GP2_o02", items = {
                        { "l", "G", "talkworry", "GP2_09" },
                    } },
                    { k = "GP2_o03", items = {
                        { "l", "G", "talkdiscomfort", "GP2_10" },
                        { "l", "C", "laugh", "GP2_11" },
                    } },
                } },
                { "l", "G", "talk", "GP2_12" },
            },
        } },
    } },
    GP3 = { g = {
        { t = "", alts = {
            {
                { "l", "N", "", "GP3_01" },
            },
        } },
        { t = "Cute things: crocheted teddy bear (furniture)", alts = {
            {
                { "l", "N", "", "GP3_02" },
                { "l", "G", "talk", "GP3_03" },
                { "l", "G", "talkworry", "GP3_04" },
            },
        } },
        { t = "Cool things: monochrome paper crane (furniture)", alts = {
            {
                { "l", "N", "", "GP3_05" },
                { "l", "G", "talk", "GP3_06" },
                { "l", "G", "talkworry", "GP3_07" },
            },
        } },
        { t = "Silly things: silly cat statue (furniture)", alts = {
            {
                { "l", "N", "", "GP3_08" },
                { "l", "G", "talk", "GP3_09" },
                { "l", "G", "talkworry", "GP3_10" },
            },
        } },
        { t = "All kinds of things: vase of flowers (furniture)", alts = {
            {
                { "l", "N", "", "GP3_11" },
                { "l", "G", "talk", "GP3_12" },
            },
        } },
        { t = "Any gift", alts = {
            {
                { "c", {
                    { k = "GP3_o01", items = {
                        { "l", "G", "talk", "GP3_13" },
                    } },
                    { k = "GP3_o02", items = {
                        { "l", "G", "talk", "GP3_13" },
                    } },
                    { k = "GP3_o03", items = {
                        { "l", "G", "talk", "GP3_13" },
                    } },
                    { k = "GP3_o04", items = {
                        { "l", "N", "", "GP3_14" },
                        { "l", "G", "talk", "GP3_15" },
                    } },
                } },
                { "a", "gift" },
            },
        } },
    } },
    GP4 = { g = {
        { t = "First time", alts = {
            {
                { "l", "G", "talk", "GP4_01" },
                { "l", "G", "talk", "GP4_02" },
                { "l", "G", "talkdiscomfort", "GP4_03" },
                { "l", "G", "talk", "GP4_04" },
                { "l", "G", "talkworry", "GP4_05" },
            },
        } },
        { t = "After a failed try, or after \"she doesn't remember\"", alts = {
            {
                { "l", "G", "talkworry", "GP4_06" },
            },
        } },
        { t = "The offer", alts = {
            {
                { "c", {
                    { k = "GP4_o01", items = {
                        { "a", "play" },
                    } },
                    { k = "GP4_o02", items = {
                        { "l", "G", "talk", "GP4_07" },
                    } },
                } },
            },
        } },
        { t = "No song available", alts = {
            {
                { "l", "G", "talk", "GP4_08" },
                { "l", "N", "", "GP4_09" },
                { "l", "G", "talkdiscomfort", "GP4_10" },
                { "l", "G", "talkworry", "GP4_11" },
                { "l", "C", "idle", "GP4_12" },
                { "l", "N", "", "GP4_13" },
            },
        } },
        { t = "Cleared", alts = {
            {
                { "l", "G", "talk", "GP4_14" },
                { "l", "G", "talk", "GP4_15" },
            },
        } },
        { t = "Failed", alts = {
            {
                { "l", "G", "talkdiscomfort", "GP4_16" },
                { "l", "N", "", "GP4_17" },
                { "l", "G", "talkworry", "GP4_18" },
                { "l", "N", "", "GP4_19" },
                { "a", "leave" },
                { "l", "C", "stern", "GP4_20" },
            },
        } },
    } },
    GP5 = { g = {
        { t = "", alts = {
            {
                { "l", "G", "talk", "GP5_01" },
                { "l", "G", "talk", "GP5_02" },
                { "c", {
                    { k = "GP5_o01", items = {
                        { "l", "G", "talkworry", "GP5_03" },
                    } },
                    { k = "GP5_o02", items = {
                        { "l", "G", "talk", "GP5_04" },
                    } },
                    { k = "GP5_o03", items = {
                        { "l", "G", "talk", "GP5_05" },
                    } },
                } },
                { "l", "G", "talkworry", "GP5_06" },
                { "l", "G", "talk", "GP5_07" },
                { "l", "G", "talkworry", "GP5_08" },
                { "l", "G", "talk", "GP5_09" },
                { "l", "G", "talk", "GP5_10" },
                { "l", "N", "", "GP5_11" },
            },
        } },
    } },
    GP6 = { g = {
        { t = "", alts = {
            {
                { "l", "G", "talkworry", "GP6_01" },
                { "l", "G", "talk", "GP6_02" },
                { "l", "G", "talkworry", "GP6_03" },
                { "c", {
                    { k = "GP6_o01", items = {
                        { "l", "G", "talk", "GP6_04" },
                    } },
                    { k = "GP6_o02", items = {
                        { "l", "G", "talkworry", "GP6_05" },
                    } },
                } },
                { "l", "C", "idle", "GP6_06" },
                { "l", "G", "talk", "GP6_07" },
                { "l", "C", "idle", "GP6_08" },
            },
        } },
        { t = "You don't have her as a character yet", alts = {
            {
                { "l", "N", "", "GP6_09" },
                { "a", "unlock_gris" },
            },
        } },
        { t = "You already have her as a character", alts = {
            {
                { "l", "G", "talkworry", "GP6_10" },
                { "l", "G", "talk", "GP6_11" },
                { "l", "C", "stern", "GP6_12" },
                { "l", "C", "idle", "GP6_13" },
                { "l", "N", "", "GP6_14" },
            },
        } },
    } },
}

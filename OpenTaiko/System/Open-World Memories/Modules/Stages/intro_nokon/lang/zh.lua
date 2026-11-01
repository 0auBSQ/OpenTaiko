-- lang/zh.lua: the Chinese dictionary of intro_nokon. Keys are the exact English texts (Lib/i18n.lua looks them up when drawing).
-- Nokon, the host, is a pompous cat. Keep {Player N name}, %s and %d as they are (filled in at run time).
local T = {}

-- Host Nokon: the opening
T["Nokon"] = "Nokon"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] = "欢迎来到今天的 Intro Nokon！\n吾辈正是主持人 Nokon，今天是耐力赛环节！"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] = "今天的挑战者 {Player 1 name} 将为我们展示音乐知识！\n让我们热烈加油，祝挑战者好运吧！"
T["Alright, quiz time!\nIt's shoooooowtime!"] = "好，答题时间到！\n秀～～～～时间！"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] = "欢迎来到今天的 Intro Nokon！\n吾辈正是主持人 Nokon，今天是对战环节！"
T["Let me present today's challengers..."] = "且让吾辈来介绍今天的挑战者们……"
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] = "谁会获胜？马上就会揭晓……\n让我们热烈加油，祝各位好运吧！"

-- Correct answers (with the song title)
T["Correct, it was \"%s\"! Splendiferous!"] = "答对了，正是“%s”！妙极了！"
T["\"%s\" it was! Greeeeeeat!"] = "就是“%s”！太～～～棒了！"
T["Yes, \"%s\"! Good answer!"] = "没错，“%s”！好答案！"
T["It was \"%s\"! Very for real!"] = "是“%s”！真·实力！"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "果然是“%s”！哇～～～～哦！"

-- One-song endurance teasing lines
T["Do you feel smart? How will you save your score Scherlock?"] = "觉得自己很聪明？那你打算怎么保存分数呢，大侦探？"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "玩得开心吗？太好了，因为你要永远留在这里了！"
T["You did beat my score? Wait until you reach the results first..."] = "你超过吾辈的分数了？先撑到结算画面再说吧……"
T["..."] = "……"

-- Wrong answer / time out
T["Too bad! I guess I cannot bet on a losing horse!"] = "太可惜了！吾辈可不能押注一匹必输的马！"
T["Come on, be for real!"] = "喂喂，认真一点啊！"
T["So you prefer dogs? Sorry, we do not do that here!"] = "原来你是狗派？抱歉，我们这里不接待！"
T["And that's a wrap! Time to end the show!"] = "到此为止！节目该结束了！"
T["No one found it?... Really?..."] = "没人答出来？……真的吗？……"
T["And a skewer of loosers, chief!"] = "一串败者烤串，上菜了，老板！"
T["I have no words..."] = "吾辈无话可说……"

-- VS win (winner name and song title)
T["And that's another W for %s! The song was \"%s\"!"] = "%s 又拿下一胜！歌曲是“%s”！"
T["%s takes the bag with \"%s\"! Brutal!"] = "%s 凭“%s”通吃全场！太狠了！"
T["I always knew %s was the best! It was \"%s\"!"] = "吾辈早就知道 %s 是最强的！歌曲是“%s”！"

-- UI and screen texts
T["Loading songs..."] = "正在加载歌曲……"
T["Round %d / %d"] = "第 %d / %d 回合"
T["Game Over!"] = "游戏结束！"
T["Final Results!"] = "最终结果！"
T["Score: %d"] = "分数：%d"
T["High Scores"] = "最高分"
T["Decide: Play again   Cancel: Exit"] = "确定：再玩一次   取消：退出"
T["Pick the correct song!"] = "选出正确的歌曲！"
T["Choose the song genre!"] = "选择歌曲类型！"
T["%s wins %d point!"] = "%s 获得 %d 分！"
T["%s wins %d points!"] = "%s 获得 %d 分！"
T["It's a wrap!"] = "到此为止！"
T["Let's set up the game show!"] = "来布置游戏节目吧！"
T["Mode"] = "模式"
T["Players"] = "玩家人数"
T["Songs"] = "歌曲"
T["Round Count"] = "回合数"
T["The game can only be started if there are at least 5 available songs."] = "至少需要 5 首可用歌曲才能开始游戏。"

return T

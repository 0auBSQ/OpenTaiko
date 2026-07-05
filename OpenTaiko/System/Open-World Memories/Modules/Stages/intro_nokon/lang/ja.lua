-- lang/ja.lua — intro_nokon の日本語辞書。キーは英語原文と完全一致（Lib/i18n.lua が表示時に引く）。
-- ノコンの声：自分を「ワガハイ」（カタカナ）と呼ぶ、ちょっと気取った猫の名司会。〜である／〜なのだ調で
-- 大仰に振る舞うが、リアクションは軽薄。{Player N name} や %s/%d はそのまま残すこと（実行時に差し込まれる）。
local T = {}

-- ── 司会ノコン：オープニング ──────────────────────────────────────────────────────
T["Nokon"] = "ノコン"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!"] =
    "ようこそ、本日のイントロノコンへ！\nワガハイこそが司会のノコンである！今日は耐久セッションなのだ！"
T["Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!"] =
    "本日の挑戦者、{Player 1 name}が音楽知識を披露してくれるのだ！\n盛大な声援と幸運を送ろうではないか！"
T["Alright, quiz time!\nIt's shoooooowtime!"] =
    "さあ、クイズの時間である！\nしょ〜〜〜〜たいむ！"
T["Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!"] =
    "ようこそ、本日のイントロノコンへ！\nワガハイこそが司会のノコンである！今日は対戦セッションなのだ！"
T["Let me present today's challengers..."] =
    "本日の挑戦者たちを、ワガハイが紹介しようではないか…"
T["Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!"] =
    "勝つのは誰か？ それはすぐに分かるのだ…\n盛大な声援と幸運を送ろうではないか！"

-- ── 正解（曲名入り） ────────────────────────────────────────────────────────────
T["Correct, it was \"%s\"! Splendiferous!"] = "正解、「%s」であった！ 見事極まれり！"
T["\"%s\" it was! Greeeeeeat!"] = "「%s」であったのだ！ 素晴らし〜〜〜い！"
T["Yes, \"%s\"! Good answer!"] = "その通り、「%s」！ 良い解答である！"
T["It was \"%s\"! Very for real!"] = "「%s」であった！ 実にリアルなのだ！"
T["\"%s\" indeed! Sheeeeeeeesh!"] = "まさしく「%s」！ ひゅ〜〜〜〜！"

-- 一曲耐久のイタズラ台詞
T["Do you feel smart? How will you save your score Scherlock?"] = "賢くなった気分かね？ そのスコア、どう保存するつもりだね名探偵くん？"
T["Are you enjoying the game? Perfect, because you are here forever!"] = "ゲームは楽しいかね？ 結構結構、キミは永遠にここにいるのだから！"
T["You did beat my score? Wait until you reach the results first..."] = "ワガハイのスコアを超えただと？ まずはリザルト画面まで辿り着いてから言うのだな…"
T["..."] = "………"

-- ── 不正解／時間切れ ────────────────────────────────────────────────────────────
T["Too bad! I guess I cannot bet on a losing horse!"] = "残念無念！ 負け馬に賭けるわけにはいかないのだ！"
T["Come on, be for real!"] = "おいおい、真面目にやりたまえ！"
T["So you prefer dogs? Sorry, we do not do that here!"] = "さては犬派だな？ 悪いがウチではお断りなのだ！"
T["And that's a wrap! Time to end the show!"] = "はい、そこまで！ ショーはお開きなのだ！"
T["No one found it?... Really?..."] = "誰も分からなかったのかね？…本当に？…"
T["And a skewer of loosers, chief!"] = "敗者の串盛り一丁、あがったのだ！"
T["I have no words..."] = "ワガハイ、言葉を失ったのである…"

-- ── VS勝利（勝者名＋曲名） ───────────────────────────────────────────────────────
T["And that's another W for %s! The song was \"%s\"!"] = "%sがまたも勝利なのだ！ 曲は「%s」であった！"
T["%s takes the bag with \"%s\"! Brutal!"] = "%sが「%s」で総取りである！ 恐るべし！"
T["I always knew %s was the best! It was \"%s\"!"] = "ワガハイは最初から%sが一番だと見抜いていたのだ！ 曲は「%s」！"

-- ── UI・画面テキスト ────────────────────────────────────────────────────────────
T["Loading songs..."] = "楽曲を読み込み中…"
T["Round %d / %d"] = "ラウンド %d / %d"
T["Game Over!"] = "ゲームオーバー！"
T["Final Results!"] = "最終結果発表！"
T["Score: %d"] = "スコア：%d"
T["High Scores"] = "ハイスコア"
T["Decide: Play again   Cancel: Exit"] = "決定：もう一度遊ぶ   キャンセル：終了"
T["Pick the correct song!"] = "正解の曲を選ぶのだ！"
T["Choose the song genre!"] = "曲のジャンルを選ぶのだ！"
T["Let's set up the game show!"] = "ゲームショーの準備をしようではないか！"
T["Mode"] = "モード"
T["Players"] = "プレイヤー数"
T["Songs"] = "楽曲"
T["Round Count"] = "ラウンド数"
T["The game can only be started if there are at least 5 available songs."] = "遊べる楽曲が5曲以上ないとゲームを開始できないのだ。"

return T

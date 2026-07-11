---@diagnostic disable: lowercase-global
-- lang/ja.lua — Japanese strings for the myroom stage (Lib/i18n exact-English-key dictionary;
-- missing keys fall back to English). Nameplate TYPE names live in data/nameplate_types.json.

return {
	-- PC screen chrome
	["My Computer"] = "マイコンピューター",
	["Characters"] = "キャラクター",
	["Puchichara"] = "ぷちキャラ",
	["Dan Titles"] = "段位称号",
	["Nameplates"] = "ネームプレート",
	["Rename"] = "名前変更",
	["Coins: %d"] = "コイン: %d",

	-- detail pane / actions
	["Equip"] = "装備",
	["Equipped"] = "装備中",
	["[ Equipped ]"] = "［ 装備中 ］",
	["Locked"] = "ロック中",
	["Buy & Equip  (%d coins)"] = "購入して装備（%dコイン）",
	["How to unlock:"] = "アンロック条件:",
	["Purchase for %d coins."] = "%dコインで購入できます。",
	["???"] = "???",
	["Plate Type %d"] = "プレートタイプ%d",

	-- action feedback
	["Equipped!"] = "装備しました！",
	["Unlocked!"] = "アンロックしました！",
	["Purchased for %d coins!"] = "%dコインで購入しました！",
	["Not enough coins."] = "コインが足りません。",
	["Dan title set."] = "段位称号を設定しました。",

	-- rename pane
	["Current name:  %s"] = "現在の名前:  %s",
	["Type a new name, then Apply (or press Enter)."] = "新しい名前を入力して「適用」（またはEnter）を押してください。",
	["new name..."] = "新しい名前...",
	["Apply"] = "適用",
	["Enter a name first."] = "先に名前を入力してください。",
	["Name changed to %s."] = "名前を「%s」に変更しました。",

	-- edit mode: tabs + inline buttons
	["Furniture"] = "家具",
	["Wall"] = "壁かけ",
	["Floor"] = "床",
	["Paint"] = "壁紙",
	["Door"] = "ドア",
	["Eraser"] = "消しゴム",
	["Rotate"] = "回転",
	["Move"] = "移動",
	["Remove"] = "撤去",

	-- edit mode: hints
	["Edit Mode  —  pick a tab, choose an item, click to place.  [Tab] done"] = "編集モード ― タブを選び、アイテムを選んでクリックで配置。[Tab]で終了",
	["Moving %s   ·   point at a wall slot (low/high) · %s · right-click cancels"] = "%sを移動中 ・ 壁のスロット（上/下）を指して・%s・右クリックでキャンセル",
	["Moving %s   ·   wheel rotates · %s · right-click cancels"] = "%sを移動中 ・ ホイールで回転・%s・右クリックでキャンセル",
	["release to drop"] = "離して設置",
	["click to drop"] = "クリックで設置",
	["Door  —  the ghost tile follows your cursor along the front edge; click to move the doorway"] = "ドア ― 前端に沿ってゴーストタイルがカーソルを追います。クリックでドアを移動",
	["Eraser  —  click a placed item, rug or paint to remove it (back to stock)"] = "消しゴム ― 配置済みのアイテム・ラグ・壁紙をクリックで撤去（在庫に戻ります）",
	["Placing %s   ·   wheel rotates · click a green spot · right-click cancels"] = "%sを配置中 ・ ホイールで回転・緑の場所をクリック・右クリックでキャンセル",
	["Placing %s   ·   wheel flips low/high mount · click a green spot · right-click cancels"] = "%sを配置中 ・ ホイールで上下切替・緑の場所をクリック・右クリックでキャンセル",
	["Painting %s   ·   click & drag to apply many · right-click cancels"] = "%sを塗装中 ・ クリック＆ドラッグで連続適用・右クリックでキャンセル",
	["%s  —  use the buttons under it, drag it to move, or click elsewhere to dismiss"] = "%s ― 下のボタンを使うか、ドラッグで移動。他をクリックで解除",
	["Sliding the door along the front"] = "ドアを前端に沿ってスライド中",
	["Door  —  drag to slide it along the front (or use the Door tab)"] = "ドア ― ドラッグで前端に沿って移動（またはドアタブを使用）",
	["Click %s to select it (drag moves it)"] = "%sをクリックで選択（ドラッグで移動）",
	["Click a highlighted tile along the front edge to move the door there."] = "前端のハイライトされたタイルをクリックするとドアがそこへ移動します。",
	["Click a placed item to remove it — furniture, wall items, rugs and wall paint go back to stock."] = "配置済みのアイテムをクリックで撤去 ― 家具・壁かけ・ラグ・壁紙は在庫に戻ります。",

	-- jukebox (dialog lines live in data/dialogs.json; these are the player-UI strings)
	["Jukebox"] = "ジュークボックス",
	["Songs"] = "曲",
	["My Room BGM"] = "マイルームBGM",
	["Sorting the records... the song list is still being prepared."] = "レコードを整理中…楽曲リストを準備しています。",
	["Nothing selected"] = "未選択",
	["Play"] = "再生",
	["Pause"] = "一時停止",
	["Stop"] = "停止",
	["Repeat"] = "リピート",
	["Speed"] = "速度",
	["[Enter] Play music"] = "[Enter] 音楽をかける",
	["[Enter] Examine"] = "[Enter] 調べる",
	["No playable songs found."] = "再生できる曲が見つかりません。",

	-- phone (the landlord's own lines live in data/dialogs.json + data/tiers.json, localized inline)
	["Phone"] = "電話",
	["Call the landlord"] = "大家さんに電話",
	["Enter a number"] = "番号を入力",
	["Invite friends (host a room)"] = "友達を招待（ルームを開く）",
	["Join a friend (enter code)"] = "友達に参加（コード入力）",
	["Stop hosting (close the room)"] = "ホスト終了（ルームを閉じる）",
	["Hang up"] = "切る",
	["Back"] = "戻る",
	["Call"] = "発信",
	["Join"] = "参加",
	["number..."] = "番号...",
	["paste the room code..."] = "ルームコードを貼り付け...",
	["the number"] = "その番号",
	["You dial %s... It rings, and rings. Nobody picks up."] = "%sにかけました…呼び出し音が鳴り続けますが、誰も出ません。",
	["You closed the room."] = "ルームを閉じました。",
	["You left the room."] = "ルームを退出しました。",
	["The host closed the room."] = "ホストがルームを閉じました。",
	["Connecting…"] = "接続中…",
	["Could not join."] = "参加できませんでした。",
	["Could not open the room."] = "ルームを開けませんでした。",
	["Room open! The code was saved to a folder — share it so friends can Join by phone."] = "ルームを開きました！コードをフォルダに保存しました ― 友達に共有すれば電話から参加できます。",
	["No code entered."] = "コードが入力されていません。",

	-- HUD
	["WASD — Move"] = "WASD — 移動",
	["RMB / Q·E — Orbit"] = "右クリック / Q·E — 視点回転",
	["Wheel — Zoom"] = "ホイール — ズーム",
	["Tab — Edit room"] = "Tab — 部屋を編集",
	["Esc — Leave"] = "Esc — 退出",
	["Esc / Door — Leave visit"] = "Esc / ドア — 訪問を終える",
	["[Enter] Use computer"] = "[Enter] パソコンを使う",
	["[Enter] Use phone"] = "[Enter] 電話を使う",
	["[Enter] Toggle the lamp"] = "[Enter] ランプを切り替え",
	["[Enter] Leave the room"] = "[Enter] 部屋を出る",
	["[Tab] Switch focus"] = "[Tab] フォーカス切り替え",
	["Join a friend"] = "友達に参加",
	["Player %d — %s"] = "プレイヤー%d — %s",
	["Whose room?"] = "誰の部屋？",

	-- phone: dialable numbers / easter eggs (data/phone_numbers.json)
	["Ahahah... Six seeeven! So funny!"] = "アハハ…シックス・セブン！超ウケる！",
	["Nice try!"] = "惜しい！",
	["OOTD is a 10+"] = "OOTDは10+だ",
}

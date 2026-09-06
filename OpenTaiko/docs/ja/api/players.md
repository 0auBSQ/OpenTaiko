<!-- api/players.md -->

# プレイヤーとプロフィール

セーブファイル、ネームプレート、キャラクター、ぷちキャラ、プレイ状態、テーマ、現在の言語。

このページのプレイヤーインデックスは、1 始まりの THEME:GetThemeSettingForPlayer を除いてすべて 0 始まり (0 から 4) です。読み取り専用モジュール (ROActivity とバックグラウンド) は、書き込みメソッドがエラーをログに出力して何もしないセーブファイルハンドルを受け取ります。それ以外はすべてのモジュール種類で同じように振る舞います。

## セーブファイル

### GetSaveFile

プレイヤースロットのセーブファイルハンドルを返すグローバル関数です。

<div class="callout warn">
通常の関数として呼び出します (`GetSaveFile(0)`)。範囲外のインデックスはエラーをログに出力して nil を返します。各呼び出しはライブのデータを読み取る新しいハンドルを作成するため、キャッシュや破棄するものはありません。
</div>

| メソッド | 説明 |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | 0 始まりのプレイヤースロットのセーブファイルハンドルを返します。インデックスが範囲外なら nil。 |

### セーブファイルハンドル

1 人のプレイヤーのプロフィール: 名前、コイン、アンロックしたアイテム、トリガーとカウンター、クリア統計、装備中のキャラクター、ぷちキャラ、ネームプレート、段位称号。

<div class="callout warn">
プロパティはドット構文で読み取ります (sf.Name、sf.Coins)。書き込みメソッドは直ちに永続化されます。読み取り専用モジュールは以下をブロックします: SpendCoins、EarnCoins、UnlockNameplate、UnlockSong、SelectedHitsounds への代入、SetGlobalTrigger、SetGlobalCounter、ChangeCharacter (false を返す)、UnlockPuchichara、ChangePuchichara、UnlockCharacter、ChangeDan、ChangeName、ChangeNameplate。
</div>

| メソッド | 説明 |
| --- | --- |
| `sf.Name  -> string` | プレイヤーの表示名。 |
| `sf.SaveId  -> integer` | このセーブの数値データベース id。 |
| `sf.SaveUID  -> string` | このセーブの一意な文字列 id。 |
| `sf.NameplateInfo  -> nameplateInfo` | 装備中のネームプレート (ネームプレート情報ハンドルを参照)。保存された id が不明な場合は既定の初心者ネームプレート。 |
| `sf.DanplateInfo  -> danplateInfo` | 現在の段位称号 (段位プレート情報ハンドルを参照)。 |
| `sf.TotalPlaycount  -> integer` | このセーブの合計プレイ回数。 |
| `sf.AIBattlePlaycount  -> integer` | AI バトルのプレイ回数。 |
| `sf.AIBattleWins  -> integer` | AI バトルの勝利数。 |
| `sf.Coins  -> integer` | 現在のコイン残高。 |
| `sf.TotalEarnedCoins  -> integer` | セーブの生涯で獲得したコインの合計。 |
| `sf:SpendCoins(price)  -> nil` | コインを差し引き (残高は 0 未満にならない)、永続化します。 |
| `sf:EarnCoins(amount)  -> nil` | 残高と獲得合計にコインを加え、永続化します。 |
| `sf:IsNameplateUnlocked(id)  -> bool` | この id のネームプレートがアンロック済みかどうか。 |
| `sf:UnlockNameplate(id)  -> nil` | ネームプレートをアンロックして永続化します (アンロック済みなら何もしません)。 |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | この固有 id の楽曲がアンロック済みかどうか。 |
| `sf:UnlockSong(uniqueId)  -> nil` | 楽曲をアンロックして永続化します (アンロック済みなら何もしません)。 |
| `sf.SelectedHitsounds  -> string` | 選択中のヒットサウンドセットのフォルダ名。別の名前を代入すると永続化され、プレイヤーのヒットサウンドが再読み込みされます。 |
| `sf:GetGlobalTrigger(name)  -> bool` | 名前付きの真偽値トリガーを読み取ります。 |
| `sf:GetGlobalCounter(name)  -> number` | 名前付きの数値カウンターを読み取ります。 |
| `sf:SetGlobalTrigger(name, value)  -> nil` | 名前付きの真偽値トリガーを設定します。 |
| `sf:SetGlobalCounter(name, value)  -> nil` | 名前付きの数値カウンターを設定します。 |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | ある難易度 (0 Easy から 4 Extra Extreme) の譜面のうち、最高のクリア状態がちょうど clearStatus (0 なし、1 アシストクリア、2 クリア、3 フルコンボ、4 全良) であるものの数。範囲外の引数には 0。 |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | 段位楽曲ノードに対する Mod なしのベストプレイ (段位ベストプレイハンドルを参照)。なければ HasRecord が false のハンドル。 |
| `sf:GetCharacter()  -> character` | このスロットに紐づくプレイヤーキャラクターハンドル (キャラクターハンドルを参照)。 |
| `sf.CharacterName  -> string` | 装備中のキャラクターのフォルダ名。 |
| `sf:ChangeCharacter(folderName)  -> bool` | このフォルダ名のキャラクターを装備します。キャラクターを装備したか、すでにアクティブだった場合は true、このフォルダ名を持つ読み込み済みのキャラクターがなければ false を返します。 |
| `sf:GetPuchichara()  -> puchichara` | 装備中のぷちキャラ (ぷちキャラハンドルを参照)。解決できなければ nil。 |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | このフォルダ名のぷちキャラがアンロック済みかどうか。 |
| `sf:UnlockPuchichara(folderName)  -> nil` | ぷちキャラをアンロックして永続化します (アンロック済みなら何もしません)。 |
| `sf:ChangePuchichara(folderName)  -> nil` | このフォルダ名のぷちキャラを装備して永続化します。このメソッドは名前を検証しません。 |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | キャラクターがアンロック済みかどうか。装備中のキャラクターは常にアンロック済みとみなされます。 |
| `sf:UnlockCharacter(folderName)  -> nil` | キャラクターをアンロックして永続化します (アンロック済みなら何もしません)。 |
| `sf.DanTitleCount  -> integer` | 既定の称号を含む、利用可能な段位称号の数 (常に 1 以上)。 |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | 0 始まりのインデックスの段位称号 (段位称号エントリハンドルを参照)。インデックス 0 は既定の称号。範囲外なら nil。 |
| `sf.SelectedDan  -> string` | アクティブな段位称号のテキスト。 |
| `sf:ChangeDan(title)  -> nil` | 指定した称号をアクティブにし、プレイヤーが獲得したものであれば金とクリア状態のフラグをコピーし、ネームプレートを更新して永続化します。 |
| `sf:ChangeName(name)  -> nil` | 表示名を変更し、ネームプレートを更新して永続化します。このメソッドは空または変更のない名前を無視します。 |
| `sf:ChangeNameplate(id)  -> nil` | この id のネームプレートを装備し、ネームプレートを更新して永続化します。データベースにない id はキャッシュされた称号テキストをクリアします。 |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## ネームプレートと段位称号

### NAMEPLATE

称号プレート、段位プレート、プレイヤーの完全なネームプレートを描画します。

<div class="callout warn">
スキンの nameplate ROActivity (Modules/ROActivities/nameplate) が描画を行い、アートワークとレイアウトを定義します。不透明度は 0 から 255 です。テキストパラメータはテキストオブジェクトから描画したテクスチャを取ります (「グラフィックとテキスト」を参照)。rarity はインデックスで、0 Poor、1 Common、2 Uncommon、3 Rare、4 Epic、5 Legendary、6 Mythical です。
</div>

| メソッド | 説明 |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | 指定した表示タイプ、事前描画した称号テクスチャ、レアリティインデックス、ネームプレート id で称号プレートを描画します。 |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | 事前描画した称号テクスチャを使い、指定した段位の段位プレートを描画します。 |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | プレイヤースロットの完全なネームプレートを描画します。赤側か青側かはゲームの 1P 側設定に従います。 |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | この id のネームプレートのローカライズされた称号をテキストオブジェクトで描画し、称号プレートとして描きます。id はネームプレートデータベースに存在する必要があります。 |

### NAMEPLATESLIST

ゲームが知っているすべてのネームプレートのデータベースで、インデックスや id による検索とフィルタリングを備えます。

<div class="callout warn">
問い合わせメソッドはネームプレート情報ハンドルを返します。FindWhere はネームプレートごとに Lua 関数を 1 回呼び、true を返したエントリを残します。
</div>

| メソッド | 説明 |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | データベース内のネームプレート数。 |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | 0 始まりのデータベース位置のネームプレート。範囲外なら nil。 |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | この id のネームプレート。見つからなければ nil。 |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | すべてのネームプレートをリストとして。 |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | `predicate(info)` が true を返すネームプレート。 |

### ネームプレート情報ハンドル

1 つのネームプレート称号: ローカライズされたテキスト、表示タイプ、id、レアリティ、アンロック条件。

<div class="callout warn">
sf.NameplateInfo と NAMEPLATESLIST がこのハンドルを返します。既定の初心者ネームプレートは id -1、レアリティ "Common" で、アンロック条件を持ちません。
</div>

| メソッド | 説明 |
| --- | --- |
| `info.Title  -> string` | 現在の言語での称号テキスト。 |
| `info.Type  -> integer` | NAMEPLATE:DrawTitlePlate に渡す表示タイプコード。 |
| `info.Id  -> integer` | ネームプレート id (既定の初心者ネームプレートは -1)。 |
| `info.Rarity  -> string` | レアリティ名: "Poor"、"Common"、"Uncommon"、"Rare"、"Epic"、"Legendary"、"Mythical"。 |
| `info.UnlockCondition  -> unlockCondition` | アンロック条件 (アンロック条件ハンドルを参照)。 |

### 段位プレート情報ハンドル

ネームプレートに表示される、プレイヤーのアクティブな段位称号です。

<div class="callout warn">
sf.DanplateInfo がこのハンドルを返します。値は読み取り時点のセーブファイルを反映します。
</div>

| メソッド | 説明 |
| --- | --- |
| `info.Title  -> string` | アクティブな段位称号のテキスト。 |
| `info.Gold  -> bool` | プレイヤーがアクティブな称号を金合格で獲得したかどうか。 |
| `info.ClearStatus  -> integer` | アクティブな称号のクリア状態コード。 |

### 段位称号エントリハンドル

プレイヤーが選択できる 1 つの段位称号です。

<div class="callout warn">
sf:GetDanTitleByIndex がこのエントリを返します。インデックス 0 は既定の称号 (金でなく、クリア状態 0)。それ以降はプレイヤーが獲得した称号です。
</div>

| メソッド | 説明 |
| --- | --- |
| `entry.Title  -> string` | 称号テキスト。 |
| `entry.IsGold  -> bool` | プレイヤーが称号を金合格で獲得したかどうか。 |
| `entry.ClearStatus  -> integer` | 称号に記録された最高のクリア状態。 |

### 段位ベストプレイハンドル

1 つの段位記録の最高の試験結果です。

<div class="callout warn">
sf:GetDanBestPlay がこのハンドルを返します。試験を読む前に HasRecord を確認してください。GetExam は .NET 配列を返します。0 始まりでインデックスし、`.Length` を読みます。
</div>

| メソッド | 説明 |
| --- | --- |
| `play.HasRecord  -> bool` | その楽曲の記録が存在するかどうか。 |
| `play:GetExam(slot)  -> int[]` | 試験スロット 1 から 7 のベストスコア: コース全体の試験では 1 つの値、楽曲ごとの試験では楽曲ごとに 1 つ。記録がないか無効なスロットでは空。 |

## キャラクターとぷちキャラ

### CHARACTER

キャラクターハンドルを作成し、標準のアニメーションとボイスのスロット名を公開します。

<div class="callout warn">
CreateCharacter はリソースを所有するハンドルを返します。IsValid を確認し、使い終わったら Dispose を呼んでください。GetPlayerCharacter はプレイヤーの装備中のキャラクターに追従するハンドルを返し、破棄は不要です。GetPlayerGradientMap はグラデーションマップを返します (「グラフィックとテキスト」を参照)。ANIM_* と VOICE_* メンバーは読み取り専用の文字列で、キャラクターハンドルのアニメーションとボイスのメソッドに渡します。
</div>

| メソッド | 説明 |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Global/Characters/{folderName} から独立したキャラクターを読み込みます。フォルダが存在しなければ IsValid は false です。 |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | プレイヤースロットに紐づき、呼び出しごとに装備中のキャラクターを解決するハンドル。 |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | プレイヤースロットで有効なパレットグラデーション。設定されていなければ nil。 |
| `CHARACTER.ANIM_PREVIEW  -> string` | プレビューポーズ (メニューとショップ)。 |
| `CHARACTER.ANIM_RENDER  -> string` | 全身のレンダーポーズ。 |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | ゲームプレイ、通常状態。 |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | ゲームプレイ、ゲージがクリアゾーン。 |
| `CHARACTER.ANIM_GAME_MAX  -> string` | ゲームプレイ、ゲージ満タン。 |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | ゲームプレイ、ゴーゴータイム。 |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | ゲームプレイ、ゲージ満タンのゴーゴータイム。 |
| `CHARACTER.ANIM_GAME_MISS  -> string` | ゲームプレイ、ミス。 |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | ゲームプレイ、ゲージが低い状態でのミス。 |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | ゲームプレイ、10 コンボの節目。 |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | ゲームプレイ、ゲージ満タンでの 10 コンボの節目。 |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | ゲームプレイ、楽曲クリア。 |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | ゲームプレイ、楽曲失敗。 |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | クリア状態から抜けるときの遷移。 |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | クリア状態に入るときの遷移。 |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | ゲージ満タン状態から抜けるときの遷移。 |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | ゲージ満タン状態に入るときの遷移。 |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | ミスに入るときの遷移。 |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | ゲージが低い状態でのミスに入るときの遷移。 |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | 通常状態への復帰。 |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | ゴーゴー開始のバースト。 |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | クリア状態でのゴーゴー開始のバースト。 |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | ゲージ満タンでのゴーゴー開始のバースト。 |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | 風船を叩いている最中。 |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | 風船が割れた。 |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | 風船を割り損ねた。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | くす玉を叩いている最中。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | くす玉が割れた。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | くす玉を割り損ねた。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | くす玉の待機。 |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | タワーモード、立ち。 |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | タワーモード、疲労状態で立ち。 |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | タワーモード、登り。 |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | タワーモード、疲労状態で登り。 |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | タワーモード、走り。 |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | タワーモード、疲労状態で走り。 |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | タワーモード、クリア。 |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | タワーモード、疲労状態でクリア。 |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | タワーモード、失敗。 |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | メニュー、待機。 |
| `CHARACTER.ANIM_MENU_START  -> string` | メニュー、開始。 |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | メニュー、通常。 |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | メニュー、選択。 |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | エントリー画面、通常。 |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | エントリー画面、ジャンプ。 |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | リザルト、通常。 |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | リザルト、クリア。 |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | リザルト、失敗状態に入る。 |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | リザルト、失敗。 |
| `CHARACTER.VOICE_END_FAILED  -> string` | 楽曲終了、失敗。 |
| `CHARACTER.VOICE_END_CLEAR  -> string` | 楽曲終了、クリア。 |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | 楽曲終了、フルコンボ。 |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | 楽曲終了、全良。 |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | 楽曲終了、AI バトル勝利。 |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | 楽曲終了、AI バトル敗北。 |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | 選曲画面に入った。 |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | 楽曲を決定した。 |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | AI バトルで楽曲を決定した。 |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | 難易度選択。 |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | 段位選択に入った。 |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | 段位選択の確認。 |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | 段位コースを決定した。 |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | タイトル画面のエントリー。 |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | タワーモードのミス。 |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | リザルト、新記録。 |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | リザルト、失敗。 |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | リザルト、クリア。 |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | リザルト、段位不合格。 |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | リザルト、段位合格。 |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | リザルト、段位金合格。 |

### キャラクターハンドル

描画可能なキャラクターです。名前付きのアニメーションとボイスを再生し、ハンドルごとの描画状態 (不透明度、スケール、ティント、回転、ブレンドモードとラップモード、パレットグラデーション) を持ちます。

<div class="callout warn">
CHARACTER:GetPlayerCharacter、CHARACTER:CreateCharacter、sf:GetCharacter、およびキャラクターリストエントリの Character プロパティがキャラクターハンドルを返します。CreateCharacter から得たハンドルだけがリソースを所有し、Dispose を必要とします。ハンドルは Set* の値を保存し、以降のすべての描画に適用します。描画メソッドのスケールと不透明度の引数は保存された値に乗算されます。保存される不透明度は 0.0 から 1.0、描画ごとの不透明度は 0 から 255 です。アニメーションとボイスの名前は CHARACTER の定数です。
</div>

| メソッド | 説明 |
| --- | --- |
| `char.IsValid  -> bool` | ハンドルが読み込み済みのキャラクターに解決されるかどうか。 |
| `char.FolderName  -> string` | フォルダ名。無効なら空文字列。 |
| `char.FullPath  -> string` | フォルダの絶対パス。無効なら空文字列。 |
| `char.DisplayName  -> string` | ローカライズされた表示名。フォルダ名にフォールバックします。 |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | 2 つ以上のカラーストップのテーブルと任意のブレンド量 (既定 1.0) から作られたパレットグラデーションを適用します。プレイヤーに紐づくハンドルは、グラデーションをプレイヤースロットにも保存します。nil を渡すとクリアされます。 |
| `char:ClearPaletteGradient()  -> nil` | パレットグラデーションを取り除きます (プレイヤーに紐づくハンドルではプレイヤースロットのグラデーションも)。 |
| `char:SetOpacity(opacity)  -> nil` | 保存される不透明度。0.0 透明から 1.0 不透明。 |
| `char:SetScale(scaleX, scaleY)  -> nil` | 保存されるスケール。負の X は水平に反転します。 |
| `char:SetColor(color)  -> nil` | 色の値から保存されるティント。 |
| `char:SetColor(r, g, b)  -> nil` | 0.0 から 1.0 の 3 つのチャンネルから保存されるティント。 |
| `char:SetRotation(degrees)  -> nil` | 保存される回転 (度)。 |
| `char:SetBlendMode(mode)  -> nil` | 保存されるブレンドモード: "normal"、"add"、"multi"、"sub"、"screen"。 |
| `char:SetWrapMode(mode)  -> nil` | 保存されるテクスチャのラップモード: "edge"、"border"、"repeat"、"mirror"。 |
| `char:GetScale()  -> vector2` | 保存されたスケール。 |
| `char:GetColor()  -> tuple` | 保存されたティントを、フィールド Item1、Item2、Item3 (赤、緑、青) を持つ .NET のタプルとして。 |
| `char:GetRotation()  -> number` | 保存された回転 (度)。 |
| `char:GetBlendMode()  -> string` | 保存されたブレンドモード。 |
| `char:GetWrapMode()  -> string` | 保存されたラップモード。 |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | x、y にアニメーションを描画します。既定: スケール 1、不透明度 255。 |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | 指定したアンカー点 (既定 "bottom") を x、y に置いてアニメーションを描画します。 |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | 矩形の左上隅にアニメーションを描画します。このメソッドはレイアウトコードのために w と h を受け付けますが、描画には影響しません。 |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | 左上隅を x、y に置き、clipX、clipY だけオフセットした clipW x clipH の矩形にクリップしてアニメーションを描画します。スケール、ティント、回転は保存された状態のみから取られます。 |
| `char:Update(animation, looping?)  -> bool` | アニメーションを進め (既定でループ)、まだ再生中かどうかを返します。 |
| `char:LoadAnimation(animation)  -> nil` | アニメーションのフレームを読み込みます。 |
| `char:DisposeAnimation(animation)  -> nil` | アニメーションのフレームを解放します。 |
| `char:AvailableAnimation(animation)  -> bool` | キャラクターがそのアニメーションを提供しているかどうか。 |
| `char:SetAnimationDuration(animation, duration)  -> nil` | アニメーションの再生時間を設定します。 |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | BPM からアニメーションのサイクル長を設定します。 |
| `char:ResetAnimationCounter(animation)  -> nil` | アニメーションを最初のフレームから再開します。 |
| `char:GetAnimationSize(animation)  -> vector2` | スキン解像度でのアニメーションの現在フレームの描画サイズ。利用できなければ (0, 0)。 |
| `char:LoadVoice(voice)  -> nil` | ボイスクリップを読み込みます。 |
| `char:DisposeVoice(voice)  -> nil` | ボイスクリップを解放します。 |
| `char:PlayVoice(voice)  -> nil` | ボイスクリップを再生します。 |
| `char:Dispose()  -> nil` | キャラクターのリソースを解放します (CreateCharacter から得たハンドルのみ)。 |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

読み込み済みのすべてのキャラクターのリストです。

<div class="callout warn">
スキンはキャラクターを読み込むときにリストを再構築し、スキンの再読み込み時に破棄するため、キャラクターが読み込まれていない間はこのグローバルが nil になることがあります。問い合わせメソッドはキャラクターリストエントリを返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | 読み込み済みのキャラクター数。 |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | すべてのキャラクターをリストとして。 |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | 0 始まりのインデックスのエントリ。範囲外なら nil。 |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | このフォルダ名のエントリ。見つからなければ nil。 |

### キャラクターリストエントリ

1 つの CHARACTERLIST エントリ: フォルダ名、表示名、レアリティ、キャラクターハンドル、アンロック条件。

<div class="callout warn">
Character プロパティの共有ハンドルはリストが所有します。破棄しないでください。描画する前にアニメーションを読み込んでください。
</div>

| メソッド | 説明 |
| --- | --- |
| `entry.FolderName  -> string` | フォルダ名。セーブファイルはこれをキーとして使います。 |
| `entry.DisplayName  -> string` | ローカライズされた表示名。 |
| `entry.Rarity  -> string` | レアリティ名 (一覧はネームプレート情報ハンドルを参照)。 |
| `entry.Character  -> character` | このエントリのキャラクターハンドル。 |
| `entry.UnlockCondition  -> unlockCondition` | アンロック条件 (アンロック条件ハンドルを参照)。 |

### PUCHICHARALIST

読み込み済みのすべてのぷちキャラのリストと、各プレイヤーの現在の選択です。

<div class="callout warn">
スキンはぷちキャラのテクスチャを読み込むときにリストを再構築し、スキンの再読み込み時に破棄するため、読み込まれていない間はこのグローバルが nil になることがあります。問い合わせメソッドはぷちキャラハンドルを返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | 読み込み済みのぷちキャラ数。 |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | すべてのぷちキャラをリストとして。 |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | 0 始まりのインデックスのぷちキャラ。範囲外なら nil。 |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | このフォルダ名のぷちキャラ。見つからなければ nil。 |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | プレイヤースロットが装備しているぷちキャラ。解決できなければ nil。 |

### ぷちキャラハンドル

1 つのぷちキャラ: テクスチャ、ローカライズされた名前と作者、レアリティ、フォルダ名、アンロック条件。

<div class="callout warn">
PUCHICHARALIST と sf:GetPuchichara がこのハンドルを返します。テクスチャはリストが所有します。破棄しないでください。画像がない場合は空のテクスチャになります。
</div>

| メソッド | 説明 |
| --- | --- |
| `puchi.tx  -> texture` | Chara.png から読み込まれたスプライトシート。 |
| `puchi.render  -> texture` | Render.png から読み込まれた全身のレンダー。 |
| `puchi.Name  -> string` | ローカライズされた表示名。 |
| `puchi.Author  -> string` | ローカライズされた作者名。 |
| `puchi.Rarity  -> string` | レアリティ名 (一覧はネームプレート情報ハンドルを参照)。 |
| `puchi.FolderName  -> string` | フォルダ名。セーブファイルはこれをキーとして使います。 |
| `puchi.UnlockCondition  -> unlockCondition` | アンロック条件 (アンロック条件ハンドルを参照)。 |
| `puchi:GetUnlockMessage()  -> string` | `puchi.UnlockCondition:GetConditionMessage()` のショートカット。 |

## プレイ状態とアンロック

### PLAYSTATE

現在または直近のプレイのライブな結果: 判定数、スコア、コンボ、クリア判定、タワーと段位の状態。

<div class="callout warn">
値はゲームプレイ画面から来るため、プレイ中とその後の画面で意味を持ちます。プレイヤーインデックスは 0 始まりで、メソッドは範囲チェックをしません。段位の判定は常にプレイヤー 0 を評価します。
</div>

| メソッド | 説明 |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | タワーモード: 最後に到達した階。 |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | タワーモード: ライフの最大数。 |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | タワーモード: 現在のライフ数。 |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | タワーモード: 曲速度に合わせて調整された無敵時間。 |
| `PLAYSTATE.InvincibilityDuration  -> integer` | タワーモード: 基本の無敵時間。 |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | 前のプレイが最後まで進んだかどうか。 |
| `PLAYSTATE:WasPlayAborted()  -> bool` | プレイヤーが前のプレイを途中で終了したかどうか。 |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | 良 (Good) 判定の数。 |
| `PLAYSTATE:GetOkCount(player)  -> integer` | 可 (Ok) 判定の数。 |
| `PLAYSTATE:GetBadCount(player)  -> integer` | 不可 (Bad) 判定の数。 |
| `PLAYSTATE:GetRollCount(player)  -> integer` | 連打の打数。 |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | 叩いたアドリブ音符の数。 |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | 逃したアドリブ音符の数。 |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | 叩いた地雷音符の数。 |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | 避けた地雷音符の数。 |
| `PLAYSTATE:GetScore(player)  -> integer` | 現在のスコア。 |
| `PLAYSTATE:GetCombo(player)  -> integer` | 現在のコンボ。 |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | 到達した最大コンボ。 |
| `PLAYSTATE:IsClear(player)  -> bool` | ゲージがクリアラインに達しているかどうか。 |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | スコアを減らす Mod が有効な状態でのクリアかどうか。 |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | アシストなしのクリアで、不可判定も地雷ヒットもない。 |
| `PLAYSTATE:IsPerfect(player)  -> bool` | 可判定のないフルコンボ。 |
| `PLAYSTATE:IsAlive()  -> bool` | タワーモード: ライフが残っているかどうか。 |
| `PLAYSTATE:IsPass()  -> bool` | 段位モード: 試験の状態が不合格でないかどうか。 |
| `PLAYSTATE:IsRedPass()  -> bool` | 段位モード: 試験の状態が通常合格かどうか。 |
| `PLAYSTATE:IsGoldPass()  -> bool` | 段位モード: 試験の状態が金合格かどうか。 |
| `PLAYSTATE:IsDanClear()  -> bool` | 段位モード: 合格かつアシストなし。 |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | 段位モード: 不可判定も地雷ヒットもない段位クリア。 |
| `PLAYSTATE:IsDanPerfect()  -> bool` | 段位モード: 可判定のない段位フルコンボ。 |

### アンロック条件ハンドル

ネームプレート、キャラクター、ぷちキャラのアンロック要件です。

<div class="callout warn">
ネームプレート情報ハンドル、キャラクターリストエントリ、ぷちキャラハンドルの UnlockCondition プロパティがこのハンドルを返します。条件のないアイテム (HasCondition が false) は既定で利用可能です。IsUnlockable は true を返し、メッセージは空です。条件の語彙は Unlock.json と譜面のアンロック要素と同じです。<a href="../guides/unlockables.md">譜面のアンロック要素</a>ガイドを参照してください。
</div>

| メソッド | 説明 |
| --- | --- |
| `cond.HasCondition  -> bool` | アイテムにアンロック条件があるかどうか。 |
| `cond:GetConditionType()  -> string` | 条件の種類 id (例: "ch"、"cs"、"gt"、"gc"、"ig")。なければ空文字列。 |
| `cond:GetCoinPrice()  -> integer` | 条件のコイン価格。なければ 0。 |
| `cond:GetConditionMessage()  -> string` | 条件のローカライズされた説明。 |
| `cond:IsUnlockable(player)  -> bool` | プレイヤーが現在条件を満たしているかどうか。 |
| `cond:GetBlockedMessage(player)  -> string` | プレイヤーが条件を満たしていない理由。満たしていれば空文字列。 |

## テーマと言語

### THEME

スキンの解像度、テーマ設定、スキンスコープのローカライズ文字列、テーマ設定の定義。

<div class="callout warn">
スキンはテーマ設定を ThemeSettings.json で宣言し、その値を隣の ThemeSettings.db3 に保存します。ゲッターは設定値を常に文字列として返します。存在しない設定は宣言された既定値を返し、宣言がなければ空文字列を返します。GetThemeSettingForPlayer は 1 始まりのプレイヤー番号を取ります。定義のインデックスは 0 始まりです。
</div>

| メソッド | 説明 |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | スキンの解像度。 |
| `THEME:GetThemeSetting(settingId)  -> string` | グローバルスコープの設定の値。 |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | 1 始まりのプレイヤーに対するセーブスコープの設定の値。セーブに値がなければその既定値。 |
| `THEME:GetSkinString(key)  -> string` | スキンの Locales フォルダのローカライズ文字列。現在の言語、次にスキンの既定ロケール、最後に `[LOCALE NOT FOUND: key]` の順です。 |
| `THEME:GetDefinitionCount()  -> integer` | ThemeSettings.json の設定定義の数。 |
| `THEME:GetDefinitionId(index)  -> string` | 0 始まりのインデックスの定義の id。なければ空文字列。 |
| `THEME:GetDefinitionScope(index)  -> string` | 定義のスコープ: "global" または "save"。 |
| `THEME:GetDefinitionType(index)  -> string` | 定義の型: "bool"、"int"、"double"、"string"、"enum"。 |

### LANG

ローカライズされたゲーム文字列、言語の切り替え、多言語テキスト値。

<div class="callout warn">
GetString は追加の引数でエントリを書式化します。GetLanguageIds と GetLanguageNames は .NET 配列 (0 始まり、`.Length`) を返します。GetAvailableLanguages は `:GetEnumerator()` で列挙する辞書を返します (「データと永続化」を参照)。FromDict は JSONLOADER で解析した JSON オブジェクトを取り (Lua テーブルは受け付けません)、AsLocalizationData は JSONLOADER:LoadJson の JsonNode を取ります。
</div>

| メソッド | 説明 |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | キーのローカライズ文字列。書式プレースホルダは追加の引数で埋められます。 |
| `LANG:ChangeLanguage(id)  -> bool` | id が存在し現在のものと異なればアクティブな言語を切り替え、読み込み済みのすべてのスクリプトで `reloadLanguage` を呼びます。切り替えたかどうかを返します。CONFIG.Language は変更しません。 |
| `LANG:GetLanguageIds()  -> string[]` | 利用可能な言語の id。 |
| `LANG:GetLanguageNames()  -> string[]` | 利用可能な言語の表示名 (同じ順序)。 |
| `LANG:GetAvailableLanguages()  -> dict` | 言語 id から表示名へ。 |
| `LANG:GetExamName(type)  -> string` | 段位の試験の種類のローカライズされた名前。 |
| `LANG:AsLocalizationData(node)  -> localizationData` | `{ "strings": { "<lang>": "text" } }` の形の JsonNode からローカライズ値を構築します。 |
| `LANG:FromDict(dict)  -> localizationData` | 言語 id をテキストに対応付ける解析済み JSON オブジェクトからローカライズ値を構築します。 |
| `LANG:FromString(json)  -> localizationData` | 言語 id をテキストに対応付ける JSON オブジェクト文字列からローカライズ値を構築します。文字列を解析できなければ空の値。 |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### ローカライズデータハンドル

言語 id をキーとし、現在の言語に解決される文字列の集合です。

<div class="callout warn">
LANG:AsLocalizationData、LANG:FromDict、LANG:FromString がこのハンドルを返します。解決の順序: 現在の言語 id、次に "default" キー、最後に GetString に渡されたフォールバック。
</div>

| メソッド | 説明 |
| --- | --- |
| `loc:GetString(fallback)  -> string` | 現在の言語のテキスト、または "default"、またはフォールバック。 |
| `loc:SetString(langId, text)  -> nil` | 言語 id のテキストを設定します。 |
| `loc:GetAllStrings()  -> string[]` | 保存されているすべてのテキスト (順序は不定)。 |

<!-- api/activities.md -->

# モジュールとライフサイクル

エンジンはモジュールの Script.lua で決まった一連のコールバックを呼び出します。このページではそれらを、アクティビティ・バックグラウンド・トランジションを駆動するグローバル、そしてすべてのモジュールが受け取るカウンター、カメラ、診断のヘルパーとともに一覧にします。モジュールを書いたことがなければ、まず[モジュールの仕組み](../getting-started.md)をお読みください。

## ライフサイクルコールバック

エンジンは各モジュールの Script.lua からトップレベルのグローバル関数を名前で探し、決まったタイミングで呼び出します。定義していないコールバックはエンジンがスキップします。どのコールバックをエンジンが呼ぶかはモジュールの種類で決まります。

<div class="callout warn">
スキンの読み込み時、エンジンはモジュールを Transitions、Stages、Activities、ROActivities の順に作成・開始します。各種類の中では、エンジンはまずすべてのモジュールの Script.lua (トップレベルのコード) を実行し、その後それぞれの onStart を呼びます。したがって、ステージの onStart の実行中はアクティビティと ROActivity はまだ読み込まれておらず、そこでの参照は nil を返します。activate で参照してください。スキンの変更時や終了時には、onDestroy が Stages、次に ROActivities と Activities、最後に Transitions の順に実行されます。
</div>

### ステージ、アクティビティ、ROActivity

| メソッド | 説明 |
| --- | --- |
| `onStart()` | エンジンがモジュールを作成した後に 1 回呼ばれます。作成は起動時と、エンジンがスキンを読み込み・再読み込みするたびに行われます。コルーチンとして実行されます (下の LOADING を参照)。アセットはここで読み込みます。 |
| `activate(...)` | ステージ: エンジンがステージに入るたびにコルーチンとして呼ばれます。アクティビティ/ROActivity: ホストが `handle:Activate(...)` を通じて、ホスト自身の引数で呼び出します。戻り値はホストに返ります。エンジンは CHARACTERLIST と PUCHICHARALIST グローバルをこの直前に更新します。 |
| `update(timestamp)` | 毎フレーム draw の前に呼ばれます。timestamp はミリ秒単位のゲームクロックです。ステージは Exit を呼んだ後 update を受け取らなくなります。draw はフェードアウトの間も呼ばれ続けます。アクティビティ/ROActivity: ホストが `handle:Update()` を通じて呼びます。 |
| `draw(...)` | 毎フレーム呼ばれます。アクティビティ/ROActivity: ホストが `handle:Draw(...)` を通じてホスト自身の引数で呼びます。戻り値はホストに返ります。 |
| `deactivate(...)` | ステージ: エンジンがステージを離れるときに呼ばれます。アクティビティ/ROActivity: ホストが `handle:Deactivate(...)` を通じて呼ぶか、モジュール自身が `DEACTIVATE()` で自分に対して呼び出します。戻り値はホストに返ります。 |
| `afterSongEnum()` | 起動時とソフト/ハードの楽曲リロード後を含め、楽曲の列挙が完了するたびに、モジュールがアクティブでなくても呼ばれます。 |
| `onDestroy()` | エンジンがスキンをアンロードする前に、モジュールが保持しているものを解放できるよう呼ばれます。 |
| `reloadLanguage(lang)` | ゲームの言語が変更されたときにすべての読み込み済みモジュールで呼ばれます。lang は新しい言語コードです。 |

### バックグラウンド

各画面が自身のバックグラウンドをホストします。下のバックグラウンドの節で state 引数とイベントフックを説明します。

| メソッド | 説明 |
| --- | --- |
| `onStart()` | ホストが最初にバックグラウンドをアクティブにしたときに、同期的に 1 回呼ばれます。 |
| `activate(state)` | ホストがバックグラウンドをアクティブにするたびに呼ばれます。再アクティブ化で onStart は再実行されません。 |
| `update(timestamp, state)` | 毎フレーム呼ばれます (ゲームプレイの一時停止中は呼ばれません)。timestamp はミリ秒単位の `state.timeStamp` です。 |
| `draw(state)` | 毎フレーム呼ばれます。 |
| `reloadLanguage(lang)` | ゲームの言語が変更されたときに呼ばれます。 |

エンジンはバックグラウンドでは afterSongEnum と onDestroy を呼びません。ホストがバックグラウンドを破棄するとき、それが作成したリソースはエンジンが解放します。

### トランジション

| メソッド | 説明 |
| --- | --- |
| `onStart()` | スキンの読み込み時に、ステージとアクティビティより前に、コルーチンとして 1 回呼ばれます。 |
| `fadeOut(t)` | 離れるステージの上にフェードアウトを描画します。t は 0 から 1 に進みます。 |
| `loading(progress, elapsed)` | ローディング画面を描画します。progress は 0 から 1、elapsed は読み込み開始からの秒数です。 |
| `fadeIn(t)` | 新しいステージの上にフェードインを描画します。t は 0 から 1 に進みます。 |
| `onDestroy()` | エンジンがスキンをアンロードする前に、ステージとアクティビティの後に呼ばれます。 |
| `reloadLanguage(lang)` | ゲームの言語が変更されたときに呼ばれます。 |

下のトランジションの節で各フェーズのタイミングを説明します。

### キャラクター

キャラクターの Script.lua は別のセットを定義します: loadAnimation、disposeAnimation、availableAnimation、setAnimationDuration、resetAnimationCounter、update、draw、getDrawSize、getAIBattlePosition、loadVoice、disposeVoice、playVoice。[キャラクターの追加](../guides/characters.md)で扱います。

### Exit

エンジンがステージスクリプトにのみ登録する関数で、呼び出すとエンジンにステージを離れるよう要求します。

<div class="callout warn">
Modules/Stages のスクリプトでのみ利用できます。アクティビティと ROActivity はホストが駆動するため、これを持ちません。0 から 3 個の引数を受け付け、どの位置の nil も許容します。呼び出すこと自体が退出の要求になります。同梱ステージは、そのフレーム内で他に何も実行されないよう update の中で `return Exit(...)` と書いています。target は "title"、"play"、"stage"、"legacy" のいずれかで、nil やその他の値は "title" を意味します。target が "stage" のとき、name は移動先の Modules/Stages モジュールです。target が "legacy" のとき、name は "heya"、"config"、"exit"、"onlinelounge" のいずれかです (それ以外の値はタイトルへ移動します)。transition は Modules/Transitions モジュールの名前です。省略するかエンジンが見つけられない場合、エンジンは "default" という名前のモジュールを使い、スキンにトランジションがまったくない場合は単純な黒のフェードアウトが再生されます。ステージとトランジションの名前は呼び出し元の側が優先されます。スキンのステージはその名前のスキンのモジュールを、Global/Modules のステージはグローバルのモジュールを先に見つけ、それぞれもう一方にフォールバックします。"heya" キーはエイリアスとして残っています。Modules/Stages にあるスキンの `myroom` モジュールを開き、スキンにそのモジュールがない場合はタイトルへ移動します。
</div>

| メソッド | 説明 |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | 指定した遷移先へのステージ退出を要求します。任意で対象モジュールとトランジションモジュールを指定できます。0 を返します。 |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "mystage")        -- jump to Modules/Stages/mystage
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- back to the title through a named transition
    end
end
```

### STAGES

ステージの名前空間のためのグローバルです。ステージは名前空間を宣言し、どのモジュールも名前空間のステージを一覧にできます。たとえば、その名前空間向けに作られたステージのメニューを作るために使います。

<div class="callout warn">
すべてのモジュールに STAGES グローバルとして登録されますが、名前空間を持てるのはステージのスクリプトだけです。ほかのモジュールでは SetNamespace は何もせず false を返します。ステージが持つ名前空間は一度に1つで、後の呼び出しで置き換わり、nil か "" で消えます。呼び出しは Script.lua の先頭、関数の外でも、どのコールバックの中でも動きます。Script.lua の先頭はスキンの読み込み時にエンジンがステージを作るときに実行されるので、どの onStart よりも前にステージが一覧に載ります。エンジンは info を読まずに単純なデータとして保存します。文字列、数値、真偽値と、それらのテーブル (入れ子は8段まで) で、関数とエンジンのオブジェクトは除かれます。ListNamespace はステージ名ごとに1つのエントリを返します。それは呼び出し元のモジュールから Exit("stage", name) で開かれるステージです。スキンのモジュールはその名前のスキンのステージを、グローバルのモジュールはグローバルのステージを得て、それぞれもう一方にフォールバックし、そのステージが名前空間を宣言しているときだけ一覧に載ります。エントリは名前順で、各 info は新しいコピーです。スキンのステージがすべて作られた時点で全ステージがそろうので、一覧は onStart 以降で取ってください。Script.lua の先頭では、呼び出し元より前に作られたステージしかありません。スキンのカスタムステージのメニューが一覧にする CStage 名前空間については [カスタムステージ](../modules/custom-stages.md) で説明しています。
</div>

| メソッド | 説明 |
| --- | --- |
| `STAGES:SetNamespace(namespace, info?)  -> boolean` | 呼び出し元のステージの名前空間を宣言します。任意の info テーブルは、それを一覧にするステージに返されます。ステージのスクリプト以外では false を返します。 |
| `STAGES:SetResolution(width, height)  -> boolean` | 呼び出し元のステージが描画する解像度を宣言します。スキンの解像度と異なる場合、ステージが描くものはすべて、その解像度のスキン全体と同じように、比率を保ったまま中央に合わせてスキンの解像度へ拡大縮小されます。マウス座標、`INPUT:GetSurfaceWidth/Height`、クリップもそれに従い、ステージが開くスキンの ROActivity はスキンの解像度のままです。0 で解除します。Stage スクリプトの外では false です。 |
| `STAGES:ListNamespace(namespace)  -> table` | 名前空間のステージを `{ name, isGlobal, info }` テーブルの配列 (1 始まり) で返します。フォルダー名、Global/Modules/Stages にあるステージなら true、宣言された info (指定がなければ空のテーブル) です。 |

```lua
-- Modules/Stages/color_tool/Script.lua, at the top
STAGES:SetNamespace("MyTools", { title = { en = "Colour picker", fr = "Pipette" } })
STAGES:SetResolution(1920, 1080)

-- another stage lists them
function activate()
    for _, s in ipairs(STAGES:ListNamespace("MyTools")) do
        debugLog(s.name .. (s.isGlobal and " (global)" or ""))
    end
end
```

### LOADING

コルーチンとして実行されるコールバック (すべてのモジュール種類の onStart と、ステージの activate) 用のローディングバーヘルパーです。

<div class="callout warn">
すべてのモジュールで LOADING グローバルとして定義されています。これらのコールバックは、エンジンが毎フレーム再開するエンジン所有のコルーチン上で実行されます。1 回の再開が時間予算を使い切るとエンジンが自動的に yield し、coroutine.yield(progress) や LOADING:Tick(sub) で自分で yield することもできます。LOADING:Add で登録したブロックは、コールバック本体が戻った後に順番に実行され、各ブロックの後にバーが進みます。ブロックの weight はバーに占める割合です (既定 1)。LOADING:Tick(sub) はブロック内から 1 フレーム yield し、そのブロック内の 0 から 1 の割合を報告します。コルーチンでないコールバック (アクティビティや ROActivity の activate、あらゆる update や draw) では、yield する先がないため LOADING:Tick は Lua エラーを発生させ、LOADING:Add でキューに入れたブロックは決して実行されません。
</div>

| メソッド | 説明 |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | コールバックが戻った後に実行する読み込みブロックを登録します。 |
| `LOADING:Add(label, fn)  -> nil` | ラベル付きの読み込みブロックを登録します。 |
| `LOADING:Add(label, weight, fn)  -> nil` | 明示的な weight を持つラベル付きの読み込みブロックを登録します。 |
| `LOADING:Tick(sub)  -> nil` | ブロック内で 1 フレーム yield し、現在のブロック内の 0 から 1 のサブ進捗を報告します。 |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## アクティビティ

### ACTIVITY

読み込み済みのアクティビティを名前で参照するためのグローバルです。

<div class="callout warn">
ROActivity とバックグラウンドを除くすべてのモジュールに ACTIVITY グローバルとして登録されています。ROActivity とバックグラウンドでは nil で、これらのモジュールは ROACTIVITY を使います。エンジンはアクティビティを Modules/Activities/{name} から読み込みます。ACTIVITY は GetROActivity も公開しており、ROACTIVITY:GetROActivity と同じように動作します。参照は呼び出し元の側が優先されます。スキンのモジュールはその名前のスキンのアクティビティを、Global/Modules のモジュールは Global/Modules/Activities のものを受け取り、自分の側にない場合はそれぞれもう一方にフォールバックします。Lib ファイルは、それを require したモジュールの側から参照します。
</div>

| メソッド | 説明 |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | 指定したフォルダ名の読み込み済みアクティビティのハンドルを返します。読み込まれていなければ nil です。 |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | ROACTIVITY:GetROActivity と同じです。 |

### ROACTIVITY

読み込み済みの読み取り専用アクティビティ (ROActivity) を名前で参照するためのグローバルです。

<div class="callout warn">
すべてのモジュールに ROACTIVITY グローバルとして登録されています。エンジンは ROActivity を Modules/ROActivities/{name} から読み込み、読み取り専用の CONFIG、DATABASE、GetSaveFile グローバルを与えるため、そのスクリプトはゲームの状態を変更できません (「モジュールの仕組み」の読み取り専用モジュールの節を参照)。アクティビティと ROActivity は名前をキーとするシングルトンです。フォルダごとに 1 つのインスタンスがあり、すべてのホストがそれを共有します。GetROActivity は、ACTIVITY:GetActivity と同じように、Modules/ROActivities と Global/Modules/ROActivities の間で呼び出し元の側を優先します。
</div>

| メソッド | 説明 |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | 指定したフォルダ名の読み込み済み ROActivity のハンドルを返します。読み込まれていなければ nil です。 |

### アクティビティハンドル

ACTIVITY:GetActivity と ROACTIVITY:GetROActivity が返すオブジェクトです。ホストはこれを使ってモジュールのコールバックを駆動します。

<div class="callout warn">
Activate、Deactivate、Draw は引数をモジュールの activate、deactivate、draw コールバックに転送します。Update は現在のゲーム時間 (ミリ秒) で update を呼びます。それぞれ、コールバックが返した値を 0 から始まるインデックスの配列として返し、コールバックが何も返さないか未定義の場合は nil を返します。最初の値は `result[0]` で読み取ります。Call はモジュールのスクリプトが定義する任意のグローバル関数を呼び出します。
</div>

| メソッド | 説明 |
| --- | --- |
| `handle.IsActive  -> boolean` | Activate が実行されてから、Deactivate (またはモジュール自身の DEACTIVATE()) が実行されるまで true です。 |
| `handle:Activate(...)  -> array` | 指定した引数でモジュールの activate コールバックを呼びます。 |
| `handle:Deactivate(...)  -> array` | 指定した引数でモジュールの deactivate コールバックを呼びます。 |
| `handle:Update()  -> array` | 現在のゲーム時間 (ミリ秒) でモジュールの update コールバックを呼びます。 |
| `handle:Draw(...)  -> array` | 指定した引数でモジュールの draw コールバックを呼びます。 |
| `handle:Call(functionName, ...)  -> array` | モジュールのスクリプトの指定した名前のグローバル関数を、指定した引数で呼びます。 |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

エンジンがアクティビティと ROActivity のスクリプト内に登録する関数で、モジュールが自分自身を非アクティブにできます。

<div class="callout warn">
呼び出すとモジュールは非アクティブ (handle.IsActive が false) になり、モジュール自身の deactivate コールバックが実行されます。同梱のダイアログはプレイヤーが決定またはキャンセルしたときにこれを呼び、ホストは IsActive を監視してダイアログが閉じたことを知ります。
</div>

| メソッド | 説明 |
| --- | --- |
| `DEACTIVATE(...)` | 現在のモジュールを非アクティブにし、指定した引数で deactivate コールバックを呼びます。 |

### エンジンがホストする ROActivity

エンジンはいくつかの ROActivity を決まったフォルダ名で参照し、自身で駆動します。スキンは、その名前の Modules/ROActivities フォルダを同梱することでこれらを置き換えます。定義しなければならないコールバックは、以下に挙げるエンジン側の呼び出し箇所で決まります。存在しない場合、エンジンは対応する機能を描画しません。

| 名前 | エンジンが呼ぶもの |
| --- | --- |
| `nameplate` | プレイヤーのプレートが変わったときに `activate(player, name, title, dan, data)`。毎フレーム `update()`。`draw(mode, ...)` は mode 0 = 完全なプレート `(x, y, opacity, player, side)`、1 = 段位プレート `(x, y, opacity, danGrade, textTexture)`、2 = 称号プレート `(x, y, opacity, type, textTexture, rarity, nameplateId)`。 |
| `modal` | キューに入ったアンロックモーダルごとに `activate(player, rarity, modalType, ...)`、その後毎フレーム `update()` と `draw()`。スクリプトは DEACTIVATE() を呼んでモーダルを閉じ、エンジンは次のものをアクティブにします。 |
| `modicons` | 一度だけ `activate()`、その後 layout が "menu" または "game" の `draw(x, y, player, layout, alpha)`。MODICONS グローバルがこれをラップします。 |
| `danplate` | リザルト画面と段位コースで `draw(x, y, opacity, danTick, r, g, b, titleText)`。 |
| `results_tower` | 塔のリザルトを開くとき、プレイ内容を LuaResultInfo (`ReachedFloor`、`TotalFloors`、`Lives`、`TowerRank`、`IsNewBestFloor`、`GetScore(player)` などの読み取り専用ゲッター) として渡して `activate(info)`。毎フレーム `update()` と `draw()`。決定時に Call で `skip()` (アニメーション中は true を返し、その決定はスキップだけになります)。`deactivate()`。 |
| `results_regular` | プレイのリザルト (AI バトルも含む) を開くとき、プレイ内容を LuaResultInfo (0 から始まるプレイヤーごとのゲッター `GetGauge(p)`、`GetClearType(p)`、`GetScoreRank(p)`、`GetScore(p)`、および `IsAIBattle`、`GetAISectionEnd(i)`) として渡して `activate(info)`。毎フレーム、リザルト背景の上で `update()` と `draw()`。決定時に Call で `skip()` (アニメーション中は true を返し、背景には `skipAnime` が送られます)。`deactivate()`。 |
| `results_dan` | 段位のリザルトを開くとき、プレイ内容を LuaResultInfo (`DanResult`、`DanCombo`、`GetDanSong(i)`、`GetExam(i)`、`exam:GetSong(i)`、`exam.PreviousBestAmount`、`DanGradeChange`、`DanGrade`) として渡して `activate(info)`。毎フレーム、段位リザルト背景の上で `update()` と `draw()`。決定時に Call で `skip()` (アニメーション中は true を返します)。続いて決定時に Call で `advance()` (報酬のポップアップの前にもう一つ画面を見せるときに true を返します。新しい段位や上がった段位はエンジンがすでにネームプレートに反映しているので、モジュールはそれを明かすまで `PreviousNameplateDan` を描きます)。左右で Call により `swap()` (画面を切り替えたときに true を返します)。`deactivate()`。 |
| `tower_hud` | 塔のプレイ開始時に `activate(maxFloor)`。毎フレーム上側背景の上に `update()` と `draw()`。`deactivate()`。階とライフは PLAYSTATE から読みます。 |
| `popup_menu` | `activate(title, items, fontSize, ...)`。items は改行で連結したラベルで、その後に PopupMenu のスキン位置が続きます。毎フレーム `draw(selected)`。閉じるときに `deactivate()`。 |
| `config_ui` | 設定モデルとともに `activate(model)`。毎フレーム `update()` を呼び、"exit" を返すと設定画面を離れます。続けて Modules/Transitions のモジュール名を返すと、戻りにそのトランジションを使います (スキン・解像度・サウンドデバイスの変更が適用待ちのときは通常のフェードになります)。`draw()`。エンジンがモデルを再構築したときに Call を通じて `reload(model)`。`deactivate()`。 |
| `song_enum` | `activate()`、その後楽曲のスキャン中は毎フレーム `draw(isCommandSongDataGet, done, total)`。`deactivate()`。 |
| `cutscene` | `activate(paths, fadeOutSeconds)`。動画ファイルを順に、続けて最後の動画の後の黒へのフェード秒数（0 でなし）。毎フレーム `update()` を呼び、すべての動画が再生し終わりフェードも終わると "finished" を返します。`draw()`、`deactivate()`。各ファイルを自身の音声トラックと共に再生し、ポーズポップアップも持ちます。カットシーンの用途は呼び出し側が決めます。 |

3 つの results ROActivity はリザルト画面の音楽を自分で再生します。報酬ポップアップの表示中、エンジンは Call で `setMusicLevel(percent)` (0 から 100) を呼んで音量を下げます。画面が暗転し終わるとエンジンはモジュールを非アクティブにし、その音楽も止まります。

### リザルト情報

リザルトステージが results_tower、results_regular、results_dan の activate コールバックに渡す LuaResultInfo オブジェクトです。

<div class="callout warn">
リザルトステージはプレイの保存後にこれを埋め、以後は変化しません。すべてのメンバーは読み取り専用です。プロパティはドット構文 (`info.Title`)、メソッドはコロン構文を使います。プレイヤー引数は 0 から始まり、範囲外ではメソッドは 0、false、または空文字列を返します。モジュール名が付いたメンバーはその画面でのみ埋められ、ほかの画面ではその既定値のままです。
</div>

| メソッド | 説明 |
| --- | --- |
| `info.Title  -> string` | 譜面のタイトル。 |
| `info.Subtitle  -> string` | 譜面のサブタイトル。 |
| `info.PlayerCount  -> number` | プレイヤー数 (タワーと段位のリザルトでは 1)。 |
| `info.PlayerSide  -> number` | プレイヤー 1 のネームプレートの側。青側でプレイするとき 1、それ以外は 0。 |
| `info:GetGoodCount(player)  -> number` | 良の数。 |
| `info:GetOkCount(player)  -> number` | 可の数。 |
| `info:GetBadCount(player)  -> number` | 不可の数。 |
| `info:GetRollCount(player)  -> number` | 連打数。 |
| `info:GetMaxCombo(player)  -> number` | 最大コンボ数。 |
| `info:GetADLibCount(player)  -> number` | 叩いた AD-LIB 音符の数。 |
| `info:GetBoomCount(player)  -> number` | 叩いた爆弾の数。 |
| `info:GetScore(player)  -> number` | 最終スコア。 |
| `info:GetPreviousBestScore(player)  -> number` | このプレイが保存される前の、この譜面と難易度でのプレイヤーのハイスコア。なければ 0。 |
| `info:IsSaved(player)  -> boolean` | プレイがプレイヤーのセーブに記録されたとき true (オートや再生中のリプレイでは記録されません)。 |
| `info:GetDifficulty(player)  -> number` | results_regular: プレイした難易度。0 かんたん から 4 裏鬼。 |
| `info:GetGauge(player)  -> number` | results_regular: プレイ終了時の魂ゲージ。0 から 100。 |
| `info:GetGaugeType(player)  -> number` | results_regular: 0 ノーマル、1 ハード、2 エクストリーム。 |
| `info:GetNorma(player)  -> number` | results_regular: ノーマルゲージがクリアとなるゲージ値。 |
| `info:GetKillZone(player)  -> number` | results_regular: エクストリームゲージのキルゾーン。0 から 100 (ほかのゲージでは 0)。 |
| `info:GetClearType(player)  -> number` | results_regular: 0 未クリア、1 アシストクリア、2 クリア、3 フルコンボ、4 全良。 |
| `info:GetScoreRank(player)  -> number` | results_regular: 0 なし、その後 1 から 7 のスコアランク。 |
| `info:IsCleared(player)  -> boolean` | results_regular: クリアしたとき true。AI バトルではプレイヤー 1 が勝ったとき。 |
| `info:GetSpeechText(player, mood)  -> string` | results_regular: プレイヤーのキャラクターの、気分ごとのリザルトのセリフ。0 (最低) から 5 (全良)。 |
| `info.IsAIBattle  -> boolean` | results_regular: AI バトルのとき true。 |
| `info.AISectionCount  -> number` | results_regular: AI バトルの区間数。 |
| `info:GetAISectionEnd(section)  -> number` | results_regular: 区間 (0 から) の結果。0 最後までプレイしていない、1 勝ち、2 負け。 |
| `info.TowerType  -> string` | results_tower: 譜面の TOWERTYPE。指定がなければ ""。 |
| `info.TowerSide  -> number` | results_tower: 0 甘口、1 辛口。 |
| `info.ReachedFloor  -> number` | results_tower: 到達した階。 |
| `info.TotalFloors  -> number` | results_tower: タワーの階数。 |
| `info.Lives  -> number` | results_tower: 残りライフ。 |
| `info.MaxLives  -> number` | results_tower: 開始時のライフ。 |
| `info.TowerRank  -> number` | results_tower: 0 なし、その後 1 から 7。10、25、50、75 %、クリア、フルコンボ、全良。 |
| `info.PreviousBestFloor  -> number` | results_tower: このプレイが保存される前の最高到達階。 |
| `info.IsNewBestFloor  -> boolean` | results_tower: 保存されたプレイが最高到達階を更新したとき true。 |
| `info.IsNewBestScore  -> boolean` | results_tower: 保存されたプレイがハイスコアを更新したとき true。 |
| `info.IsTired  -> boolean` | results_tower: タワーキャラクターが疲れた状態 (残りライフが少ない) でプレイが終わったとき true。 |
| `info.Bpm  -> number` | results_tower: プレイ終了時の BPM。 |
| `info.DanTick  -> number` | results_dan: 段位プレートの目盛り (DANTICK)。 |
| `info.DanTickColor  -> color` | results_dan: 目盛りの色 (DANTICKCOLOR)。 |
| `info.DanResult  -> number` | results_dan: 0 不合格、1 合格 (赤)、2 合格 (金)。 |
| `info.DanCombo  -> number` | results_dan: 0、1 不可なし (フルコンボ)、2 不可も可もなし (全良)。 |
| `info.TotalHits  -> number` | results_dan: 良、可、連打を合わせた数。 |
| `info.DanSongCount  -> number` | results_dan: 段位の曲数。 |
| `info.DanSongReached  -> number` | results_dan: プレイが終わった曲 (0 から)。 |
| `info.DanExamSong  -> number` | results_dan: 曲ごとの試験が最後に表示された曲 (0 から)。 |
| `info:GetDanSong(song)  -> dan song` | results_dan: 段位の i 曲目 (0 から)。なければ nil。 |
| `info.ExamCount  -> number` | results_dan: 段位の試験数。 |
| `info:GetExam(exam)  -> dan exam` | results_dan: i 番目の試験 (0 から、譜面の順)。なければ nil。 |
| `info.DanGradeChange  -> number` | results_dan: 0 変化なし、1 新しい段位、2 合格済みの段位がより良くなった (金、またはより良いクリア)。新しい段位や上がった段位はエンジンがプレイヤーのネームプレートに反映します。 |
| `info.DanGrade  -> dan grade` | results_dan: このプレイ後のセーブにおけるこの段位の評価。一度も合格していなければ Title は ""。 |
| `info.PreviousNameplateDan  -> dan grade` | results_dan: このプレイの前にネームプレートが表示していた段位 (なければ Title は "")。 |

### 段位の曲

info:GetDanSong が返す読み取り専用のオブジェクトです。

| メソッド | 説明 |
| --- | --- |
| `song.Title  -> string` | 曲のタイトル。譜面が隠している曲では "???"。 |
| `song.Subtitle  -> string` | 曲のサブタイトル。譜面が隠している曲では ""。 |
| `song.Difficulty  -> number` | 0 かんたん から 4 裏鬼。 |
| `song.Level  -> number` | 曲のレベル。 |
| `song.Good  -> number` | その曲の良の数。 |
| `song.Ok  -> number` | その曲の可の数。 |
| `song.Bad  -> number` | その曲の不可の数。 |
| `song.Roll  -> number` | その曲の連打数。 |

### 段位の試験

info:GetExam と exam:GetSong が返す読み取り専用のオブジェクトで、プレイ終了時点の試験を表します。

| メソッド | 説明 |
| --- | --- |
| `exam.Type  -> number` | 0 魂ゲージ、1 良、2 可、3 不可、4 スコア、5 連打、6 叩けた数、7 コンボ、8 精度、9 AD-LIB、10 BOOM。名前は LANG:GetExamName(type) で得られます。 |
| `exam.Range  -> number` | 0 Value 以上、1 Value 未満。 |
| `exam.Value  -> number` | 合格 (赤) の値。 |
| `exam.Amount  -> number` | 表示される値。「未満」の試験では、不合格になるまでの残り。 |
| `exam.PreviousBestAmount  -> number` | このプレイが保存される前の、この試験でのプレイヤーの最高の Amount。Amount と同じ基準 (大きいほど良い)。なければ -1。曲ごとの試験では、各曲 (exam:GetSong) がそれぞれ持ちます。 |
| `exam.Percent  -> number` | Value に対する Amount の割合。0 から 100。 |
| `exam.Status  -> number` | 0 不合格、1 合格、2 金合格。 |
| `exam.IsPerSong  -> boolean` | 曲ごとに判定される試験のとき true。その場合、この試験自体は最後の曲のものです。 |
| `exam:GetSong(song)  -> dan exam` | 曲ごとの試験の、i 曲目 (0 から) のもの。なければ nil。 |

### 段位の評価

info.DanGrade と info.PreviousNameplateDan が返すオブジェクト (読み取り専用)。

| メソッド | 説明 |
| --- | --- |
| `grade.Title  -> string` | ネームプレートに表示される段位の名前 (譜面タイトルの最初の 2 文字)。なければ ""。 |
| `grade.Gold  -> boolean` | 金合格なら true。 |
| `grade.Tier  -> number` | 0 クリア、1 フルコンボ、2 全良。 |

## バックグラウンド

### バックグラウンドモジュール

エンジン自身の画面にホストされ、画面背景、ゲームプレイのレイヤー、モブ、クリアアニメーション、くす玉エフェクトのいずれか 1 つを描画する Script.lua です。

<div class="callout warn">
バックグラウンドは Modules フォルダの外、スキンの Graphics フォルダの下の、装飾する画面のディレクトリに置かれます。例えば Graphics/0_Startup/Script.lua、Graphics/6_Result/Script.lua、Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua、Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua、Graphics/5_Game/3_Mob/{variant}/Script.lua、Graphics/5_Game/9_End/{result}/Script.lua、Graphics/5_Game/11_Balloon/Kusudama/Script.lua です。フォルダが複数のバリアントを持つ場合、エンジンはプレイごとにランダムに (または譜面のシーンプリセットから) 1 つを選びます。ホスト画面がバックグラウンドのインスタンスを作成し (ゲームプレイのバックグラウンドはエンジンがゲーム画面に入るたびに)、画面とともに破棄するため、ゲームプレイ中は複数が同時に生存します。バックグラウンドスクリプトは ROActivity と同じグローバルを受け取ります (読み取り専用の CONFIG、DATABASE、GetSaveFile。ACTIVITY はなし)。以下のイベントフックは任意で、エンジンはそれぞれをイベントが発生したときに 1 回呼びます。 塔では、譜面の `TOWERTYPE` が Down フォルダを指定します。
</div>

| メソッド | 説明 |
| --- | --- |
| `clearIn(player)` | ゲームプレイの Up および Down バックグラウンド: プレイヤーのゲージがクリアゾーンに達しました。 |
| `clearOut(player)` | ゲームプレイの Up および Down バックグラウンド: プレイヤーのゲージがクリアゾーンから外れました。 |
| `playEndAnime(player)` | クリアアニメーション (Graphics/5_Game/9_End): プレイヤーの終了アニメーションが始まります。 |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | くす玉: 風船が現れた、プレイヤーが割った、またはプレイヤーが失敗した。 |
| `skipAnime()` | リザルトのバックグラウンド: プレイヤーがリザルトアニメーションをスキップしました。 |

### バックグラウンド状態

ホストがバックグラウンドの activate、update、draw に渡すオブジェクトです。

<div class="callout warn">
ホストごとに 1 つのインスタンスで、ホストが毎フレームその場で更新します。配列フィールドはエンジンのプレイヤーごとの配列を共有し、0 から始まるインデックスです (`state.gauge[0]` がプレイヤー 1)。ゲームプレイのフィールドはゲームプレイのホストだけが更新し、他のホストでは既定値のままで、timeStamp はゲームプレイ外では -1 のままです。state はフレームのタイミングを持ちません。fps グローバルを読んでください。
</div>

| メソッド | 説明 |
| --- | --- |
| `state.playerCount  -> number` | プレイヤー数。 |
| `state.p1IsBlue  -> boolean` | プレイヤー 1 が青側を使うとき true。 |
| `state.lang  -> string` | 現在の言語コード。 |
| `state.simplemode  -> boolean` | シンプルモードが有効なとき true。 |
| `state.puchicharaRarities  -> string[]` | 各プレイヤーのぷちキャラのレアリティ。 |
| `state.characterRarities  -> string[]` | 各プレイヤーのキャラクターのレアリティ。 |
| `state.isClear  -> boolean[]` | 各プレイヤーが現在クリアゾーンにいるかどうか。ノーマルゲージの段位では、最初のヒットで true になり、プレイが失敗するか試験が不合格になると false になる。 |
| `state.gauge  -> number[]` | 各プレイヤーのゲージ値。 |
| `state.bpm  -> number[]` | 各プレイヤーの現在の BPM。 |
| `state.gogo  -> boolean[]` | 各プレイヤーがゴーゴータイム中かどうか。 |
| `state.towerNightNum  -> number` | 塔の昼から夜への係数 (0 から 1)。 |
| `state.towerFloor  -> number` | プレイ中の階 (0 から、塔のプレイ)。 |
| `state.towerMaxFloor  -> number` | 譜面の階数 (塔のプレイ)。 |
| `state.battleState  -> number` | AI バトルの状態コード。 |
| `state.battleWin  -> boolean` | プレイヤーが AI バトルで優勢のとき true。 |
| `state.timeStamp  -> number` | 譜面に同期した秒単位の時間。ゲームプレイ外では -1。 |
| `state.paused  -> boolean` | ゲームプレイの一時停止中は true。 |
| `state.player  -> number` | プレイヤーごとのホスト (クリアアニメーション) が描画対象としているプレイヤー。 |
| `state.hitRing  -> number` | hitNote、hitBig、hitJudge に保持される直近のヒットの数 (32)。 |
| `state.hitCount  -> number[]` | バックグラウンドの開始以降の各プレイヤーのヒット数: 判定された音符 (ミスを含む)、連打のヒット、割れた風船。0 から数えて n 番目のヒットは、hitNote、hitBig、hitJudge のインデックス n % hitRing にあります。 |
| `state.hitNote  -> string[][]` | 保持された各ヒットの音符 (プレイヤーごと): "don"、"ka"、"kadon" のいずれか。 |
| `state.hitBig  -> boolean[][]` | 保持されたヒットが大きな音符だったとき true (プレイヤーごと)。 |
| `state.hitJudge  -> string[][]` | 保持された各ヒットの判定 (プレイヤーごと): "perfect"、"great"、"good"、"poor"、"miss"、"roll"、"balloon" のいずれか。 |
| `state.landCount  -> number[]` | バックグラウンドの開始以降に飛行の終点に達した、各プレイヤーの飛んでいく音符の数。 |
| `state:SetFlyTarget(player, x, y)` | このゲームプレイのバックグラウンドが表示されている間、プレイヤーの飛んでいく音符はスクリーンピクセル単位の (x, y) に着地し、スキンの着地エフェクトは省かれます。 |
| `state:ClearFlyTarget(player)` | プレイヤーの飛んでいく音符が、再びスキンの通常の位置に着地します。 |

## トランジション

### トランジションモジュール

2 つのステージ間のフェードアウト、ローディング、フェードインの各フェーズを描画する Modules/Transitions/{name}/Script.lua です。

<div class="callout warn">
Exit の第 3 引数がトランジションを選択します。呼び出しが名前を指定しないか、エンジンが名前を見つけられない場合、エンジンは "default" を使います。Exit("play") 後のゲームプレイへの読み込みは常に "song_loading" という名前のトランジションを使い、スキンにそれがなければ "default" を使います。エンジンはフェーズを順に駆動します。まず離れるステージの上で t が 1 に達するまで毎フレーム fadeOut(t) を呼び、次に離れるステージをアンマウントし、新しいステージを読み込む間は毎フレーム loading(progress, elapsed) を呼び、その後新しいステージの上で t が 1 に達するまで fadeIn(t) を呼びます。各フェードは、スクリプトが FADE_OUT_SECONDS または FADE_IN_SECONDS を設定しない限り 0.5 秒です。正の数でない値はエンジンが無視します。ステージ切り替えでは、エンジンは読み込みが 0.5 秒を超えた場合にのみ loading を呼び、それまでは短い読み込みでローディング画面が一瞬表示されないよう fadeOut(1) を呼びます。楽曲読み込みの経路では、ローディングフェーズが直ちに表示されます。読み込みまたはフェードインの間に新しいステージが開始した BGM は待機し、フェードインの終わりに再生されます (ゲームプレイへの読み込みは除く)。トランジション自身のサウンドはすぐに再生されます。フェードイン中、新しいステージは入力を受け取りません。ただし、フェードインがゲームプレイを表示する場合は除きます。プレイへの読み込みをプレイヤーが中止すると、エンジンは選曲画面に戻り、fadeIn(t) でそれを表示します。そのため、どのトランジションも fadeIn(1) の呼び出しで終わります。
</div>

| メソッド | 説明 |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | 任意のトップレベルグローバル: フェードアウトの長さ (秒)。 |
| `FADE_IN_SECONDS  -> number` | 任意のトップレベルグローバル: フェードインの長さ (秒)。 |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## タイミングとカメラ

### COUNTER

値を開始値から終了値へ時間をかけて動かすアニメーションカウンターのファクトリです。

<div class="callout warn">
COUNTER グローバルとして登録されています。カウンターは Tick を呼んだときにのみ、フレームのデルタを interval で割った分だけ進みます。interval は値 1 単位あたりの秒数です。interval の符号は方向と一致していなければなりません。end が begin より大きければ正、小さければ負です。符号が一致しない場合、カウンターは両端を入れ替え、最初の tick で終了します。CreateCounterDuration は合計時間を取り、符号を自動で決めます。interval が 0 の場合、または begin と end が等しい場合、カウンターは最初の tick で終了します。カウンターは値が終了値に達したときに任意の ended 関数を呼び、ループやバウンス時はサイクルが完了するごとに 1 回呼びます。
</div>

| メソッド | 説明 |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | begin から end へ 1 単位あたり interval 秒で動き、完了時に ended を呼ぶカウンターを作成します。 |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | begin から end へ指定した秒数で動くカウンターを作成します。seconds が正でないか begin と end が等しい場合は空のカウンターを返します。 |
| `COUNTER:EmptyCounter()  -> counter` | 値が 0 のままの不活性なカウンターを作成します。プレースホルダとして使えます。 |

### カウンターハンドル

COUNTER で作成されたカウンターです。

<div class="callout warn">
毎フレーム Value を読み、進めるために毎フレーム Tick を呼びます。開始していない、または停止したカウンターは Tick を無視します。Begin、End、Interval は読み書き可能なフィールドです。SetLoop と SetBounce は互いに排他です。SetEasing は報告される Value の形を変えるだけで、内部ではカウンターは線形に進み続けます。リスナーは最後のものを含め、tick ごとに現在の値を受け取ります。
</div>

| メソッド | 説明 |
| --- | --- |
| `counter.Value  -> number` | 現在の値。設定されていればイージングが適用されます。代入するとカウンターがその値にジャンプします。 |
| `counter.Begin  -> number` | 開始値 (読み書き可能)。 |
| `counter.End  -> number` | 終了値 (読み書き可能)。 |
| `counter.Interval  -> number` | 値 1 単位あたりの秒数 (読み書き可能)。 |
| `counter:Start()  -> nil` | 値を Begin にリセットして進行を開始します。 |
| `counter:Resume()  -> nil` | 値をリセットせずに進行を開始します。 |
| `counter:Stop()  -> nil` | 進行を停止します。 |
| `counter:Pause()  -> nil` | Stop と同じです。 |
| `counter:Reset()  -> nil` | 進行中かどうかを変えずに値を Begin に戻します。 |
| `counter:Tick()  -> nil` | 値を 1 フレーム分進め、必要に応じてリスナーと ended 関数を呼びます。 |
| `counter:SetLoop(loop)  -> nil` | 値が終了値に達したら Begin に戻ります。バウンスを無効にします。 |
| `counter:SetBounce(bounce)  -> nil` | 値がどちらかの端に達したら方向を反転します。ループを無効にします。 |
| `counter:GetLoop()  -> boolean` | ループが有効かどうか。 |
| `counter:GetBounce()  -> boolean` | バウンスが有効かどうか。 |
| `counter:SetEasing(type, function)  -> nil` | 報告される値にイージングカーブを適用します。type は IN、OUT、INOUT、OUTIN、function は LINEAR、SINE、QUAD、CUBIC、QUART、QUINT、EXPO、CIRC、ELASTIC、BACK、BOUNCE のいずれかです (大文字小文字を区別しません。不明な名前はカウンターが無視します)。 |
| `counter:ClearEasing()  -> nil` | イージングを取り除きます。 |
| `counter:Listen(listener)  -> nil` | tick ごとに現在の値で呼ばれる関数を登録します。 |
| `counter:ClearListeners()  -> nil` | すべてのリスナーを取り除きます。 |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

画面全体の 2D カメラです。描画されたフレーム全体をパン、ズーム、回転させ、減衰する画面の揺れを加えます。

<div class="callout warn">
GLOBALCAMERA グローバルとして登録されています。TJA の #CAMERA コマンドと同じ画面変換を駆動し、ブリットされた 3D シーンを含む描画されるすべてのものに影響します。オフセットは 1280x720 基準のピクセル、回転は度で、ズーム 1 は拡大縮小なしを意味します。基本の変換は自分で変更するまで保持されます。適用と揺れの進行のために毎フレーム Update(dt) を呼び、ステージを離れるときは次のステージに持ち越されないよう Reset() を呼んでください。3D シーン自身のカメラはシーンオブジェクトに設定します。
</div>

| メソッド | 説明 |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | 画面を (x, y) ピクセルだけパンします。 |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | X と Y で別々の倍率で画面をズームします。 |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | 画面を一様にズームします。 |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | 画面を中心の周りに回転させます。 |
| `GLOBALCAMERA:GetOffsetX()  -> number` | 基本の X オフセット。 |
| `GLOBALCAMERA:GetOffsetY()  -> number` | 基本の Y オフセット。 |
| `GLOBALCAMERA:GetZoomX()  -> number` | X のズーム倍率。 |
| `GLOBALCAMERA:GetZoomY()  -> number` | Y のズーム倍率。 |
| `GLOBALCAMERA:GetRotation()  -> number` | 基本の回転 (度)。 |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | 指定したピクセル振幅から指定した秒数かけて線形に減衰する揺れを開始します。任意で度単位の回転の揺れを加えます。seconds が 0 以下の呼び出しはカメラが無視します。新しい揺れは、その振幅が現在のものと同じか大きい場合にのみ現在の揺れを置き換えます。 |
| `GLOBALCAMERA.IsShaking  -> boolean` | 揺れがまだ減衰中の間 true。 |
| `GLOBALCAMERA:Update(dt)  -> nil` | 揺れを dt 秒 (0.25 に制限) 進め、基本の変換と揺れを画面に適用します。 |
| `GLOBALCAMERA:Reset()  -> nil` | カメラを中央に戻し、ズームと回転をリセットし、揺れを止めます。 |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## 楽曲の列挙

2 つのグローバル関数が楽曲スキャンの状態を報告します。afterSongEnum コールバックと組み合わせて使います。

| メソッド | 説明 |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | 楽曲の列挙処理が実行中の間 true。 |
| `IsSongsEnumDone()  -> boolean` | 楽曲のスキャンが完了すると true。スキャン開始前と実行中は false です。IsSongsEnumerating は未開始と完了のどちらの状態でも false なので、リストが準備できたことを知るにはこの関数を確認してください。 |

## 診断

### info

基本的なゲーム状態とモジュール自身のディレクトリを持つ読み取り専用オブジェクトです。

<div class="callout warn">
info グローバルとして登録され、モジュールごとに作成されます。各フィールドはアクセス時に値を算出します。online は読み取るたびにオペレーティングシステムに問い合わせます。
</div>

| メソッド | 説明 |
| --- | --- |
| `info.playerCount  -> number` | 設定されているプレイヤー数。 |
| `info.lang  -> string` | 現在の言語コード。 |
| `info.simplemode  -> boolean` | シンプルモードが有効なとき true。 |
| `info.p1IsBlue  -> boolean` | プレイヤー 1 が青側を使うとき true。 |
| `info.online  -> boolean` | 使用可能なネットワークインターフェースがあるとき true。 |
| `info.dir  -> string` | このモジュールのディレクトリ。 |

### fps

フレームのタイミングと高分解能クロックを持つ読み取り専用オブジェクトです。

| メソッド | 説明 |
| --- | --- |
| `fps.deltaTime  -> number` | 前のフレームからの経過秒数。 |
| `fps.fps  -> number` | 現在計測されている 1 秒あたりのフレーム数。 |
| `fps.ms  -> number` | ミリ秒単位の単調増加クロック。差分を取って Lua の区間を計測するのに使います。 |

### debugLog

| メソッド | 説明 |
| --- | --- |
| `debugLog(message)  -> nil` | Lua のログであることを示す接頭辞を付けて、文字列をエンジンのトレースログに書き込みます。 |

## その他のグローバル

エンジンはこれらのグローバルをすべてのモジュールに登録します。それぞれのページで説明しています。

| グローバル | ページ |
| --- | --- |
| `GetSaveFile(player)` | [プレイヤーとプロフィール](players.md)。ROActivity とバックグラウンドの中では読み取り専用のハンドルを返します。 |
| `RequestSongList(settings)`, `GenerateSongListSettings()` | [楽曲と譜面](songs.md)。 |
| `MODICONS` | [楽曲と譜面](songs.md)。 |
| `CONFIG`, `DATABASE`, `SHARED`, `STORAGE`, `JSONLOADER`, `INILOADER`, `SQL` | [データと永続化](data.md)。 |
| `TEXTURE`, `CANVAS`, `GRAPHICS`, `TEXT`, `VIDEO`, `COLOR`, `GRADIENT`, `SIZE` | [グラフィックとテキスト](graphics.md)。 |
| `SOUND`, `HITSOUNDSLIST` | [オーディオ](audio.md)。 |
| `INPUT` | [入力](input.md)。 |
| `NAMEPLATE`, `NAMEPLATESLIST`, `CHARACTER`, `CHARACTERLIST`, `PUCHICHARALIST`, `PLAYSTATE`, `THEME`, `LANG` | [プレイヤーとプロフィール](players.md)。 |
| `VECTOR`, `VECTOR2`, `VECTOR3`, `VECTOR4`, `MATRIX`, `MATRIX2`, `MATRIX3`, `MATRIX4`, `QUATERNION` | [数学](math.md)。 |
| `SONGMOUNT`, `REPLAY`, `DANBUILDER`, `VIRTUALSLOTS` | [楽曲と譜面](songs.md)。 |
| `NET` | [オンラインネットワーク](networking.md)。 |
| `SCENE3D`, `MODEL`, `PHYSICS`, `COLLIDERS`, `PATHFIND`, `HEIGHTMAP` | [3D エンジン: ラスタライザーの世界](3d.md), [3D エンジン: レイトレーサーの世界](3d-raytrace.md), [3D エンジン: 物理](3d-physics.md) <span class="badge-exp">実験的</span>。 |

<!-- getting-started.md -->

# モジュールの仕組み

OpenTaiko 0.6.1 では、スキンが Lua で画面を追加したり置き換えたりできます。スキンの Modules フォルダにはモジュールごとに 1 つのフォルダがあり、各モジュールは、決まった名前のグローバルコールバック関数を定義する Script.lua を持ちます。ゲームはそれぞれの Script.lua をサンドボックス化された独立の Lua ステートに読み込み、エンジンのグローバル (TEXTURE、SOUND、INPUT、CONFIG など、[API リファレンス](api/README.md)に記載されているもの) を登録し、適切なタイミングでコールバックを呼び出します。正確なシグネチャは[モジュールとライフサイクル](api/activities.md)に一覧があります。

## 始める前に

- OpenTaiko 0.6.1 と、Modules ディレクトリを持つスキンフォルダ。同梱スキンは System/Open-World Memories です。
- テキストエディタと、基本的な Lua の知識 (関数、テーブル、require)。
- System/Open-World Memories/Modules/Stages と Global/Modules/Stages にある同梱ステージ。Global/Modules/Stages/demo_overall の小さなデモはコールバックの形を示し、大きなステージは実際の画面がどう構成されているかを示します。

## モジュールの置き場所

モジュールの種類ごとに Modules の下に専用のフォルダがあり、各モジュールは、フォルダ名がモジュールの id となる 1 つのフォルダです。

```
Modules/
  Stages/        <name>/Script.lua   full screens
  Activities/    <name>/Script.lua   sub-screens driven by a stage
  ROActivities/  <name>/Script.lua   read-only sub-screens and overlays
  Transitions/   <name>/Script.lua   fades between stages (loaded first)
  Lib/           shared .lua files reachable through require; not scanned as modules
```

エントリファイルは常に Script.lua です。TEXTURE、SOUND、VIDEO などのローダーに渡すアセットのパスはモジュールフォルダからの相対パスです。同梱モジュールは慣例として Textures、Sounds、Videos、Databases サブフォルダに置き、翻訳は lang フォルダに置いています。

2 種類のスクリプトは別の場所にあります。

- バックグラウンド (画面背景、ゲームプレイのレイヤー、モブ、クリアアニメーション、くす玉) は、スキンの Graphics フォルダの下、装飾する画面のディレクトリにある Script.lua です。[モジュールとライフサイクル](api/activities.md)のバックグラウンドの節を参照してください。
- キャラクターは Global/Characters の下のフォルダです。キャラクターフォルダは独自の Script.lua を持つことができ、持たない場合はゲーム組み込みのキャラクタースクリプトが使われます。[キャラクターの追加](guides/characters.md)を参照してください。

## Script.lua はグローバル関数を定義する

モジュールの Script.lua は決まった名前のトップレベルのグローバル関数を定義し、ゲームはそれぞれをグローバルとして読み取ります。ローカルテーブルに包んで返した関数をゲームは見つけられず、名前を間違えた関数 (onStart の代わりに OnStart など) は呼びません。未定義のコールバックは何もしない扱いになり、何も報告されないためです。ファイル内のそれ以外はすべてローカルにでき、モジュールを require で読み込む複数のファイルに分割できます。

これがコピーすべき最小の形です。

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- once when the skin loads: load assets here
    text = TEXT:Create(16)
end

function activate()         -- each time the stage is entered
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- every frame: input and state changes
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- every frame: drawing only
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- when the stage is left: stop sounds, close databases
end

function onDestroy()        -- before the skin is unloaded: dispose what you created
    if textTex ~= nil then textTex:Dispose() end
end
```

## ステージのライフサイクル

- onStart(): ゲームはスキンの読み込み後に 1 回、そしてスキンの再読み込みのたびに、ステージが表示中かどうかにかかわらずこれを呼びます。テクスチャ、サウンド、動画はここで読み込みます。コルーチンとして実行されるため、時間のかかる読み込みは LOADING ヘルパーを使ってローディングバーの裏で複数フレームに分散できます。
- activate(): ゲームはステージに入るたびにこれを呼びます。訪問ごとの状態のリセット、音楽の開始、データベースのオープンはここで行います。これもコルーチンとして実行され、LOADING を使えます。ゲームはキャラクターとぷちキャラのリスト (CHARACTERLIST、PUCHICHARALIST) を activate の実行直前に更新するので、ここで読み取ってください。onStart はその更新より前に実行されます。
- update(timestamp): ゲームは毎フレーム draw の前にこれを呼び、ミリ秒単位のゲームクロックを渡します。入力の処理と状態の変更はここで行います。ステージが Exit を呼んだ後、ゲームは update を呼ぶのをやめ、draw はフェードアウトの間も呼び続けます。
- draw(): ゲームは毎フレームこれを呼びます。描画のみを行い、フレームごとのアロケーションは少なく保ってください。
- deactivate(): ゲームはステージを離れるときにこれを呼びます。demo_overall のデータベースデモはここでデータベースを破棄し、最初のデモは音楽と動画を停止します。
- afterSongEnum(): ゲームは起動時とソフト/ハードリロード後を含め、楽曲の列挙が完了するたびに、ステージがアクティブでなくてもこれを呼びます。モジュールが楽曲リストに依存する場合に使います。
- onDestroy(): ゲームはスキンをアンロードする前にこれを呼びます。demo_overall の最初のデモはここでテクスチャ、動画、テキストテクスチャ、サウンドを破棄します。
- reloadLanguage(lang): ゲームは言語が変更されたときにこれを呼びます (下のローカライズの節を参照)。

スキンが読み込まれると、ゲームはモジュールを種類ごとに、Transitions、次に Stages、Activities、ROActivities の順に作成します。種類の中では、どの onStart を呼ぶよりも先にすべての Script.lua を実行します。したがって、ステージの onStart の実行中はアクティビティと ROActivity はまだ存在しません。activate で参照してください。

他の種類はこの一連のコールバックの変種を使います。アクティビティと ROActivity は同じコールバックを持ちますが、activate、deactivate、draw、update はホストするステージが呼び出し、その戻り値を受け取ります。バックグラウンドは activate(state)、update(timestamp, state)、draw(state) で状態オブジェクトを受け取り、clearIn、playEndAnime、kusuBroke などのイベントフックを定義できます。トランジションは fadeOut(t)、loading(progress, elapsed)、fadeIn(t) を定義します。キャラクターは独自のアニメーションと音声のセットを定義します。[モジュールとライフサイクル](api/activities.md)にすべての一覧があります。

## モジュールの種類の選び方

- ステージ (Modules/Stages): ゲームが切り替える完全な画面。フレームを所有し、入力を処理し、Exit を呼んで離れます。それ自体が 1 つの画面であるものに使います。
- アクティビティ (Modules/Activities): ダイアログなど、ステージが内側から使うサブ画面。ACTIVITY:GetActivity(name) で参照するシングルトンで、ホストするステージがその Activate、Update、Draw、Deactivate を呼びます。ゲームの状態を書き換える可能性がある共有部品に使います。
- ROActivity (Modules/ROActivities): アクティビティの読み取り専用の形で、ROACTIVITY:GetROActivity(name) で参照します。読み取り専用の CONFIG、DATABASE、GetSaveFile を受け取り、ACTIVITY グローバルを持ちません。状態を読むだけの部品に使います。再利用可能な UI の大半はこれに当てはまります。エンジンは自身のオーバーレイのいくつかを決まった名前の ROActivity としてホストしています (nameplate、modal、modicons、danplate、popup_menu、config_ui、song_enum, cutscene)。スキンは、その名前のフォルダを同梱し、エンジンが呼ぶコールバックを維持することでこれらを置き換えます。
- バックグラウンド: Graphics の下の Script.lua で、エンジンの画面の背後または手前に描画します。バックグラウンドは ROActivity と同じ読み取り専用のグローバルを受け取ります。
- トランジション (Modules/Transitions): ゲームがステージ間で再生するフェードアウト、ローディング、フェードイン。ステージは Exit の第 3 引数で名前を指定して選びます。ステージが名前を指定しないか名前が存在しない場合、ゲームは default という名前のものにフォールバックし、ゲームプレイに入るときは song_loading という名前のものを再生します。
- キャラクター: [キャラクターの追加](guides/characters.md)を参照してください。

## Exit でステージを離れる

Exit グローバルを持つのはステージだけです。最大 3 つの引数を取り、どの位置にも nil を受け付けます。引数は、遷移先 ("title"、"play"、"stage"、"legacy"。nil は "title")、遷移先が "stage" のときの目的ステージの名前 ("legacy" のときはレガシーキー)、そしてトランジションモジュールの名前です。同梱ステージは、そのフレーム内で他に何も実行されないよう、update の中で `return Exit(...)` と書いています。

```lua
-- inside a stage's update()
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "mystage") -- jump to Modules/Stages/mystage
end
-- ...
return Exit("title", nil)           -- back to the title screen
```

## サンドボックス

すべての Script.lua は制限された Lua ステートで実行されます。

- os には time、date、difftime だけが残ります。サンドボックスは io、debug、loadfile、dofile を取り除き、import は何もしません。
- package はカスタムローダーに縮小されます。package.path と package.cpath は空で、サンドボックスが標準のサーチャーを置き換えるため、以下のパスだけが検索対象です。
- require はまずモジュール自身のフォルダを探し、次にスキンの Modules/Lib フォルダ、次にゲームフォルダの Global/Modules/Lib を探して、最初に見つかったファイルを読み込みます。Global/Modules のモジュールは、スキンの Modules/Lib より先に Global/Modules/Lib を探します。同じ名前のモジュールファイルと Lib ファイルがあればモジュールファイルが優先されます。名前のドットはパス区切りになるため、require("DBControllers.dbScores") と require("DBControllers/dbScores") はどちらも DBControllers/dbScores.lua を読み込みます。検索は大文字と小文字を区別せず、非 ASCII のパスも動作します。
- Lib ファイルは、それを require したモジュールの Lua ステートで実行されるため、その中の require はそのモジュールの順序に従います。ゲームは Global/Modules/Lib に Lib ファイルのライブラリを同梱しており、require("Core.Easing") や require("UI.Pick") のような名前空間付きの名前で読み込めます。[提供モジュール](modules/README.md)で説明しています。

```lua
-- from intro_nokon/Script.lua
local DBScores  = require("DBControllers/dbScores")  -- the module's own subfolder
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- the module folder
local Dialogue  = require("nokon_dialogue")           -- the module folder
```

## 読み取り専用モジュール

ゲームは ROActivity とバックグラウンドを、Script.lua が実行される前に制限されたグローバルで作成します。CONFIG は読み取り専用のビュー、GetSaveFile(player) は読み取り専用のセーブファイルを返し、DATABASE は読み取り専用のストアを開き、ACTIVITY は nil です (ROACTIVITY を使ってください)。これらを通じた書き込みはエラー通知をログに出力し、何もせず、Lua エラーも発生させません。設定、セーブデータ、データベースを変更する必要があるモジュールは、アクティビティかステージでなければなりません。

## lang/ によるローカライズ

モジュールは、スキンの Locales フォルダではなく提供モジュール [Locale.i18n](modules/locale.md) で自身の文字列を翻訳するため、翻訳はモジュールと一緒に移動します。テキストは Script.lua の隣の lang/<code>/<name>.json に、機能ごとに 1 ファイル、安定した id をキーとして置きます。lang/en が英語を持ち、他のすべての言語のフォールバックになります。

ライブラリの主な関数は次のとおりです。

- texts(name) は、モジュール自身のフォルダにある lang/<code>/<name>.json のテキストを返します。Lib ファイルは 2 番目の引数として自身のフォルダを渡します ([提供モジュール](modules/README.md)を参照)。
- detect() はグローバルの LANG を通じて現在のゲーム言語を読み取り、以降のすべての参照がその言語を使います。呼び出すまでは、参照は英語を使います。
- texts:tr(id) は id のテキストを返し、どの言語にもない場合は id そのものを返します。texts:trf(id, ...) はそのテキストを string.format のパターンとして使います。

activate で detect() を呼び、その後 texts:tr と texts:trf でテキストを作ります。activate はモジュールに入るたびに実行されるため、プレイヤーが設定で変更した言語は次の訪問時に反映され、モジュールに他のフックは不要です。各言語のファイルは英語のファイルと同じキーを持ち、翻訳は %s のようなプレースホルダをそのまま保持しなければなりません。

```
Modules/Stages/mystage/lang/en/quiz.json   { "speaker": "Nokon", "start": "Alright, quiz time!" }
Modules/Stages/mystage/lang/ja/quiz.json   { "speaker": "ノコン", "start": "さあ、クイズの時間である！" }
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("Locale.i18n")
local T = I18N.texts("quiz")
local title

function activate()
    I18N.detect()
    title = T:tr("start")
end
```

intro_nokon など、同梱スキンの一部の古いステージは、代わりにライブラリの最初の形式を使います。各英語文字列からその訳へのテーブルを返す lang/ja.lua を、I18N.tr(s) で読む形式です。[Locale.i18n](modules/locale.md) は両方を説明しています。

ゲームは言語が変更されると、すべての読み込み済みモジュールでグローバルの reloadLanguage(lang) も呼びます。必要なのは、言語セレクターのある画面のように、言語変更中も画面に表示されたままのモジュールだけです。そこでは detect() をもう一度呼び、事前描画したテキストを作り直してください。

## 注意点

- モジュールが作成したテクスチャ、サウンド、動画、テキストオブジェクトは、ゲームがそのモジュールを破棄するときに解放するため、スキンの再読み込みでリークすることはありません。データベースなど訪問ごとに開くリソースは、demo_overall のデータベースデモのように deactivate で破棄し、作成したものは その最初のデモのように onDestroy で破棄してください。
- GetText は、テキストオブジェクト上で相異なる文字列ごとに 1 つのテクスチャをキャッシュします。毎フレーム変わる文字列は毎フレームテクスチャを追加し、ゲームは徐々に遅くなります。変化する値はグリフレンダラー (TEXT:CreateGlyphCached) で描画するか、値が変わるまで 1 つのテクスチャを保持してください。
- LOADING はコルーチンとして実行されるコールバック、つまり任意のモジュールの onStart とステージの activate でのみ動作します。アクティビティの activate や、update、draw から LOADING:Tick を呼ぶと Lua エラーになります。
- onStart と afterSongEnum はモジュールが画面外にある間に実行されます。ステージが表示されていなくても動作するように書いてください。

<!-- api/data.md -->

# データと永続化

再起動後も残るデータの保存、SQLite ファイルへの問い合わせ、モジュールフォルダからのファイル・JSON・INI の読み込み、モジュール間でのリソース共有、ゲーム設定の読み取りと変更。

DATABASE、SQL、STORAGE、JSONLOADER、INILOADER、SHARED に渡す相対パスは、実行中のモジュールのディレクトリ (その `Script.lua` があるフォルダ) を基準に解決されます。

このページのいくつかのメソッドは .NET のコレクションを返します。これらは Lua のテーブルとは振る舞いが異なります。

- 配列 (`string[]`、`int[]`、`double[]`) はインデックス 0 から始まり、`.Length` を公開します。
- 辞書 (解析された JSON、SQL の行、言語マップ) は `d["key"]` (JSON から解析された配列では `d[1]`) を取り、`d:GetEnumerator()` で列挙します。`pairs` と `#` はこれらには使えません。

```lua
local files = STORAGE:GetFiles("maps", "*.json")
for i = 0, files.Length - 1 do
    print(files[i])
end

local e = dict:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end
```

## キー/値データベース

### DATABASE

再起動をまたいで文字列値を永続化する LMDB のキー/値ストアを、モジュールフォルダ内またはゲーム全体のデータフォルダに開きます。

<div class="callout warn">
Read と Write はそれぞれ自前の LMDB 環境を開いて閉じるため、各呼び出しはコストが高い操作です。Lua 側で値をキャッシュし、書き込むときにキャッシュを更新してください。読み取り専用モジュール (ROActivity とバックグラウンド) では、DATABASE は Write がエラーをログに出力して何もしないストアを返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | モジュールディレクトリからの相対パスにストアを開きます (必要なら作成します)。 |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | ゲームフォルダの `Global/ApplicationData/LMDB/` の下に、すべてのモジュールで共有されるストアを開きます (必要なら作成します)。 |

### データベースハンドル

DATABASE が返す、文字列キーを文字列値に対応付けるキー/値ストアです。

<div class="callout warn">
値は文字列のみです。数値や真偽値は自分で変換してください (例えば tostring と tonumber で)。
</div>

| メソッド | 説明 |
| --- | --- |
| `database:Write(key, value)  -> nil` | キーの下に文字列を保存してコミットします。読み取り専用モジュールではエラーをログに出力して何もしません。 |
| `database:Read(key)  -> string` | キーの下に保存された文字列を返します。キーがないか読み取りに失敗した場合は nil。 |
| `database:Dispose()  -> nil` | 何もしません。ハンドルは開いたリソースを保持しません。 |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## SQL データベース

### SQL

SQL 文を実行するために、モジュールディレクトリ内の SQLite データベースファイルを開きます。

| メソッド | 説明 |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | モジュールディレクトリからの相対パスにある SQLite データベースを開きます。 |

### SQL ハンドル

SQL:OpenSQLDatabase が返す SQLite 接続です。

<div class="callout warn">
Query は 1..n をキーとする辞書を返し、各行は列名をキーとする辞書です (ページ冒頭の .NET コレクションに関する注記を参照)。失敗した文はエラーをログに出力し、空の結果を返します。Query は文のテキストをパラメータバインディングなしでそのまま SQLite に渡すため、文に埋め込む値はすべてエスケープしてください。
</div>

| メソッド | 説明 |
| --- | --- |
| `sql:Query(query)  -> rows` | SQL テキストを実行し、結果の行を返します。 |

```lua
local db

function activate()
    db = SQL:OpenSQLDatabase("Databases/Items.db3")
    local rows = db:Query("SELECT * FROM itempool WHERE Slot = 'regular'")
    for i = 1, rows.Count do
        print(rows[i]["Code"])
    end
end
```

## ファイル、JSON、INI

### STORAGE

モジュールディレクトリを起点とするファイルアクセスと、オンラインロビーコードを共有するためのヘルパーです。

<div class="callout warn">
WriteText はモジュールフォルダ内にのみ書き込みます。絶対パスと、フォルダの外に解決されるパスを拒否します。ReadText は絶対パスも受け付けます。ロビーコードのヘルパーは実行ファイルの隣の共有 `Global/Lobbycodes/` フォルダを使います。読み取り専用モジュールも STORAGE のすべてのメソッドを使えます。
</div>

| メソッド | 説明 |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | モジュールフォルダのサブディレクトリにある、`"*.png"` のような検索パターンに一致するファイルを列挙します。要素はサブディレクトリを含む、モジュールフォルダからの相対パスです。ディレクトリは存在している必要があります。 |
| `STORAGE:FileExists(path)  -> bool` | モジュールフォルダからの相対パスにファイルが存在すれば true。 |
| `STORAGE:DirectoryExists(path)  -> bool` | モジュールフォルダからの相対パスにディレクトリが存在すれば true。 |
| `STORAGE:WriteText(name, contents)  -> bool` | モジュールフォルダ下のファイルにテキストを書き込み、サブディレクトリを作成します。絶対パスやフォルダ外に出るパス、または失敗時は false を返します。 |
| `STORAGE:ReadText(name)  -> string` | ファイル (モジュールフォルダからの相対パス、または絶対パス) の生のテキストを返します。存在しないか読めない場合は nil。 |
| `STORAGE:GetFullPath(name)  -> string` | モジュールフォルダ下のファイルの絶対パスを返します。空または絶対パスの入力には nil。 |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | `Global/Lobbycodes/` にファイルを書き込みます。空、絶対パス、`..` を含む名前、または失敗時は false を返します。 |
| `STORAGE:RevealLobbyCodes()  -> bool` | OS のファイルブラウザで `Global/Lobbycodes/` を開きます。 |
| `STORAGE:RevealInExplorer(name)  -> bool` | OS のファイルブラウザで、指定したモジュールファイルを選択した状態で開きます (Windows と macOS)。それ以外ではそのフォルダを開きます。 |

### JSONLOADER

モジュールディレクトリの JSON ファイルと文字列を解析します。

<div class="callout warn">
解析された値は .NET の辞書です。オブジェクトはメンバー名、配列は 1..n をキーとします。存在しないキーを直接インデックスする (`d["x"]`) とエラーになるため、失敗しうる検索には JsonGet を使ってください。数値は整数または double になります。文字列、真偽値、null は対応する Lua の値になります。LoadJson は JsonNode のツリーを返します。`node["member"]` でインデックスし、末端は ExtractNumber / ExtractText で変換します。
</div>

| メソッド | 説明 |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | モジュールディレクトリからの相対パスの JSON ファイルを JsonNode ツリーに解析します。ファイルがなければエラーになります。 |
| `JSONLOADER:ExtractNumber(value)  -> number` | JsonNode の末端を数値に変換します。nil または数値でない値には 0 を返します。 |
| `JSONLOADER:ExtractText(value)  -> string` | JsonNode の末端を文字列に変換します。nil には nil を返します。 |
| `JSONLOADER:JsonParseFile(name)  -> dict` | ルートがオブジェクトの JSON ファイルを解析します (ファイルが空なら空の辞書)。ファイルがないかルートがオブジェクトでなければエラーになります。 |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | ルートがオブジェクトまたは配列の JSON ファイル (相対パスまたは絶対パス) を解析します。ファイルがないか空なら nil を返します。 |
| `JSONLOADER:JsonParseString(json)  -> dict` | ルートがオブジェクトの JSON 文字列を解析します (空なら空の辞書)。ルートがオブジェクトでなければエラーになります。 |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | ルートがオブジェクトまたは配列の JSON 文字列を解析します。空または不正な入力には nil を返します。 |
| `JSONLOADER:JsonGet(dict, key)  -> value` | オブジェクトの文字列キー、または配列の整数キーを検索します。存在しない場合や dict が解析された値でない場合は nil を返します。 |
| `JSONLOADER:JsonCount(dict)  -> int` | 解析されたオブジェクトまたは配列のメンバー数。それ以外には 0。 |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

モジュールディレクトリからフラットな `key=value` ファイルを読み込みます。

<div class="callout warn">
ローダーは各行を最初の `=` で分割し、`=` のない行をスキップし、重複するキーには前の値を上書きさせます。セクション、コメント、引用はありません。ファイルがない場合は空のハンドルになります。
</div>

| メソッド | 説明 |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | モジュールディレクトリからの相対パスの `key=value` ファイルを読み込みます。 |

### INI ハンドル

INILOADER:LoadIni が返す、型付きのゲッターを持つ解析済み INI ファイルです。

<div class="callout warn">
ゲッターはキーがない場合に指定された既定値を返します。キーはあるがその値の解析に失敗する場合、数値のゲッターは 0 を返します。配列のゲッターはカンマで分割し、キーがなければ空の配列を返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | 値が整数 1 として解析できれば true。キーがなければ default。 |
| `ini:GetInt(key, default)  -> int` | 値を整数として返します。 |
| `ini:GetDouble(key, default)  -> number` | 値を double として返します。 |
| `ini:GetString(key, default)  -> string` | 生の文字列値。 |
| `ini:GetStringArray(key)  -> string[]` | 値をカンマで分割したもの。 |
| `ini:GetIntArray(key)  -> int[]` | 値をカンマで分割し、各要素を整数として解析したもの (解析できなければ 0)。 |
| `ini:GetDoubleArray(key)  -> double[]` | 値をカンマで分割し、各要素を double として解析したもの (解析できなければ 0)。 |

## 共有リソースと設定

### SHARED

ステージの切り替えをまたいで残る、ゲーム全体のテクスチャ、サウンド、文字列のストアです。一度 (例えば起動時に) 読み込んだリソースをすべてのモジュールが使えます。

<div class="callout warn">
Set* メソッドはバックグラウンドスレッドで読み込み、レンダースレッドでリソースを差し替えます。任意の onCreate コールバックは、新しいハンドルが配置された時点でそれを受け取ります。同じキーに対する新しい Set* は、進行中の読み込みを破棄します。差し替えは前のリソースを破棄するため、再読み込み前に取得したハンドルは無効になります。Get* で取得し直してください。UsingAbsolutePath 系は完全なパスを取ります。テクスチャハンドルは「グラフィックとテキスト」、サウンドハンドルは「オーディオ」を参照してください。
</div>

| メソッド | 説明 |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | キーの下に文字列を保存します。 |
| `SHARED:GetSharedString(key)  -> string` | キーの下に保存された文字列を返します。なければ空文字列。 |
| `SHARED:GetSharedTexture(key)  -> texture` | キーの共有テクスチャを返します。設定されていなければ空のテクスチャ。 |
| `SHARED:GetSharedSound(key)  -> sound` | キーの共有サウンドを返します。設定されていなければ空のサウンド。 |
| `SHARED:ClearSharedTexture(key)  -> nil` | キーの下に保存されたテクスチャを破棄し、空のものに置き換えます。 |
| `SHARED:ClearSharedSound(key)  -> nil` | キーの下に保存されたサウンドを破棄し、空のものに置き換えます。 |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | モジュールからの相対パスからテクスチャをストアに読み込みます。 |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | 同上。オプションテーブル付き。`{ maxSize = N }` はデコードされたテクスチャの長辺を N ピクセルに制限します。 |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | 絶対パスからテクスチャを読み込みます。 |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | オプションテーブル付きで絶対パスからテクスチャを読み込みます。 |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | モジュールからの相対パスから効果音を読み込みます。 |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | モジュールからの相対パスから BGM (楽曲再生音量グループ) を読み込みます。 |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | モジュールからの相対パスからボイスクリップを読み込みます。 |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | モジュールからの相対パスから楽曲プレビュークリップを読み込みます。 |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | 絶対パスから効果音を読み込みます。 |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | 絶対パスから BGM を読み込みます。 |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | 絶対パスからボイスクリップを読み込みます。 |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | 絶対パスから楽曲プレビュークリップを読み込みます。 |

```lua
-- in the boot stage
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- in any later module
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

ゲーム設定を読み取り、変更します: プレイヤー数、モード、スコア計算、プレイヤーごとのゲームプレイ Mod、音量。

<div class="callout warn">
プロパティはドット構文 (`CONFIG.PlayerCount`)、メソッドはコロン構文を使います。プレイヤーインデックスは 0 始まり (0 から 4) です。セッターは範囲外のインデックスを無視し、ゲッターはその場合に既定値を返します。読み取り専用モジュール (ROActivity とバックグラウンド) では、すべてのセッターがエラーをログに出力して何もしません。変更はメモリ上に直ちに適用され、ゲームは正常に終了するときに Config.ini を書き込みます。
</div>

| メソッド | 説明 |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | 今回の起動でゲームが設定を新規作成したとき true。 |
| `CONFIG.Language  -> string (read-only)` | Config.ini に保存された言語 id。 |
| `CONFIG.PlayerCount  -> int` | アクティブなプレイヤー数。セッターは 1..5 の範囲外の値を無視します。 |
| `CONFIG.IsAIBattleMode  -> bool` | AI バトルモードが有効かどうか。 |
| `CONFIG.AILevel  -> int` | AI の難易度レベル。書き込み時に 1..10 に制限されます。 |
| `CONFIG.IsTrainingMode  -> bool` | トレーニングモードが有効かどうか。 |
| `CONFIG.UseModernScoringMethod  -> bool` | ゲームが新方式 (真打) のスコア計算を使うかどうか。 |
| `CONFIG.UsedLegacyScoringMethod  -> int` | 旧方式のスコア計算の世代 (`CONFIG.LEGACY_SCORING` を参照)。書き込み時に 0..3 に制限されます。 |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | ゲームが楽曲のアンロック条件を無視するかどうか。 |
| `CONFIG.SongSpeed  -> int` | 曲速度 (倍率の 20 分の 1 単位): 20 が 1.0x。書き込み時に 2..200 (0.1x から 10x) に制限されます。 |
| `CONFIG.MasterVolume  -> int` | マスター音量。書き込み時に 0..100 に制限されます。 |
| `CONFIG.SoundEffectVolume  -> int` | 効果音の音量。書き込み時に 0..100 に制限されます。 |
| `CONFIG.VoiceVolume  -> int` | ボイスの音量。書き込み時に 0..100 に制限されます。 |
| `CONFIG.SongVolume  -> int` | 楽曲再生の音量。書き込み時に 0..100 に制限されます。 |
| `CONFIG.PreviewVolume  -> int` | 楽曲プレビューの音量。書き込み時に 0..100 に制限されます。 |
| `CONFIG:GetGameType(player)  -> int` | プレイヤーのゲームタイプ (`CONFIG.GAMETYPE` を参照)。範囲外のインデックスには Taiko。 |
| `CONFIG:SetGameType(player, gameType)  -> nil` | プレイヤーのゲームタイプを設定し、未定義の値は無視します。 |
| `CONFIG:GetDefaultCourse(player)  -> int` | 既定の難易度 (`CONFIG.DEFAULT_COURSE` を参照)。範囲外のインデックスには Normal。player 引数はありますが、1 つのグローバルな設定がすべてのプレイヤーに適用されます。 |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | グローバルな既定の難易度を設定します。Easy から Extra Extreme の 1 つ先 (Extra/Extra-Extra の統合表示) までに制限されます。 |
| `CONFIG:GetScrollSpeed(player)  -> int` | プレイヤーのスクロール速度の値: 9 が 1.0x で、1 ステップは 0.1x です (`CONFIG.SCROLLSPEED` を参照)。範囲外のインデックスには 9。 |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | プレイヤーのスクロール速度の値を設定します。0..99 に制限されます。 |
| `CONFIG:GetTimingZone(player)  -> int` | プレイヤーの判定幅: 0 Loose、1 Lenient、2 Normal、3 Strict、4 Rigorous。範囲外のインデックスには 2。 |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | プレイヤーの判定幅を設定します。0..4 に制限されます。 |
| `CONFIG:GetAutoStatus(player)  -> bool` | プレイヤーがオートプレイ中またはリプレイ観賞中のとき true。 |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | プレイヤーのオートプレイを有効または無効にします。 |
| `CONFIG:GetRandomMod(player)  -> int` | プレイヤーのランダム Mod (`CONFIG.RANDOM` を参照)。範囲外のインデックスには Off。 |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | プレイヤーのランダム Mod を設定し、未定義の値は無視します。 |
| `CONFIG:GetFunMod(player)  -> int` | プレイヤーの Fun Mod (`CONFIG.FUN` を参照)。範囲外のインデックスには None。 |
| `CONFIG:SetFunMod(player, mod)  -> nil` | プレイヤーの Fun Mod を設定し、未定義の値は無視します。 |
| `CONFIG:GetStealthMod(player)  -> int` | プレイヤーのステルス Mod (`CONFIG.STEALTH` を参照)。範囲外のインデックスには Off。 |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | プレイヤーのステルス Mod を設定し、未定義の値は無視します。 |
| `CONFIG:GetJusticeMod(player)  -> int` | プレイヤーの判定 Mod: 0 オフ、1 Just (可を不可として扱う)、2 Safe (不可を可として扱う)。 |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | プレイヤーの判定 Mod を設定します。0..2 に制限されます。 |
| `CONFIG:GetModFlags(player)  -> integer` | スクロール速度、ステルス、ランダム、曲速度、判定幅、判定、Fun の各 Mod を 1 つの 64 ビット値にパックします (各 1 バイト)。 |
| `CONFIG:SetModFlags(player, flags)  -> nil` | GetModFlags が生成した値をプレイヤーに適用し直します (曲速度はグローバルです)。 |

CONFIG の定数テーブルは上記の整数値に名前を与えます。`SONGSPEED` と `SCROLLSPEED` は保存値と倍率の相互変換も行います。

| メンバー | 説明 |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20。1.0x の保存値。 |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | 保存された曲速度を倍率に変換します (value / 20)。 |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | 倍率を最も近い保存値の曲速度に変換します。 |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9。1.0x の保存値。 |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | 保存されたスクロール速度を倍率に変換します ((value + 1) / 10)。 |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | 倍率を最も近い保存値のスクロール速度に変換します。 |
| `CONFIG.GAMETYPE` | `Taiko`、`Konga`。 |
| `CONFIG.DEFAULT_COURSE` | `Easy`、`Normal`、`Hard`、`Oni`、`Edit`。 |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`、`Gen1_2`、`Gen2`、`Gen3`。 |
| `CONFIG.RANDOM` | `Off`、`Random`、`Mirror`、`SuperRandom`、`MirrorRandom`。 |
| `CONFIG.STEALTH` | `Off`、`Doron`、`Stealth`。 |
| `CONFIG.FUN` | `None`、`Avalanche`、`Minesweeper`、`DynamicBeat`、`Total`。 |
| `CONFIG.JUSTICE` | `None`、`Just`、`Safe`。 |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- mirrored chart
end
```

<!-- api/songs.md -->

# 楽曲と譜面

楽曲リストの要求、ノードと譜面の走査、スコアの読み取り、段位 (試験) コースの構築。

このページで使う規約:

- 難易度インデックスは 0 始まりです: 0 Easy、1 Normal、2 Hard、3 Extreme (`Oni`)、4 Extra Extreme (`Edit`)、5 Tower、6 Dan。
- プレイヤーとセーブファイルのインデックスは 0 始まりです (0 がプレイヤー 1)。
- ドットで書かれたメンバー (`node.Title`) はプロパティ、コロンで書かれたメンバー (`node:GetChart(3)`) はメソッドです。
- 一部のメンバーは C# のコレクションを返します。リストは `.Count` を持ち 0 始まりでインデックスします (`list[0]`)。配列は `.Length` を持ち、同じく 0 始まりでインデックスします。以下の各項目にどちらを返すかを記載しています。
- 楽曲リストは楽曲の列挙が完了した後に初めて完全になります。`afterSongEnum()` コールバック ([モジュールとライフサイクル](activities.md)を参照) から要求するか、先に `IsSongsEnumDone()` グローバルを確認してください。これは列挙が完了すると true を返します。

## 楽曲リストの要求

### RequestSongList

設定オブジェクトから移動可能な楽曲リストを構築するグローバル関数です。

<div class="callout warn">
通常のグローバル関数として利用できます。GenerateSongListSettings() で作成した設定オブジェクトを渡してください。呼び出しは、ゲームが列挙した楽曲から楽曲ツリーを 1 回だけ構築します。ハンドルは設定オブジェクトを参照で保持するため、フィールドを変更してハンドルの ReloadSongList() を呼べば再構築できます。
</div>

| メソッド | 説明 |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | 指定した楽曲リスト設定から楽曲リストハンドルを構築して返します。 |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

既定値を持つ楽曲リスト設定オブジェクトを作成するグローバル関数です。

| メソッド | 説明 |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | 既定のフィールド値を持つ新しい楽曲リスト設定オブジェクトを返します。 |

### 楽曲リスト設定

楽曲リストにどのノードを含めるか、移動がどう振る舞うかを制御する設定オブジェクトです。

<div class="callout warn">
以下のメンバーはすべて Lua が直接読み書きする公開フィールドです (settings.HideEmptyFolders = false)。例外は Lua テーブルを取る 2 つのセッターメソッドです。ExcludedGenreFolders と MandatoryDifficultyList は C# の配列なので、セッターメソッドを通じて設定してください。
</div>

| メソッド | 説明 |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | true のとき、リストはルートにランダムボックスを追加します。 |
| `settings.AppendSubRandomBoxes  (bool, default true)` | true のとき、リストは各フォルダの末尾にランダムボックスを追加します。 |
| `settings.SubBackBoxFrequency  (int, default 7)` | 各フォルダ内で、リストは先頭と N 個のエントリごとに戻るボックスを挿入します。0 で生成される戻るボックスを無効にします。 |
| `settings.ExcludedGenreFolders  (string array)` | リストが除外するジャンルフォルダ名。SetExcludedGenreFolders で設定します。 |
| `settings.RootGenreFolder  (string, default nil)` | 設定すると、ジャンルがこの名前に一致する最初のフォルダ (深さ優先) がリストのルートになります。nil のとき、ルートは最上位です。 |
| `settings.RootGenreFolderNode  (song node, default nil)` | RootGenreFolder のノード形式。設定されていれば文字列より優先され、同じジャンル名を持つフォルダを区別できます。 |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | 楽曲がリストに現れるために持っていなければならない難易度。nil は要件なし。SetMandatoryDifficultyList で設定します。 |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true は列挙したすべての難易度を要求し (AND)、false は少なくとも 1 つを要求します (OR)。 |
| `settings.HideEmptyFolders  (bool, default true)` | 表示される楽曲を含まないフォルダを再帰的に隠します。 |
| `settings.FlattenOpenedFolders  (bool, default true)` | true のとき、現在のページは開いたフォルダをその場で展開したツリー全体です (閉じたフォルダは 1 エントリとして数えます)。false のとき、ページはカーソルノードの兄弟だけを持ちます。 |
| `settings.ModuloPagination  (bool, default true)` | true のとき GetSongNodeAtOffset はページ内で循環し、false のときどちらかの端を超えると nil を返します。 |
| `settings.ModuloMovement  (bool, default true)` | true のとき Move はページ内で循環し、false のときどちらかの端で止まります。 |
| `settings.ExcludeHiddenSongs  (bool, default true)` | HiddenIndex が 3 (非表示) の楽曲を除外します。 |
| `settings.ExcludeLockedSongs  (bool, default false)` | true のとき、ページはロックされた楽曲を除外し、移動でそこに止まることがなくなります。 |
| `settings.IgnoreUnlockables  (bool, default false)` | true のとき、リストは ExcludeLockedSongs を無視し、GetRandomNodeInFolder はロックされた楽曲を返すことがあります。IsLocked などのノードのプロパティは実際の状態を報告し続けます。 |
| `settings:SetExcludedGenreFolders(table)  -> void` | ジャンル名文字列の Lua テーブルから ExcludedGenreFolders を設定します。 |
| `settings:SetMandatoryDifficultyList(table)  -> void` | 難易度インデックスの Lua テーブルから MandatoryDifficultyList を設定します。 |

### 楽曲リストハンドル

RequestSongList が返す、カーソル、フォルダ移動、検索を備えた移動可能な楽曲ツリーです。

<div class="callout warn">
検索メソッドは、楽曲ノードを受け取って真偽値を返す Lua 関数を取ります。複数のノードを返すメソッドは C# のリスト (.Count、0 始まりのインデックス) を返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `list:ReloadSongList()  -> void` | 現在の楽曲と設定からツリー全体を再構築し、カーソルを最初のノードに移動します。 |
| `list:GetRoot()  -> song node` | ツリーのルートノードを返します。 |
| `list:GetSelectedSongNode()  -> song node` | カーソル下のノードを返します。リストが空なら nil。 |
| `list:GetSongNodeAtOffset(offset)  -> song node` | 現在のページ内でカーソルから指定したオフセットにあるノードを返します。ModuloPagination に従って循環するか nil を返します。 |
| `list:Move(offset)  -> void` | 現在のページ内でカーソルを指定したオフセットだけ移動します。ModuloMovement に従って循環するか端で止まります。 |
| `list:OpenFolder()  -> bool` | カーソル下のフォルダを開き、カーソルをその最初の子に移動します。カーソルが閉じた空でないフォルダ上にない場合は false を返します。 |
| `list:CloseFolder()  -> bool` | カーソルを含むフォルダを閉じ、カーソルをそのフォルダに移動します。閉じるものがなければ false を返します。仮想フォルダを離れると OpenVirtualFolder が保存したカーソルが復元されます。 |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Lua テーブル `songs` (キー 1..n) の楽曲ノードを含む `title` という名前の一時フォルダを、生成された戻るボックスと末尾のランダムボックス付きで開き、カーソルをその中に移動します。`baseFolder` が仮想フォルダの親になります。テーブルに楽曲ノードがなければ false を返します。 |
| `list:GetSongByUniqueId(id)  -> song node` | 固有 id が一致する最初の楽曲を返します。なければ nil。 |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | `node` の兄弟 (それを含むページ) の中からランダムに楽曲を選びます。`recursive` (既定 true) のとき、兄弟フォルダ内の楽曲も選択の対象になります。IgnoreUnlockables が設定されていない限りロックされた楽曲はスキップします。`predicate` は任意です。該当がなければ nil を返します。 |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | ツリー内で述語が true を返すすべての楽曲ノードを返します。 |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | 述語が true を返す最初の楽曲ノードを返します。なければ nil。 |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | SearchSongsByPredicate と同様ですが、フォルダなど楽曲でないノードも判定します。 |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- has an Extreme chart
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## ノード、譜面、スコア

### 楽曲ノード

楽曲リストの 1 つのエントリ: 楽曲、フォルダ、戻るボックス、またはランダムボックスです。

<div class="callout warn">
楽曲リストハンドル、SONGMOUNT:ChosenSongNode()、DANBUILDER:GetSong() は楽曲ノードを返します。プロパティは読み取り専用です。メタデータのプロパティは楽曲でないノードでは nil を返します。リスト内の移動は楽曲リストハンドル (Move、OpenFolder、CloseFolder、検索メソッド) を通して行ってください。
</div>

| メソッド | 説明 |
| --- | --- |
| `node.NotNull  (bool)` | ノードが実際の楽曲リストエントリをラップしているとき true。 |
| `node.IsFolder  (bool)` | ノードがフォルダのとき true。 |
| `node.IsRandom  (bool)` | ノードがランダムボックスのとき true。 |
| `node.IsReturn  (bool)` | ノードが戻るボックスのとき true。 |
| `node.IsSong  (bool)` | ノードがプレイ可能な楽曲のとき true。 |
| `node.SongCount  (int)` | 直接の子である楽曲の数。 |
| `node.RecursiveSongCount  (int)` | サブフォルダを含む、このノード配下の楽曲数。 |
| `node.VisibleSongCount  (int)` | HiddenIndex が 3 でない、直接の子である楽曲の数。 |
| `node.RecursiveVisibleSongCount  (int)` | サブフォルダを含む、このノード配下の表示される楽曲数。 |
| `node.BoxType  (string)` | フォルダのボックススタイル文字列。なければ nil。 |
| `node.BgType  (string)` | 背景スタイル文字列。なければ nil。 |
| `node.BoxChara  (string)` | ボックスキャラクター文字列。なければ nil。 |
| `node.ForeColor  (color)` | ノードの前景色。なければ nil。 |
| `node.BackColor  (color)` | ノードの背景色。なければ nil。 |
| `node.BoxColor  (color)` | ノードのボックス色。なければ nil。 |
| `node.Title  (string)` | 表示タイトル。戻るボックスとランダムボックスは、親フォルダのタイトルから作られたローカライズ済みの「戻る」/「ランダム」テキストを返します。 |
| `node.Subtitle  (string)` | 楽曲のサブタイトル。なければ nil。 |
| `node.Genre  (string)` | ジャンル文字列。なければ nil。 |
| `node.UniqueId  (string)` | 楽曲の固有 id。なければ nil。 |
| `node.Maker  (string)` | MAKER フィールドを 1 つの文字列として。なければ nil。 |
| `node.Charters  (string array)` | MAKER フィールドをカンマで分割したもの。 |
| `node.Side  (int)` | SIDE の値: 0 通常、1 裏、2 両方。 |
| `node.Explicit  (bool)` | 楽曲が露骨な内容としてフラグ付けされているとき true。楽曲以外では nil。 |
| `node.HasVideo  (bool)` | 楽曲に背景動画があるとき true。楽曲以外では nil。 |
| `node.DemoStart  (int)` | プレビュー BGM のオフセット (ミリ秒)。 |
| `node.AudioPath  (string)` | 楽曲の BGM ファイルの絶対パス。なければ空文字列。 |
| `node.HasPreimage  (bool)` | 楽曲がプレビュー画像を宣言しているとき true。 |
| `node.PreimagePath  (string)` | プレビュー画像の絶対パス。先に HasPreimage を確認してください。プレビュー画像がない場合、これは楽曲フォルダのみです。 |
| `node:GetPreimage()  -> texture` | ディスクからプレビュー画像を読み込み、新しいテクスチャを返します。楽曲に画像がなければ nil。使い終わったらテクスチャを破棄してください。 |
| `node.ChartMd5  (string)` | 譜面ファイルの MD5 (大文字の 16 進)。なければ空文字列。インストールをまたいでも同じ値のままです。UniqueId は変わります。 |
| `node:GetChart(diff)  -> chart` | 指定した難易度インデックスの譜面を返します。楽曲にその譜面がなければ nil。 |
| `node:GetCustomCommand(key)  -> string` | グローバルスコープのカスタムコマンド (最初の COURSE より前に置かれたドット始まりのヘッダ。key はドットを含む。例: ".VAULT_NAME") の値を返します。なければ nil。 |
| `node:GetCustomCommands()  -> dictionary` | すべてのグローバルスコープのカスタムコマンドを C# の辞書オブジェクトとして返します。検索には GetCustomCommand を使ってください。 |
| `node.UnlockCondition  (unlock condition)` | アンロック条件オブジェクト (アンロック条件を参照)。 |
| `node.UnlockText  (string)` | 楽曲が定義していればカスタムのアンロックテキスト、そうでなければ生成された条件メッセージ。 |
| `node.IsLocked  (bool)` | この楽曲が現在ロックされているとき true。楽曲以外では常に false。 |
| `node.HiddenIndex  (int)` | アンロックシステムの表示状態: 0 表示、1 グレー表示、2 ぼかし表示、3 非表示 (楽曲以外では 0)。 |
| `node.Rarity  (string)` | レアリティラベル。アンロックエントリのない楽曲では "Common"、楽曲以外では "-"。 |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | プレイヤーごとに指定した難易度インデックス (既定 0) でこの楽曲をプレイ用に選択します。先頭の CONFIG.PlayerCount 個のインデックスだけを確認し、ノードが楽曲でないか、アクティブなプレイヤーの難易度が存在しないか範囲外の場合は false を返します。 |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Mount と同じですが、楽曲がロックされている場合はマウントせずに false を返します。 |

### 譜面

楽曲の 1 つの難易度: レベル、BPM、譜面作者、Tower と Dan のデータ、ベストスコア、カスタムコマンド。

<div class="callout warn">
楽曲ノードの GetChart(diff) が譜面を返します。プロパティは読み取り専用です。BPM、Life、TotalFloorCount、TowerType、DanTick は譜面に譜面情報がない場合 nil を返します。Difficulty と LevelIcon は列挙オブジェクトです。比較は DifficultyAsInt、IsPlus、IsMinus を通じて行ってください。
</div>

| メソッド | 説明 |
| --- | --- |
| `chart.NotNull  (bool)` | 譜面が実際の譜面データをラップしているとき true。 |
| `chart.Parent  (song node)` | この譜面が属する楽曲ノード。 |
| `chart.Difficulty  (enum)` | 列挙オブジェクトとしての難易度。 |
| `chart.DifficultyAsInt  (int)` | 難易度インデックス。 |
| `chart.Level  (int)` | 星の数によるレベル。 |
| `chart.LevelDecimal  (number)` | 小数部を含むレベル (例: 12.888)。設定されていなければ整数のレベル。 |
| `chart.LevelFirstDecimal  (int)` | LevelDecimal の小数第 1 位 (0-9)。 |
| `chart.LevelIcon  (enum)` | 列挙オブジェクトとしてのレベルアイコン。 |
| `chart.IsPlus  (bool)` | レベルアイコンが「プラス」のとき true。 |
| `chart.IsMinus  (bool)` | レベルアイコンが「マイナス」のとき true。 |
| `chart.NotesDesigner  (string)` | NOTESDESIGNER フィールドを 1 つの文字列として。 |
| `chart.Charters  (string array)` | NOTESDESIGNER フィールドをカンマで分割したもの。 |
| `chart.BPM  (number)` | メインの BPM。なければ nil。 |
| `chart.BaseBPM  (number)` | 基準 BPM。なければ nil。 |
| `chart.MinBPM  (number)` | 最小 BPM。なければ nil。 |
| `chart.MaxBPM  (number)` | 最大 BPM。なければ nil。 |
| `chart.Life  (int)` | Tower のライフ数。なければ nil。 |
| `chart.TotalFloorCount  (int)` | Tower の階数。なければ nil。 |
| `chart.TowerType  (string)` | Tower のタイプ文字列。なければ nil。 |
| `chart.DanTick  (int)` | 段位プレートの目盛り値。なければ nil。 |
| `chart.DanTickColor  (color)` | 段位プレートの目盛り色 (譜面に譜面情報がない場合は白)。 |
| `chart.DanSongs  (array of dan songs)` | この Dan 譜面を構成する楽曲。 |
| `chart.DanExams  (array of dan exams)` | この Dan 譜面のグローバルな試験条件。 |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | 1 始まりの楽曲インデックスと 1 始まりの試験スロットに対する楽曲ごとの試験を返します。存在しなければ結果の IsSet は false です。 |
| `chart:GetPlayerBestScore(save)  -> best score info` | 指定したセーブファイルのこの譜面に対するベストプレイの要約を返します。 |
| `chart:GetCustomCommand(key)  -> string` | 譜面スコープのカスタムコマンド (この COURSE ブロック内のドット始まりのヘッダ。key はドットを含む) の値を返します。なければ nil。 |
| `chart:GetCustomCommands()  -> dictionary` | すべての譜面スコープのカスタムコマンドを C# の辞書オブジェクトとして返します。 |
| `chart.SongFolder  (string)` | 譜面ファイルを含むフォルダの絶対パス。 |
| `chart.ChartPath  (string)` | 譜面ファイルの絶対パス。 |
| `chart.UniqueId  (string)` | 楽曲の固有 id。なければ空文字列。 |
| `chart:Select(player)  -> bool` | この譜面を指定したプレイヤーの選択難易度としてマークします。プレイヤー 0 は選択楽曲も設定します。譜面が有効でなければ false を返します。 |

### ベストスコア情報

1 つの譜面に対するセーブファイルのベストプレイの要約です。

<div class="callout warn">
chart:GetPlayerBestScore(save) がこのオブジェクトを返します。すべてのメンバーは読み取り専用です。無効なセーブインデックスは空のレコードになります。
</div>

| メソッド | 説明 |
| --- | --- |
| `info.ScoreRank  (int)` | 到達した最高のスコアランク。 |
| `info.ClearStatus  (int)` | 到達した最高のクリア状態。 |
| `info.HighScore  (int)` | ハイスコア。 |
| `info.HasBeenPlayed  (bool)` | 結果にかかわらず、譜面に少なくとも 1 回の記録されたプレイがあるとき true。 |
| `info.PlayCount  (int)` | すべての Mod の組み合わせを合わせた、この譜面のプレイ回数の合計。 |

## 段位の試験

### 段位の楽曲

段位コース内の 1 つの楽曲エントリです。

<div class="callout warn">
chart.DanSongs の要素です。すべてのメンバーは読み取り専用です。
</div>

| メソッド | 説明 |
| --- | --- |
| `dansong.Title  (string)` | 楽曲のタイトル。 |
| `dansong.SubTitle  (string)` | 楽曲のサブタイトル。 |
| `dansong.Genre  (string)` | 楽曲のジャンル。 |
| `dansong.Level  (int)` | 楽曲の星の数によるレベル。 |
| `dansong.Difficulty  (enum)` | 列挙オブジェクトとしての難易度。 |
| `dansong.DifficultyAsInt  (int)` | 難易度インデックス。 |

### 段位の試験

段位コースの 1 つの合否条件です。

<div class="callout warn">
chart.DanExams の要素、または chart:GetSongExam() から返されます。すべてのメンバーは読み取り専用です。TypeAsInt の値: 0 ゲージ、1 良判定、2 可判定、3 不可判定、4 スコア、5 連打、6 叩いた数、7 コンボ、8 精度、9 アドリブ判定、10 地雷判定。RangeAsInt の値: 0 「以上」、1 「未満」。
</div>

| メソッド | 説明 |
| --- | --- |
| `danexam.IsSet  (bool)` | この試験スロットが有効なとき true。 |
| `danexam.RedValue  (int)` | 赤 (合格) のしきい値。 |
| `danexam.GoldValue  (int)` | 金のしきい値。 |
| `danexam.TypeAsInt  (int)` | 試験の種類。 |
| `danexam.RangeAsInt  (int)` | 比較の方向。 |

### DANBUILDER

楽曲ノード、難易度、試験条件から段位コースをメモリ上に組み立て、プレイ用にマウントするためのグローバルです。

<div class="callout warn">
グローバル DANBUILDER として利用できます。楽曲とスロットのインデックスは 1 始まりです。ただし AddSong に渡す難易度は 0 始まりの難易度インデックスです。試験スロットは 1 から 7 です。試験の種類の文字列 (大文字小文字を区別しません。括弧内は短縮形): "judgeperfect" (jp)、"judgegood" (jg)、"judgebad" (jb)、"score" (s)、"roll" (r)、"hit" (h)、"combo" (c)、"accuracy" (a)、"judgeadlib" (ja)、"judgemine" (jm)。それ以外の文字列はゲージを意味します。lessThan = true で試験は「未満」の判定に、false で「以上」の判定になります。ビルダーは呼び出し間で状態を保持します。新しいコースを構築する前に Clear() を呼んでください。
</div>

| メソッド | 説明 |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | これまでに追加された楽曲数。 |
| `DANBUILDER:AddSong(node, diff)  -> void` | 指定した 0 始まりの難易度インデックスで楽曲ノードを追加します。 |
| `DANBUILDER:GetSong(i)  -> song node` | 1 始まりのインデックス i の楽曲ノードを返します。なければ nil。 |
| `DANBUILDER:GetSongDiff(i)  -> int` | 1 始まりのインデックス i の楽曲に保存された難易度インデックスを返します。なければ -1。 |
| `DANBUILDER:SetTitle(title)  -> void` | コースのタイトルを設定します (既定 "Dynamic Dan")。 |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | コースのサブタイトルを設定します。 |
| `DANBUILDER:SetDanTick(tick)  -> void` | 段位プレートの目盛り値を設定します (既定 2)。 |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | 0-255 の成分から段位プレートの目盛り色を設定します (既定は白)。 |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | 指定したスロットにコース全体の試験を設定します。 |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | 1 始まりの楽曲インデックスとスロットで、1 つの楽曲に適用される試験を設定します。 |
| `DANBUILDER:Clear()  -> void` | すべての楽曲と試験を取り除き、メタデータを既定値にリセットします。 |
| `DANBUILDER:Mount()  -> bool` | コースの譜面をメモリ上に構築し、プレイヤー 1 の Dan 難易度でプレイ用に選択します。ビルダーに楽曲がないか構築に失敗した場合は false を返します。 |

```lua
DANBUILDER:Clear()
DANBUILDER:SetTitle("Custom course")
DANBUILDER:AddSong(list:GetSongByUniqueId(id1), 3)
DANBUILDER:AddSong(list:GetSongByUniqueId(id2), 3)
DANBUILDER:SetGlobalExam(1, "gauge", 90, 100, false)
DANBUILDER:SetPerSongExam(2, 2, "judgebad", 10, 5, true)
if DANBUILDER:Mount() then
    return Exit("play")
end
```

## アンロック、仮想スロット、Mod アイコン

### アンロック条件

何が楽曲をアンロックするか、およびプレイヤーが現在その条件を満たしているかを表します。

<div class="callout warn">
node.UnlockCondition がこのオブジェクトを返します。HasCondition はプロパティで、残りはメソッドです。アンロックエントリのない楽曲は HasCondition = false、IsUnlockable = true を報告します。
</div>

| メソッド | 説明 |
| --- | --- |
| `cond.HasCondition  (bool)` | 楽曲に明示的なアンロック条件があるとき true。 |
| `cond:GetConditionMessage()  -> string` | 条件の人が読める説明を返します。なければ空文字列。 |
| `cond:GetConditionType()  -> string` | 条件の種類 id (例: "ch"、"cs"、"gt"、"gc"、"ig") を返します。なければ空文字列。 |
| `cond:GetCoinPrice()  -> int` | コインの価格を返します。なければ 0。 |
| `cond:IsUnlockable(player)  -> bool` | 指定したプレイヤーが条件を満たしているとき true を返します。 |
| `cond:GetBlockedMessage(player)  -> string` | プレイヤーが条件を満たしていない理由を返します。満たしていれば空文字列。 |

### VIRTUALSLOTS

5 つの仮想キャラクタースロット (V1-V5) の読み書きと、プレイヤースポットをスロットの見た目で表示するようリダイレクトするためのグローバルです。

<div class="callout warn">
グローバル VIRTUALSLOTS として利用できます。スロットインデックスは 1 から 5 です。セッターは範囲外のインデックスを無視し、ゲッターはその場合に既定値を返します。これらのメソッドはディスクには何も書き込みません。AI スロットはエンジンが管理しており、このグローバルからは編集できません。
</div>

| メソッド | 説明 |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | スロットのキャラクターフォルダ名を返します。なければ "None"。 |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | スロットのキャラクターフォルダ名を設定します。 |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | スロットのぷちキャラフォルダ名を返します。なければ "None"。 |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | スロットのぷちキャラフォルダ名を設定します。 |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | スロットのネームプレートのプレイヤー名を返します。なければ "VSlot"。 |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | スロットのネームプレートのプレイヤー名を設定します。 |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | スロットのネームプレートの称号テキストを返します。 |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | スロットのネームプレートの称号テキストを設定します。 |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | スロットのネームプレートの段位テキストを返します。 |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | スロットのネームプレートの段位テキストを設定します。 |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | ネームプレートデータベースのネームプレートを id で適用します: 称号テキスト、種類、レアリティを設定します。不明な id は id のみを記録します。 |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | ネームプレートの称号の種類 (スタイルインデックス) を直接設定します。 |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | ネームプレートの称号のレアリティインデックスを直接設定します。 |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | 段位プレートの種類を設定します。 |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | 段位プレートを金色で表示するかどうかを設定します。 |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | プレイヤースポット 1-5 に `slotInfo` の見た目を表示させます: "1P"-"5P" (プレイヤーのセーブファイル)、"AI"、または "V1"-"V5"。この上書きは、そのスポットに対する次の MountSlot 呼び出しまで続きます。 |

### MODICONS

プレイヤーの有効な Mod アイコンを画面上の位置に描画するためのグローバルです。

<div class="callout warn">
グローバル MODICONS として利用できます。描画は modicons ROActivity が行い、最初の Draw 呼び出しがそれをアクティブ化します。
</div>

| メソッド | 説明 |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | 指定したプレイヤーの Mod アイコンをメニューレイアウトで (x, y) に描画します。alpha は任意です (既定 255)。 |

## リプレイと選択された楽曲

### REPLAY

譜面の保存済みリプレイを列挙し、その再生を開始するためのグローバルです。

<div class="callout warn">
グローバル REPLAY として利用できます。ListReplays はリプレイヘッダの C# 配列 (.Length、0 始まり) を返します。Watch はリプレイを読み込み、次のプレイに限って再生を準備します。ゲームはリプレイの Mod をメモリ上で適用し、その後以前の Mod に戻します。songFolder と chartPath は譜面の SongFolder と ChartPath から得ます。
</div>

| メソッド | 説明 |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | 譜面と難易度のリプレイを、スコア順に最大 topN 件返します。chartPath を渡すと、一覧が ChecksumMismatch を計算できます。 |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | 同上。譜面パスなし (一覧は ChecksumMismatch を省略します)。 |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | 同じ列挙をバックグラウンドスレッドで実行し、ポーリング用のハンドルを返します。 |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | リプレイファイルを読み込み、次のプレイの再生を準備します。ファイルの読み込みに失敗するかリプレイが観賞不可の場合は false を返します。chartPath はゲーム内の「無効なリプレイ」警告を有効にします。 |
| `REPLAY:Watch(filepath)  -> bool` | 同上。譜面パスなし。 |
| `REPLAY.MODFLAG  (object)` | ModFlags のビット値: None (0)、Mirror (1)、Random (2)、SuperRandom (4)、Invisible (8)、PerfectMemory (16)、Avalanche (32)、Minesweeper (64)、Just (128)、Safe (256)、DynamicBeat (512)。 |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### リプレイリストハンドル

REPLAY:ListReplaysAsync が返すハンドルです。

<div class="callout warn">
毎フレーム IsDone をポーリングし、true になったら Result を読み取ります。
</div>

| メソッド | 説明 |
| --- | --- |
| `handle.IsDone  (bool)` | バックグラウンドの列挙が完了すると true。 |
| `handle.Result  (array of replay headers)` | 列挙されたリプレイ (IsDone が true になるまでは空)。 |

### リプレイヘッダ

保存済みリプレイ 1 件のメタデータです。

<div class="callout warn">
REPLAY:ListReplays が返す配列、またはリプレイリストハンドルの Result の要素です。すべてのメンバーは読み取り専用です。
</div>

| メソッド | 説明 |
| --- | --- |
| `rep.FilePath  (string)` | リプレイファイルの絶対パス。REPLAY:Watch に渡します。 |
| `rep.PlayerName  (string)` | リプレイを記録したプレイヤーの名前。 |
| `rep.Score  (int)` | 最終スコア。 |
| `rep.ClearStatus  (int)` | プレイのクリア状態。 |
| `rep.ScoreRank  (int)` | プレイのスコアランク。 |
| `rep.Good  (int)` | 良 (Good) 判定の数。 |
| `rep.Ok  (int)` | 可 (Ok) 判定の数。 |
| `rep.Bad  (int)` | 不可 (Bad) 判定の数。 |
| `rep.Roll  (int)` | 連打の打数。 |
| `rep.MaxCombo  (int)` | 最大コンボ。 |
| `rep.Boom  (int)` | 地雷を叩いた数。 |
| `rep.ADLib  (int)` | アドリブを叩いた数。 |
| `rep.ModFlags  (int)` | 使用した Mod のビットマスク (REPLAY.MODFLAG を参照)。 |
| `rep.ScrollSpeed  (int)` | プレイ時のスクロール速度設定。 |
| `rep.SongSpeed  (int)` | プレイ時の曲速度設定。 |
| `rep.JudgeStrictness  (int)` | プレイ時の判定幅設定。 |
| `rep.Date  (string)` | "yyyy-MM-dd HH:mm" 形式のプレイ日時。 |
| `rep.Timestamp  (int)` | 生の ticks としてのプレイ日時。 |
| `rep.ChartUniqueID  (string)` | 譜面の固有 id。 |
| `rep.ChartDifficulty  (int)` | 記録されたプレイの難易度インデックス。 |
| `rep.ChartChecksum  (string)` | リプレイとともに保存された譜面の MD5。 |
| `rep.RandomSeed  (int)` | 音符シャッフルのシード。ファイルに保存されていなければ -1。 |
| `rep.GameMode  (int)` | 記録されたプレイのゲームモード。 |
| `rep.GameVersion  (int)` | リプレイを記録したゲームバージョン。 |
| `rep.Watchable  (bool)` | ゲームがリプレイを忠実に再生できるとき true。 |
| `rep.UnwatchableReason  (string)` | Watchable が false のとき、リプレイを観賞できない理由。 |
| `rep.OldVersion  (bool)` | 古いゲームバージョンがリプレイを記録したとき true。 |
| `rep.ChecksumMismatch  (bool)` | 譜面ファイルが記録時のものと一致しなくなったとき true (譜面パスを渡した場合にのみ計算されます)。 |

### SONGMOUNT

現在プレイ用に選択されている楽曲の読み取り専用グローバルです。

<div class="callout warn">
グローバル SONGMOUNT として利用できます。楽曲ノードの Mount()、譜面の Select()、DANBUILDER:Mount() によって設定された状態を反映します。
</div>

| メソッド | 説明 |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | 選択された楽曲の固有 id を返します。なければ空文字列。 |
| `SONGMOUNT:ChosenDifficulty()  -> int` | プレイヤー 1 に選択された難易度インデックスを返します。 |
| `SONGMOUNT:ChosenSongNode()  -> song node` | 選択された楽曲を (子を持たない) 楽曲ノードとして返します。何も選択されていなければ nil。 |

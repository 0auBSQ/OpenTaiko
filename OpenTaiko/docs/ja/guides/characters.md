<!-- guides/characters.md -->

# キャラクターの追加

キャラクターは、ゲームのインストールフォルダの `Global/Characters/` の下にあるフォルダです。ゲームがそこで見つけたすべてのサブフォルダが、選択可能な 1 つのキャラクターになります。フォルダには `Metadata.json` (名前、レアリティ、作者)、`CharaConfig.txt` (位置とアニメーションのタイミング)、アニメーションのコンテンツ、そして任意で `Effects.json`、`Unlock.json`、`Palettes.json`、ボイスクリップが入ります。アニメーションのコンテンツは、ゲーム組み込みのキャラクタースクリプトが描画する番号付き PNG フレームのフォルダか、キャラクターごとの `Script.lua` が描画すると決めた任意のものです (同梱の 3D テンプレートは glTF モデルを描画します)。

互換性：OpenTaiko 0.6.1 は 0.6.0 向けに作られたキャラクターを引き続き変更なしで読み込みます。このページで説明する構成が現行のものなので、新しいキャラクターにはこちらを使ってください。

## 始める前に

- OpenTaiko 0.6.1 がインストールされていること。ゲームはキャラクターをゲームの実行ファイルの隣の `Global/Characters/` から読み込み、すべてのスキンがそれらを共有します。
- JSON と INI 形式のファイル用のテキストエディタ。
- 2D キャラクターの場合: 透明背景で、アニメーション状態ごとに 1 フォルダの番号付き PNG フレーム (`0.png`、`1.png`、...) として書き出したアート。
- 3D キャラクターの場合: アニメーションクリップを含む `model.glb` (バイナリ glTF) と、静止画の `Render.png`。
- 同梱の `01 - Template` (2D) と `01 - Template3D` フォルダ。どちらかをコピーして出発点にしてください。

## ステップ 1: 検出、順序、識別を理解する

起動時にゲームは `Global/Characters/` のサブフォルダを列挙し、ファイルシステムが返す順序でフォルダごとに 1 つのキャラクターを作成します。ゲームはリストをソートしないため、同梱のフォルダは順序を予測可能にするために数字の接頭辞 (`00 - None`、`01 - Template`、`02 - Student (A)`、...) を持っています。`00 - None` は先頭に置いたままにしてください。インデックス 0 は空のスロットであり、保存されたキャラクターが見つからないときのフォールバックです。

セーブファイルは選択したキャラクターをフォルダ名 (`characterName`) で保存し、起動のたびにインデックスに解決し直します。他のフォルダを追加または削除しても保存された選択が壊れることはありませんが、フォルダの名前を変えると、それを参照していたセーブは `00 - None` にフォールバックします。2 つのキャラクターが同じ表示名を持つことはできます。フォルダ名は一意でなければなりません。

ゲームはキャラクターを起動時に 1 回と、スキンが再読み込みされたときに列挙します。ゲームの実行中に追加されたフォルダは、次の起動またはスキンの再読み込み後に現れます。

## ステップ 2: フォルダと Metadata.json を作成する

`30 - MyChara` のようなフォルダを作成し、`Metadata.json` を追加します。

- `name`: 表示名。プレーンな文字列か、ローカライズオブジェクト `{ "strings": { "default": "...", "ja": "...", ... } }`。`default` がフォールバックで、他のキーはゲームの言語コードです。
- `rarity`: `Poor`、`Common`、`Uncommon`、`Rare`、`Epic`、`Legendary`、`Mythical` のいずれか。レアリティは色とアンロック通知の段階だけを制御します。すべてのレアリティでコイン倍率は 1 です。
- `author`: プレーンな文字列またはローカライズオブジェクト。
- `description`: 任意。プレーンな文字列またはローカライズオブジェクト。
- `speechtext`: 任意。リザルト画面がキャラクターの吹き出しに表示する 6 つのローカライズオブジェクトの配列。ゲームはエントリを結果に応じて次の順で選びます: ゲージが低い状態での失敗、ゲージ 40% 以上での失敗、クリア、ゲージ満タンでのクリア、フルコンボ、全良。6 つ未満しか与えない場合、ゲームは最後のものを繰り返します。

`Metadata.json` がない場合でも、キャラクターは名前 `(None)`、レアリティ `Common`、作者 `(None)` で読み込まれます。

```json
{
  "name": {
    "strings": {
      "default": "My Character",
      "ja": "マイキャラ"
    }
  },
  "rarity": "Common",
  "author": {
    "strings": {
      "default": "Your Name"
    }
  }
}
```

## ステップ 3 (2D の場合): フレームフォルダを追加する

フォルダに `Script.lua` がない場合、ゲームは組み込みのスクリプト (ゲームのインストールフォルダにある `CharaScript.lua`) でキャラクターを描画します。このスクリプトは各アニメーション状態をサブフォルダに対応付け、そこから `0.png`、`1.png`、`2.png`、... を読み込みます。読み込みは最初に欠けた番号で止まるため、番号は連続していなければなりません。

| アニメーション状態 | フォルダ |
|---|---|
| Game/Normal、Game/Clear、Game/Max | `Normal`、`Clear`、`Clear_Max` |
| Game/Gogo、Game/Gogo_Max | `GoGo`、`GoGo_Max` |
| Game/Miss、Game/Miss_Down | `Miss`、`MissDown` |
| Game/10combo、Game/10combo_Max | `10combo`、`10combo_Max` |
| Game/Cleared、Game/Failed | `Cleared`、`Failed` |
| Game/Clear_In、Game/Clear_Out | `Clearin`、`ClearOut` |
| Game/Max_In、Game/Max_Out | `Soulin`、`SoulOut` |
| Game/Miss_In、Game/Miss_Down_In、Game/Return | `MissIn`、`MissDownIn`、`Return` |
| Game/GoGoStart、Game/GoGoStart_Clear、Game/GoGoStart_Max | `GoGoStart`、`GoGoStart_Clear`、`GoGoStart_Max` |
| Game/Balloon_Breaking、Game/Balloon_Broke、Game/Balloon_Miss | `Balloon_Breaking`、`Balloon_Broke`、`Balloon_Miss` |
| Game/Kusudama_Breaking、Game/Kusudama_Broke、Game/Kusudama_Miss、Game/Kusudama_Idle | `Kusudama_Breaking`、`Kusudama_Broke`、`Kusudama_Miss`、`Kusudama_Idle` |
| Game/Tower/Standing、Climbing、Running、Clear、Fail (および `_Tired` 系) | `Tower_Char/Standing`、`Tower_Char/Climbing`、`Tower_Char/Running`、`Tower_Char/Clear`、`Tower_Char/Fail` (加えて `Tower_Char/Standing_Tired` など) |
| Menu/Wait、Menu/Start、Menu/Normal、Menu/Select | `Menu_Wait`、`Menu_Start`、`Menu_Loop`、`Menu_Select` |
| Entry/Normal、Entry/Jump | `Title_Normal`、`Title_Entry` |
| Result/Normal、Result/Clear、Result/Failed_In、Result/Failed | `Result_Normal`、`Result_Clear`、`Result_Failed_In`、`Result_Failed` |

組み込みのスクリプトはフォルダのルートから 2 つの静止画を読み込みます: `Render.png` (フルサイズのポートレート。部屋画面など、ゲームが Render アニメーションタイプを要求する場所で描画されます) と `Preview.png` (サムネイル。ない場合、スクリプトは `Normal/0.png` を使います)。

存在しない状態は別の状態にフォールバックするため、キャラクターは一部だけを同梱できます。フォールバックの連鎖は次のとおりです: Clear -> Normal、Max -> Clear、Miss -> Normal、Miss_Down -> Miss、Gogo -> Normal、Gogo_Max -> Gogo、10combo_Max -> 10combo、GoGoStart_Clear -> GoGoStart、GoGoStart_Max -> GoGoStart_Clear、Tower の `_Tired` 状態 -> 対応する通常状態、Tower/Fail -> Tower/Standing_Tired、Kusudama_Idle -> Normal、Menu/Wait -> Gogo、Menu/Start と Menu/Select と Entry/Jump -> 10combo、Menu/Normal と Entry/Normal と Result/Normal -> Normal、Result/Clear -> Clear、Result/Failed_In -> Miss_In、Result/Failed -> Miss。フォールバックのない状態 (例えば Cleared、Failed、Return、風船の各状態) は、存在しなければ何も描画しません。動作するキャラクターの最小構成は `Normal/0.png` です。

```
30 - MyChara/
  Metadata.json
  CharaConfig.txt
  Render.png
  Normal/0.png 1.png 2.png ...
  Clear/0.png ...
  GoGo/0.png ...
  Miss/0.png ...
  Menu_Loop/0.png ...
  Result_Clear/0.png ...
  Sounds/                (optional voice clips, see Step 6)
```

## ステップ 4: CharaConfig.txt を書く

`CharaConfig.txt` は `Key=Value` 形式のテキストファイルで、`;` で始まる行はコメントです。組み込みのスクリプトは次のキーを読み取ります (同梱の 3D テンプレートも位置のキーを読み取ります)。

- `Chara_Resolution=W,H` (既定 `1280,720`): 以下の座標を作成する基準となる解像度。ゲームは描画時に位置をこの解像度からスキンの解像度へスケールします。
- `Chara_LegacyMode` (既定 `1`): 0.6.0 のアンカーとオフセットの補正を維持します。古いバージョンから移植したキャラクターはこれに依存しています。
- `Game_Chara_X=...` / `Game_Chara_Y=...`: ゲームプレイでの位置。スクリプトは各リストの最初の値を使います。`Game_Chara_Offset=X,Y` は別の書き方です。
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`: AI バトル用のプレイヤーごとの値。両方のキーがあるとき、このキャラクターについてスキンの AI バトル位置を置き換えます。
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`、`Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`: 風船とくす玉のシーケンス中の位置 (最初の値が使われます)。`Game_Chara_Balloon_Offset`、`Game_Chara_Kusudama_Offset`、`Game_Chara_Tower_Offset` は `X,Y` の組を取ります。
- `Menu_Offset=X,Y`、`Menu_Chara_Scale`、`Result_Offset=X,Y`、`Heya_Chara_Render_Offset=X,Y`: メニュー、リザルト、部屋画面のレンダーのオフセット。
- `Game_Chara_Motion_<State>=0,1,2,...`: 状態のフレームを再生する順序を、0 始まりのフレームインデックスで指定します。省略するとフレームはファイル順に再生されます。状態名はフォルダ名に従います。例: `Game_Chara_Motion_Normal`、`Game_Chara_Motion_GoGo`、`Game_Chara_Motion_Miss_Down`、`Game_Chara_Motion_Balloon_Broke`、`Game_Chara_Motion_Tower_Climbing`。
- `Game_Chara_Beat_<State>=N`: 状態の 1 ループが何拍にわたるか。例: `Game_Chara_Beat_Normal=1`、`Game_Chara_Beat_GoGo=2`。
- メニュー、タイトル、リザルトの状態は `Menu_Chara_Motion_Loop/Wait/Start/Select`、`Title_Chara_Motion_Normal/Entry`、`Result_Chara_Motion_Normal/Clear/Failed_In/Failed` を使い、対応する `_Beat_` キーか、ミリ秒単位の固定時間 `Chara_Menu_Loop_AnimationDuration`、`Chara_Menu_Wait_AnimationDuration`、`Chara_Menu_Start_AnimationDuration`、`Chara_Menu_Select_AnimationDuration`、`Chara_Normal_AnimationDuration`、`Chara_Entry_AnimationDuration`、`Chara_Result_Normal_AnimationDuration`、`Chara_Result_Clear_AnimationDuration`、`Chara_Result_Failed_In_AnimationDuration`、`Chara_Result_Failed_AnimationDuration` を指定します。

既定値付きの完全なキー一覧は、組み込みの `CharaScript.lua` の先頭にある `load_chara_config_defs` テーブルです。スクリプトは知らないキーを無視するため、同梱の `01 - Template/CharaConfig.txt` にはこのファイルでは効果のないスキン側のキーもいくつか含まれています。

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;Character X position (1P,2P)
Game_Chara_X=0,0
;Character Y position (1P,2P)
Game_Chara_Y=0,805

;Normal-state frame order and beats per loop
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;GoGo frame order and beats per loop
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## ステップ 5 (3D の場合): model.glb とキャラクターごとの Script.lua を同梱する

キャラクターフォルダに `Script.lua` があるとき、それは組み込みのスクリプトを完全に置き換えます。ゲームは次のグローバル関数を名前で呼び出します。

- `loadAnimation(animationType)`、`disposeAnimation(animationType)`
- 真偽値を返す `availableAnimation(animationType)`。ゲームは古いスペルミスの `avaialbeAnimation` も引き続き受け付けます。まず `availableAnimation` を試し、`avaialbeAnimation` にフォールバックします。同梱の 3D テンプレートはまだ古い名前を使っています。
- `setAnimationDuration(animationType, durationMs)`、`resetAnimationCounter(animationType)`
- ループしないアニメーションが終了したときに `true` を返す `update(delta, animationType, looping)`
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- 幅と高さを返す `getDrawSize(animationType)`
- x と y を返す `getHeyaRenderOffset()`。x と y、またはスキンの位置を使う場合は `nil` を返す `getAIBattlePosition(player, charaScale)`
- `loadVoice(voiceType)`、`disposeVoice(voiceType)`、`playVoice(voiceType)`

アニメーションタイプは `CHARACTER.ANIM_*` 定数の背後にある文字列 (`"Game/Normal"`、`"Menu/Normal"`、...) と、2 つの特別なタイプ `CHARACTER.ANIM_PREVIEW` (サムネイル) と `CHARACTER.ANIM_RENDER` (全身のポートレート) です。ボイスタイプは `CHARACTER.VOICE_*` 定数です。ステップ 3 のフォールバックの連鎖はスクリプト化されたキャラクターにも適用されます。ゲームは `availableAnimation` に問い合わせ、利用可能なものが見つかるまで代替をたどります。

同梱の `01 - Template3D` フォルダには `CharaConfig.txt`、`Effects.json`、`Metadata.json`、`model.glb`、`Render.png`、`Script.lua` だけが入っています。そのスクリプトは `MODEL:Load` で `model.glb` を読み込み、`SCENE3D:CreateScene` で自身が作成したシーンに描画し、`CharaConfig.txt` の位置キーを読み取り、`CLIP` テーブルですべてのアニメーションタイプをクリップインデックスと拍数に対応付けます。3D キャラクターを作るには、フォルダをコピーし、`model.glb` と `Render.png` を置き換え、各タイプがモデルの正しいクリップインデックスを指すように `CLIP` を編集します。

```lua
-- excerpt from 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- one entry per animation state the model supports
}

function loadAnimation(animationType)
  -- build the clip / preview / render data and mark it available
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## ステップ 6: 任意のファイル: Effects.json、Unlock.json、Palettes.json、ボイス

- `Effects.json`: `gauge` (`Normal`、`Hard`、`Extreme`。既定 `Normal`) は魂ゲージの種類を選びます。ゲームが通常のゲージを強制しない限り、`Hard` はコイン獲得を 1.5 倍、`Extreme` は 1.8 倍にします。Minesweeper の Fun Mod が有効なとき、`bombFactor` (1-100、既定 20) は Mod が爆弾に変える音符の割合、`fuseRollFactor` (0-100、既定 0) は Mod がヒューズ連打に変える風船の割合です。
- `Unlock.json`: 存在するとき、プレイヤーが条件を満たすまでキャラクターはロックされたままです。形式と条件 id は楽曲のものと同じです。アンロック要素のガイドを参照してください。コイン条件はプレイヤーが部屋画面で購入し、他の条件はゲームがリザルト画面で自動的に確認します。同梱の例: Kuro は `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }` (Extreme 譜面をフルコンボ以上で 10 回クリア)、Aoi は `{ "condition": "ch", "type": "me", "values": [200] }` (200 コイン) を使っています。
- `Palettes.json`: プレイヤーがキャラクターに適用できるカラーパレットの配列。各エントリは `name`、`blend` (0-1)、`stops` (`[position, R, G, B]` または `[position, R, G, B, A]` のグラデーションストップの配列。2 つ以上与えてください)、そして `plays` (パレットをアンロックする、このキャラクターでのプレイ回数。0 または省略で直ちに利用可能) を持ちます。`"stops": null` のエントリは着色なしの既定です。
- ボイス: 組み込みのスクリプトはキャラクターフォルダ内の決まったパスから `.ogg` ファイルを読み込みます。例: `Sounds/Clear/Clear.ogg`、`Sounds/Clear/Failed.ogg`、`Sounds/Clear/FullCombo.ogg`、`Sounds/Clear/AllPerfect.ogg`、`Sounds/Menu/SongSelect.ogg`、`Sounds/Menu/SongDecide.ogg`、`Sounds/Menu/DiffSelect.ogg`、`Sounds/Title/Sanka.ogg`、`Sounds/Result/BestScore.ogg`、`Sounds/Result/ClearSuccess.ogg`、`Sounds/Result/ClearFailed.ogg`。完全な一覧は、組み込みの `CharaScript.lua` の先頭にある `voice_files` テーブルです。存在しないファイルはスクリプトがスキップします。

```json
{
  "gauge": "Normal",
  "bombFactor": 20,
  "fuseRollFactor": 0
}
```

```json
{
  "condition": "ch",
  "type": "me",
  "values": [ 200 ]
}
```

```json
[
  { "name": "Default", "stops": null },
  { "name": "Green", "blend": 1.0, "stops": [ [0, 0, 0, 0], [0.25, 0, 255, 0] ], "plays": 10 }
]
```

## ステップ 7: 再起動してキャラクターを選択する

ゲームを再起動します (または設定からスキンを再読み込みします)。キャラクターは部屋画面のキャラクターリストに現れ、ロックされたキャラクターはアンロック条件を表示します。Lua ステージは `CHARACTERLIST` グローバルを通じてもリストを読み取れます。これは各エントリのフォルダ名、表示名、レアリティ、アンロック条件を公開します。

## トラブルシューティングと注意点

- キャラクターが現れない: フォルダが `Global/Characters/` の直下にあることを確認し、ゲームを再起動してください。ゲームはリストを起動時に 1 回構築します。
- キャラクターが何も描画しない: `Normal/0.png` がないか、フォルダ名がステップ 3 の表と一致していません。フレームは `0.png`、`1.png`、... と隙間なく名付ける必要があります。隙間があると、アニメーションはエラーなしにそのインデックスで終わります。
- キャラクターが画面外にあるか、サイズが違う: `Chara_Resolution` は位置の値を作成した解像度と一致していなければなりません。キーがない場合、ゲームは `1280,720` とみなします。
- アニメーションセットの一部しか再生されない: フォールバックのない状態 (Cleared、Failed、Return、風船とくす玉の各状態) には独自のフォルダが必要です。
- 3D キャラクターのすべてのアニメーションが利用不可と表示される: `Script.lua` は `availableAnimation` (または `avaialbeAnimation`) を定義し、読み込んだタイプに対して `true` を返さなければなりません。
- `Script.lua` が存在すると組み込みのスクリプトを完全に置き換えます。スクリプト化されたキャラクターでも番号付き PNG フォルダを読み込めますが、スクリプト自身がそれらを読み込む場合に限られます。
- セーブはフォルダ名を参照するため、プレイヤーがすでに選択しているフォルダの名前を変えると、その選択は空のスロットにリセットされます。
- 同梱の JSON ファイルには末尾のカンマが含まれています。ゲームの JSON パーサーはこれを受け付けます。厳密なバリデータは拒否します。

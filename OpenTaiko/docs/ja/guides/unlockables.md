<!-- guides/unlockables.md -->

# カスタム譜面へのアンロック条件の追加

カスタム楽曲は、`.tja` と `uniqueID.json` の隣の楽曲フォルダに `Unlock.json` ファイルを置くことで、条件 (コインの価格、他の楽曲のクリア、合計プレイ回数、ストーリーのフラグなど) の裏にロックできます。ゲームは楽曲リストを構築するときにそのファイルを読み取り、プレイヤーが条件を満たすまで楽曲をロックしたままにします。条件は個々の楽曲に付きます。`box.def` にはアンロックのキーがないため、ジャンルフォルダをこの方法でロックすることはできません。

## 始める前に

- すでに楽曲リストに表示されているカスタム譜面: `Songs/` の下の、`.tja` と、ゲームが一度スキャンした後は `uniqueID.json` を含むフォルダ。
- UTF-8 で保存できるテキストエディタ。
- 他の楽曲を参照する条件の場合: 参照する各楽曲の `uniqueID.json` の `id` の値。
- ゲームはアンロックの進行状況をセーブファイルごとに保存するため、楽曲がアンロックされるのを確認するには、ゲーム内で条件を満たす必要があります。ゲーム設定の `Ignore Song Unlockables` オプションは、テスト中にすべての楽曲をアンロック済みとして扱います。

## ステップ 1: Unlock.json ファイルを作成する

楽曲フォルダに `Unlock.json` を作成します。すべてのフィールドは任意で、既定値を持ちます。

- `hidden_index` (int、既定 0): 楽曲リストがロックされた楽曲をどう表示するか (下の表を参照)。ゲームは値を 0-3 に制限します。
- `rarity` (string、既定 `Common`): レアリティ名 (下の表を参照)。レアリティ表示の色とアンロック通知の段階を決めます。空の値は `Common` になります。
- `condition` (string、既定 `ch`): 条件 id (ステップ 2)。
- `values` (int 配列、既定 `[100]`): 条件の数値パラメータ。
- `type` (string、既定 `me`): 値に基づく条件が使う比較: `l` 未満、`le` 以下、`e` 等しい、`me` 以上、`m` より大きい、`d` 異なる。コイン条件は常に `me` を使います。
- `references` (string 配列、既定 `[""]`): 条件に応じて、楽曲 id、ジャンル名、譜面作者名、フラグ名。
- `custom_unlock_text` (object、任意): 生成されるヒントテキストを置き換えます。ローカライズオブジェクト `{ "strings": { "default": "...", "ja": "..." } }` で、ゲームはアクティブな言語のキー、次に `default` を使い、どちらもなければ生成されたテキストを表示します。

キーはここに挙げたとおり、小文字とアンダースコアです。

hidden_index の値:

| 値 | 楽曲リストでの表示 |
| --- | --- |
| 0 | ロックアイコン付きで表示。音声プレビューは再生される |
| 1 | グレー表示。プレビューなし |
| 2 | タイトルとプレビュー画像を隠したグレー表示 |
| 3 | アンロックされるまで非表示 |

レアリティの値:

| レアリティ | 通知の段階 |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## ステップ 2: 条件を選ぶ

| Id | 意味 | `values` | `references` |
|----|---------|----------|--------------|
| `ch`、`cs`、`cm` | コインでの購入。ゲームは 3 つの id を同じように評価します (コインでの価格、`type` は `me` に強制、自動的には付与されない)。`cm` は楽曲向けの id です。スキンスクリプトはこの id を読み取り、購入をどこで提示するかの判断に使えます。 | `[price]` | 未使用 |
| `ce` | セーブファイルの作成以降に獲得したコインの合計 | `[coins]` | 未使用 |
| `tp` | 合計プレイ回数 | `[plays]` | 未使用 |
| `ap` | AI バトルのプレイ回数 | `[plays]` | 未使用 |
| `aw` | AI バトルの勝利数 | `[wins]` | 未使用 |
| `sd` | クリア状態に達した異なる譜面の数 | `[chart count, clear status]` | 未使用 |
| `dp` | クリア状態に達した、ある難易度の譜面の数 | `[difficulty, clear status, chart count]` | 未使用 |
| `lp` | クリア状態に達した、ある星の数のレベルの譜面の数 | `[level, clear status, chart count]` | 未使用 |
| `sp` | クリア状態に達した特定の楽曲 | 楽曲ごとに `[difficulty, clear status]` (`-1` = 任意の難易度) | 組ごとに楽曲 id 1 つ |
| `sg` | クリア状態に達した、指定したジャンル内の楽曲 | ジャンルごとに `[song count, clear status]` | 組ごとにジャンル名 1 つ |
| `sc` | クリア状態に達した、指定した譜面作者の譜面 | 譜面作者ごとに `[chart count, clear status]` | 組ごとに譜面作者名 1 つ |
| `gt` | グローバルトリガー (スクリプトが設定するセーブファイル内の名前付きオン/オフフラグ) | ON なら `[1]`、OFF なら `[0]` | `[trigger name]` |
| `gc` | グローバルカウンター (スクリプトが設定するセーブファイル内の名前付き数値) | `[value]`。`type` で比較 | `[counter name]` |
| `ig` | 入手不可能: 決してアンロックされない | なし | なし |
| `andcomb` | プレイヤーがすべての子条件を満たす必要がある (組み合わせの例を参照) | `[]` | エントリごとに JSON 文字列としての子条件 1 つ |
| `orcomb` | プレイヤーが少なくとも 1 つの子条件を満たす必要がある (組み合わせの例を参照) | `[]` | エントリごとに JSON 文字列としての子条件 1 つ |

クリア状態の値 (状態の要件は「この状態以上」を意味します):

| 値 | クリア状態 |
| --- | --- |
| 0 | プレイ済み |
| 1 | アシストクリア |
| 2 | クリア |
| 3 | フルコンボ |
| 4 | 全良 |

難易度の値:

| 値 | 難易度 |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

`dp` では難易度 3 に Extra Extreme の譜面も含まれます。`dp` と `lp` は通常の譜面だけを数え、Dan と Tower の譜面はスキップします。`sp` は任意の難易度を意味する `-1` を受け付けます。

ゲームは値の個数を検査します: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` はちょうど 1 つ、`sd` はちょうど 2 つ、`dp`/`lp` はちょうど 3 つの値が必要で、`sp`/`sg`/`sc` は参照ごとに 2 つの値と、組の数と同じ数の参照が必要です。個数が違うと条件は失敗し、ゲーム内にエラーメッセージが表示されます。

コイン条件をアンロックするのは、それを提示する画面での明示的な購入だけです (同梱スキンは、どのコイン id を使っていても、選曲画面からロックされた楽曲の購入を提示します)。それ以外のすべての条件はゲームが各プレイ後のリザルト画面で確認します。プレイヤーが条件を満たすと、ゲームは楽曲 id をセーブファイルに追加し、通知を表示します。

## ステップ 3: ゲーム内でテストする

ゲームを起動し、選曲画面を開きます。`hidden_index` に応じて、楽曲はロックを表示するか、グレー表示か、隠されるか、存在しません。購入するか条件を満たし、再起動後もアンロックされたままであることを確認します。繰り返し作業中は、ゲーム設定の `Ignore Song Unlockables` を有効にするとすべてのロックを迂回できます。

## 例

以下は、同梱楽曲が使っている条件のパターンを `Unlock.json` ファイルとして書いたものです。id、名前、数値は自分のものに置き換えてください。

**コインで楽曲を購入する (`cm`)。** 最も一般的なパターンです。価格が唯一の値で、ゲームは `type` を無視します。`hidden_index` 0 は楽曲を表示したままにするので、プレイヤーはそれを見つけて購入できます。生成されるヒントはすでに価格を示すため、`custom_unlock_text` は不要です。

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**合計プレイ回数 (`tp`)。** 同梱のチャプターは、必要なプレイ回数を上げながら (10、15、20 など) 楽曲を 1 つずつ開放していくため、プレイヤーには常に手の届く次の楽曲があります。`ap` (AI バトルのプレイ回数)、`aw` (AI バトルの勝利数)、`ce` (獲得したコインの合計) も同じ単一値の形式を使います。

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**特定の楽曲をクリアする (`sp`)。** 続編やリミックスのために同梱楽曲の多くが使うパターンです。難易度に `-1` を指定すると任意の難易度でのクリアを受け付けます。id は参照する楽曲の `uniqueID.json` の `id` フィールドです (ゲームは id のない楽曲を初めてスキャンしたときに 64 文字の id を生成し、同梱楽曲は手書きの id を持つことがあります)。この例は、プレイヤーが参照する楽曲をクリアした後にアンロックされます。

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**複数の楽曲をフルコンボする (`sp`)。** 楽曲を追加するごとに値の組と参照が 1 つずつ増え、プレイヤーは参照するすべての楽曲を満たす必要があります。この例は 2 曲のフルコンボ (3) を要求します。

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**ジャンルの楽曲をクリアする (`sg`)。** 各エントリは `[song count, clear status]` で、ジャンル名を `references` に入れます。ジャンルは、プレイヤーが楽曲をプレイしたときにゲームが記録するものです。つまり、それを含むフォルダの `box.def` の `#GENRE`、フォルダにない場合は譜面自身の `#GENRE` です。同梱のチャプターはこれをメドレーに使っています。この例は、プレイヤーがそのジャンルの異なる 10 曲をクリアした後にアンロックされます。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**特定の譜面作者の譜面をクリアする (`sc`)。** `#NOTESDESIGNER` の名前を参照とする同じ形式です。この例は 1 人の譜面作者の譜面 5 つのクリアを要求します。2 人目の譜面作者を加えると、値の組と参照がもう 1 つ増えます。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**一定数の異なる譜面をクリアする (`sd`)。** セーブファイルが指定した状態以上で持つすべての譜面を数えます。この例は 10 譜面のフルコンボを要求します。

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**特定の難易度または星の数の譜面をクリアする (`dp` / `lp`)。** `dp` は 1 つの難易度の譜面を、`lp` は 1 つの星の数のレベルの譜面を数えます。最初の例は Extreme の譜面 20 個のクリアを、2 つ目の例は星 7 つの譜面 1 個のクリアを要求します。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**2 つの条件のどちらか (`orcomb`)。** `values` は空で、`references` の各エントリは JSON 文字列として書かれた完全な子条件 (`condition`、`type`、`values`、`references`) です。同梱楽曲はこれを近道の提供に使っています。十分な回数プレイするか、支払うかです。`orcomb` は購入の価格を決めるとき、満たされた分岐のうち最も安いものを使います。

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**複数の条件のすべて (`andcomb`)。** 同じ形式で、プレイヤーはすべての子を満たす必要があります。子はそれ自体が組み合わせであってもかまわず、`andcomb` は組み合わせ内のコイン価格を合計します。この例は、1 つの楽曲のクリアと、任意の星 7 つの譜面のクリアを要求します。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**カスタムヒント付きのストーリーフラグ (`gt`)。** `gt` はセーブファイルから名前付きのオン/オフフラグを読み取ります。フラグは Lua スクリプト (ストーリーシーン、カットシーン) が設定するため、`gt` は自分が制御するスクリプトがフラグを設定する場合にのみ使ってください。`values` は ON なら `[1]`、OFF なら `[0]` で、`references` にフラグ名を入れます。ストーリーのアンロックには自明な生成ヒントがないため、ここで `custom_unlock_text` が役立ちます。任意の言語キーを指定でき、`default` がフォールバックです。

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**なぞなぞ付きの隠し楽曲。** 同梱の隠し楽曲は、高い `hidden_index` (アンロックされるまでタイトルとプレビューを隠したままにするか、楽曲自体を存在させない) と、条件を明示する代わりにほのめかす `custom_unlock_text` を組み合わせています。裏側にはどの条件も使えます。この例はフルコンボの要件をなぞなぞの裏に隠しています。

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## トラブルシューティングと注意点

- 楽曲がロックされない: ファイル名が `Unlock.json` でない、`.tja` と `uniqueID.json` を含むフォルダにない、または `Ignore Song Unlockables` が有効です。
- ヒントに条件が無効と表示される: 値の個数が条件と一致しない (ステップ 2 を参照) か、`sp`/`sg`/`sc` の参照の数が値の組の数と異なります。
- キーが効かない: キーは `hidden_index`、`rarity`、`condition`、`values`、`type`、`references`、`custom_unlock_text` で、すべて小文字です。ゲームはそれ以外を無視し、既定値を適用します。
- `sp` が決してアンロックされない: 参照は対象楽曲の `uniqueID.json` の id でなければなりません (タイトルやファイル名は決して一致しません)。また、プレイヤーがその難易度と状態に到達できる必要があります (状態 3 はフルコンボなので、クリアしかしていない譜面は数えられません)。
- `sg` が決してアンロックされない: ジャンル名は、プレイした楽曲に対してゲームが記録するジャンル (フォルダの `box.def` の `#GENRE`、フォルダにジャンルがなければ譜面の `#GENRE`) と一致する必要があります。
- `gt`/`gc` が決してアンロックされない: 指定した名前のフラグを何も設定していません。譜面自身がそれを設定することはできません。
- `ig` をプレイでアンロックするものはありません。別のシステムが付与するコンテンツにのみ使ってください。
- `custom_unlock_text` は `{ "strings": { ... } }` オブジェクトでなければなりません。素の文字列は機能しません。
- 同梱の `Unlock.json` ファイルには末尾のカンマが含まれています。ゲームの JSON パーサーはこれを受け付けます。厳密なバリデータは拒否します。

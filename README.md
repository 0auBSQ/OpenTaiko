# TJAPlayer3-Develop-BSQ
TJAPlayer3-Develop-ReWrite-1のフォーク, 太鼓の達人 ニジイロVerを切っ掛けに開発したシミュレーター

(English version coming soon)

- 現在の版： v0.4.0

- スキンの最新版： v0.4.0

## 予告情報 （重要）

このプログラムはまだスキンを含みません。

訴えを全く避けるためにデフォルトスキンを作る予定でございます。

それまでニジイロスキンが希望なら僕のDiscordでご連絡ください：

- 申しコミ#5734 （日本語・英語・仏語可）

Discord以外で配布しておりません。

`スキンの最新版`は更新されたら再びご連絡ください。

## 質疑応答

- 段位道場の選曲画面に曲ごとの表示される難易度は全部鬼☆１０

```
.tjaファイルの#NEXTSONG行に「,[難易度],[COURSE]」をご追加ください。

例：

旧： #NEXTSONG Calculator,M-O-T-U,ナムコオリジナル,Calculator.ogg,920,330

新（簡単☆４）： #NEXTSONG Calculator,M-O-T-U,ナムコオリジナル,Calculator.ogg,920,330,4,0
```

- 「太鼓叩いてスタート」と言える画面を通り過ぎれません

```
長くPキーを押してください。
```

- バグを発見してしました、どうしますか？

```
バグを発見したらGithubのIssueをご提出ください。
```

## 更新記録

<details>
	<summary>v0.4.0</summary>
	
	- EXAM5,6,7の実装 (下記の映像をご覧ください)
	
	- EXAM数にギャップのあるクラッシュ場面を修正
	
	- Danに関してコードの構造を改善（コード蓄積の修正）
  
  ![selected_item](https://user-images.githubusercontent.com/58159635/136692306-c429680c-881d-44f8-9c9f-69882f25fda5.png)
	
</details>

<details>
	<summary>v0.3.4.2</summary>
	
	- 段位道場選曲画面にプチキャラを追加
	
</details>

<details>
	<summary>v0.3.4.1</summary>
	
	- Mobアニメーション速度の変化バグを修正
	
</details>

<details>
	<summary>v0.3.4</summary>
	
	- 段位道場の結果を保存を可能にする機能を実装
	
	- 段位道場選曲画面に合格プレートを表示
	
</details>

<details>
	<summary>v0.3.3</summary>
	
	- 段位道場の魂ゲージの表示を修正
	
	- 段位道場の結果画面の基盤を実装（まだ実装中）
	
</details>

<details>
	<summary>v0.3.2</summary>
	
	- 演奏セーブの重ね書きバグを修正
	
</details>

<details>
	<summary>v0.3.1</summary>
	
	- P2にスコアランクを表示されないバグを修正
	
</details>

<details>
	<summary>v0.3.0</summary>
	
	- メニュにプチキャラを表示
	
	- Nameplate.jsonファイルにプレイヤー別々のプチキャラを選べる可能にする機能を実装
	
</details>

<details>
	<summary>v0.2.0</summary>
	
	- 様々な演奏選曲画面のバグを修正
	
	- メインメニュに様々なバグを修正、コード蓄積を修正
	
</details>

<details>
	<summary>v0.1.0</summary>
	
	- 演奏結果画面のアニメーションを実装
	
</details>

## 短期で実装する予定機能
```
☐ 段位道場結果画面を実装し切る
☐ コイン（ドンメダル）を貯蓄可能にする機能を実装＋ドンメダル商店
☐ PS2の太鼓タワーを実装し切る (COURSE: 5) (Ex: https://www.youtube.com/watch?v=rtSe70X1QII)
☐ 複数な背景と踊り子セットを選べる機能を実装
☐ ２P結果画面を実装
☐ プログラムの性能を改善、メモリリークを修正
☐ 段位道場で４曲以上連続でやれる機能を実装
```

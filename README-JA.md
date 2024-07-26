<p align="center">
  <img src="https://user-images.githubusercontent.com/58159635/140600257-f712fc48-d09a-4a5e-a78d-e7c65ca19b80.png">
</p>

# OpenTaiko

旧名称 TJAPlayer3-Develop-BSQ (非公式なTJAPlayer3の継続)

- 現在のバージョン： Pre v0.6.0 b2

- Discord : https://discord.gg/aA8scTvZ6B
- Discord (日本語) : https://discord.gg/CJ4nTkpy7t

## 使用上の注意 （重要）

- このゲームを使用する場合は、**全て自己責任**でお願いします。

- 著作権を反するスキンや派生ビルドのサポートは原則行いませんのでご了承ください。

- 質問する前に、まずは自分で調べたりしてください。
また、[取扱説明書](https://drive.google.com/file/d/1VJoia2M_EWrf58xsATL7udIJ0ozR2YFf/view?usp=sharing)や[Wiki](https://seesaawiki.jp/opentaiko-unofficial/)もご確認ください。

- 常時60fpsを保てないPCでの動作は快適なプレイは期待できません。

- 上記のリリースのバージョン以外でサポートは行いませんので自己責任でお願いします。

### 動画、配信等でのご利用について

タグ機能のあるサイトの場合、「OpenTaiko」「TJAPlayer3」「TJAP3」といったタグを付けることで、誤解を防ぐとともに、関連動画として出やすくなるメリットがあるため、推奨します。

知的所有権侵害は支援しておりませんので、自国の著作権規則に基づいて行動してください。
OpenTaikoチームも著作権を反するスキンの二次配布についても固く反対します。

### 改造・再配布(二次配布)を行う場合について

OpenTaikoはオープンソースソフトウェア(MIT)です。
MITライセンスのルールのもと、改造・再配布を行うことは自由ですが、**全て自己責任**でお願いします。
また、使用しているライブラリのライセンス上、**必ず**「Licenses」フォルダを同梱の上、改造・再配布をお願いします。
外部スキンや、譜面パッケージを同梱する場合は、それぞれの制作者のルールや規約を守ってください。
これらにOpenTaikoのライセンスは適用されません。

### 目標・目標ではない物事

**目標**

- 様々な楽しい太鼓のプレイ手段を設けること。

- より多くのスキンのカスタマイズを可能にすること、「色々な見た目のスキンを簡単に作れる」ということを可能にすること。

- 最適化、バグ修正、ＱＯＬ機能を改善すること。

**目標ではない**

- 某ゲームを再現すること。

## IssueとPull Request投稿上の注意 （重要）

Issue/Pull Requestへ投稿を送ってくれてありがとうございます、大変助かります。

- 投稿内容は**必ず**フランス及び日本の著作権規則に従ってください。

- **重要** ：Issueを提供したら、リリースバージョンと再現方法を述べてください。クラッシュの場合、TJAPlayer3.logの内容をご添付ください。

- CLang言語翻訳を追加して欲しい場合は予めDiscordでご連絡ください.

### 提案について

特別な機能の実装が希望ならDiscordでご連絡ください.

提案を気に入れば実装する事は可能です。

- **重要** ： 「こういうUI・UXのパートを本家っぽく実装してください」のような提案は基本的に拒否しております。

## Q＆A

- 選曲画面の段位道場の曲の難易度が全部鬼の☆１０になってます

```
.tjaファイルの#NEXTSONG行に「,[LEVEL],[COURSE]」を追加してください。

例：

旧： #NEXTSONG [TITLE],[SUBTITLE],[GENRE],[WAVE],[SCOREINIT],[SCOREDIFF]

新： #NEXTSONG [TITLE],[SUBTITLE],[GENRE],[WAVE],[SCOREINIT],[SCOREDIFF],[LEVEL],[COURSE]
```

- エントリー画面から進めません。

```
Pキーを長押し、または面のキー（デフォルトではF、J／B、N）を押してください。
```

- バグを発見しました、どうすればいいですか？

```
バグを発見したらGithubのIssue、またはDiscordサーバーの#bugsチャンネルまでご報告ください。
```

- 追加したキャラクター・プチキャラが表示されていません。

```
v0.5.3.1からキャラクターおよびプチキャラはGlobalフォルダに読み込まれます（スキンフォルダ以外）、そこに入れてください。
```

## 更新記録

<details>
	<summary>v0.5.4</summary>

	- バグ修正

	- オンライン譜面ダウンロード機能の追加

	- キャラクター及びプチキャラの個人音声SFXの対応

	- 音色の対応

	- お任せ曲選択のコンテキストボックスの追加

	- コンガゲームモード

	- PREIMAGEメタデータ対応

	- モードアイコンもモードの使い方の更新

	- 紫音符(G), 爆弾音符(C) の追加及び相手音符(A、B)、ADLIB音符(F)の修正

</details>

<details>
	<summary>v0.5.3.1</summary>

	- バグ修正

	- グローバルキャラクター・プチキャラ

	- 「最近遊んだ曲」のフォルダデータを保存する機能を実装

	- かんたん・ふつうの適当な判断範囲を実装

	- 選曲画面および結果画面にカスタムキャラクターの対応

	- 難易度に基づいての曲検索機能を実装

</details>

<details>
	<summary>v0.5.3</summary>

	- バグ修正

	- 段結果画面を実装（１／２）

	- 曲数の３つ以外の段譜面のクラッシュを修正

	- ２P側対応を追加

	- 主要な２P更新を追加

	- 演奏選曲画面に段及びタワーの譜面を選べるオプションを追加

	- Modal（注目ボックス）を追加

	- コインを使ってコンテンツを解除できる機能を追加（１／２）

	- 「お気に入りの曲」のフォルダーを追加（選曲画面にCTRLを押したら現在選択中の曲を「お気に入り」にする）

	- キャラクター及びプチキャラのメタデータファイル対応を追加

	- 中国語を追加 (WHMHammer)

	- SlimDXの依存を排除 (Mr Ojii)

	- SimpleStyleスキンを追加 (feat. cien)

	- 各曲に自動作成のユニークIDを追加

	- Discord RPCを修正

	- 様々な設定画面項目を修正 (l1m0n3)

</details>

<details>
	<summary>v0.5.2.1</summary>

	- バグ修正

	- オート用のAIレベルを追加

	- グローバルオフセットを追加

	- AUTO ROLLをロールスピードに置き換え

</details>

<details>
	<summary>v0.5.2</summary>

	- 太鼓部屋の機能を追加

	- カスタム名札やキャラクター可能にする機能を実装

	- メダルを取得可能にする手順を追加

	- 段位タイトルを解放可能にする機能を追加

	- 複数な手続き型テクスチャを追加

	- スペイン語の翻訳を追加

	- 「おまかせ」オプションを追加

	- 様々なUI/UX改善

	- 譜面読み込みの最適化

	- 分岐譜面を修正

</details>

<details>
	<summary>v0.5.1</summary>

	- 道場に複数なアニメーションを追加

	- ゲーム終了画面やメニュアイコンを追加

	- 様々なバグ修正

	- 複数な外国語サポートを追加

	- 様々なUI改善

	- 演奏選曲画面の複数なレイアウトを追加

</details>

<details>
	<summary>v0.5.0</summary>

	- タワーを実装 (背景+結果画面の基盤)

	- タワー譜面で「TOWERTYPE」の設定を追加 （タワー譜面に複数なスキンを用いてプレイを可能にする機能）

	- 道場にAccuracy（精度）のEXAMを追加

	- box.defで「#BOXCOLOR」, 「#BOXTYPE」, 「#BGCOLOR」, 「#BGTYPE」, 「#BOXCHARA」の設定を追加

</details>

<details>
	<summary>v0.4.3</summary>

	- タワーを実装 (Gameplay)

</details>

<details>
	<summary>v0.4.2</summary>

	- 演奏選曲画面に複数のバグとクラッシュを修正

	- COURSE:Towerの.tjaファイルのクラッシュを修正、太鼓タワーメニュ・LIFE管理・結果画面がまだ実装されていません。

</details>

<details>
	<summary>v0.4.1</summary>

	- 演奏選曲画面に複数のバグとクラッシュ場面を修正

</details>

<details>
	<summary>v0.4.0</summary>

	- EXAM5,6,7の実装 (下記の映像をご覧ください)

	- EXAM数にギャップのあるクラッシュ場面を修正

	- Danに関してコードの構造を改善（コード蓄積の修正）

</details>

<details>
	<summary>v0.3.4.2</summary>

	- 道場選曲画面にプチキャラを追加

</details>

<details>
	<summary>v0.3.4.1</summary>

	- Mobアニメーション速度の変化バグを修正

</details>

<details>
	<summary>v0.3.4</summary>

	- 道場の結果を保存を可能にする機能を実装

	- 道場選曲画面に合格プレートを表示

</details>

<details>
	<summary>v0.3.3</summary>

	- 道場の魂ゲージの表示を修正

	- 道場の結果画面の基盤を実装（まだ実装中）

</details>

<details>
	<summary>v0.3.2</summary>

	- 演奏セーブの重ね書きバグを修正

</details>

<details>
	<summary>v0.3.1</summary>

	- P2がスコアランクを表示できないバグを修正

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

## スペシャルサンクス

> * [Takkkom/Major OpenTaiko features (1080p support, AI Battle mode, 5P mode and so on)](https://github.com/Takkkom)
> * [AkiraChnl/OpenTaiko Icon](https://github.com/AkiraChnl)(@akirach_jp)
> * [Reichisama/OpenTaiko 0.6.0 Icon](https://twitter.com/himikoreichi135)(@himikoreichi135)
> * [cien/OpenTaiko Logo/Various Default Skin Assets](https://twitter.com/CienpixeL)(@CienpixeL)
> * [funnym0th/OpenTaiko Spanish Translation](https://github.com/funnym0th) (@funnym0th)
> * [basketballsmash/English README Translation](https://twitter.com/basketballsmash)(@basketballsmash)
> * [Meowgister/OpenTaiko English Translation](https://www.youtube.com/channel/UCDi5puZaJLMUA6OgIAb7rmQ)
> * [WHMHammer/OpenTaiko Chinese Translation](https://github.com/whmhammer)(@WHMHammer)
> * [Expédic Habbet/OpenTaiko Chinese Text Assistance, Russian Text](https://github.com/ExpedicHabbet)(@ExpedicHabbet)
> * [Aioilight/TJAPlayer3](https://github.com/aioilight/TJAPlayer3)(@aioilight)
> * [TwoPointZero/TJAPlayer3](https://github.com/twopointzero/TJAPlayer3)(@twopointzero)
> * [KabanFriends/TJAPlayer3](https://github.com/KabanFriends/TJAPlayer3/tree/features)(@KabanFriends)
> * [Mr-Ojii/TJAPlayer3-f](https://github.com/Mr-Ojii/TJAPlayer3-f)(@Mr-Ojii)
> * [Akasoko/TJAPlayer3](https://github.com/Akasoko-Master/TJAPlayer3)(@AkasokoR)
> * [FROM/DTXMaina](https://github.com/DTXMania)(@DTXMania)
> * [Kairera0467/TJAP2fPC](https://github.com/kairera0467/TJAP2fPC)(@Kairera0467)
> * [touhourenren/TJAPlayer3-Develop-Rewrite](https://github.com/touhourenren)

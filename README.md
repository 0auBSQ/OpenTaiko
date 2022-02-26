<p align="center">
  <img src="https://user-images.githubusercontent.com/58159635/140600257-f712fc48-d09a-4a5e-a78d-e7c65ca19b80.png">
</p>

English : https://github.com/0auBSQ/OpenTaiko/blob/main/README-EN.md

中文：https://github.com/0auBSQ/OpenTaiko/blob/main/README-ZH.md

# OpenTaiko

TJAPlayer3-Develop-ReWriteの改造版, 太鼓シミュレーターです。
旧名称 TJAPlayer3-Develop-BSQ

- 現在のバージョン： Pre v0.5.3

- Discord : https://discord.gg/aA8scTvZ6B

## 使用上の注意 （重要）

- 現時点では通常リリースにはスキンが同梱してません(CommitのPreリリースには同梱しています)

- このシミュレータを使用する場合は、**全て自己責任**でお願いします。

- 本家風スキンや派生ビルドのサポートは原則行いませんのでご了承ください。

- 質問する前に、まずは自分で調べたりしてください。

- 常時60fpsを保てないPCでの動作は快適なプレイは期待できません。

- 上記のリリースのバージョン以外でサポートは行いませんので自己責任でお願いします。

### 動画、配信等でのご利用について

OpenTaikoを動画共有サイトやライブ配信サービス、ウェブサイトやブログ等でご利用になられる場合、バンダイナムコエンターテインメント公式のものでないこと、他の太鼓の達人シミュレーターと混同しないよう配慮をお願いいたします。

また、タグ機能のあるサイトの場合、「OpenTaiko」「TJAPlayer3-Develop-BSQ」「TJAP3-BSQ」といったタグを付けることで、他シミュレータとの誤解を防ぐとともに、関連動画として出やすくなるメリットがあるため、推奨します。

知的所有権侵害は支援しておりませんので、自国の著作権規則に基づいて行動してください。

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

- 本家(ニジイロver など)を再現すること。

## IssueとPull Request投稿上の注意 （重要）

Issue/Pull Requestへ投稿を送ってくれてありがとうございます、大変助かります。

- 投稿内容は**必ず**フランス及び日本の著作権規則に従ってください。

- **重要** ：Issueを提供したら、リリースバージョンと再現方法を述べてください。クラッシュの場合、TJAPlayer3.logの内容をご添付ください。

- CLang言語翻訳を追加して欲しいなら予めDiscordでご連絡ください.

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
Pキーを長押ししてください。
```

- バグを発見しました、どうすればいいですか？

```
バグを発見したらGithubのIssueをご提出ください。
```

- 「太鼓タワー」のメニュに入れません。

```
「太鼓タワー」のメニュはまだ実装されていません。
タワーの譜面をプレイする場合は演奏ゲームの選曲画面から選択してください。
```

- 「Tower_Floors」が見つからないというエラーが出ます！

```
System/Graphicsではなく
System/（スキン名）/Graphicsにフォルダを配置してください。
```

- 「11_Characters」が見つからないというエラーが出ます！

```
v0.5.2から11_Charactersにキャラクターに関してファイルが読み込みされます。

「（スキン名）/Graphics」フォルダーに11_Charactersフォルダーを作って、「（スキン名）/Graphics/11_Characters/（キャラクター数）」にキャラクターファイルをご挿入ください。
キャラクター設定が必要なら「CharaConfig.txt」を変更してください。
キャラクターを使用しないなら「（スキン名）/Graphics/11_Characters/0」を空っぽにしてください。
```

## 更新記録

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

## 早めに実装する予定の機能
```
☐ 段位道場のリザルト画面の完全実装
☐ ドンメダル商店
☐ タワー結果画面の完全実装
☐ 複数の背景と踊り子を追加できる機能を実装
☐ ２P結果画面を実装
☐ プログラムの軽量化、メモリリークの修正
☐ 段位道場で４曲以上を連続でプレイできるようにする
```
## スペシャルサンクス

> * [AkiraChnl/OpenTaiko Icon](https://github.com/AkiraChnl)(@akirach_jp)
> * [cien/OpenTaiko Logo/Various Default Skin Assets](https://twitter.com/CienpixeL)(@CienpixeL)
> * [funnym0th/OpenTaiko Spanish Translation](https://github.com/funnym0th) (@funnym0th)
> * [basketballsmash/English README Translation](https://twitter.com/basketballsmash)(@basketballsmash)
> * [Meowgister/OpenTaiko English Translation](https://www.youtube.com/channel/UCDi5puZaJLMUA6OgIAb7rmQ)
> * [Aioilight/TJAPlayer3](https://github.com/aioilight/TJAPlayer3)(@aioilight)
> * [TwoPointZero/TJAPlayer3](https://github.com/twopointzero/TJAPlayer3)(@twopointzero)
> * [KabanFriends/TJAPlayer3](https://github.com/KabanFriends/TJAPlayer3/tree/features)(@KabanFriends)
> * [Mr-Ojii/TJAPlayer3-f](https://github.com/Mr-Ojii/TJAPlayer3-f)(@Mr-Ojii)
> * [Akasoko/TJAPlayer3](https://github.com/Akasoko-Master/TJAPlayer3)(@AkasokoR)
> * [FROM/DTXMaina](https://github.com/DTXMania)(@DTXMania)
> * [Kairera0467/TJAP2fPC](https://github.com/kairera0467/TJAP2fPC)(@Kairera0467)
> * [touhourenren/TJAPlayer3-Develop-Rewrite](https://github.com/touhourenren)

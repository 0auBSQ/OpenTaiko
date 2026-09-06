<!-- api/audio.md -->

# オーディオ

サウンドの読み込みと再生、およびインストール済みのヒットサウンドセットの読み取り。

## SOUND

音声ファイルからサウンドハンドルを作成します。各ファクトリメソッドはサウンドを音量グループ (効果音、ボイス、楽曲再生、楽曲プレビュー) に割り当て、グループがどのユーザー音量設定を適用するかを決めます。

<div class="callout warn">
相対パスはスクリプトのディレクトリを基準に解決されます。FromAbsolutePath 系は完全なパスを取ります。返されるハンドルは、レンダースレッドが以降のフレームでストリームを構築する間は空です。それまでに行った Play や Set* の呼び出しはハンドルがキューに入れ、ストリームの準備ができた時点で適用します。ゲームはスクリプトのアンロード時にハンドルを破棄します。早めに解放するには Dispose を呼んでください。
</div>

| メソッド | 説明 |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | 効果音を読み込みます。 |
| `SOUND:CreateVoice(path)  -> sound` | ボイスクリップを読み込みます。 |
| `SOUND:CreateBGM(path)  -> sound` | BGM を読み込みます (楽曲再生グループ)。 |
| `SOUND:CreatePreview(path)  -> sound` | 楽曲プレビュークリップを読み込みます (楽曲プレビューグループ)。 |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | 完全なパスから効果音を読み込みます。 |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | 完全なパスからボイスクリップを読み込みます。 |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | 完全なパスから BGM を読み込みます。 |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | 完全なパスから楽曲プレビュークリップを読み込みます。 |

```lua
local bgm, decide

function onStart()
    bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    bgm:SetLoop(true)
    decide = SOUND:CreateSFX("Sounds/Decide.ogg")
end

function activate()
    bgm:Play()
end

function update()
    if INPUT:Pressed("Decide") then decide:Play() end
end

function deactivate()
    bgm:Stop()
end

function onDestroy()
    bgm:Dispose()
    decide:Dispose()
end
```

## サウンドハンドル

`SOUND` ファクトリメソッドが返す再生可能なサウンドです。

<div class="callout warn">
ストリームの準備ができるまで、ハンドルは Play とすべての Set* 呼び出しをバッファし、ゲッターは以下に示す既定値を返します。音量はパーセントです (100 = ファイル本来のレベル)。パンは -100 (左) から 0 (中央) を経て 100 (右) までです。位置と長さはミリ秒です。Play はストリームを先頭から再開するため、シークは Play を呼んだ後に行ってください。
</div>

| メソッド | 説明 |
| --- | --- |
| `sound:Play()  -> nil` | 先頭から再生を開始 (または再開始) します。 |
| `sound:Start()  -> nil` | Play と同じです。 |
| `sound:Pause()  -> nil` | 再生を一時停止します。 |
| `sound:Resume()  -> nil` | Pause の後に再生を再開します。 |
| `sound:Stop()  -> nil` | 再生を停止します。 |
| `sound:Reset()  -> nil` | 先頭にシークします。 |
| `sound:SetLoop(loop)  -> nil` | ループを有効または無効にします。 |
| `sound:GetLoop()  -> bool` | ループが有効かどうかを返します。 |
| `sound:SetVolumePercent(vol)  -> nil` | 音量をパーセントで設定します。 |
| `sound:GetVolumePercent()  -> number` | 音量をパーセントで返します (未読み込み時は 100)。 |
| `sound:SetVolume(vol)  -> nil` | SetVolumePercent の古い名前 (整数)。 |
| `sound:SetPan(pan)  -> nil` | ステレオのパンを設定します (-100..100)。 |
| `sound:GetPan()  -> int` | 現在のパンを返します (未読み込み時は 0)。 |
| `sound:SetSpeed(speed)  -> nil` | 再生速度の倍率を設定します (1 = 通常)。 |
| `sound:GetSpeed()  -> number` | 再生速度の倍率を返します (未読み込み時は 1)。 |
| `sound:SetTimestampMs(ms)  -> nil` | ミリ秒単位の位置にシークします。 |
| `sound:SetTimestamp(ms)  -> nil` | SetTimestampMs の古い名前。 |
| `sound:GetTimestampMs()  -> number` | 現在の位置をミリ秒で返します (未読み込み時は 0)。 |
| `sound.DurationMs  -> number` | 合計時間 (ミリ秒) (未読み込み時は 0)。 |
| `sound:GetDurationMs()  -> number` | DurationMs の古い名前。 |
| `sound.Loaded  -> bool` | レンダースレッドがストリームを構築すると true。 |
| `sound.IsPlaying  -> bool` | サウンドの再生中は true。 |
| `sound:IsFinished()  -> bool` | 再生が末尾に達すると true (一時停止中や未読み込み時は false)。 |
| `sound.Path  -> string` | 読み込んだファイルの完全なパス (読み込まれるまでは空)。 |
| `sound:Dispose()  -> nil` | サウンドを解放します。 |

## HITSOUNDSLIST

現在のスキンにインストールされているヒットサウンドセットの読み取り専用リストです (ゲームは `Global/HitSounds/` からこれらを読み込みます)。

<div class="callout warn">
すべてのスクリプトで利用できます。インデックスは 0 始まりです。GetByName はセットのフォルダ名を大文字小文字を区別せずに照合します。どちらの検索も一致しなければ nil を返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | ヒットサウンドセットの数。 |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | 指定した 0 始まりのインデックスのエントリを返します。なければ nil。 |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | フォルダ名が一致するエントリを返します。なければ nil。 |

```lua
local don

function onStart()
    for i = 0, HITSOUNDSLIST.Count - 1 do
        local hs = HITSOUNDSLIST:GetByIndex(i)
        debugLog(hs.FolderName .. " -> " .. hs.DisplayName)
    end
    local taiko = HITSOUNDSLIST:GetByName("Taiko")
    if taiko then
        don = SOUND:CreateSFXFromAbsolutePath(taiko.DonPath)
    end
end

function onDestroy()
    if don then don:Dispose() end
end
```

## ヒットサウンドエントリ

`HITSOUNDSLIST:GetByIndex` または `GetByName` が返す 1 つのヒットサウンドセットです。すべてのメンバーは読み取り専用です。

| メソッド | 説明 |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | セットのフォルダ名 (例: `"Taiko"`)。 |
| `hitsoundEntry.DisplayName  -> string` | ローカライズされた表示名。フォルダ名にフォールバックします。 |
| `hitsoundEntry.DonPath  -> string` | セットのドン音ファイルの完全なパス。 |
| `hitsoundEntry.KaPath  -> string` | セットのカッ音ファイルの完全なパス。 |

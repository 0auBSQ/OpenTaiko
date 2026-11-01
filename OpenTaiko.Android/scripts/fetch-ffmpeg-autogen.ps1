# Fetch the FFmpeg.AutoGen 5.1.1 binding source (pinned upstream commit) and apply the mobile
# patches (iOS + Android). Windows counterpart of OpenTaiko.iOS/scripts/fetch-ffmpeg-autogen.sh
# (keep both in sync). The vendored project lives under third_party/ at the repo root and is shared
# by both mobile hosts.
$ErrorActionPreference = "Stop"

# The last 5.1.1 commit before the abstractions rewrite; matches the published NuGet 5.1.1.
$Commit = "40873965266b526eeb7982ad45b1e51957eb5411"
$Repo = "https://github.com/Ruslan-B/FFmpeg.AutoGen"

$ThirdParty = Join-Path $PSScriptRoot "..\..\third_party\FFmpeg.AutoGen"
$Dest = Join-Path $ThirdParty "upstream"

if (Test-Path (Join-Path $Dest "FFmpeg.cs")) {
    Write-Host "[fetch-ffmpeg-autogen] upstream/ already present"
    exit 0
}

$Work = Join-Path ([IO.Path]::GetTempPath()) ("ffag-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $Work | Out-Null
try {
    Write-Host "[fetch-ffmpeg-autogen] fetching $Repo @ $Commit ..."
    git -C $Work init -q
    git -C $Work remote add origin $Repo
    git -C $Work fetch -q --depth 1 origin $Commit
    if ($LASTEXITCODE -ne 0) { throw "git fetch failed" }
    git -C $Work checkout -q FETCH_HEAD -- FFmpeg.AutoGen
    if ($LASTEXITCODE -ne 0) { throw "git checkout failed" }

    if (Test-Path $Dest) { Remove-Item -Recurse -Force $Dest }
    New-Item -ItemType Directory -Path $Dest | Out-Null
    Copy-Item -Recurse -Force (Join-Path $Work "FFmpeg.AutoGen\*") $Dest
    Remove-Item -Force (Join-Path $Dest "FFmpeg.AutoGen.csproj")   # replaced by ours

    Push-Location $Dest
    try {
        foreach ($p in @("ios-darwin-fallback.patch", "android-bionic-fallback.patch")) {
            git apply -p1 --ignore-whitespace (Join-Path $ThirdParty $p)
            if ($LASTEXITCODE -ne 0) { throw "failed to apply $p" }
        }
    } finally { Pop-Location }

    Write-Host "[fetch-ffmpeg-autogen] done -> $Dest"
} finally {
    Remove-Item -Recurse -Force $Work -ErrorAction SilentlyContinue
}

# Unified build script for OpenTaiko Android (the Windows counterpart of
# OpenTaiko.iOS/scripts/deploy.sh). Checks the .NET android workload, locates the Android SDK and
# a JDK, fetches missing prerequisites (BASS + FFmpeg natives, vendored FFmpeg.AutoGen source),
# then builds the APK with the required -p:UseVendoredFFmpeg=true.
#
# Usage: powershell -ExecutionPolicy Bypass -File OpenTaiko.Android/scripts/build.ps1 [options]
#
#   -Release          Build Release instead of Debug
#   -Install          Also install on a connected device/emulator (dotnet -t:Install)
#   -Run              Install, then launch the app (implies -Install)
#   -Clean            Wipe OpenTaiko.Android/obj + bin before building
#   -BundleSongs      Pack a song library INTO the APK (as one songs.zip asset, unpacked into
#                     Songs/ on first run). Limited to ~1.8 GB — APKs are 32-bit zips and bigger
#                     ones fail to install; for the full soundtrack use -PushSongs.
#   -PushSongs        After the build (device connected), adb-push the song library into the
#                     app's files/Songs — no APK size limit. On Android 11+ some devices block
#                     shell access to Android/data; emulators and most test devices allow it.
#   -SongsPath PATH   Song folder for -BundleSongs / -PushSongs
#                     (default: ..\..\OpenTaiko-Soundtrack next to the repo)
#   -AndroidSdk PATH  Android SDK root (default: ANDROID_HOME / ANDROID_SDK_ROOT /
#                     %LOCALAPPDATA%\Android\Sdk)
#   -JavaSdk PATH     JDK root, must be JDK 17 (default: Android Studio's bundled JBR,
#                     then Microsoft/Adoptium JDK 17, then JAVA_HOME)
param(
    [switch]$Release,
    [switch]$Install,
    [switch]$Run,
    [switch]$Clean,
    [switch]$BundleSongs,
    [switch]$PushSongs,
    [string]$SongsPath = "",
    [string]$AndroidSdk = "",
    [string]$JavaSdk = ""
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot          # OpenTaiko.Android/
$repo = Split-Path -Parent $root                  # repo root
$csproj = Join-Path $root "OpenTaiko.Android.csproj"
$config = "Debug"; if ($Release) { $config = "Release" }
if ($Run) { $Install = $true }

# ---- .NET android workload ----------------------------------------------------------------
$workloads = & dotnet workload list 2>$null | Out-String
if ($workloads -notmatch "(?m)^\s*android\s") {
    throw "The .NET android workload is not installed. Run: dotnet workload install android"
}

# ---- Android SDK ----------------------------------------------------------------------------
if (-not $AndroidSdk) {
    foreach ($cand in @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, "$env:LOCALAPPDATA\Android\Sdk")) {
        if ($cand -and (Test-Path (Join-Path $cand "platforms"))) { $AndroidSdk = $cand; break }
    }
}
if (-not $AndroidSdk -or -not (Test-Path (Join-Path $AndroidSdk "platforms"))) {
    throw "Android SDK not found. Install it (Android Studio's default works) or pass -AndroidSdk PATH."
}

# ---- JDK (17 required by the android workload) ----------------------------------------------
if (-not $JavaSdk) {
    $cands = @("C:\Program Files\Android\Android Studio\jbr")
    foreach ($pattern in @("C:\Program Files\Microsoft\jdk-17*", "C:\Program Files\Eclipse Adoptium\jdk-17*")) {
        $hit = Get-Item $pattern -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
        if ($hit) { $cands += $hit.FullName }
    }
    if ($env:JAVA_HOME) { $cands += $env:JAVA_HOME }
    foreach ($cand in $cands) {
        if (Test-Path (Join-Path $cand "bin\java.exe")) { $JavaSdk = $cand; break }
    }
}
if (-not $JavaSdk -or -not (Test-Path (Join-Path $JavaSdk "bin\java.exe"))) {
    throw "No JDK found. Install JDK 17 (Android Studio's bundled JBR works) or pass -JavaSdk PATH."
}

Write-Host "Android SDK : $AndroidSdk"
Write-Host "JDK         : $JavaSdk"
Write-Host "Config      : $config"

# ---- prerequisites (skipped when their outputs already exist) --------------------------------
if (-not (Test-Path (Join-Path $root "jniLibs\arm64-v8a\libbass.so"))) {
    Write-Host "BASS natives missing -> download-bass.ps1"
    & (Join-Path $PSScriptRoot "download-bass.ps1")
}
if (-not (Test-Path (Join-Path $root "jniLibs\arm64-v8a\libavcodec.so"))) {
    Write-Host "FFmpeg natives missing -> download-ffmpeg.ps1"
    & (Join-Path $PSScriptRoot "download-ffmpeg.ps1")
}
if (-not (Test-Path (Join-Path $repo "third_party\FFmpeg.AutoGen\upstream\FFmpeg.cs"))) {
    Write-Host "FFmpeg.AutoGen source missing -> fetch-ffmpeg-autogen.ps1"
    & (Join-Path $PSScriptRoot "fetch-ffmpeg-autogen.ps1")
}

# ---- build -----------------------------------------------------------------------------------
if ($Clean) {
    foreach ($d in @("obj", "bin")) {
        $p = Join-Path $root $d
        if (Test-Path $p) { Write-Host "Cleaning $p"; Remove-Item -Recurse -Force $p }
    }
}

$buildArgs = @(
    "build", $csproj,
    "-f", "net8.0-android",
    "-c", $config,
    "-p:UseVendoredFFmpeg=true",
    "-p:AndroidSdkDirectory=$AndroidSdk",
    "-p:JavaSdkDirectory=$JavaSdk"
)
if ($Release) {
    # csc-optimized IL crashes the Mono 8.0.10 interpreter at boot (interp.c:3847 assert, verified
    # on the emulator by bisection: Debug+Optimize=true crashes identically, Release+Optimize=false
    # boots). Global so it reaches FDK/OpenTaiko too; the csproj guards against forgetting it.
    $buildArgs += "-p:Optimize=false"
}

function Resolve-SongsPath {
    $p = if ($SongsPath) { $SongsPath } else { Join-Path $repo "..\OpenTaiko-Soundtrack" }
    if (-not (Test-Path $p)) { throw "Songs folder not found: $p (pass -SongsPath)" }
    (Resolve-Path $p).Path
}

if ($BundleSongs) {
    $sp = Resolve-SongsPath
    # Size check mirrors make-songs-zip.ps1's filter: dot-segments (.git!) don't count.
    $bytes = (Get-ChildItem -Recurse -File $sp | Where-Object {
            $rel = $_.FullName.Substring($sp.Length + 1)
            (($rel -split '[\\/]') | Where-Object { $_.StartsWith(".") }).Count -eq 0
        } | Measure-Object Length -Sum).Sum
    if ($bytes -gt 1800MB) {
        throw ("Songs folder is {0:N1} GB - too big to bundle (APKs are 32-bit zips; past ~2 GB they fail to install). Use -PushSongs, or point -SongsPath at a subset (e.g. one chapter)." -f ($bytes / 1GB))
    }
    Write-Host ("Bundling songs: {0} ({1:N0} MB)" -f $sp, ($bytes / 1MB))
    $buildArgs += "-p:BundleSongs=true"
    $buildArgs += "-p:BundleSongsPath=$sp"
}
if ($Install) { $buildArgs += "-t:Install" }

& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }

$apk = Get-ChildItem (Join-Path $root "bin\$config\net8.0-android") -Filter "*-Signed.apk" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($apk) {
    Write-Host ("APK: {0} ({1:N0} MB)" -f $apk.FullName, ($apk.Length / 1MB))
}

# ---- songs over adb (no APK size limit) --------------------------------------------------------
if ($PushSongs) {
    $adb = Join-Path $AndroidSdk "platform-tools\adb.exe"
    if (-not (Test-Path $adb)) { throw "adb not found under the Android SDK (platform-tools missing)." }
    $sp = Resolve-SongsPath
    $dst = "/storage/emulated/0/Android/data/com.opentaiko.OpenTaiko/files/Songs/"
    Write-Host "Pushing songs from $sp (existing files are overwritten by adb)..."
    # Per top-level entry so dot-folders (.git!) never travel; adb push has no exclude option.
    Get-ChildItem -LiteralPath $sp | Where-Object { -not $_.Name.StartsWith(".") } | ForEach-Object {
        & $adb push $_.FullName $dst
        if ($LASTEXITCODE -ne 0) { throw "adb push failed on '$($_.Name)' - is a device connected? (On Android 11+ some devices block shell writes to Android/data.)" }
    }
}

# ---- launch ----------------------------------------------------------------------------------
if ($Run) {
    $adb = Join-Path $AndroidSdk "platform-tools\adb.exe"
    if (-not (Test-Path $adb)) { throw "adb not found under the Android SDK (platform-tools missing)." }
    Write-Host "Launching com.opentaiko.OpenTaiko..."
    & $adb shell monkey -p com.opentaiko.OpenTaiko -c android.intent.category.LAUNCHER 1 | Out-Null
    Write-Host "Logs: adb logcat -s OpenTaiko AndroidRuntime mono-stdout"
}

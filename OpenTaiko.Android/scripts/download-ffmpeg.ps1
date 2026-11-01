# Downloads the FFmpeg 5.1.2 shared libraries for Android (Bytedeco/JavaCPP builds, LGPL base
# artifact, ABI-compatible with the FFmpeg.AutoGen 5.1.1 binding) from Maven Central and unpacks
# the per-ABI shared objects into jniLibs/.
# Only the libraries the game decodes with are extracted: avformat/avcodec/avutil/swscale +
# swresample (avcodec dependency). Their SONAMEs and DT_NEEDED entries are unversioned and only
# reference NDK-public system libs (libm/libc/libdl), so they drop straight into an APK.
# Run once before building: powershell -ExecutionPolicy Bypass -File scripts/download-ffmpeg.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$tmp = Join-Path $env:TEMP "opentaiko-ffmpeg-android"
New-Item -ItemType Directory -Force $tmp | Out-Null

$version = "5.1.2-1.5.8"
$base = "https://repo1.maven.org/maven2/org/bytedeco/ffmpeg/$version"
# Maven classifier -> jniLibs ABI folder
$abis = @(
    @{ classifier = "android-arm64";  abi = "arm64-v8a" },
    @{ classifier = "android-x86_64"; abi = "x86_64" }
)
$wanted = @("libavutil.so", "libswresample.so", "libavcodec.so", "libavformat.so", "libswscale.so")

Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($entry in $abis) {
    $jar = Join-Path $tmp "ffmpeg-$version-$($entry.classifier).jar"
    if (-not (Test-Path $jar)) {
        Write-Host "Downloading ffmpeg $version $($entry.classifier)..."
        Invoke-WebRequest "$base/ffmpeg-$version-$($entry.classifier).jar" -OutFile $jar
    }
    $libDir = Join-Path $root "jniLibs\$($entry.abi)"
    New-Item -ItemType Directory -Force $libDir | Out-Null

    $zip = [System.IO.Compression.ZipFile]::OpenRead($jar)
    try {
        foreach ($name in $wanted) {
            $found = $zip.Entries | Where-Object { $_.Name -eq $name } | Select-Object -First 1
            if ($null -eq $found) { throw "$name not found in $jar" }
            $out = Join-Path $libDir $name
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($found, $out, $true)
            Write-Host "  $($entry.abi)/$name"
        }
    } finally { $zip.Dispose() }
}
Write-Host "Done. jniLibs/ now holds the FFmpeg natives."

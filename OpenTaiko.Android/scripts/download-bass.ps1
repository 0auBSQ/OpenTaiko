# Downloads the BASS audio libraries for Android from un4seen.com and unpacks the per-ABI
# shared objects into jniLibs/ (the counterpart of OpenTaiko.iOS/scripts/download-bass.sh).
# Run once before building: powershell -ExecutionPolicy Bypass -File scripts/download-bass.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$tmp = Join-Path $env:TEMP "opentaiko-bass-android"
New-Item -ItemType Directory -Force $tmp | Out-Null

# bass_fx is a third-party addon (Jobnik) hosted under files/z/0/ on un4seen.com
$packages = @(
    @{ name = "bass24-android";    url = "https://www.un4seen.com/files/bass24-android.zip" },
    @{ name = "bassmix24-android"; url = "https://www.un4seen.com/files/bassmix24-android.zip" },
    @{ name = "bass_fx24-android"; url = "https://www.un4seen.com/files/z/0/bass_fx24-android.zip" }
)
$abis = @("arm64-v8a", "x86_64")

foreach ($entry in $packages) {
    $pkg = $entry.name
    $zip = Join-Path $tmp "$pkg.zip"
    if (-not (Test-Path $zip)) {
        Write-Host "Downloading $pkg..."
        Invoke-WebRequest $entry.url -OutFile $zip
    }
    $dst = Join-Path $tmp $pkg
    if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
    Expand-Archive $zip -DestinationPath $dst
    foreach ($abi in $abis) {
        $libDir = Join-Path $root "jniLibs\$abi"
        New-Item -ItemType Directory -Force $libDir | Out-Null
        Get-ChildItem -Recurse (Join-Path $dst "libs\$abi") -Filter "*.so" -ErrorAction SilentlyContinue |
            ForEach-Object { Copy-Item $_.FullName $libDir -Force; Write-Host "  $abi/$($_.Name)" }
    }
}
Write-Host "Done. jniLibs/ now holds the BASS natives."

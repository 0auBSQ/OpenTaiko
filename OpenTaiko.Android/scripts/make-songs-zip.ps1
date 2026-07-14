# Zip a song library folder for APK bundling (called by the _BundleSongsZip msbuild target).
# Unlike MSBuild's ZipDirectory this FILTERS: any path segment starting with "." is skipped —
# song folders checked out from git would otherwise drag the whole .git object store into the
# APK — plus Windows junk files. Audio barely deflates, so Fastest keeps GB-scale zips quick.
# Usage: powershell -File make-songs-zip.ps1 <sourceFolder> <destZip>
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Destination
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$Source = (Resolve-Path $Source).Path.TrimEnd('\')
$junk = @("thumbs.db", "desktop.ini")

if (Test-Path $Destination) { Remove-Item -Force $Destination }
$dir = Split-Path -Parent $Destination
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force $dir | Out-Null }

$zip = [System.IO.Compression.ZipFile]::Open($Destination, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $count = 0
    Get-ChildItem -Recurse -File -LiteralPath $Source | ForEach-Object {
        $rel = $_.FullName.Substring($Source.Length + 1)
        $segments = $rel -split '[\\/]'
        if (($segments | Where-Object { $_.StartsWith(".") }).Count -gt 0) { return }
        if ($junk -contains $_.Name.ToLowerInvariant()) { return }
        $entry = $rel -replace '\\', '/'
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip, $_.FullName, $entry, [System.IO.Compression.CompressionLevel]::Fastest) | Out-Null
        $count++
    }
    Write-Host "make-songs-zip: $count files -> $Destination"
    if ($count -eq 0) { throw "no files matched under $Source" }
} finally {
    $zip.Dispose()
}

@echo off
setlocal enabledelayedexpansion

if defined ProgramFiles(x86) (
    set VSWHERE="!ProgramFiles(x86)!\Microsoft Visual Studio\Installer\vswhere.exe"
) else (
    set VSWHERE="!ProgramFiles!\Microsoft Visual Studio\Installer\vswhere.exe"
)

if exist !VSWHERE! (
    for /f "usebackq delims=" %%i in (`!VSWHERE! -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        set MSBUILD="%%i"
    )

    if exist !MSBUILD! (
        !MSBUILD! TJAPlayer3.sln /t:build -restore /p:configuration=release /p:platform=x64
    ) else (
        echo MSBuild is not installed. Please read: https://github.com/l1m0n3/OpenTaiko/wiki/How-to-build-OpenTaiko-without-using-Visual-Studio-^(on-Windows^)
    )
) else (
    echo Visual Studio Installer is not installed. Please read: https://github.com/l1m0n3/OpenTaiko/wiki/How-to-build-OpenTaiko-without-using-Visual-Studio-^(on-Windows^)
)

pause

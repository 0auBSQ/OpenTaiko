#!/bin/bash

# OpenTaiko Installation Script - Linux version
# Does not require OpenTaiko Hub

# Check system compatibility BEFORE doing anything
echo "Checking system compatibility..."

# Detect session type
session_type="$XDG_SESSION_TYPE"
if [ -z "$session_type" ]; then
    session_type=$(loginctl show-session $(loginctl | grep $(whoami) | awk '{print $1}') -p Type --value 2>/dev/null || echo "unknown")
fi

# Detect desktop environment (only check for known working ones)
desktop_env="$XDG_CURRENT_DESKTOP"
if [ -z "$desktop_env" ]; then
    if [ -n "$KDE_SESSION_VERSION" ] || [ -n "$KDE_FULL_SESSION" ]; then
        desktop_env="KDE"
    elif pgrep -x "sway" > /dev/null; then
        desktop_env="sway"
    elif [ -n "$I3SOCK" ] || pgrep -x "i3" > /dev/null; then
        desktop_env="i3"
    elif [ -n "$GNOME_DESKTOP_SESSION_ID" ] || pgrep -x "gnome-session" > /dev/null; then
        desktop_env="GNOME"

    else
        desktop_env="unknown"
    fi
fi

# Detect GPU driver
gpu_driver="unknown"
if lspci | grep -i vga | grep -i amd > /dev/null 2>&1 || \
   lsmod | grep amdgpu > /dev/null 2>&1; then
    gpu_driver="amdgpu"
elif lspci | grep -i vga | grep -i nvidia > /dev/null 2>&1; then
    if lsmod | grep nvidia > /dev/null 2>&1; then
        gpu_driver="nvidia"
    else
        gpu_driver="nvidia-not-loaded"
    fi
elif lspci | grep -i vga | grep -i intel > /dev/null 2>&1; then
    gpu_driver="intel"
fi

echo "Detected configuration:"
echo "Session: $session_type"
echo "Desktop: $desktop_env"
echo "GPU Driver: $gpu_driver"

# Provide compatibility warnings/confirmations
echo ""

# Known good configurations
if [ "$session_type" = "x11" ]; then
    echo "GOOD: X11 session - Should work well with OpenTaiko!"
elif [ "$gpu_driver" = "amdgpu" ] && [ "$desktop_env" = "sway" ] && [ "$session_type" = "wayland" ]; then
    echo "GOOD: AMD GPU + Sway + Wayland - Known working configuration!"
elif [ "$gpu_driver" = "intel" ] && [ "$desktop_env" = "sway" ] && [ "$session_type" = "wayland" ]; then
    echo "GOOD: Intel GPU + Sway + Wayland - Should work well!"
elif [ "$gpu_driver" = "intel" ] && [[ "$desktop_env" =~ GNOME|gnome ]]; then
    echo "GOOD: Intel GPU + GNOME - Should work well with OpenGL setting!"


# Known problematic configurations
elif [[ "$desktop_env" =~ KDE|kde ]] && [ "$session_type" = "wayland" ]; then
    echo "WARNING: KDE + Wayland detected - OpenTaiko may not work properly!"
    echo "- Recommendation: Switch to X11 session for KDE"
elif [ "$gpu_driver" = "nvidia" ] && [ "$session_type" = "wayland" ]; then
    echo "WARNING: NVIDIA + Wayland - May have compatibility issues!"
    echo "- Recommendation: Try X11 session or consider using Nouveau driver"

# Everything else is untested/unknown
else
    echo "ℹ Your configuration is untested but may work:"
    echo "If you experience issues, try:"
    echo "- Switching between X11 and Wayland sessions"
    echo "- Using a known working desktop environment (Sway for Wayland, any for X11)"
    echo "- Check OpenTaiko logs for graphics-related errors"
fi

# Always ask user to continue
echo ""
while true; do
    read -rp "Do you want to continue with the installation? (Y/N): " yn
    case $yn in
        [Yy]* ) break;;
        [Nn]* ) echo "Installation cancelled."; exit 1;;
        * ) echo "Please answer Y or N.";;
    esac
done

echo ""
echo "Starting OpenTaiko installation..."


# Installation

current_dir=$(pwd)

# Constants
cache_dir="$current_dir/cache"
desktop_folder="$HOME/.local/share/applications"
archive_filename="OpenTaiko.Linux.x64.zip"
installation_folder="$current_dir/OpenTaiko"
git_repo="0auBSQ/OpenTaiko"
default_config_file=$installation_folder/publish/Config.ini

# Create cache directory
mkdir -p "$cache_dir"

# Get latest release tag via GitHub API
echo "Fetching latest release tag for $git_repo..."
latest_tag=$(curl -s "https://api.github.com/repos/$git_repo/releases/latest" | jq -r '.tag_name')

if [ -z "$latest_tag" ] || [ "$latest_tag" = "null" ]; then
    echo "Failed to fetch the latest version tag."
    exit 1
fi

echo "Latest version: $latest_tag"

# Create and move to installation_folder
mkdir -p "$installation_folder"
cd "$installation_folder" || exit 1

# Check if we already have this version cached
cached_file="$cache_dir/$latest_tag-$archive_filename"
if [ -f "$cached_file" ]; then
    echo "Using cached version: $cached_file"
    cp "$cached_file" "$archive_filename"
else
    # Download using curl with redirect support
    download_url="https://github.com/$git_repo/releases/download/$latest_tag/$archive_filename"
    echo "Downloading from: $download_url"
    curl -L -o "$archive_filename" "$download_url" || { echo "Download failed."; exit 1; }
    
    # Cache the download
    echo "Caching download for future use..."
    cp "$archive_filename" "$cached_file"
fi

echo "Unzipping..."
unzip -o "$archive_filename"

echo "Cleaning up zip archive..."
rm -f "$archive_filename"

echo "Done. Extracted to $installation_folder"

# Navigate to publish
cd "publish" || exit 1

# === Update/clone soundtrack repository ===
cached_soundtrack="$cache_dir/OpenTaiko-Soundtrack"
if [ -d "$cached_soundtrack" ]; then
    echo "Updating cached soundtrack repository..."
    cd "$cached_soundtrack" || exit 1
    git pull
    cd - || exit 1
else
    echo "Cloning soundtrack repository to cache..."
    git clone https://github.com/OpenTaiko/OpenTaiko-Soundtrack "$cached_soundtrack"
fi

# Merge soundtrack into publish/Songs
mkdir -p "Songs"
cp -rf "$cached_soundtrack/"* "Songs/"
echo "Soundtrack merged into Songs/"

# === Update/clone skins repository ===
cached_skins="$cache_dir/OpenTaiko-Skins"
if [ -d "$cached_skins" ]; then
    echo "Updating cached skins repository..."
    cd "$cached_skins" || exit 1
    git pull
    cd - || exit 1
else
    echo "Cloning skins repository to cache..."
    git clone https://github.com/OpenTaiko/OpenTaiko-Skins "$cached_skins"
fi

# Merge skins directly into publish
cp -rf "$cached_skins/"* "./"
echo "Skins merged into publish/"

# Make OpenTaiko binary executable
chmod +x "OpenTaiko"
echo "OpenTaiko binary is now executable."

# Done
echo "OpenTaiko setup complete in $installation_folder"

# Create launcher script
launcher="$installation_folder/start_opentaiko.sh"
cat << EOF > "$launcher"
#!/bin/bash
echo "Session type: \$XDG_SESSION_TYPE"
export DIR="$installation_folder"
env -C "\$DIR/publish" "\$DIR/publish/OpenTaiko"
EOF
chmod +x "$launcher"

# Create desktop entry for launching
mkdir -p "$desktop_folder"
cat << EOF > "$desktop_folder/start_opentaiko.desktop"
[Desktop Entry]
Name=Start OpenTaiko
Exec=$installation_folder/start_opentaiko.sh
Path=$installation_folder
Icon=$installation_folder/publish/OpenTaiko.ico
Terminal=false
Type=Application
Categories=Game;
EOF

# Create update script
update_script="$installation_folder/update_opentaiko.sh"
cat << EOF > "$update_script"
#!/bin/bash

# Check if OpenTaiko is running and wait for it to close
if pgrep -x "OpenTaiko" > /dev/null; then
    echo "Warning: OpenTaiko is currently running."
    echo "Please close OpenTaiko before continuing with the update."
    echo ""
    echo "Waiting for OpenTaiko to close..."
    
    while pgrep -x "OpenTaiko" > /dev/null; do
        echo -n "."
        sleep 1
    done
    
    echo ""
    echo "OpenTaiko has been closed. Proceeding with update..."
    sleep 1
fi

echo "Updating OpenTaiko repositories..."

# Update soundtrack repository
cached_soundtrack="$cache_dir/OpenTaiko-Soundtrack"
if [ -d "\$cached_soundtrack" ]; then
    echo "Updating soundtrack repository..."
    cd "\$cached_soundtrack" || exit 1
    git pull
    cd - || exit 1
    
    # Copy updated soundtrack to Songs
    cd "$installation_folder/publish" || exit 1
    cp -rf "\$cached_soundtrack/"* "Songs/"
    echo "Soundtrack updated."
else
    echo "Soundtrack cache not found. Please run the main installer first."
fi

# Update skins repository
cached_skins="$cache_dir/OpenTaiko-Skins"
if [ -d "\$cached_skins" ]; then
    echo "Updating skins repository..."
    cd "\$cached_skins" || exit 1
    git pull
    cd - || exit 1
    
    # Copy updated skins to publish
    cd "$installation_folder/publish" || exit 1
    cp -rf "\$cached_skins/"* "./"
    echo "Skins updated."
else
    echo "Skins cache not found. Please run the main installer first."
fi

echo "Update complete!"
echo "You can now launch OpenTaiko with the updated content."

# Optional: Check for new OpenTaiko releases
echo ""
echo "Checking for new OpenTaiko releases..."
latest_tag=\$(curl -s "https://api.github.com/repos/$git_repo/releases/latest" | jq -r '.tag_name')
if [ -n "\$latest_tag" ] && [ "\$latest_tag" != "null" ]; then
    echo "Latest OpenTaiko version available: \$latest_tag"
    echo "If you want to update the main application, please run the installer script again."
else
    echo "Could not check for new releases."
fi

echo ""
echo "Window will close in 10 seconds, or press any key to close now..."
read -n 1 -s -t 10

EOF
chmod +x "$update_script"

# Create desktop entry for updating
cat << EOF > "$desktop_folder/update_opentaiko.desktop"
[Desktop Entry]
Name=Update OpenTaiko
Exec=$installation_folder/update_opentaiko.sh
Path=$installation_folder
Icon=$installation_folder/publish/OpenTaiko.ico
Terminal=true
Type=Application
Categories=Game;
EOF

# Make sure we're in the right directory and write the config file
cd "$installation_folder/publish" || exit 1

# Write the default Config.ini file
echo "Writing default Config.ini..."
cat << EOF > "$default_config_file"
[System]
Version=0.6.0.81
TJAPath=Songs/
SkinPath=Open-World Memories/
PreAssetsLoading=1
FastRender=1
ASyncTextureLoad=1
SimpleMode=0
Lang=en
LayoutType=1
SaveFileName=1P,2P,3P,4P,5P
IgnoreSongUnlockables=0
# 0 = OpenGL, 1=DirectX11, 2=Vulkan, 3=Metal
GraphicsDeviceType=0
FullScreen=0
WindowWidth=1280
WindowHeight=720
WindowX=100
WindowY=100
DoubleClickFullScreen=0
EnableSystemMenu=1
BackSleep=1
FontName=
BoxFontName=
VSyncWait=1
SleepTimePerFrame=-1
SoundDeviceType=0
BassBufferSizeMs=1
WASAPIBufferSizeMs=50
ASIODevice=0
SoundTimerType=1
BGAlpha=100
AVI=0
BGA=1
ClipDispType=1
PreviewSoundWait=1000
PreviewImageWait=100
BGMSound=1
DanTowerHide=0
MinComboDrums=10
RandomFromSubBox=1
ShowDebugStatus=0
ApplyLoudnessMetadata=1
TargetLoudness=-7.4
ApplySongVol=0
SoundEffectLevel=80
VoiceLevel=90
SongPreviewLevel=90
SongPlaybackLevel=85
KeyboardSoundLevelIncrement=5
MusicPreTimeMs=1000
BufferedInput=1
ControllerDeadzone=50
AutoResultCapture=0
SendDiscordPlayingInformation=1
TimeStretch=0
DirectShowMode=0
GlobalOffset=0
TJAP3FolderMode=0
EndingAnime=0

[AutoPlay]
Taiko=0
Taiko2P=0
Taiko3P=0
Taiko4P=0
Taiko5P=0
TaikoAutoRoll=1
RollsPerSec=15
DefaultAILevel=4

[Log]
OutputLog=1
TraceSongSearch=0
TraceCreatedDisposed=0
TraceDTXDetails=0

[PlayOption]
ShowChara=1
ShowDancer=1
ShowRunner=1
ShowMob=1
ShowFooter=1
ShowPuchiChara=1
EnableCountDownTimer=1
DrumsReverse=0
Risky=0
DrumsTight=0
DrumsScrollSpeed1P=9
DrumsScrollSpeed2P=9
DrumsScrollSpeed3P=9
DrumsScrollSpeed4P=9
DrumsScrollSpeed5P=9
TimingZones1P=2
TimingZones2P=2
TimingZones3P=2
TimingZones4P=2
TimingZones5P=2
Gametype1P=0
Gametype2P=0
Gametype3P=0
Gametype4P=0
Gametype5P=0
FunMods1P=0
FunMods2P=0
FunMods3P=0
FunMods4P=0
FunMods5P=0
PlaySpeed=20
PlaySpeedNotEqualOneNoSound=0
DefaultCourse=1
ScoreMode=2
ShinuchiMode=1
BigNotesWaitTime=50
BigNotesJudge=0
ForceNormalGauge=0
NoInfo=0
BranchAnime=1
DefaultSongSort=2
RecentlyPlayedMax=5
TaikoRandom1P=0
TaikoRandom2P=0
TaikoRandom3P=0
TaikoRandom4P=0
TaikoRandom5P=0
TaikoStealth1P=0
TaikoStealth2P=0
TaikoStealth3P=0
TaikoStealth4P=0
TaikoStealth5P=0
GameMode=0
TokkunSkipMeasures=0
TokkunMashInterval=750
Just1P=0
Just2P=0
Just3P=0
Just4P=0
HitSounds1P=0
HitSounds2P=0
HitSounds3P=0
HitSounds4P=0
JudgeCountDisplay=0
ShowExExtraAnime=1
PlayerCount=1

[GUID]

[DrumsKeyAssign]
LeftRed=K015
RightRed=K019
LeftBlue=K013
RightBlue=K020
LeftRed2P=K011
RightRed2P=K023
LeftBlue2P=K012
RightBlue2P=K047
LeftRed3P=
RightRed3P=
LeftBlue3P=
RightBlue3P=
LeftRed4P=
RightRed4P=
LeftBlue4P=
RightBlue4P=
LeftRed5P=
RightRed5P=
LeftBlue5P=
RightBlue5P=
Clap=K017
Clap2P=
Clap3P=
Clap4P=
Clap5P=
Decide=K015,K019
Cancel=
LeftChange=K013
RightChange=K020

[SystemKeyAssign]
Capture=K065
SongVolumeIncrease=K074
SongVolumeDecrease=K0115
DisplayHits=K057
DisplayDebug=K049
QuickConfig=K055
NewHeya=K062
SortSongs=K0126
ToggleAutoP1=K056
ToggleAutoP2=K057
ToggleTrainingMode=K060
CycleVideoDisplayMode=K058

[TrainingKeyAssign]
TrainingIncreaseScrollSpeed=K0132
TrainingDecreaseScrollSpeed=K050
TrainingIncreaseSongSpeed=K047
TrainingDecreaseSongSpeed=K012
TrainingToggleAuto=K059
TrainingBranchNormal=K01
TrainingBranchExpert=K02
TrainingBranchMaster=K03
TrainingPause=K0126,K019
TrainingBookmark=K010
TrainingMoveForwardMeasure=K0118,K020
TrainingMoveBackMeasure=K076,K013
TrainingSkipForwardMeasure=K0109
TrainingSkipBackMeasure=K0108
TrainingJumpToFirstMeasure=K070
TrainingJumpToLastMeasure=K051

[DEBUG]
ImGui=1
EOF

echo "Config.ini created successfully."

echo "OpenTaiko installation complete. You can now launch it from your applications menu."
echo "You can also update the soundtrack and skins from your applications menu."
echo ""
echo "Available commands:"
echo "  Launch: $launcher"
echo "  Update: $update_script"

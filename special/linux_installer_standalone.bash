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

echo "OpenTaiko installation complete. You can now launch it from your applications menu."
echo "You can also update the soundtrack and skins from your applications menu."
echo ""
echo "Available commands:"
echo "  Launch: $launcher"
echo "  Update: $update_script"

#!/bin/bash

# OpenTaiko Installation Script - Linux version
# Does not require OpenTaiko Hub

echo "Starting OpenTaiko installation..."

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
rm -f "$archive_filename"

# Navigate to publish
cd "publish" || exit 1

# === Update/clone soundtrack repository ===
cached_soundtrack="$cache_dir/OpenTaiko-Soundtrack"
if [ -d "$cached_soundtrack" ]; then
    echo "Updating cached soundtrack repository..."
    cd "$cached_soundtrack" && git pull && cd - || exit 1
else
    echo "Cloning soundtrack repository to cache..."
    git clone https://github.com/OpenTaiko/OpenTaiko-Soundtrack "$cached_soundtrack"
fi

# Merge soundtrack into publish/Songs
mkdir -p "Songs"
cp -rf "$cached_soundtrack/"* "Songs/"

# === Update/clone skins repository ===
cached_skins="$cache_dir/OpenTaiko-Skins"
if [ -d "$cached_skins" ]; then
    echo "Updating cached skins repository..."
    cd "$cached_skins" && git pull && cd - || exit 1
else
    echo "Cloning skins repository to cache..."
    git clone https://github.com/OpenTaiko/OpenTaiko-Skins "$cached_skins"
fi

# Merge skins directly into publish
cp -rf "$cached_skins/"* "./"

# Make OpenTaiko binary executable
chmod +x "OpenTaiko"

# Create launcher script with YOUR custom Vulkan/Environment logic
launcher="$installation_folder/start_opentaiko.sh"
cat << EOF > "$launcher"
#!/bin/bash
# 1. Clear environment to let the game choose its backend
unset SDL_VIDEODRIVER
unset StoreValue

# 2. Set essential paths and force Vulkan
export DIR="$installation_folder/publish"
export LD_LIBRARY_PATH="\$DIR:\$LD_LIBRARY_PATH"
export ANGLE_DEFAULT_PLATFORM=vulkan

# 3. Launch from the correct directory
cd "\$DIR"
./OpenTaiko
EOF
chmod +x "$launcher"

# Create desktop entry for launching
mkdir -p "$desktop_folder"
cat << EOF > "$desktop_folder/start_opentaiko.desktop"
[Desktop Entry]
Name=Start OpenTaiko
Exec=$installation_folder/start_opentaiko.sh
Path=$installation_folder/publish
Icon=$installation_folder/publish/OpenTaiko.ico
Terminal=false
Type=Application
Categories=Game;
EOF

# Create update script
update_script="$installation_folder/update_opentaiko.sh"
cat << EOF > "$update_script"
#!/bin/bash
if pgrep -x "OpenTaiko" > /dev/null; then
    echo "Waiting for OpenTaiko to close..."
    while pgrep -x "OpenTaiko" > /dev/null; do sleep 1; done
fi

echo "Updating OpenTaiko repositories..."
cd "$cache_dir/OpenTaiko-Soundtrack" && git pull
cp -rf "$cache_dir/OpenTaiko-Soundtrack/"* "$installation_folder/publish/Songs/"
cd "$cache_dir/OpenTaiko-Skins" && git pull
cp -rf "$cache_dir/OpenTaiko-Skins/"* "$installation_folder/publish/"

echo "Update complete!"
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

echo "OpenTaiko installation complete!"
echo "Your custom Vulkan launcher has been created at: $launcher"
echo "You can launch your game either from that file or use an standard application launcher"


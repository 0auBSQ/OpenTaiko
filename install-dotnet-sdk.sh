#!/bin/bash
set -e
source /etc/os-release

if [[ "$ID" == "ubuntu" || "$ID_LIKE" == *"ubuntu"* ]]; then
    sudo apt-get update
    if ! sudo apt-get install -y dotnet-sdk-8.0; then
        echo "The package was not found by your package manager, setting up alternate repository."
        
        MAJOR_VERSION=$(echo "$VERSION_ID" | cut -d. -f1)
        
        if [[ "$MAJOR_VERSION" -ge 26 ]]; then
            echo "Adding dotnet backports PPA..."
            sudo add-apt-repository ppa:dotnet/backports -y
        else
            echo "Adding microsoft repository..."
            wget "https://packages.microsoft.com/config/ubuntu/${VERSION_ID}/packages-microsoft-prod.deb" -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
        fi
        sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
    fi
elif [[ "$ID" == "fedora" || "$ID_LIKE" == *"fedora"* || "$ID_LIKE" == *"rhel"* ]]; then
    sudo dnf install -y dotnet-sdk-8.0
elif [[ "$ID" == "arch" || "$ID_LIKE" == *"arch"* ]]; then
    sudo pacman -S --noconfirm dotnet-sdk
else
  echo "Your OS is not supported by the script: ${ID}"
fi

#!/bin/bash
set -e
source /etc/os-release

if [[ "$ID" == "ubuntu" || "$ID_LIKE" == *"ubuntu"* ]]; then
    sudo apt-get update
    if ! sudo apt-get install -y dotnet-sdk-10.0; then
        # Ubuntu 22.04 and 24.04 get .NET 10 from the dotnet backports PPA, not from Microsoft's package repository
        echo "The package was not found by your package manager, adding the dotnet backports PPA..."
        sudo apt-get install -y software-properties-common
        sudo add-apt-repository ppa:dotnet/backports -y
        sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
    fi
elif [[ "$ID" == "debian" ]]; then
    # Debian 12 and 13 get .NET 10 from Microsoft's package repository
    sudo apt-get update && sudo apt-get install -y wget
    wget "https://packages.microsoft.com/config/debian/${VERSION_ID}/packages-microsoft-prod.deb" -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
elif [[ "$ID" == "fedora" || "$ID_LIKE" == *"fedora"* || "$ID_LIKE" == *"rhel"* ]]; then
    sudo dnf install -y dotnet-sdk-10.0
elif [[ "$ID" == "arch" || "$ID_LIKE" == *"arch"* ]]; then
    sudo pacman -S --noconfirm dotnet-sdk
else
  echo "Your OS is not supported by the script: ${ID}"
fi

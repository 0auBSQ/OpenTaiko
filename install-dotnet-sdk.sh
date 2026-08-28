#!/bin/bash
set -e
source /etc/os-release

if [[ "$ID" == "ubuntu" || "$ID_LIKE" == *"ubuntu"* ]]; then
    if [[ "$ID" == "ubuntu" ]]; then
        case "$VERSION_ID" in
            "22.04"|"24.04"|"25.04"|"25.10"|"26.04")
                ;;
            *)
                echo "Your OS is not supported by dotnet: Ubuntu ${VERSION_ID}"
                exit 1
                ;;
        esac
    fi

    sudo apt-get update
    if ! sudo apt-get install -y dotnet-sdk-8.0; then
        if [[ "$ID" == "ubuntu" ]]; then
            if [[ "$VERSION_ID" == "26.04" ]]; then
                echo "Adding dotnet backports PPA..."
                sudo add-apt-repository ppa:dotnet/backports -y
                sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
            elif [[ "$VERSION_ID" == "22.04" ]]; then
                echo "Adding microsoft repository..."
                wget "https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb" -O packages-microsoft-prod.deb
                sudo dpkg -i packages-microsoft-prod.deb
                rm packages-microsoft-prod.deb
                sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
            else
                echo "The dotnet-sdk-8.0 package was not found natively and no alternate repository is available."
                exit 1
            fi
        else
            echo "The dotnet-sdk-8.0 package was not found natively and no alternate repository is available."
            exit 1
        fi
    fi
elif [[ "$ID" == "fedora" || "$ID_LIKE" == *"fedora"* || "$ID_LIKE" == *"rhel"* ]]; then
    MAJOR_VERSION=$(echo "$VERSION_ID" | cut -d. -f1)
    if [[ "$ID" == "fedora" && "$MAJOR_VERSION" -le 42 ]]; then
        echo "Your OS is not supported by dotnet: Fedora ${MAJOR_VERSION}"
        exit 1
    fi
    sudo dnf install -y dotnet-sdk-8.0
elif [[ "$ID" == "debian" || "$ID_LIKE" == *"debian"* ]]; then
    sudo apt-get update
    if ! sudo apt-get install -y dotnet-sdk-8.0; then
        echo "The package was not found by your package manager, setting up alternate repository."
        
        MAJOR_VERSION=$(echo "$VERSION_ID" | cut -d. -f1)
        
        if [[ "$MAJOR_VERSION" == "12" || "$MAJOR_VERSION" == "13" ]]; then
            echo "Adding microsoft repository..."
            wget "https://packages.microsoft.com/config/debian/${MAJOR_VERSION}/packages-microsoft-prod.deb" -O packages-microsoft-prod.deb
            sudo dpkg -i packages-microsoft-prod.deb
            rm packages-microsoft-prod.deb
            sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
        else
            echo "Your OS is not supported by dotnet: Debian ${MAJOR_VERSION}"
        fi
    fi
elif [[ "$ID" == "arch" || "$ID_LIKE" == *"arch"* ]]; then
    sudo pacman -S --noconfirm dotnet-sdk-8.0
else
  echo "Your OS is not supported by dotnet: ${ID}"
fi

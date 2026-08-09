
os=$(cat /etc/os-release | grep -w 'ID')

if [[ "$os" == "ID=ubuntu" ]]; then

    wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
else
    echo "System not supported"
fi

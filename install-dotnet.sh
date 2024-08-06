#!/bin/bash
set -euo pipefail

solutionPath=$(dirname "$0")
sdkFile="$solutionPath/global.json"

dotnetVersion=$(grep '"sdk": {' "$sdkFile" -A 5 | grep '"version":' | sed -E 's/.*"version": "(.*)".*/\1/')

installDotNetSdk=false

if ! command -v dotnet &> /dev/null; then
    echo "The .NET SDK is not installed."
    installDotNetSdk=true
else
    installedDotNetVersion=$(dotnet --version 2>&1 || echo "?")
    if [ "$installedDotNetVersion" != "$dotnetVersion" ]; then
        echo "The required version of the .NET SDK is not installed. Expected $dotnetVersion."
        installDotNetSdk=true
    fi
fi

if [ "$installDotNetSdk" = true ]; then
    DOTNET_INSTALL_DIR="$solutionPath/.dotnet"
    sdkPath="$DOTNET_INSTALL_DIR/sdk/$dotnetVersion"

    if [ ! -d "$sdkPath" ]; then
        mkdir -p "$DOTNET_INSTALL_DIR"
        if [ "$(uname)" != "Darwin" ] && [ "$(uname)" != "Linux" ]; then
            installScript="$DOTNET_INSTALL_DIR/install.ps1"
            curl -sSL https://dot.net/v1/dotnet-install.ps1 -o "$installScript"
            pwsh "$installScript" -Version "$dotnetVersion" -InstallDir "$DOTNET_INSTALL_DIR" -NoPath -SkipNonVersionedFiles
        else
            installScript="$DOTNET_INSTALL_DIR/install.sh"
            curl -sSL https://dot.net/v1/dotnet-install.sh -o "$installScript"
            chmod +x "$installScript"
            "$installScript" --version "$dotnetVersion" --install-dir "$DOTNET_INSTALL_DIR" --no-path --skip-non-versioned-files
        fi
    fi
else
    DOTNET_INSTALL_DIR=$(dirname "$(command -v dotnet)")
fi

dotnet="$DOTNET_INSTALL_DIR/dotnet"

if [ "$installDotNetSdk" = true ]; then
    export PATH="$DOTNET_INSTALL_DIR:$PATH"
fi

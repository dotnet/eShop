#!/bin/sh

# Change ownership of the .dotnet directory to the vscode user (to avoid permission errors)
sudo chown -R vscode:vscode /home/vscode/.dotnet
    
# Restore .NET workloads
#sudo dotnet workload update

# Import and trust the ASP.NET Core HTTPS development certificate
dotnet dev-certs https --import /https/eshop-ssl.pfx --clean --password $PFX_PASSWORD --verbose
dotnet dev-certs https --trust --verbose

# Install Agent Package Manager CLI tool
curl -sSL https://aka.ms/apm-unix | sh
apm --version
#!/bin/sh

dotnet tool restore

sudo apt-get update && sudo apt-get install -y protobuf-compiler bubblewrap socat

# Check if the ASP.NET Core HTTPS development certificate is already trusted by ASP.NET Core and OpenSSL
dotnet dev-certs https --check --trust --verbose

devproxy cert ensure
sudo openssl pkcs12 -in $HOME/.config/dev-proxy/rootCert.pfx -nokeys -nodes \
  -out /usr/local/share/ca-certificates/devproxy-ca.crt -passin pass:
sudo update-ca-certificates
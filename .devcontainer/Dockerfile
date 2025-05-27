FROM mcr.microsoft.com/devcontainers/dotnet:1.5.0-9.0-bookworm

# Install gRPC tools
# see: https://danielstoyanoff.medium.com/apple-silicon-docker-dotnet-grpc-is-that-compatible-8a05f1a71d89
RUN apt-get update \
 && DEBIAN_FRONTEND=noninteractive \
    apt-get install --no-install-recommends --assume-yes \
      protobuf-compiler-grpc

ENV PROTOBUF_PROTOC=/usr/bin/protoc

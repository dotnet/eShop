FROM registry.access.redhat.com/ubi8/dotnet-90:9.0 AS build

ENV SSL_CERT_DIR=$HOME/.aspnet/dev-certs/trust:/etc/pki/tls/certs \
    ASPIRE_CONTAINER_RUNTIME=podman
WORKDIR /opt/app-root/src

USER root
# Use rootless podman to launch app processes
RUN microdnf install openssl podman -y && \
    usermod --add-subuids 100000-165535 default && \
    usermod --add-subgids 100000-165535 default && \
    setcap cap_setuid+eip /usr/bin/newuidmap && \
    setcap cap_setgid+eip /usr/bin/newgidmap && \
    chmod -R g=u /etc/subuid /etc/subgid

USER 1001
COPY --chown=1001:0 / /opt/app-root/src

# Trust the ASP.NET Core HTTPS dev cert and build the .NET project
RUN dotnet dev-certs https --trust && \
    dotnet build src/eShop.AppHost/eShop.AppHost.csproj -c Release

EXPOSE 19888

# Building the nested project file creates several .dll binaries in respective folders.  I opted for a
# lift-and-shift approach to run the project file, but a preferred approach would be to refactor as microservices and
# use a multistage build to execute a single binary with the .NET runtime image per container
ENTRYPOINT ["bash", "-c", "dotnet run -c Release --project src/eShop.AppHost/eShop.AppHost.csproj --no-build"]

FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /source

# Install .NET Core 3.1 as an additional version. Remove after net6 upgrade.
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 3.1 --install-dir /usr/share/dotnet

COPY . .

ENTRYPOINT ["dotnet", "test", "./source/Nevermore.IntegrationTests"]
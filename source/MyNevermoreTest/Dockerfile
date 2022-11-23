FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MyNevermoreTest/MyNevermoreTest.csproj", "MyNevermoreTest/"]
RUN dotnet restore "MyNevermoreTest/MyNevermoreTest.csproj"
COPY . .
WORKDIR "/src/MyNevermoreTest"
RUN dotnet build "MyNevermoreTest.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyNevermoreTest.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyNevermoreTest.dll"]

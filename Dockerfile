FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LustreCollector/LustreCollector.csproj", "LustreCollector/"]
RUN dotnet restore "LustreCollector/LustreCollector.csproj"

COPY . .
WORKDIR "/src/LustreCollector"
RUN dotnet build "LustreCollector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LustreCollector.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base

LABEL org.opencontainers.image.source=https://github.com/dlcs/lustre-watcher

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LustreCollector.dll"]
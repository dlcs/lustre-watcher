FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LustreCollector.csproj", "./"]
RUN dotnet restore "LustreCollector.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "LustreCollector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LustreCollector.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LustreCollector.dll"]

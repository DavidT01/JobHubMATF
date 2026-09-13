# Build context: repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/BuildingBlocks/JobHub.Grpc.Contracts/JobHub.Grpc.Contracts.csproj", "src/BuildingBlocks/JobHub.Grpc.Contracts/"]
COPY ["src/Services/Catalog/Catalog.csproj", "src/Services/Catalog/"]
RUN dotnet restore "src/Services/Catalog/Catalog.csproj"
COPY src/BuildingBlocks/JobHub.Grpc.Contracts/ src/BuildingBlocks/JobHub.Grpc.Contracts/
COPY src/Services/Catalog/ src/Services/Catalog/
RUN dotnet publish "src/Services/Catalog/Catalog.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Catalog.dll"]

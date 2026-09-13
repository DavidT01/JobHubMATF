# Build context: repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Gateways/Gateway/Gateway.csproj", "src/Gateways/Gateway/"]
RUN dotnet restore "src/Gateways/Gateway/Gateway.csproj"
COPY src/Gateways/Gateway/ src/Gateways/Gateway/
RUN dotnet publish "src/Gateways/Gateway/Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Gateway.dll"]

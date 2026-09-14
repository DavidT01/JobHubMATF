# Build context: repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Chat/Chat.API/Chat.API.csproj", "src/Services/Chat/Chat.API/"]
RUN dotnet restore "src/Services/Chat/Chat.API/Chat.API.csproj"
COPY src/Services/Chat/Chat.API/ src/Services/Chat/Chat.API/
RUN dotnet publish "src/Services/Chat/Chat.API/Chat.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Chat.API.dll"]

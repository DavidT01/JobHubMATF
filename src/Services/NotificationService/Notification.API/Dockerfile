# Build context: repository root (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
RUN mkdir -p /app/data && chown $APP_UID /app/data
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/NotificationService/Notification.API/Notification.API.csproj", "src/Services/NotificationService/Notification.API/"]
RUN dotnet restore "src/Services/NotificationService/Notification.API/Notification.API.csproj"
COPY src/Services/NotificationService/Notification.API/ src/Services/NotificationService/Notification.API/
RUN dotnet publish "src/Services/NotificationService/Notification.API/Notification.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Notification.API.dll"]

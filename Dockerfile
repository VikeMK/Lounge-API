# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between code changes
COPY src/Lounge.Web/Lounge.Web.csproj src/Lounge.Web/
RUN dotnet restore src/Lounge.Web/Lounge.Web.csproj

COPY . .
# LibraryManager.Build pulls client-side libs (libman.json) during this step, so the build needs internet
RUN dotnet publish src/Lounge.Web/Lounge.Web.csproj -c Release -o /app/publish --no-restore

# ---------- EF Core migrations bundle ----------
FROM build AS bundle
RUN dotnet tool install --global dotnet-ef --version 9.0.9
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
      --project src/Lounge.Web/Lounge.Web.csproj \
      --configuration Release \
      --output /app/efbundle

# ---------- migrator (one-shot: applies migrations, then exits) ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS migrator
WORKDIR /app
COPY --from=bundle /app/efbundle .
COPY --from=build /app/publish/appsettings*.json ./
ENTRYPOINT ["sh", "-c", "./efbundle --connection \"$ConnectionStrings__DefaultConnection\""]

# ---------- runtime (default target) ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Lounge.Web.dll"]

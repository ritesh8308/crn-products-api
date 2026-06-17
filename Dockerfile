# ---- Build stage: full SDK, restores and publishes the app ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first and restore — this layer is cached and only
# re-runs when a .csproj changes, so day-to-day source edits don't re-restore.
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/API/API.csproj src/API/
RUN dotnet restore src/API/API.csproj

# Copy the rest of the source and publish a Release build.
COPY src/ src/
RUN dotnet publish src/API/API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Final stage: small runtime-only image, just the published output ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Kestrel listens on 8080 inside the container (mapped to a host port by Compose).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "API.dll"]

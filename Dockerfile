# syntax=docker/dockerfile:1

# ---------------------------------------------------------------- build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, on the project files alone, so that a source-only change does
# not invalidate the restore layer.
COPY Directory.Build.props ./
COPY DentalSurgery.sln ./
COPY src/DentalSurgery.Domain/*.csproj                  src/DentalSurgery.Domain/
COPY src/DentalSurgery.Application/*.csproj             src/DentalSurgery.Application/
COPY src/DentalSurgery.Infrastructure/*.csproj          src/DentalSurgery.Infrastructure/
COPY src/DentalSurgery.Migrations.SqlServer/*.csproj    src/DentalSurgery.Migrations.SqlServer/
COPY src/DentalSurgery.Migrations.Sqlite/*.csproj       src/DentalSurgery.Migrations.Sqlite/
COPY src/DentalSurgery.Web/*.csproj                     src/DentalSurgery.Web/
COPY tests/DentalSurgery.Tests/*.csproj                 tests/DentalSurgery.Tests/
COPY tests/DentalSurgery.IntegrationTests/*.csproj      tests/DentalSurgery.IntegrationTests/
RUN dotnet restore DentalSurgery.sln

COPY . .
RUN dotnet publish src/DentalSurgery.Web/DentalSurgery.Web.csproj \
        -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------------------------------------------------------------- runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Culture-sensitive formatting is used for dates and money, so the globalisation
# data has to be present. InvariantGlobalization is false in Directory.Build.props.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libicu72 curl \
 && rm -rf /var/lib/apt/lists/*

# A non-root user: a container compromise should not also be root.
RUN useradd --uid 64198 --create-home --shell /usr/sbin/nologin dental
COPY --from=build --chown=dental:dental /app/publish ./

# Documents are written here. Mount a volume, or swap IFileStorage for object
# storage, before running more than one instance.
RUN mkdir -p /app/App_Data && chown -R dental:dental /app/App_Data
VOLUME ["/app/App_Data"]

USER dental

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1
EXPOSE 8080

# Readiness, not liveness: the orchestrator's own liveness probe should call
# /health/live so that a database blip does not restart every instance at once.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "DentalSurgery.Web.dll"]

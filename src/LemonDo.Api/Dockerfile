# syntax=docker/dockerfile:1

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY src/LemonDo.slnx .
COPY src/Directory.Build.props .
COPY src/LemonDo.Domain/LemonDo.Domain.csproj LemonDo.Domain/
COPY src/LemonDo.Application/LemonDo.Application.csproj LemonDo.Application/
COPY src/LemonDo.Infrastructure/LemonDo.Infrastructure.csproj LemonDo.Infrastructure/
COPY src/LemonDo.Migrations.Sqlite/LemonDo.Migrations.Sqlite.csproj LemonDo.Migrations.Sqlite/
COPY src/LemonDo.Migrations.SqlServer/LemonDo.Migrations.SqlServer.csproj LemonDo.Migrations.SqlServer/
COPY src/LemonDo.ServiceDefaults/LemonDo.ServiceDefaults.csproj LemonDo.ServiceDefaults/
COPY src/LemonDo.Api/LemonDo.Api.csproj LemonDo.Api/

RUN dotnet restore LemonDo.Api/LemonDo.Api.csproj

# Copy all source code and publish
COPY src/LemonDo.Domain/ LemonDo.Domain/
COPY src/LemonDo.Application/ LemonDo.Application/
COPY src/LemonDo.Infrastructure/ LemonDo.Infrastructure/
COPY src/LemonDo.Migrations.Sqlite/ LemonDo.Migrations.Sqlite/
COPY src/LemonDo.Migrations.SqlServer/ LemonDo.Migrations.SqlServer/
COPY src/LemonDo.ServiceDefaults/ LemonDo.ServiceDefaults/
COPY src/LemonDo.Api/ LemonDo.Api/

RUN dotnet publish LemonDo.Api/LemonDo.Api.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for healthcheck and create non-root user
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd -r appgroup && useradd -r -g appgroup -s /bin/false appuser

COPY --from=build /app/publish .

# Set environment defaults
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -sf http://localhost:8080/alive || exit 1

ENTRYPOINT ["dotnet", "LemonDo.Api.dll"]

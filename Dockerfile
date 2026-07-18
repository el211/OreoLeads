# -- Build stage ---------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/OreoLeads.Api/OreoLeads.Api.csproj",                       "src/OreoLeads.Api/"]
COPY ["src/OreoLeads.Application/OreoLeads.Application.csproj",       "src/OreoLeads.Application/"]
COPY ["src/OreoLeads.Domain/OreoLeads.Domain.csproj",                 "src/OreoLeads.Domain/"]
COPY ["src/OreoLeads.Infrastructure/OreoLeads.Infrastructure.csproj", "src/OreoLeads.Infrastructure/"]
RUN dotnet restore "src/OreoLeads.Api/OreoLeads.Api.csproj"

COPY . .
WORKDIR "/src/src/OreoLeads.Api"
RUN dotnet publish "OreoLeads.Api.csproj" -c Release -o /app/publish --no-restore

# -- Runtime stage -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# mcr.microsoft.com/dotnet/aspnet:10.0 is Debian-based and ships with a
# built-in non-root user "app" (UID 1000) exposed via $APP_UID.
# Do NOT use Alpine addgroup/adduser — they do not exist on Debian.

COPY --from=build /app/publish .

# Install curl (health check), Kerberos libs (Npgsql), create log dir
RUN apt-get update -qq \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/logs && chown -R $APP_UID /app/logs

USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=300s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "OreoLeads.Api.dll"]

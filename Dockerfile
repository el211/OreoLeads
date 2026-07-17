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

# Non-root user for security
RUN addgroup --system --gid 1001 appgroup && \
    adduser  --system --uid 1001 --ingroup appgroup appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

RUN mkdir -p /app/logs && chown appuser:appgroup /app/logs

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "OreoLeads.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/OreoLeads.Api/OreoLeads.Api.csproj", "OreoLeads.Api/"]
COPY ["src/OreoLeads.Application/OreoLeads.Application.csproj", "OreoLeads.Application/"]
COPY ["src/OreoLeads.Domain/OreoLeads.Domain.csproj", "OreoLeads.Domain/"]
COPY ["src/OreoLeads.Infrastructure/OreoLeads.Infrastructure.csproj", "OreoLeads.Infrastructure/"]

RUN dotnet restore "OreoLeads.Api/OreoLeads.Api.csproj"

COPY src/ .

RUN dotnet build "OreoLeads.Api/OreoLeads.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OreoLeads.Api/OreoLeads.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OreoLeads.Api.dll"]

# Installation locale

## Prerequis

- .NET 10 SDK
- Node.js 22+
- PostgreSQL 17
- Redis 7 (optionnel, le backend demarre sans)
- Docker (optionnel, pour les services externes)

## Backend

```bash
# Restaurer les dependances
dotnet restore

# Configurer la base de donnees (appsettings.Development.json)
# Par defaut : Host=localhost;Port=5432;Database=oreo_leads_dev;Username=oreo;Password=oreo_password

# Lancer le backend
dotnet run --project src/OreoLeads.Api
# API disponible sur http://localhost:5000
# Swagger UI sur http://localhost:5000/swagger (mode Development)
```

## Frontend

```bash
cd frontend
npm install
npm run dev
# Application disponible sur http://localhost:5173
```

## Variables d'environnement requises

| Variable | Description | Exemple |
|----------|-------------|---------|
| `Jwt__SecretKey` | Cle JWT (min 32 chars) | `OreoLeads_Dev_SecretKey_AtLeast32Chars!!` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL | `Host=localhost;...` |
| `Ai__EncryptionKey` | Cle de chiffrement AI | `OreoLeads_Dev_AI_EncryptionKey_32ch!` |

## Base de donnees

Les migrations EF Core s'appliquent automatiquement au demarrage.

Pour appliquer manuellement :

```bash
dotnet ef database update --project src/OreoLeads.Infrastructure --startup-project src/OreoLeads.Api
```

## Docker (alternative)

```bash
cp .env.example .env
# Editer .env
docker compose up -d
```

Cela lance PostgreSQL, Redis, le backend et le frontend.

## Tests

```bash
dotnet test
```

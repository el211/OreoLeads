# OreoLeads

CRM SaaS de prospection commerciale pour Oreo Studios.

## Fonctionnalites principales

- **Lead Management** -- Pipeline CRM complet avec leads, notes, follow-ups, activites
- **Brevo Integration** -- Envoi d'emails transactionnels, webhooks, files d'attente, statistiques
- **Airtable Integration** -- Synchronisation bidirectionnelle avec resolution de conflits
- **AI Integration** -- Generation de brouillons d'emails et analyse de sites web par IA
- **Automation Engine** -- Workflows no-code avec 25+ triggers/actions, scheduler, queue
- **Executive Analytics** -- KPI dashboard, funnel commercial, email analytics, previsions

## Stack technique

| Couche     | Technologie                                    |
|------------|------------------------------------------------|
| Backend    | ASP.NET Core 10, C# 14, Clean Architecture     |
| Frontend   | React 19, TypeScript 6, TanStack Query, Recharts |
| Base       | PostgreSQL 17, EF Core 10                       |
| Cache      | Redis 7                                         |
| UI         | Tailwind CSS v4, Radix UI                       |
| CI/CD      | GitHub Actions, Docker                          |

## Demarrage rapide

```bash
# 1. Cloner le repo
git clone https://github.com/your-org/OreoLeads.git
cd OreoLeads

# 2. Configurer les variables d'environnement
cp .env.example .env
# Editer .env avec vos valeurs

# 3. Lancer avec Docker Compose
docker compose -f docker-compose.prod.yml up -d

# 4. Verifier la sante
curl http://localhost:80/health
```

## Developpement local

```bash
# Backend
dotnet restore
dotnet run --project src/OreoLeads.Api

# Frontend
cd frontend
npm install
npm run dev
```

## Documentation

- [INSTALL.md](INSTALL.md) -- Installation locale
- [DEPLOYMENT.md](DEPLOYMENT.md) -- Deploiement production
- [ARCHITECTURE.md](ARCHITECTURE.md) -- Architecture technique
- [SECURITY.md](SECURITY.md) -- Securite
- [API.md](API.md) -- Documentation API
- [BACKUP.md](BACKUP.md) -- Sauvegardes et restauration
- [PRODUCTION_CHECKLIST.md](PRODUCTION_CHECKLIST.md) -- Checklist pre-deploiement
- [CHANGELOG.md](CHANGELOG.md) -- Notes de version

## Tests

```bash
dotnet test    # 342 tests (unit + integration)
```

## Licence

Proprietary -- Oreo Studios.

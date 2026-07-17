# Deploiement production

## Via Docker Compose

```bash
# 1. Preparer l'environnement
cp .env.example .env
# Editer .env avec des mots de passe forts et uniques

# 2. Lancer les services
docker compose -f docker-compose.prod.yml up -d

# 3. Verifier la sante
curl http://localhost:80/health
curl http://localhost:80/health/ready
curl http://localhost:80/health/live

# 4. Verifier les logs
docker logs oreoleads-backend --tail 50
```

## Via Coolify (toutes infrastructures dans Coolify)

1. Connecter le repository Git dans Coolify
2. Selectionner Docker Compose comme type de deploiement
3. Pointer vers `docker-compose.prod.yml`
4. Configurer les variables d'environnement (voir `.env.example`)
5. Configurer le health check : `GET /health/live` sur le port 8080
6. Configurer le domaine et le certificat SSL (Let's Encrypt automatique)
7. Deployer

---

## Déploiement Coolify avec bases externes

Utilise ce mode si PostgreSQL, Redis et RabbitMQ sont déjà des ressources actives dans Coolify.
Le fichier `docker-compose.coolify.yml` déploie uniquement le **backend** et le **frontend**.

### Pourquoi ce fichier séparé ?

`docker-compose.prod.yml` recrée PostgreSQL, Redis et RabbitMQ.
`docker-compose.coolify.yml` ne contient que backend + frontend et se connecte aux ressources existantes via variables d'environnement.

### Architecture réseau

```
Internet
   │
   ▼
Coolify reverse proxy (HTTPS + domaine)
   │
   ▼
frontend (Nginx :80)
   │  proxy_pass /api/ → http://backend:8080
   ▼
backend (ASP.NET Core :8080)
   │
   ├── PostgreSQL (ressource Coolify existante)
   └── Redis      (ressource Coolify existante)
```

Le frontend et le backend partagent le réseau interne `oreoleads-internal`.
Nginx proxie toutes les requêtes `/api/` vers `backend:8080` — aucune variable `VITE_API_URL` n'est nécessaire.

### Étapes de déploiement

**1. Créer la ressource dans Coolify**

- Type : `Docker Compose`
- Repository : ce dépôt Git
- Branche : `master`
- Fichier Compose : `docker-compose.coolify.yml`

**2. Déclarer les variables d'environnement obligatoires**

| Variable | Description | Format |
|---|---|---|
| `DATABASE_URL` | Connexion PostgreSQL | `Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<pass>` |
| `REDIS_CONNECTION_STRING` | Connexion Redis | `<host>:<port>,password=<pass>,abortConnect=false` |
| `JWT_SECRET` | Clé de signature JWT | Chaîne aléatoire ≥ 64 caractères |
| `ENCRYPTION_KEY` | Clé AES-256-GCM | Chaîne aléatoire ≥ 32 caractères |
| `CORS_ALLOWED_ORIGINS` | URL publique du frontend | `https://app.ton-domaine.com` |

**Variables optionnelles** (selon fonctionnalités utilisées) :

| Variable | Description |
|---|---|
| `JWT_ISSUER` | Défaut : `OreoLeads` |
| `JWT_AUDIENCE` | Défaut : `OreoLeads` |
| `OPENAI_API_KEY` | Clé OpenAI pour le module IA |
| `BREVO_API_KEY` | Clé API Brevo pour l'emailing |
| `AIRTABLE_TOKEN` | Token Airtable pour la synchronisation |

**3. Récupérer les infos de connexion des ressources Coolify**

Dans Coolify, chaque ressource (PostgreSQL, Redis) expose son host interne.
Il ressemble généralement à : `oreoleads-postgres.coolify.internal` ou à une IP de réseau Docker interne.

Exemple de valeurs :
```
DATABASE_URL=Host=oreoleads-postgres.coolify.internal;Port=5432;Database=oreoleads;Username=oreoleads;Password=XXXX
REDIS_CONNECTION_STRING=oreoleads-redis.coolify.internal:6379,password=XXXX,abortConnect=false
```

**4. Configurer les domaines dans Coolify**

- **Frontend** : assigner le domaine public (`app.ton-domaine.com`) au service `frontend` (port `80`)
- **Backend** : ne pas exposer de domaine public direct — le traffic API passe par le proxy Nginx du frontend
- Coolify gère Let's Encrypt automatiquement sur le frontend

**5. Health checks**

| Endpoint | Usage |
|---|---|
| `GET /health/live` | Liveness probe — port `8080` du backend |
| `GET /health/ready` | Readiness probe — vérifie PostgreSQL |
| `GET /health` | État complet JSON (PostgreSQL + Redis + services) |

Dans Coolify, configurer le health check du service `backend` sur :
- Path : `/health/live`
- Port : `8080`

**6. Ports internes**

| Service | Port interne | Exposé publiquement |
|---|---|---|
| `backend` | `8080` | Non — uniquement via proxy Nginx |
| `frontend` | `80` | Oui — via Coolify reverse proxy |

**7. Migrations EF Core**

Les migrations s'appliquent automatiquement au démarrage du backend.
Si une migration échoue en production, le container s'arrête — Coolify affichera un crash loop.
Vérifier les logs du backend pour diagnostiquer.

**8. Vérification post-déploiement**

```bash
# Health check complet (depuis le domaine public)
curl https://app.ton-domaine.com/health

# Vérifier les logs backend dans Coolify
# Conteneur : oreoleads-backend → Logs

# Test de connexion API
curl https://app.ton-domaine.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"xxx"}'
```

## Migrations EF

Les migrations s'appliquent automatiquement au demarrage de l'application.
En cas d'erreur en production, le demarrage sera bloque (securite).

## Rollback

```bash
# Arreter les services
docker compose -f docker-compose.prod.yml down

# Restaurer la base de donnees
./scripts/restore.sh backups/oreoleads_YYYYMMDD_HHMMSS.sql.gz

# Redemarrer avec l'image precedente
docker compose -f docker-compose.prod.yml up -d
```

## Monitoring post-deploiement

- `/health` -- Etat complet de tous les services (JSON)
- `/health/ready` -- Readiness probe (base de donnees)
- `/health/live` -- Liveness probe (processus actif)
- Logs structures JSON dans `/app/logs/` (volume Docker `backend-logs`)
- Configurer UptimeRobot ou equivalent sur `/health`

## Mise a jour

```bash
git pull origin master
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
```

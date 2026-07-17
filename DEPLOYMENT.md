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

## Via Coolify

1. Connecter le repository Git dans Coolify
2. Selectionner Docker Compose comme type de deploiement
3. Pointer vers `docker-compose.prod.yml`
4. Configurer les variables d'environnement (voir `.env.example`)
5. Configurer le health check : `GET /health/live` sur le port 8080
6. Configurer le domaine et le certificat SSL (Let's Encrypt automatique)
7. Deployer

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

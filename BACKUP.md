# Sauvegardes

## Sauvegarde PostgreSQL

```bash
# Sauvegarde manuelle
./scripts/backup.sh

# Sauvegarde dans un repertoire specifique
./scripts/backup.sh /path/to/backups
```

Le script cree un dump compresse (`oreoleads_YYYYMMDD_HHMMSS.sql.gz`)
et conserve les 30 dernieres sauvegardes.

## Frequence recommandee

Configurer un cron job :

```bash
# Toutes les 6 heures
0 */6 * * * /opt/oreoleads/scripts/backup.sh /opt/oreoleads/backups
```

## Restauration

```bash
# Restaurer depuis un fichier de sauvegarde
./scripts/restore.sh backups/oreoleads_20260717_120000.sql.gz
```

Le script affiche un avertissement et attend 5 secondes avant de proceder.

## Volumes Docker

Les donnees persistantes sont stockees dans des volumes Docker :

| Volume | Contenu |
|--------|---------|
| `postgres-data` | Base de donnees PostgreSQL |
| `redis-data` | Cache Redis (AOF) |
| `minio-data` | Fichiers MinIO |
| `rabbitmq-data` | Messages RabbitMQ |
| `backend-logs` | Logs applicatifs |

Pour sauvegarder un volume Docker :

```bash
docker run --rm -v oreoleads_postgres-data:/data -v $(pwd)/backups:/backup \
    alpine tar czf /backup/postgres-volume.tar.gz -C /data .
```

## Plan de reprise

1. Restaurer PostgreSQL depuis la derniere sauvegarde
2. Relancer `docker compose -f docker-compose.prod.yml up -d`
3. Les migrations EF s'appliquent automatiquement au demarrage
4. Verifier `/health` et les logs

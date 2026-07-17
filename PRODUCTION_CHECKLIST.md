# Production Checklist -- OreoLeads v1.0.0

## Before First Deployment

### Environment Variables

- [ ] `DATABASE_URL` -- PostgreSQL connection string
- [ ] `POSTGRES_PASSWORD` -- Strong random password
- [ ] `JWT_SECRET` -- Minimum 64 characters, random
- [ ] `ENCRYPTION_KEY` -- Minimum 32 characters, random
- [ ] `BREVO_ENCRYPTION_KEY` -- Set if using Brevo
- [ ] `AIRTABLE_ENCRYPTION_KEY` -- Set if using Airtable
- [ ] `AI_ENCRYPTION_KEY` -- Set if using AI features
- [ ] `REDIS_PASSWORD` -- Redis password
- [ ] `MINIO_ROOT_PASSWORD` -- MinIO admin password
- [ ] `RABBITMQ_PASSWORD` -- RabbitMQ password

### Infrastructure

- [ ] DNS configured and propagated
- [ ] HTTPS certificate obtained (Let's Encrypt via Coolify)
- [ ] PostgreSQL backups configured (cron + `./scripts/backup.sh`)
- [ ] Volume persistence verified
- [ ] Firewall rules: only ports 80/443 exposed publicly

### Application

- [ ] EF Core migrations ran successfully (auto on startup)
- [ ] Health check `/health` returns `Healthy`
- [ ] Health check `/health/ready` returns `Healthy`
- [ ] Admin account created
- [ ] Brevo API key configured in app settings
- [ ] Airtable token configured (if using)

### Security

- [ ] No secrets in source code or appsettings.json
- [ ] HTTPS enforced (HSTS enabled)
- [ ] Rate limiting active
- [ ] Security headers visible in browser DevTools
- [ ] JWT secret is strong (> 64 chars)

### Monitoring

- [ ] Logs visible in `/app/logs/` volume
- [ ] Serilog structured JSON output verified
- [ ] `/health` polled by external monitor (UptimeRobot etc.)
- [ ] Alerts configured for failed health checks

### CI/CD

- [ ] GitHub Actions CI passing on master
- [ ] Docker images build successfully
- [ ] Tests run in CI (342 tests)

## After Each Deployment

- [ ] Health checks pass
- [ ] Smoke test: login, create a lead, send an email
- [ ] Check logs for errors (first 5 minutes)
- [ ] Verify background services started (automation scheduler, queue worker)

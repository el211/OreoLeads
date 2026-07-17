# Securite

## Authentification

- JWT Bearer tokens avec validation stricte (issuer, audience, lifetime, signing key)
- Refresh tokens avec expiration configurable
- Lockout apres 5 tentatives echouees (15 minutes)

## Autorisation

- Roles : `Admin`, `Manager`, `Sales`
- Autorisation par attribut `[Authorize(Roles = "Admin")]`
- Chaque entite porte un `OrganizationId` pour l'isolation multi-tenant

## Multi-tenancy

- Query filters EF Core automatiques sur `OrganizationId`
- `TenantContext` injecte par requete depuis le JWT
- Isolation complete des donnees entre organisations

## Chiffrement

- AES-256-GCM pour les cles API stockees (Brevo, Airtable, AI)
- Cles de chiffrement configurees via variables d'environnement
- Jamais de secrets en clair dans le code ou les fichiers de configuration

## En-tetes de securite

Appliques automatiquement par `SecurityHeadersMiddleware` :

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Content-Security-Policy: default-src 'self'; ...`

## Rate limiting

- Global : 100 req/min (200 en production)
- Auth endpoints : 10 req/min par IP
- Search : 30 req/min par utilisateur
- AI : 20 req/min par utilisateur
- Analyse web : 10 req/min par utilisateur

## HTTPS / HSTS

- HSTS active en production (max-age 365 jours, includeSubDomains, preload)
- Redirection HTTPS automatique en production

## Variables d'environnement

Tous les secrets sont fournis via des variables d'environnement.
Voir `.env.example` pour la liste complete.
Ne jamais commiter de fichier `.env` dans le repository.

## Audit

- `AuditLog` enregistre les actions sensibles avec utilisateur, timestamp, details

# Architecture

## Clean Architecture

```
src/
  OreoLeads.Domain/          Entites, enums, value objects
  OreoLeads.Application/     Use cases, interfaces, DTOs, validation
  OreoLeads.Infrastructure/  EF Core, Redis, Identity, services externes
  OreoLeads.Api/             Controllers, middleware, Program.cs
```

Les dependances pointent vers l'interieur :
`Api -> Application <- Infrastructure`, `Domain` est independant.

## Modules

| Module | Responsabilite |
|--------|---------------|
| **Leads** | CRUD leads, notes, follow-ups, activites, pipeline |
| **Brevo** | Envoi email transactionnel, webhooks, file d'attente |
| **Airtable** | Sync bidirectionnelle, mapping de champs, webhooks |
| **AI** | Generation d'emails, analyse de sites, multi-provider |
| **Automation** | Workflows no-code, triggers, actions, scheduler |
| **Analytics** | KPI, funnel, forecasting, rapports, dashboards |
| **Identity** | JWT, roles (Admin/Manager/Sales), refresh tokens |

## Patterns

- **Multi-tenancy** -- `OrganizationId` sur chaque entite, query filters EF Core
- **BackgroundService** -- Email queue, Airtable sync, automation scheduler/worker
- **AES-256-GCM** -- Chiffrement des cles API (Brevo, Airtable, AI)
- **Repository** -- Abstraction EF Core par entite
- **FluentValidation** -- Validation des commandes/requetes

## Base de donnees

PostgreSQL 17 via EF Core 10. Entites principales :

- `Lead`, `LeadActivity`, `LeadNote`, `FollowUp`, `Tag`
- `BrevoConfiguration`, `EmailSendJob`, `EmailEvent`
- `AirtableConfiguration`, `AirtableSyncJob`, `AirtableRecordLink`
- `AutomationWorkflow`, `AutomationTrigger`, `AutomationAction`, `AutomationExecution`
- `AnalyticsDashboard`, `AnalyticsWidget`, `AnalyticsReport`, `AnalyticsForecast`
- `Organization`, `AuditLog`, `RefreshToken`

## Observabilite

- **Serilog** -- Logs structures JSON, rotation quotidienne, 30 jours
- **OpenTelemetry** -- Tracing et metriques (ASP.NET Core, HttpClient)
- **Health checks** -- PostgreSQL, Redis, services d'automatisation
- **Correlation ID** -- Propagevers chaque requete HTTP (X-Correlation-Id)

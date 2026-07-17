# API Documentation

## Base URL

- Development: `http://localhost:5000/api`
- Production: `https://your-domain.com/api`

## Authentication

All endpoints (except `/api/auth/*`) require a Bearer JWT token.

```
Authorization: Bearer <token>
```

### POST /api/auth/register

Register a new user. Body: `{ email, password, firstName, lastName }`.

### POST /api/auth/login

Login. Body: `{ email, password }`. Returns `{ token, refreshToken, expiresAt }`.

### POST /api/auth/refresh

Refresh JWT. Body: `{ token, refreshToken }`.

## Leads

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/leads` | List leads (paginated, filterable) |
| GET | `/api/leads/{id}` | Get lead details |
| POST | `/api/leads` | Create lead |
| PUT | `/api/leads/{id}` | Update lead |
| DELETE | `/api/leads/{id}` | Delete lead |
| POST | `/api/leads/import` | Import leads from CSV |
| GET | `/api/leads/export` | Export leads to Excel |
| GET | `/api/leads/{id}/activities` | Lead activity history |
| POST | `/api/leads/{id}/notes` | Add note to lead |
| POST | `/api/leads/{id}/follow-ups` | Create follow-up |

## Brevo (Email)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/brevo/config` | Get Brevo configuration |
| PUT | `/api/brevo/config` | Update Brevo configuration |
| POST | `/api/brevo/send` | Queue email for sending |
| GET | `/api/brevo/stats` | Email statistics |
| POST | `/api/brevo/webhook` | Webhook endpoint for Brevo events |

## Airtable

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/airtable/config` | Get Airtable configuration |
| PUT | `/api/airtable/config` | Update Airtable configuration |
| POST | `/api/airtable/sync` | Trigger manual sync |
| GET | `/api/airtable/sync/status` | Sync job status |

## Automation

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/automation/workflows` | List workflows |
| POST | `/api/automation/workflows` | Create workflow |
| PUT | `/api/automation/workflows/{id}` | Update workflow |
| POST | `/api/automation/workflows/{id}/activate` | Activate workflow |
| GET | `/api/automation/executions` | Execution history |

## Analytics

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/analytics/kpis` | KPI summary |
| GET | `/api/analytics/funnel` | Sales funnel data |
| GET | `/api/analytics/forecast` | Revenue forecast |
| GET | `/api/analytics/reports` | List reports |

## AI

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/ai/draft` | Generate email draft |
| POST | `/api/ai/analyze` | Analyze website |
| GET | `/api/ai/config` | AI provider configuration |

## Error Format

All errors return a consistent JSON structure:

```json
{
  "status": 400,
  "title": "Validation Error",
  "errors": { "field": ["error message"] }
}
```

Common status codes: `400` (validation), `401` (unauthenticated),
`403` (forbidden), `404` (not found), `429` (rate limited), `500` (server error).

## Health Checks

| Endpoint | Description |
|----------|-------------|
| GET `/health` | Full health status (JSON) |
| GET `/health/ready` | Readiness probe (DB connected) |
| GET `/health/live` | Liveness probe (process alive) |

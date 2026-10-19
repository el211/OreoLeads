# Prospecting delivery and deployment

This delivery implements the registry discovery → existing enrichment → evidence scoring → human review workflow. It is an incremental extension, not completion of every capability in the eight-phase specification. No outbound campaign is started by the prospecting worker.

## Architecture reviewed before changes

The solution contains Domain entities/enums, Application DTOs/interfaces/validators, Infrastructure implementations, an ASP.NET Core API, and a React dashboard. PostgreSQL/EF Core owns CRM, enrichment, email, automation and analytics persistence. Redis is optional. Identity/JWT and TenantContext provide tenant scope. Infrastructure registers registry search, Brave, page fetchers, enrichment workers, AI providers, SMTP/Brevo, Airtable, automation and report workers. React uses the existing Axios authentication flow, TanStack Query, React Router and shared Tailwind/Radix components.

Existing SearchService imports, ILeadSource/OpenDataGouvSource registry pagination, WebsiteDiscoveryService/BraveSearchClient, EmailDiscoveryService, CompanyEnrichmentService, WebsiteAnalyzerService, AI providers/draft generator, email approval inbox and delivery workers remain in use. The new coordinator is domain-specific: the generic automation scheduler cannot maintain a registry page/row cursor or atomically commit imported companies with campaign quotas. Existing generic automation workflows remain separate.

## Implemented

- Tenant-scoped campaigns, persisted criteria, lifecycle, UTC daily discovery/page limits, configurable discovery interval and pending-enrichment limit, score threshold and execution-history retention. Qualification continues while discovery is waiting.
- Optional automatic AI recommendations use persisted results, three-minute leases, a 90-second call timeout, exponential retry delays and a maximum of three attempts. Successful recommendations are reused after restart. Provider and reported token usage are retained with the recommendation; interrupted calls can still consume provider quota.
- Marketing drafts require a documented eligibility review, recipient jurisdiction, professional relevance, sender identification and HTTPS privacy/opt-out links. Content approval is bound to the message and recipient; changes or expired reviews block delivery. Reviews and approvals create audit records and footer preparation creates a draft version. Public availability alone is not an eligibility basis.
- Authenticated encrypted unsubscribe links show a confirmation on GET and persist suppression on POST. Repeated opt-outs are safe and pending deliveries are cancelled. The existing suppression checks also run before queuing and sending.
- Registry page plus row cursor supports daily limits smaller than a registry page. Exhausted searches restart the next day, but existing companies do not get new enrichment jobs.
- France registry filters include region, department, city, postal code, industry, NAF and employee-band bounds. Employee counts are registry band lower bounds, not exact employee counts. Unknown counts do not pass explicit numeric bounds. Use postal code/department with city to avoid filtering national results unnecessarily.
- SIREN/SIRET and normalized host matching in imports. Scheme, path, www and case do not split a website identity; other subdomains remain distinct. Existing ASCII website hosts are backfilled by migration; IDN hosts normalize on subsequent updates. Campaign and manual registry imports share a PostgreSQL transaction advisory lock.
- Imports, enrichment queue writes, page cursor and quota changes commit together. A process failure rolls back the unit; transaction locks are automatically released. Exceptions use exponential retry delays capped at 60 minutes; eight consecutive failures pause the campaign.
- Existing enrichment jobs use atomic claims and recover Running jobs older than 15 minutes. Website discovery still uses existing Brave throttling/cache and query limits.
- Shared HTTP fetching pins validated DNS addresses at connection time, checks every redirect, preserves TLS validation, rejects URL credentials/nonstandard ports, disables proxies/cookies, bounds decompressed HTML to 2 MiB and robots to 256 KiB, and enforces a 20-second deadline per fetch. Fixed semaphore stripes serialize each host within a process. Robots disallows, wildcard rules, longest-match allow precedence and OreoLeads-specific groups are recognized. Missing robots (404) is allowed; unavailable, redirected or non-plain-text robots is conservatively deferred.
- Additional eight-page same-origin inspection records URLs, timestamps, HTTP failures, fetch measurements, viewport observation, technology signatures, images lacking alt attributes, public emails/telephone links and social links. It does not fetch LinkedIn pages. Only bounded linked public pages are inspected.
- ContactDiscoveryService reads explicit structured Person records. Names, titles, email and profile URLs are copied only from source data. Company association is marked Inferred (0.7), not independently verified employment. Existing manual contacts remain intact.
- Transparent evidence points, confidence, source and observation times are persisted per prospect. Failed fetches never score as defects. Configurable weights support deterministic qualification. AI service recommendations use existing providers and accept only enumerated services referencing valid evidence indexes; unsupported output is rejected.
- React campaign creation/lifecycle/progress, evidence review, score filter, execution history, provider configuration state/recent outcomes, contact and inspection panel, and French/English/Spanish/German draft generation through the existing AI inbox.
- Send-time suppression, current approval/content checks, tenant checks and recipient frequency caps. Pending/sending/sent jobs are deduplicated per draft; job claiming is atomic. Existing Brevo unsubscribe/complaint/hard-bounce records are consulted before sending. Campaign drafts stay Generated until approved by a person.
- Authentication now runs before tenant selection. Imported leads, enrichment jobs and drafts retain the organization ID.

## Endpoints

Campaign and eligibility-review endpoints require authentication and an organization. IDs are explicitly checked against that organization. Only the token-protected unsubscribe confirmation and submission are anonymous.

| Method | Path | Purpose |
|---|---|---|
| GET | /api/prospecting/campaigns | Latest 200 campaigns |
| POST | /api/prospecting/campaigns | Create paused campaign |
| GET | /api/prospecting/campaigns/{id}?minimumScore=75 | Campaign, up to 500 prospects and last 100 execution events |
| POST | /api/prospecting/campaigns/{id}/start | Activate |
| POST | /api/prospecting/campaigns/{id}/pause | Stop scheduling new work |
| POST | /api/prospecting/campaigns/{id}/resume | Resume durable cursor |
| POST | /api/prospecting/campaigns/{id}/cancel | Terminal cancellation |
| DELETE | /api/prospecting/campaigns/{id} | Delete inactive campaign/history/memberships; CRM leads remain |
| GET | /api/prospecting/leads/{leadId}/research | Contacts and latest persisted inspection JSON |
| POST | /api/prospecting/prospects/{id}/draft?language=fr | Generate a draft for a qualified prospect, using documented evidence |
| POST | /api/prospecting/prospects/{id}/recommendation | AI selects service/evidence references; does not send anything |
| GET | /api/prospecting/status | Configuration flags and recent recorded execution/enrichment counts; not an active provider health probe |
| GET / POST | /api/outreach/reviews/{draftId} | Read / record eligibility and prepare the mandatory footer before human content approval |
| GET / POST | /api/outreach/unsubscribe?token=... | Anonymous confirmation / suppression using the authenticated token from the email footer |

Example create body:

```json
{
  "name": "Strasbourg restaurant website review",
  "criteria": {
    "city": "Strasbourg", "postalCode": "67000", "nafCode": "56.10A",
    "activeOnly": true, "maximumEmployees": 50
  },
  "dailyLimit": 100, "dailyPageLimit": 20,
  "minimumScore": 75, "retentionDays": 90,
  "discoveryIntervalMinutes": 60, "maxPendingEnrichments": 20,
  "automaticAiRecommendation": false
}
```

Create separate campaigns for different NAF codes. Limits: 1–1,000 discoveries/day, 1–100 registry pages/day, 0–100 minimum score, 7–365 days of execution history, 200 campaigns/organization. Pause/cancel stops new scheduling; already committed enrichment jobs and an in-flight discovery transaction may finish.

## Configuration

Use secure environment configuration for credentials. No new secret is stored in source.

- Existing `BraveSearch__ApiKey`, `BraveSearch__MinIntervalMs`, `BraveSearch__CacheHours` configure Brave. Brave is optional for registry discovery but required for automatic website search. Consult your provider account for current quota/pricing; requests may incur charges.
- Existing `Enrichment__MaxConcurrentJobs`, `Enrichment__MaxJobsPerTick`, `Enrichment__MaxQueriesPerLead`, `Enrichment__TickSeconds` control enrichment. Keep job duration below the 15-minute recovery threshold.
- Existing AI settings configure local Ollama or hosted providers. Hosted recommendations/drafts may incur charges. Recommendations run automatically only when the campaign explicitly enables `automaticAiRecommendation`; drafts remain user-requested.
- `Outreach__FrequencyCapDays` defaults to 7 and is bounded to 1–365.
- `Encryption__Key` must be a non-default secret of at least 32 characters for unsubscribe tokens. Keep it stable and securely backed up: rotating it invalidates previously issued opt-out links as well as affecting existing encrypted provider configuration.
- `Outreach__PublicBaseUrl` is the externally reachable HTTPS application origin. Route `/api/outreach/unsubscribe` to the API without requiring login. Eligibility preparation fails closed if the public origin or encryption key is absent.
- `Outreach__AllowedCountries__0=FR` is the default jurisdiction allowlist. Additional configured countries require documented consent under this conservative application policy. `Outreach__AllowFrenchProfessionalLegitimateInterest` defaults to true; operators must document professional relevance and privacy information. These controls record human decisions; they do not independently establish legal permission. See [CNIL business outreach guidance](https://www.cnil.fr/fr/la-prospection-commerciale-par-courrier-electronique-sms-mms-et-automate-dappel).
- Optional `Prospecting__Weights__Business`, `Geography`, `Size`, `Contactability`, `Confidence`, `Website`, `Performance`, `Transport`. When overriding, provide the full set with a total of 100. Defaults respectively: 20,15,10,20,10,15,5,5. Weights affect subsequent qualification; they do not rewrite historical evidence.
- Scheduler polls every 15 seconds, checks qualification every minute and uses the campaign discovery interval (1–10,080 minutes). Pending enrichment limits range from 1 to 1,000. Imports are serialized across application replicas by PostgreSQL.
- Shared IPageFetcher now uses the guarded HTTP implementation. Existing Playwright code remains available but is not selected for autonomous public fetches, because its browser networking does not provide equivalent connection pinning and bounds. JavaScript-only sites may yield limited evidence.

## Migration and deployment

1. Back up PostgreSQL and review `20261018200054_AddProspectingCampaigns` and `20261019033727_AddOutreachReviewAndCampaignRecommendations`. The second migration adds eligibility reviews, draft language/generation context, scheduling controls and durable AI recommendation state. Existing campaigns receive interval=1 minute and pending limit=20. Existing unsent marketing drafts become subject to eligibility review.
2. Use .NET SDK 10 and the repository's existing frontend toolchain. Run `dotnet test` and `npm ci && npm run build` in frontend.
3. Apply migrations through the existing API startup migration path or the existing deployment migration tooling. The migration adds ProspectingCampaigns, CampaignProspects and CampaignExecutions, contact provenance fields, LeadEnrichments.InspectionJson, leads.WebsiteDomain and indexes. It is tested from an empty PostgreSQL 17 database and leaves unrelated chat/invite schema unchanged. Test against a backup of the actual deployment before rollout, since its historical schema may differ.
4. Deploy the API/worker and frontend together using the existing Docker deployment. Supply existing database, JWT, encryption, optional Brave/AI and sending configuration securely.
5. Open `/prospecting`, create a small paused campaign, then start it. Inspect evidence and contacts before generating a draft. Review factual accuracy, recipient relevance, lawful basis, jurisdiction, sender details and opt-out mechanism before approving outbound mail.
6. Monitor execution history, errors, pending enrichments and existing provider settings. A failed registry request retries rather than looking like an exhausted search.

The migration Down method removes only new tables/columns/indexes. It discards new prospecting metadata; back up before rollback. Existing leads/CRM records remain.

## Validation

Latest local validation (2026-10-19, .NET SDK 10.0.402): `dotnet test OreoLeads.sln --no-restore` passed **454 tests, 0 failed, 0 skipped**, including the PostgreSQL 17 integration test. `npm run build` passed TypeScript and Vite (3,409 modules; largest JS chunk 1,210.36 kB). `dotnet ef migrations has-pending-model-changes` reported no model changes. Existing NuGet package/vulnerability warnings, an existing nullable test warning, and the frontend chunk-size warning remain. No live provider calls or outbound messages were used to validate this delivery.

The PostgreSQL integration test uses `OREOLEADS_TEST_POSTGRES` and creates/drops a uniquely named test database. The account needs CREATE DATABASE; never point this test at production. Without this environment variable the test is explicitly skipped.

It exercises all migrations, advisory-lock transaction rollback, a fresh context/worker after restart, daily limits smaller than a page, durable offsets, and duplicate avoidance with real SearchService/EnrichmentQueueService persistence. External registry responses are fixtures; no paid APIs or outbound email are used by tests.

Unit coverage includes identity normalization, reserved/mapped SSRF destinations, private fetch rejection, robots rules, failed-crawl confidence, structured contact matching, tenant filtering, and suppression/approval. Existing suites continue covering queues, retries, Brave discovery, analysis, automation and Brevo.

Outreach tests cover content/recipient changes, expired reviews, missing human approval, unsupported jurisdictions, privacy attestations, tampered unsubscribe tokens, repeat opt-outs and pending-delivery cancellation. The PostgreSQL test additionally verifies recommendation retry backoff, expired lease recovery, persisted-result reuse after restart, and idempotent suppression inserts.

## Main implementation files

- Domain: `ProspectingCampaign.cs`, `OutreachReview.cs`, and provenance/generation fields on existing lead, contact, enrichment and email entities.
- Application: `CompanyIdentity.cs`, prospecting criteria/evidence/review DTOs, contact discovery and qualification interfaces.
- Infrastructure: `Prospecting/CampaignWorker.cs`, `CampaignRecommendationService.cs`, `CampaignRecommendationWorker.cs`, `Enrichment/ContactDiscoveryService.cs`, `Ai/LeadQualificationService.cs`, `Brevo/OutreachReviewService.cs`, `OutreachGuard.cs`, guarded fetching/robots handling, and extensions to existing imports/enrichment/email queues.
- API: `ProspectingController.cs`, `OutreachController.cs`, existing draft approval and delivery endpoints, and worker registrations.
- React: `ProspectingPage.tsx`, `ResearchPanel.tsx`, `OutreachReviewPanel.tsx`, existing navigation/lead/editor pages and email hooks.
- Persistence: the two migrations named above and updated EF model/snapshot. Tests: `tests/OreoLeads.Tests/Prospecting/` plus existing fetch/discovery fixtures.

## Remaining scope / operational limits

- This is not the full eight-phase platform. Discovery is registry-led; Brave enriches those businesses. General search-led company discovery, Google-compatible search and authorized paid directory integrations are not implemented.
- Monetary budget accounting, hard per-provider spend reservations, per-campaign worker concurrency, cron/time-zone schedules and a persistent per-request billing ledger are not implemented. Recorded search counts are not billing reconciliation. Failed/rolled-back external calls can still consume provider quota. Discovery intervals and pending-enrichment limits are configurable.
- Numeric qualification is deterministic weighted evidence. AI selects a service from supported observations; it does not invent the numeric score. Service relevance/opportunity value are not revenue predictions. Automatic recommendations stop after three failures; there is no operator retry-reset UI yet.
- Contact extraction is limited to structured public Person records. Search-result decision-maker extraction, manual verification UI, advanced matching, and independent employment verification remain future work.
- Static HTML indicators are not a full accessibility audit, visual design review, browser performance measurement or proof that a website lacks a feature. Crawl coverage is bounded; 404/410 observations identify failed visited URLs, not an exhaustive broken-link graph. HTTP observations do not prove HTTPS is unavailable.
- Robots is enforced, but website terms need operator review. There is no automatic interpretation of terms or licensing. Robots crawl-delay is not implemented; per-host serialization is per process, not distributed.
- Eligibility evidence and sender identity are human assertions, not independently verified facts. A suppression-management UI and exhaustive jurisdiction rules are not implemented. Unknown/disallowed jurisdictions fail closed. Existing transactional Reply/Proposal/AfterMeeting drafts are outside the marketing eligibility gate; suppression and approval still apply to sending.
- Execution-history retention runs when active campaigns execute. Contact/inspection deletion policies are not automatic. CRM lead deletion uses existing cascades. Campaign deletion deliberately preserves CRM leads and suppression records.
- Email delivery is not exactly-once across provider/network failures. Jobs left Sending after an ambiguous crash need reconciliation before retry. The existing generic automation SendEmail action is a pre-existing log-only handler and is not used by this workflow. Existing global SMTP/Brevo configuration semantics remain; this delivery does not redesign per-tenant sender configuration.
- Tests do not validate real Brave/AI/Brevo credentials, browser interaction, live website terms, or every existing deployment's migration history. Existing NuGet vulnerability/package warnings and the frontend large-chunk warning remain.

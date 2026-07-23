import { Sparkles, Globe, Mail, ExternalLink, Loader2, Check, AlertTriangle } from 'lucide-react'
import { useLeadEnrichments, useTriggerEnrichment, useValidateEnrichment } from '@/hooks/useEnrichment'
import type { EnrichmentStatus, LeadEnrichment } from '@/types/enrichment'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const STATUS_LABEL: Record<EnrichmentStatus, { label: string; variant: 'secondary' | 'default' | 'destructive' | 'outline' }> = {
  Pending: { label: 'En attente', variant: 'secondary' },
  Running: { label: 'En cours', variant: 'secondary' },
  Completed: { label: 'Terminé', variant: 'default' },
  NeedsReview: { label: 'À vérifier', variant: 'outline' },
  Failed: { label: 'Échec', variant: 'destructive' },
}

function pct(v?: number) {
  return v == null ? '—' : `${Math.round(v * 100)} %`
}

export function EnrichmentPanel({ leadId }: { leadId: string }) {
  const { data: enrichments = [], isLoading } = useLeadEnrichments(leadId)
  const trigger = useTriggerEnrichment(leadId)
  const validate = useValidateEnrichment(leadId)

  const latest = enrichments[0]
  const running = latest?.status === 'Pending' || latest?.status === 'Running'

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0">
        <CardTitle className="text-sm flex items-center gap-2">
          <Sparkles className="h-4 w-4" />
          Enrichissement automatique
        </CardTitle>
        <div className="flex items-center gap-2">
          {latest && <Badge variant={STATUS_LABEL[latest.status].variant}>{STATUS_LABEL[latest.status].label}</Badge>}
          <Button
            size="sm"
            variant="outline"
            disabled={trigger.isPending || running}
            onClick={() => trigger.mutate(!!latest)}
          >
            {trigger.isPending || running
              ? <><Loader2 className="h-3.5 w-3.5 mr-1 animate-spin" />En cours…</>
              : <><Sparkles className="h-3.5 w-3.5 mr-1" />{latest ? 'Relancer' : 'Lancer'}</>}
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {isLoading && <p className="text-sm text-muted-foreground">Chargement…</p>}

        {!isLoading && !latest && (
          <p className="text-sm text-muted-foreground">
            Aucun enrichissement pour ce prospect. Lancez la recherche automatique du site et de l'e-mail.
          </p>
        )}

        {latest && <EnrichmentResult enrichment={latest} onValidate={validate.mutate} validating={validate.isPending} />}
      </CardContent>
    </Card>
  )
}

function EnrichmentResult({
  enrichment: e,
  onValidate,
  validating,
}: {
  enrichment: LeadEnrichment
  onValidate: (args: { id: string; request: { acceptWebsite: boolean; acceptEmail: boolean; website?: string; email?: string } }) => void
  validating: boolean
}) {
  return (
    <div className="space-y-4">
      {/* Site web */}
      <div className="rounded-md border p-3">
        <div className="flex items-center gap-2 text-sm font-medium">
          <Globe className="h-4 w-4" /> Site web
          {e.websiteConfidence != null && (
            <span className="text-xs text-muted-foreground">confiance {pct(e.websiteConfidence)}</span>
          )}
          {e.autoApplied && <Badge variant="secondary" className="text-xs">appliqué</Badge>}
        </div>
        {e.chosenWebsiteUrl ? (
          <div className="mt-2 flex items-center justify-between gap-2">
            <a href={e.chosenWebsiteUrl} target="_blank" rel="noreferrer"
               className="text-sm text-primary hover:underline flex items-center gap-1 truncate">
              {e.chosenWebsiteUrl} <ExternalLink className="h-3 w-3 shrink-0" />
            </a>
            {!e.autoApplied && !e.validatedAt && (
              <Button size="sm" variant="outline" disabled={validating}
                onClick={() => onValidate({ id: e.id, request: { acceptWebsite: true, acceptEmail: false } })}>
                <Check className="h-3.5 w-3.5 mr-1" />Valider
              </Button>
            )}
          </div>
        ) : (
          <p className="mt-2 text-sm text-muted-foreground">Aucun site officiel trouvé.</p>
        )}
        {e.matchedSignals.length > 0 && (
          <div className="mt-2 flex flex-wrap gap-1">
            {e.matchedSignals.map(s => <Badge key={s} variant="outline" className="text-xs">{s}</Badge>)}
          </div>
        )}
      </div>

      {/* E-mail */}
      <div className="rounded-md border p-3">
        <div className="flex items-center gap-2 text-sm font-medium">
          <Mail className="h-4 w-4" /> E-mail
          {e.emailConfidence != null && (
            <span className="text-xs text-muted-foreground">confiance {pct(e.emailConfidence)}</span>
          )}
          {e.emailSourceType && <Badge variant="outline" className="text-xs">{e.emailSourceType}</Badge>}
        </div>
        {e.discoveredEmail ? (
          <div className="mt-2 flex items-center justify-between gap-2">
            <a href={`mailto:${e.discoveredEmail}`} className="text-sm text-primary hover:underline truncate">
              {e.discoveredEmail}
            </a>
            {!e.validatedAt && (
              <Button size="sm" variant="outline" disabled={validating}
                onClick={() => onValidate({ id: e.id, request: { acceptWebsite: false, acceptEmail: true } })}>
                <Check className="h-3.5 w-3.5 mr-1" />Valider
              </Button>
            )}
          </div>
        ) : (
          <p className="mt-2 text-sm text-muted-foreground">Aucun e-mail public trouvé.</p>
        )}
      </div>

      {/* Candidats à vérifier */}
      {e.status === 'NeedsReview' && e.candidates.length > 0 && (
        <div className="rounded-md border border-amber-300 bg-amber-50 dark:bg-amber-950/20 p-3">
          <div className="flex items-center gap-2 text-sm font-medium">
            <AlertTriangle className="h-4 w-4 text-amber-600" /> Candidats à vérifier
          </div>
          <ul className="mt-2 space-y-1">
            {e.candidates.map(c => (
              <li key={c.url} className="flex items-center justify-between gap-2 text-sm">
                <a href={c.url} target="_blank" rel="noreferrer" className="text-primary hover:underline truncate">
                  {c.url}
                </a>
                <div className="flex items-center gap-2 shrink-0">
                  <span className="text-xs text-muted-foreground">{pct(c.score)}</span>
                  <Button size="sm" variant="ghost" disabled={validating}
                    onClick={() => onValidate({ id: e.id, request: { acceptWebsite: true, acceptEmail: false, website: c.url } })}>
                    Choisir
                  </Button>
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Sources secondaires */}
      {e.externalProfiles.length > 0 && (
        <div className="text-xs text-muted-foreground">
          <span className="font-medium">Profils externes : </span>
          {e.externalProfiles.map((p, i) => (
            <a key={p.url} href={p.url} target="_blank" rel="noreferrer" className="hover:underline">
              {p.category}{i < e.externalProfiles.length - 1 ? ', ' : ''}
            </a>
          ))}
        </div>
      )}

      {e.errorMessage && <p className="text-xs text-destructive">Erreur : {e.errorMessage}</p>}
      {e.searchQueriesUsed > 0 && (
        <p className="text-xs text-muted-foreground">{e.searchQueriesUsed} requête(s) de recherche utilisée(s)</p>
      )}
    </div>
  )
}

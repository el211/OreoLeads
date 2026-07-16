import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { format } from 'date-fns'
import { fr } from 'date-fns/locale'
import {
  ArrowLeft, RefreshCw, Globe, Shield, ShieldOff, Zap, ZapOff,
  CheckCircle2, XCircle, Loader2, Clock, Monitor, AlertTriangle,
} from 'lucide-react'
import { useLead } from '@/hooks/useLeads'
import { useLeadAnalysis, useLeadAnalysisHistory, useRunAnalysis, useRecalculateAnalysis } from '@/hooks/useAnalysis'
import type { WebsiteAnalysisDto } from '@/types/analysis'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

// ── Score ring ────────────────────────────────────────────────────────────────
function ScoreRing({ score }: { score: number }) {
  const color = score >= 70 ? '#ef4444' : score >= 40 ? '#f59e0b' : '#22c55e'
  const label = score >= 70 ? 'Forte' : score >= 40 ? 'Moyenne' : 'Faible'
  return (
    <div className="flex flex-col items-center gap-1">
      <div
        className="relative flex h-24 w-24 items-center justify-center rounded-full border-4"
        style={{ borderColor: color }}
      >
        <span className="text-3xl font-bold" style={{ color }}>{score}</span>
        <span className="absolute bottom-2 text-[10px] text-muted-foreground">/100</span>
      </div>
      <p className="text-xs font-medium" style={{ color }}>Opportunité {label}</p>
    </div>
  )
}

// ── Check row ─────────────────────────────────────────────────────────────────
function Check({ ok, label, inverse = false }: { ok: boolean; label: string; inverse?: boolean }) {
  const isGood = inverse ? !ok : ok
  return (
    <div className="flex items-center gap-2 text-sm">
      {isGood
        ? <CheckCircle2 className="h-4 w-4 shrink-0 text-green-500" />
        : <XCircle className="h-4 w-4 shrink-0 text-destructive" />
      }
      <span className={isGood ? 'text-foreground' : 'text-muted-foreground'}>{label}</span>
    </div>
  )
}

// ── Analysis panel ────────────────────────────────────────────────────────────
function AnalysisPanel({ a }: { a: WebsiteAnalysisDto; industry?: string }) {
  return (
    <div className="space-y-4">
      {/* Score + HTTP header */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex flex-col sm:flex-row items-start sm:items-center gap-6">
            <ScoreRing score={a.businessScore} />

            <div className="flex-1 space-y-2 text-sm">
              <div className="flex items-center gap-2 font-medium text-base truncate">
                <Globe className="h-4 w-4 text-muted-foreground shrink-0" />
                <a href={a.url} target="_blank" rel="noopener noreferrer"
                   className="hover:underline truncate">{a.url}</a>
              </div>
              <div className="flex flex-wrap gap-2 text-muted-foreground">
                <span className="flex items-center gap-1">
                  <Monitor className="h-3.5 w-3.5" />
                  HTTP {a.httpStatus}
                </span>
                <span className="flex items-center gap-1">
                  <Clock className="h-3.5 w-3.5" />
                  {a.responseTimeMs} ms
                  {a.responseTimeMs > 3000 && <AlertTriangle className="h-3 w-3 text-warning" />}
                </span>
                {a.redirectCount > 0 && <span>{a.redirectCount} redirection{a.redirectCount > 1 ? 's' : ''}</span>}
              </div>
              <div className="flex flex-wrap gap-2">
                {a.usesHttps
                  ? <Badge variant="success"><Shield className="h-3 w-3 mr-1" />HTTPS</Badge>
                  : <Badge variant="destructive"><ShieldOff className="h-3 w-3 mr-1" />HTTP</Badge>
                }
                {a.usesHttps && (
                  a.certificateValid
                    ? <Badge variant="success">Cert. valide</Badge>
                    : <Badge variant="destructive">Cert. invalide</Badge>
                )}
                {a.cmsDetected && <Badge variant="secondary">{a.cmsDetected}</Badge>}
                {a.responseTimeMs <= 2000
                  ? <Badge variant="success"><Zap className="h-3 w-3 mr-1" />Rapide</Badge>
                  : <Badge variant="warning"><ZapOff className="h-3 w-3 mr-1" />Lent</Badge>
                }
              </div>
              {a.pageTitle && <p className="text-xs text-muted-foreground truncate">Titre : {a.pageTitle}</p>}
              {a.metaDescription && <p className="text-xs text-muted-foreground truncate">Meta : {a.metaDescription}</p>}
            </div>
          </div>

          {a.analysisError && (
            <div className="mt-4 rounded-md bg-destructive/10 px-3 py-2 text-xs text-destructive">
              Erreur : {a.analysisError}
            </div>
          )}
        </CardContent>
      </Card>

      <div className="grid gap-4 md:grid-cols-2">
        {/* Fonctionnalités */}
        <Card>
          <CardHeader className="pb-3"><CardTitle className="text-sm">Fonctionnalités & SEO</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            <Check ok={a.hasContactForm}   label="Formulaire de contact" />
            <Check ok={a.hasQuoteForm}     label="Formulaire de devis" />
            <Check ok={a.hasBookingSystem} label="Système de réservation" />
            <Check ok={a.hasChatWidget}    label="Chat en ligne" />
            <Check ok={a.hasViewport}      label="Responsive (viewport mobile)" />
            <Check ok={!!a.metaDescription} label="Meta description (SEO)" />
          </CardContent>
        </Card>

        {/* Informations & conformité */}
        <Card>
          <CardHeader className="pb-3"><CardTitle className="text-sm">Informations & conformité</CardTitle></CardHeader>
          <CardContent className="space-y-2">
            <Check ok={a.hasEmailVisible}   label="Email visible" />
            <Check ok={a.hasPhoneVisible}   label="Téléphone visible" />
            <Check ok={a.hasAddressVisible} label="Adresse visible" />
            <Check ok={a.hasPrivacyPolicy}  label="Politique de confidentialité" />
            <Check ok={a.hasLegalNotice}    label="Mentions légales" />
          </CardContent>
        </Card>
      </div>

      {/* Technologies */}
      {a.technologies.length > 0 && (
        <Card>
          <CardHeader className="pb-3"><CardTitle className="text-sm">Technologies détectées</CardTitle></CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {a.technologies.map(t => (
                <Badge key={t} variant="outline">{t}</Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Opportunités */}
      {a.opportunities.length > 0 && (
        <Card className="border-yellow-200 dark:border-yellow-900">
          <CardHeader className="pb-3">
            <CardTitle className="text-sm text-yellow-700 dark:text-yellow-400">
              Opportunités détectées ({a.opportunities.length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <ul className="space-y-1.5">
              {a.opportunities.map((o, i) => (
                <li key={i} className="flex items-start gap-2 text-sm">
                  <span className="mt-0.5 h-1.5 w-1.5 shrink-0 rounded-full bg-yellow-500" />
                  {o}
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {/* Services Oreo */}
      {a.oreoServicesRecommended.length > 0 && (
        <Card className="border-primary/30 bg-primary/5">
          <CardHeader className="pb-3">
            <CardTitle className="text-sm text-primary">
              Services Oreo Studios recommandés
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-2">
              {a.oreoServicesRecommended.map((s, i) => (
                <Badge key={i} style={{ backgroundColor: 'hsl(var(--primary))', color: 'white' }}>
                  {s}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

// ── Page principale ────────────────────────────────────────────────────────────
export function LeadAnalysisPage() {
  const { id } = useParams<{ id: string }>()
  const { data: lead } = useLead(id!)
  const { data: analysis, isLoading } = useLeadAnalysis(id!)
  const { data: history = [] } = useLeadAnalysisHistory(id!)
  const runAnalysis = useRunAnalysis(id!)
  const recalculate = useRecalculateAnalysis(id!)
  const [showHistory, setShowHistory] = useState(false)

  const isRunning = runAnalysis.isPending

  return (
    <div className="max-w-4xl space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3 flex-wrap">
        <Link to={`/leads/${id}`}>
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <div className="flex-1 min-w-0">
          <h1 className="text-2xl font-bold truncate">
            Analyse — {lead?.companyName ?? '…'}
          </h1>
          {lead?.website && (
            <p className="text-sm text-muted-foreground truncate">{lead.website}</p>
          )}
        </div>

        <div className="flex gap-2">
          {analysis && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => recalculate.mutate()}
              disabled={recalculate.isPending}
            >
              {recalculate.isPending
                ? <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                : <RefreshCw className="h-4 w-4 mr-1" />
              }
              Recalculer
            </Button>
          )}
          <Button
            size="sm"
            onClick={() => runAnalysis.mutate()}
            disabled={isRunning || !lead?.website}
          >
            {isRunning
              ? <><Loader2 className="h-4 w-4 mr-1 animate-spin" />Analyse...</>
              : <><Globe className="h-4 w-4 mr-1" />{analysis ? 'Relancer' : 'Analyser'}</>
            }
          </Button>
        </div>
      </div>

      {/* No website */}
      {lead && !lead.website && (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            <Globe className="h-8 w-8 mx-auto mb-3 opacity-30" />
            <p>Aucun site web renseigné pour ce prospect.</p>
            <Link to={`/leads/${id}/edit`}>
              <Button variant="outline" size="sm" className="mt-4">Ajouter un site web</Button>
            </Link>
          </CardContent>
        </Card>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="text-center py-16 text-muted-foreground">
          <Loader2 className="h-6 w-6 mx-auto mb-3 animate-spin opacity-50" />
          Chargement...
        </div>
      )}

      {/* Running */}
      {isRunning && (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            <Loader2 className="h-8 w-8 mx-auto mb-3 animate-spin text-primary" />
            <p className="font-medium">Analyse en cours...</p>
            <p className="text-xs mt-1">Récupération de la page et détection des technologies</p>
          </CardContent>
        </Card>
      )}

      {/* No analysis yet */}
      {!isLoading && !isRunning && !analysis && lead?.website && (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            <Globe className="h-8 w-8 mx-auto mb-3 opacity-30" />
            <p>Aucune analyse disponible pour ce prospect.</p>
            <Button size="sm" className="mt-4" onClick={() => runAnalysis.mutate()}>
              Lancer l'analyse
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Analysis result */}
      {!isRunning && analysis && (
        <>
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>Dernière analyse : {format(new Date(analysis.lastAnalysis), 'dd MMM yyyy à HH:mm', { locale: fr })}</span>
            {history.length > 1 && (
              <button
                className="hover:text-foreground underline"
                onClick={() => setShowHistory(v => !v)}
              >
                {showHistory ? 'Masquer' : 'Voir'} l'historique ({history.length})
              </button>
            )}
          </div>

          <AnalysisPanel a={analysis} industry={lead?.industry} />

          {/* History */}
          {showHistory && history.length > 1 && (
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Historique des analyses</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {history.slice(1).map(h => (
                  <div key={h.id} className="flex items-center justify-between text-sm py-2 border-b last:border-0">
                    <div className="flex items-center gap-3">
                      <span className="font-medium">{h.businessScore}/100</span>
                      <span className="text-muted-foreground">
                        HTTP {h.httpStatus} · {h.responseTimeMs} ms
                      </span>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {format(new Date(h.createdAt), 'dd MMM yyyy HH:mm', { locale: fr })}
                    </span>
                  </div>
                ))}
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  )
}

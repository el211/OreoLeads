import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type Campaign = { id: string; name: string; status: string; dailyLimit: number; discoveredToday: number; pagesToday: number; nextPage: number; nextRunAt: string; lastError?: string }
type Evidence = { dimension: string; points: number; observation: string; sourceUrl?: string; observedAt: string; confidence: number }
type Prospect = { id: string; leadId: string; companyName: string; state: string; score: number; evidenceJson: string; recommendationJson?: string; aiError?: string; aiAttempts: number }
type Detail = { prospects: Prospect[]; history: { id: string; createdAt: string; outcome: string; discovered: number; duplicates: number; error?: string }[] }

export function ProspectingPage() {
  const client = useQueryClient()
  const status = useQuery({ queryKey: ['prospecting-status'], queryFn: async () => (await api.get<{ braveConfigured: boolean; aiEnabled: boolean; registryCallsToday: number; registryFailuresToday: number; searchQueriesRecordedToday: number; pendingEnrichments: number }>('/prospecting/status')).data, refetchInterval: 10000 })
  const [selected, setSelected] = useState('')
  const [minimumScore, setMinimumScore] = useState(0)
  const [name, setName] = useState('Strasbourg Web Design Prospects')
  const [city, setCity] = useState('Strasbourg')
  const [postalCode, setPostalCode] = useState('67000')
  const [nafCode, setNafCode] = useState('56.10A')
  const [limit, setLimit] = useState(100)
  const [threshold, setThreshold] = useState(75)
  const [language, setLanguage] = useState('fr')
  const [interval, setInterval] = useState(60)
  const [pendingLimit, setPendingLimit] = useState(20)
  const [automaticAi, setAutomaticAi] = useState(false)
  const recommend = useMutation({ mutationFn: (id: string) => api.post(`/prospecting/prospects/${id}/recommendation`), onSuccess: () => client.invalidateQueries({ queryKey: ['prospecting'] }) })
  const draft = useMutation({ mutationFn: async (id: string) => (await api.post<{ id: string }>(`/prospecting/prospects/${id}/draft`, null, { params: { language } })).data, onSuccess: d => { window.location.href = `/emails/${d.id}` } })
  const campaigns = useQuery({ queryKey: ['prospecting'], queryFn: async () => (await api.get<Campaign[]>('/prospecting/campaigns')).data, refetchInterval: 5000 })
  const detail = useQuery({ queryKey: ['prospecting', selected, minimumScore], queryFn: async () => (await api.get<Detail>(`/prospecting/campaigns/${selected}`, { params: { minimumScore } })).data, enabled: !!selected, refetchInterval: 5000 })
  const create = useMutation({ mutationFn: async () => (await api.post<Campaign>('/prospecting/campaigns', { name, dailyLimit: limit, minimumScore: threshold, discoveryIntervalMinutes: interval, maxPendingEnrichments: pendingLimit, automaticAiRecommendation: automaticAi, criteria: { city, postalCode, nafCode, activeOnly: true } })).data, onSuccess: c => { setSelected(c.id); client.invalidateQueries({ queryKey: ['prospecting'] }) } })
  const transition = useMutation({ mutationFn: ({ id, action }: { id: string; action: string }) => api.post(`/prospecting/campaigns/${id}/${action}`), onSuccess: () => client.invalidateQueries({ queryKey: ['prospecting'] }) })
  const error = campaigns.error || detail.error || create.error || transition.error || draft.error || recommend.error
  return <div className="space-y-6 p-6">
    <div className="flex justify-between items-center"><div><h1 className="text-2xl font-bold">Prospection automatique</h1><p className="text-muted-foreground">Découverte → enrichissement → qualification → revue humaine</p></div><Link className="underline" to="/emails">Brouillons et approbation</Link></div>
    {status.data && <Card><CardContent className="pt-4 text-sm space-y-2"><p>Brave : {status.data.braveConfigured ? 'configuré' : 'clé manquante'} · IA : {status.data.aiEnabled ? 'activée' : 'désactivée'}</p><p>Exécutions registre : {status.data.registryCallsToday} · Erreurs : {status.data.registryFailuresToday} · Recherches Brave enregistrées : {status.data.searchQueriesRecordedToday} · Enrichissements en attente : {status.data.pendingEnrichments}</p><p><Link className="underline" to="/settings/ai">Configuration IA</Link> · <Link className="underline" to="/settings/brevo">Configuration email</Link></p></CardContent></Card>}
    {error && <p role="alert" className="text-destructive">Impossible de terminer la demande : {error.message}</p>}
    <Card><CardHeader><CardTitle>Nouvelle campagne France</CardTitle></CardHeader><CardContent>
      <form onSubmit={e => { e.preventDefault(); create.mutate() }} className="grid gap-4 md:grid-cols-3">
        <label>Nom<Input required minLength={3} maxLength={200} value={name} onChange={e => setName(e.target.value)} /></label>
        <label>Ville<Input value={city} onChange={e => setCity(e.target.value)} /></label>
        <label>Code postal<Input value={postalCode} onChange={e => setPostalCode(e.target.value)} /></label>
        <label>Code NAF<Input value={nafCode} onChange={e => setNafCode(e.target.value)} /></label>
        <label>Découvertes / jour<Input type="number" min={1} max={1000} value={limit} onChange={e => setLimit(Number(e.target.value))} /></label>
        <label>Score minimal<Input type="number" min={0} max={100} value={threshold} onChange={e => setThreshold(Number(e.target.value))} /></label>
        <label>Intervalle de découverte (minutes)<Input type="number" min={1} max={10080} value={interval} onChange={e => setInterval(Number(e.target.value))} /></label>
        <label>Maximum en attente d’enrichissement<Input type="number" min={1} max={1000} value={pendingLimit} onChange={e => setPendingLimit(Number(e.target.value))} /></label>
        <label className="flex items-center gap-2"><input type="checkbox" checked={automaticAi} onChange={e => setAutomaticAi(e.target.checked)} />Recommandations IA automatiques (consomme le quota du fournisseur)</label>
        <Button disabled={create.isPending} type="submit">Créer en pause</Button>
      </form>
    </CardContent></Card>
    {campaigns.isLoading && <p>Chargement des campagnes…</p>}
    {campaigns.data?.length === 0 && <p>Aucune campagne. Créez votre première recherche ci-dessus.</p>}
    <div className="grid gap-4 md:grid-cols-2">{campaigns.data?.map(c => <Card key={c.id}><CardHeader><CardTitle><button onClick={() => setSelected(c.id)} className="text-left underline">{c.name}</button></CardTitle></CardHeader><CardContent className="space-y-3">
      <p>{c.status} · {c.discoveredToday}/{c.dailyLimit} découvertes aujourd’hui · {c.pagesToday} appels registre</p>
      <progress aria-label="Découvertes aujourd’hui" className="w-full" value={c.discoveredToday} max={c.dailyLimit} />
      <p className="text-sm text-muted-foreground">Page suivante : {c.nextPage} · Exécution : {new Date(c.nextRunAt).toLocaleString()}</p>
      {c.lastError && <p className="text-destructive">{c.lastError}</p>}
      {c.status !== 'Cancelled' && <div className="flex gap-2"><Button disabled={transition.isPending} onClick={() => transition.mutate({ id: c.id, action: c.status === 'Active' ? 'pause' : 'resume' })}>{c.status === 'Active' ? 'Pause' : 'Démarrer / reprendre'}</Button><Button variant="outline" disabled={transition.isPending} onClick={() => transition.mutate({ id: c.id, action: 'cancel' })}>Annuler</Button></div>}
    </CardContent></Card>)}</div>
    {selected && <Card><CardHeader><CardTitle>Prospects et preuves</CardTitle></CardHeader><CardContent className="space-y-4">
      <label className="block max-w-xs">Filtrer par score minimal<Input type="number" min={0} max={100} value={minimumScore} onChange={e => setMinimumScore(Number(e.target.value))} /></label>
      {detail.data?.prospects.filter(p => p.state === 'Review').map(p => <div key={`recommendation-${p.id}`} className="rounded border p-3 text-sm"><strong>{p.companyName}</strong>{p.recommendationJson ? <p>Recommandation IA : {JSON.parse(p.recommendationJson).service} · Références de preuves : {JSON.parse(p.recommendationJson).evidenceIndexes.map((i: number) => i + 1).join(', ') || 'aucune'}</p> : <Button variant="outline" disabled={recommend.isPending || p.aiAttempts >= 3} onClick={() => recommend.mutate(p.id)}>Analyser les preuves avec l’IA</Button>}{p.aiError && <p role="alert" className="text-destructive">{p.aiError}</p>}</div>)}
      {detail.data?.prospects.length === 0 && <p>Aucun prospect pour ce filtre. La progression se met à jour toutes les 5 secondes.</p>}
      {detail.data?.prospects.map(p => <details key={p.id} className="rounded border p-3"><summary className="cursor-pointer">{p.companyName} · {p.score}/100 · {p.state}</summary><div className="space-y-2 pt-3">{p.state === 'Review' && <Button disabled={draft.isPending} onClick={() => draft.mutate(p.id)}>Générer un brouillon à approuver</Button>}<Link className="underline" to={`/leads/${p.leadId}`}>Ouvrir le prospect, les contacts et les brouillons</Link>{(JSON.parse(p.evidenceJson) as Evidence[]).map((e, i) => <div key={i} className="text-sm"><p>+{e.points} · {e.observation}</p><p className="text-muted-foreground">Confiance : {Math.round(e.confidence * 100)}% · {new Date(e.observedAt).toLocaleString()} {e.sourceUrl && <a className="underline" href={e.sourceUrl} target="_blank" rel="noreferrer">Source</a>}</p></div>)}</div></details>)}
      <label>Langue du brouillon <select value={language} onChange={e => setLanguage(e.target.value)}><option value="fr">Français</option><option value="en">English</option><option value="es">Español</option><option value="de">Deutsch</option></select></label><h2 className="font-semibold">Historique des exécutions</h2>
      {detail.data?.history.map(h => <p key={h.id} className="text-sm">{new Date(h.createdAt).toLocaleString()} · {h.outcome} · {h.discovered} nouveaux · {h.duplicates} doublons {h.error}</p>)}
    </CardContent></Card>}
  </div>
}

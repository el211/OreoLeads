import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'
import { useLeadEnrichments, useTriggerEnrichment } from '@/hooks/useEnrichment'
import { Button } from '@/components/ui/button'

type Contact = { id: string; firstName: string; lastName: string; position?: string; email?: string; sourceUrl?: string; confidence: number; verificationStatus: string }
type LinkedInResearch = { kind: 'linkedinSearch'; status: string; observedAt: string; queriesUsed: number; error?: string; profiles: { url: string; kind: string; title: string; description: string; emails: string[]; phones: string[] }[] }
type Page = { sourceUrl: string; observedAt: string; statusCode: number; error?: string; responseTimeMs?: number; viewportObserved?: boolean; technologiesObserved?: string[]; emails?: string[]; phones?: string[]; candidateOnly?: boolean; socialProfiles?: string[] }

export function ResearchPanel({ leadId }: { leadId: string }) {
  const trigger = useTriggerEnrichment(leadId)
  const { data: enrichments } = useLeadEnrichments(leadId)
  const running = enrichments?.[0]?.status === 'Pending' || enrichments?.[0]?.status === 'Running'
  const { data, isLoading, error } = useQuery({ queryKey: ['research', leadId], queryFn: async () => (await api.get<{ contacts: Contact[]; inspectionJson: string }>(`/prospecting/leads/${leadId}/research`)).data })
  if (isLoading) return <p>Chargement des observations…</p>
  if (error) return <p role="alert">Recherche indisponible : {error.message}</p>
  const reports: (Page | LinkedInResearch)[] = JSON.parse(data?.inspectionJson ?? '[]')
  const pages = reports.filter((p): p is Page => !('kind' in p))
  const linkedIn = reports.find((p): p is LinkedInResearch => 'kind' in p && p.kind === 'linkedinSearch')
  return <div className="space-y-3 border rounded p-4">
    <h3 className="font-semibold">Contacts et inspection publique</h3>
    <section className="space-y-2 rounded border p-3">
      <div className="flex items-center justify-between gap-2"><h4 className="font-semibold">LinkedIn — recherche publique</h4><Button size="sm" variant="outline" disabled={running || trigger.isPending} onClick={() => trigger.mutate(false)}>{running ? 'Recherche en cours…' : 'Relancer l’enrichissement'}</Button></div>
      <p className="text-sm text-muted-foreground">Pages d’entreprise et profils professionnels trouvés via Brave. Les extraits peuvent être anciens : l’identité et le poste actuel restent à confirmer. Jusqu’à 3 recherches supplémentaires par enrichissement.</p>
      {trigger.isError && <p role="alert">Impossible de lancer la recherche. Réessayez.</p>}
      {!linkedIn && <p>Aucune recherche LinkedIn enregistrée. Relancez l’enrichissement pour la lancer.</p>}
      {linkedIn?.error && <p role="alert">{linkedIn.error}</p>}
      {linkedIn && <p className="text-xs text-muted-foreground">{new Date(linkedIn.observedAt).toLocaleString()} · {linkedIn.queriesUsed} recherche(s)</p>}
      {linkedIn?.status === 'completed' && !linkedIn.profiles.length && <p>Aucun profil public correspondant trouvé. Cela ne prouve pas l’absence de profil LinkedIn.</p>}
      {linkedIn?.profiles.map(p => <article key={p.url} className="space-y-1 border-t pt-2"><a href={p.url} target="_blank" rel="noreferrer" className="font-medium underline">{p.title || p.url}</a><p className="text-xs">{p.kind === 'company' ? 'Page entreprise candidate' : 'Profil professionnel candidat'} · À confirmer</p><p className="text-sm">{p.description}</p>{p.emails.length > 0 && <p className="text-sm">E-mails présents dans l’extrait, à confirmer : {p.emails.join(', ')}</p>}{p.phones.length > 0 && <p className="text-sm">Téléphones présents dans l’extrait, à confirmer : {p.phones.join(', ')}</p>}</article>)}
    </section>
    {pages.some(p => p.candidateOnly) && <p>Coordonnées issues d’un site candidat : confirmez le site avant de les utiliser pour cette entreprise.</p>}
    {pages.flatMap(p => (p.phones ?? []).map(phone => <p key={`${p.sourceUrl}-${phone}`}><a href={`tel:${phone.replace(/^tel:/, '')}`} className="underline">{phone}</a> · {/^(?:\+33|0)[67]/.test(phone) ? 'Mobile français présumé — réception SMS non vérifiée' : 'Téléphone — compatibilité SMS inconnue'} · <a href={p.sourceUrl} target="_blank" rel="noreferrer" className="underline">Source</a></p>))}
    {data?.contacts.map(c => <div key={c.id}><strong>{c.firstName} {c.lastName}</strong> · {c.position} · {c.verificationStatus} ({Math.round(c.confidence * 100)}%)<p>{c.email} {c.sourceUrl && <a href={c.sourceUrl} target="_blank" rel="noreferrer" className="underline">Source</a>}</p></div>)}
    {!data?.contacts.length && <p>Aucun décideur documenté trouvé. L’identité et la fonction restent inconnues.</p>}
    {pages.map((p, i) => <details key={i}><summary className="cursor-pointer">HTTP {p.statusCode} · {p.sourceUrl}</summary><div className="text-sm space-y-1 p-2"><a href={p.sourceUrl} target="_blank" rel="noreferrer" className="underline">Source</a><p>{new Date(p.observedAt).toLocaleString()}</p>{p.error ? <p>{p.error} — état du site inconnu</p> : <><p>Récupération : {p.responseTimeMs} ms (vérification robots incluse)</p><p>Balise viewport observée : {p.viewportObserved ? 'oui' : 'non — comportement mobile à vérifier'}</p><p>Technologies observées : {p.technologiesObserved?.join(', ') || 'aucune signature reconnue'}</p><p>Emails publics : {p.emails?.join(', ') || 'aucun observé'}</p>{p.socialProfiles?.map(url => <a key={url} href={url} target="_blank" rel="noreferrer" className="block underline">{url}</a>)}</>}</div></details>)}
    {!pages.length && <p>Aucune inspection enregistrée.</p>}
  </div>
}

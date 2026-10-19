import { useQuery } from '@tanstack/react-query'
import api from '@/lib/api'

type Contact = { id: string; firstName: string; lastName: string; position?: string; email?: string; sourceUrl?: string; confidence: number; verificationStatus: string }
type Page = { sourceUrl: string; observedAt: string; statusCode: number; error?: string; responseTimeMs?: number; viewportObserved?: boolean; technologiesObserved?: string[]; emails?: string[]; socialProfiles?: string[] }

export function ResearchPanel({ leadId }: { leadId: string }) {
  const { data, isLoading, error } = useQuery({ queryKey: ['research', leadId], queryFn: async () => (await api.get<{ contacts: Contact[]; inspectionJson: string }>(`/prospecting/leads/${leadId}/research`)).data })
  if (isLoading) return <p>Chargement des observations…</p>
  if (error) return <p role="alert">Recherche indisponible : {error.message}</p>
  const pages: Page[] = JSON.parse(data?.inspectionJson ?? '[]')
  return <div className="space-y-3 border rounded p-4">
    <h3 className="font-semibold">Contacts et inspection publique</h3>
    {data?.contacts.map(c => <div key={c.id}><strong>{c.firstName} {c.lastName}</strong> · {c.position} · {c.verificationStatus} ({Math.round(c.confidence * 100)}%)<p>{c.email} {c.sourceUrl && <a href={c.sourceUrl} target="_blank" rel="noreferrer" className="underline">Source</a>}</p></div>)}
    {!data?.contacts.length && <p>Aucun décideur documenté trouvé. L’identité et la fonction restent inconnues.</p>}
    {pages.map((p, i) => <details key={i}><summary className="cursor-pointer">HTTP {p.statusCode} · {p.sourceUrl}</summary><div className="text-sm space-y-1 p-2"><a href={p.sourceUrl} target="_blank" rel="noreferrer" className="underline">Source</a><p>{new Date(p.observedAt).toLocaleString()}</p>{p.error ? <p>{p.error} — état du site inconnu</p> : <><p>Récupération : {p.responseTimeMs} ms (vérification robots incluse)</p><p>Balise viewport observée : {p.viewportObserved ? 'oui' : 'non — comportement mobile à vérifier'}</p><p>Technologies observées : {p.technologiesObserved?.join(', ') || 'aucune signature reconnue'}</p><p>Emails publics : {p.emails?.join(', ') || 'aucun observé'}</p>{p.socialProfiles?.map(url => <a key={url} href={url} target="_blank" rel="noreferrer" className="block underline">{url}</a>)}</>}</div></details>)}
    {!pages.length && <p>Aucune inspection enregistrée.</p>}
  </div>
}

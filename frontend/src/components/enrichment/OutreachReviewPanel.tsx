import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function OutreachReviewPanel({ draftId, disabled }: { draftId: string; disabled: boolean }) {
  const client = useQueryClient()
  const [countryCode, setCountry] = useState('FR')
  const [basis, setBasis] = useState('Consent')
  const [basisEvidence, setEvidence] = useState('')
  const [senderIdentification, setSender] = useState('Oreo Studios — ')
  const [privacyNoticeUrl, setPrivacyUrl] = useState('')
  const [professionRelevant, setRelevant] = useState(false)
  const [privacyInformationProvided, setInformed] = useState(false)
  const review = useQuery({ queryKey: ['outreach-review', draftId], queryFn: async () => (await api.get<{ required: boolean; review?: { countryCode: string; basis: string; reviewedAt: string; approvedAt?: string; validUntil: string } }>(`/outreach/reviews/${draftId}`)).data })
  const prepare = useMutation({ mutationFn: () => api.post(`/outreach/reviews/${draftId}`, { countryCode, basis, basisEvidence, senderIdentification, privacyNoticeUrl, professionRelevant, privacyInformationProvided, validDays: 30 }), onSuccess: () => { client.invalidateQueries({ queryKey: ['email-draft', draftId] }); client.invalidateQueries({ queryKey: ['outreach-review', draftId] }) } })
  if (review.isLoading) return <p>Chargement de la revue de prospection…</p>
  if (review.error) return <p role="alert">Revue de prospection indisponible.</p>
  if (!review.data?.required) return null
  const error = prepare.error as { response?: { data?: { error?: string } }; message?: string } | null
  return <Card><CardHeader><CardTitle>Éligibilité du contact et informations obligatoires</CardTitle></CardHeader><CardContent className="space-y-3">
    <p className="text-sm">Une adresse publique ne suffit pas à autoriser la prospection. Documentez la base retenue et la pertinence professionnelle. Après préparation, relisez le message et son pied de page avant de l’approuver.</p>
    {review.data.review && <p className="text-sm">Revue : {review.data.review.countryCode} · {review.data.review.basis} · valable jusqu’au {new Date(review.data.review.validUntil).toLocaleDateString()}{review.data.review.approvedAt ? ' · approuvée' : ' · approbation du contenu requise'}</p>}
    {!disabled && <form onSubmit={e => { e.preventDefault(); prepare.mutate() }} className="grid gap-3 md:grid-cols-2">
      <label>Pays du destinataire<select className="block border rounded p-2 w-full" value={countryCode} onChange={e => setCountry(e.target.value)}><option value="FR">France</option><option value="DE">Allemagne</option><option value="ES">Espagne</option><option value="GB">Royaume-Uni</option><option value="US">États-Unis</option></select></label>
      <label>Base documentée<select className="block border rounded p-2 w-full" value={basis} onChange={e => setBasis(e.target.value)}><option value="Consent">Consentement documenté</option><option value="ProfessionalLegitimateInterest">Intérêt légitime professionnel — France</option></select></label>
      <label className="md:col-span-2">Preuve, date, source et raison de la pertinence<textarea className="block w-full border rounded p-2" required minLength={20} maxLength={2000} value={basisEvidence} onChange={e => setEvidence(e.target.value)} /></label>
      <label>Identification complète de l’expéditeur<Input required minLength={10} maxLength={500} value={senderIdentification} onChange={e => setSender(e.target.value)} placeholder="Raison sociale, adresse et coordonnées" /></label>
      <label>Notice de confidentialité HTTPS<Input required type="url" value={privacyNoticeUrl} onChange={e => setPrivacyUrl(e.target.value)} /></label>
      <label className="md:col-span-2"><input type="checkbox" required checked={professionRelevant} onChange={e => setRelevant(e.target.checked)} /> L’offre concerne directement l’activité professionnelle du destinataire.</label>
      <label className="md:col-span-2"><input type="checkbox" required checked={privacyInformationProvided} onChange={e => setInformed(e.target.checked)} /> L’information sur le traitement des données et le droit d’opposition a été fournie.</label>
      <Button type="submit" disabled={prepare.isPending}>Préparer le pied de page et enregistrer la revue</Button>
    </form>}
    {error && <p role="alert" className="text-destructive">{error.response?.data?.error ?? error.message}</p>}
    {prepare.isSuccess && <p>Revue enregistrée. Relisez le contenu actualisé avant approbation.</p>}
  </CardContent></Card>
}

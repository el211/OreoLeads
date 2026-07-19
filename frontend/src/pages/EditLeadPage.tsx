import { useEffect, useState } from 'react'
import { useNavigate, useParams, Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { useLead, useUpdateLead } from '@/hooks/useLeads'
import { useTags } from '@/hooks/useTags'
import type { CreateLeadDto, LeadPriority, LeadStatus } from '@/types/lead'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'

const STATUS_OPTIONS: { value: LeadStatus; label: string }[] = [
  { value: 'New', label: 'Nouveau' },
  { value: 'Qualified', label: 'Qualifié' },
  { value: 'ReadyToContact', label: 'Prêt à contacter' },
  { value: 'EmailPrepared', label: 'Email préparé' },
  { value: 'EmailSent', label: 'Email envoyé' },
  { value: 'FollowUp1', label: 'Relance 1' },
  { value: 'FollowUp2', label: 'Relance 2' },
  { value: 'Meeting', label: 'Rendez-vous' },
  { value: 'ProposalSent', label: 'Devis envoyé' },
  { value: 'Client', label: 'Client' },
  { value: 'Rejected', label: 'Refusé' },
  { value: 'DoNotContact', label: 'Ne pas contacter' },
]

export function EditLeadPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: lead, isLoading } = useLead(id!)
  const updateLead = useUpdateLead(id!)
  const { data: allTags = [] } = useTags()
  const [selectedTagIds, setSelectedTagIds] = useState<string[]>([])
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [initialized, setInitialized] = useState(false)

  const [form, setForm] = useState<CreateLeadDto>({
    companyName: '',
    status: 'New',
    priority: 'Medium',
    score: 0,
    country: 'France',
    tagIds: [],
  })

  useEffect(() => {
    if (lead && !initialized) {
      setForm({
        companyName: lead.companyName,
        tradeName: lead.tradeName,
        industry: lead.industry,
        description: lead.description,
        website: lead.website,
        email: lead.email,
        phone: lead.phone,
        address: lead.address,
        postalCode: lead.postalCode,
        city: lead.city,
        department: lead.department,
        region: lead.region,
        country: lead.country ?? 'France',
        siren: lead.siren,
        siret: lead.siret,
        nafCode: lead.nafCode,
        employeeCount: lead.employeeCount,
        status: lead.status,
        priority: lead.priority,
        score: lead.score,
        tagIds: lead.tags.map(t => t.id),
      })
      setSelectedTagIds(lead.tags.map(t => t.id))
      setInitialized(true)
    }
  }, [lead, initialized])

  const set = (field: keyof CreateLeadDto) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    setForm(prev => ({ ...prev, [field]: e.target.value }))

  const toggleTag = (id: string) => {
    setSelectedTagIds(prev => prev.includes(id) ? prev.filter(t => t !== id) : [...prev, id])
  }

  const validate = () => {
    const errs: Record<string, string> = {}
    if (!form.companyName?.trim()) errs.companyName = 'Nom obligatoire'
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errs.email = 'Email invalide'
    if (form.score !== undefined && (form.score < 0 || form.score > 100)) errs.score = 'Score entre 0 et 100'
    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!validate()) return
    try {
      await updateLead.mutateAsync({ ...form, tagIds: selectedTagIds })
      navigate(`/leads/${id}`)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      alert(msg ?? 'Erreur lors de la sauvegarde. Vérifie les champs et réessaie.')
    }
  }

  if (isLoading) return <div className="p-8 text-muted-foreground">Chargement...</div>
  if (!lead) return <div className="p-8 text-destructive">Prospect introuvable.</div>

  return (
    <div className="max-w-4xl space-y-6">
      <div className="flex items-center gap-4">
        <Link to={`/leads/${id}`}>
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <h1 className="text-2xl font-bold">Modifier — {lead.companyName}</h1>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* General */}
        <Card>
          <CardHeader><CardTitle className="text-base">Informations générales</CardTitle></CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label htmlFor="companyName">Raison sociale *</Label>
              <Input id="companyName" value={form.companyName} onChange={set('companyName')} className={errors.companyName ? 'border-destructive' : ''} />
              {errors.companyName && <p className="text-xs text-destructive mt-1">{errors.companyName}</p>}
            </div>
            <div>
              <Label htmlFor="tradeName">Nom commercial</Label>
              <Input id="tradeName" value={form.tradeName ?? ''} onChange={set('tradeName')} />
            </div>
            <div>
              <Label htmlFor="industry">Secteur d'activité</Label>
              <Input id="industry" value={form.industry ?? ''} onChange={set('industry')} placeholder="Restaurant, Garage..." />
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="description">Description</Label>
              <textarea
                id="description"
                value={form.description ?? ''}
                onChange={e => setForm(p => ({ ...p, description: e.target.value }))}
                rows={3}
                className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              />
            </div>
            <div>
              <Label>Statut</Label>
              <Select value={form.status} onValueChange={v => setForm(p => ({ ...p, status: v as LeadStatus }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>{STATUS_OPTIONS.map(o => <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>)}</SelectContent>
              </Select>
            </div>
            <div>
              <Label>Priorité</Label>
              <Select value={form.priority} onValueChange={v => setForm(p => ({ ...p, priority: v as LeadPriority }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Low">Basse</SelectItem>
                  <SelectItem value="Medium">Moyenne</SelectItem>
                  <SelectItem value="High">Haute</SelectItem>
                  <SelectItem value="Urgent">Urgente</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label htmlFor="score">Score (0-100)</Label>
              <Input id="score" type="number" min={0} max={100} value={form.score ?? 0}
                onChange={e => setForm(p => ({ ...p, score: Number(e.target.value) }))}
                className={errors.score ? 'border-destructive' : ''} />
              {errors.score && <p className="text-xs text-destructive mt-1">{errors.score}</p>}
            </div>
          </CardContent>
        </Card>

        {/* Contact */}
        <Card>
          <CardHeader><CardTitle className="text-base">Contact</CardTitle></CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div>
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" value={form.email ?? ''} onChange={set('email')} className={errors.email ? 'border-destructive' : ''} />
              {errors.email && <p className="text-xs text-destructive mt-1">{errors.email}</p>}
            </div>
            <div>
              <Label htmlFor="phone">Téléphone</Label>
              <Input id="phone" value={form.phone ?? ''} onChange={set('phone')} />
            </div>
            <div className="sm:col-span-2">
              <Label htmlFor="website">Site web</Label>
              <Input id="website" type="text" value={form.website ?? ''} onChange={set('website')} placeholder="https://www.example.com" />
            </div>
          </CardContent>
        </Card>

        {/* Location */}
        <Card>
          <CardHeader><CardTitle className="text-base">Localisation</CardTitle></CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div className="sm:col-span-2">
              <Label htmlFor="address">Adresse</Label>
              <Input id="address" value={form.address ?? ''} onChange={set('address')} />
            </div>
            <div>
              <Label htmlFor="postalCode">Code postal</Label>
              <Input id="postalCode" value={form.postalCode ?? ''} onChange={set('postalCode')} />
            </div>
            <div>
              <Label htmlFor="city">Ville</Label>
              <Input id="city" value={form.city ?? ''} onChange={set('city')} />
            </div>
            <div>
              <Label htmlFor="department">Département</Label>
              <Input id="department" value={form.department ?? ''} onChange={set('department')} />
            </div>
            <div>
              <Label htmlFor="region">Région</Label>
              <Input id="region" value={form.region ?? ''} onChange={set('region')} />
            </div>
            <div>
              <Label htmlFor="country">Pays</Label>
              <Input id="country" value={form.country ?? 'France'} onChange={set('country')} />
            </div>
          </CardContent>
        </Card>

        {/* Business */}
        <Card>
          <CardHeader><CardTitle className="text-base">Informations légales</CardTitle></CardHeader>
          <CardContent className="grid gap-4 sm:grid-cols-3">
            <div>
              <Label htmlFor="siren">SIREN</Label>
              <Input id="siren" value={form.siren ?? ''} onChange={set('siren')} />
            </div>
            <div>
              <Label htmlFor="siret">SIRET</Label>
              <Input id="siret" value={form.siret ?? ''} onChange={set('siret')} />
            </div>
            <div>
              <Label htmlFor="nafCode">Code NAF</Label>
              <Input id="nafCode" value={form.nafCode ?? ''} onChange={set('nafCode')} />
            </div>
            <div>
              <Label htmlFor="employeeCount">Effectif</Label>
              <Input id="employeeCount" type="number" min={0}
                value={form.employeeCount ?? ''}
                onChange={e => setForm(p => ({ ...p, employeeCount: e.target.value ? Number(e.target.value) : undefined }))} />
            </div>
          </CardContent>
        </Card>

        {/* Tags */}
        {allTags.length > 0 && (
          <Card>
            <CardHeader><CardTitle className="text-base">Tags</CardTitle></CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-2">
                {allTags.map(tag => (
                  <button
                    key={tag.id}
                    type="button"
                    onClick={() => toggleTag(tag.id)}
                    className="transition-transform hover:scale-105"
                  >
                    <Badge
                      style={{
                        backgroundColor: selectedTagIds.includes(tag.id) ? tag.color : tag.color + '20',
                        color: selectedTagIds.includes(tag.id) ? 'white' : tag.color,
                        borderColor: tag.color,
                      }}
                    >
                      {tag.name}
                    </Badge>
                  </button>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {/* Actions */}
        <div className="flex gap-3">
          <Button type="submit" disabled={updateLead.isPending}>
            {updateLead.isPending ? 'Sauvegarde...' : 'Enregistrer les modifications'}
          </Button>
          <Link to={`/leads/${id}`}><Button variant="outline" type="button">Annuler</Button></Link>
        </div>
      </form>
    </div>
  )
}

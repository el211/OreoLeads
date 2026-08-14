import { useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { fr } from 'date-fns/locale'
import {
  ArrowLeft, Edit, Trash2, Mail, MapPin,
  Building2, Clock, Plus, X, Globe, Bot, Loader2, MessageSquare,
} from 'lucide-react'
import { useLead, useLeadActivities, useLeadNotes, useDeleteLead, useDeleteNote, useCreateNote } from '@/hooks/useLeads'
import { useLeadFollowUps, useCreateFollowUp } from '@/hooks/useFollowUps'
import { useGenerateEmail } from '@/hooks/useEmails'
import { useTags, useAttachTag, useDetachTag } from '@/hooks/useTags'
import type { EmailStyle, EmailLength, EmailType } from '@/types/emails'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { EnrichmentPanel } from '@/components/enrichment/EnrichmentPanel'
import { SmsComposeModal } from '@/components/SmsComposeModal'
import { useSmsJobs } from '@/hooks/useSms'
import { StatusBadge, PriorityBadge } from '@/components/ui/status-badge'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

function InfoField({ label, value, link }: { label: string; value?: string | number | null; link?: string }) {
  if (!value && value !== 0) return null
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      {link ? (
        <a href={link} target="_blank" rel="noopener noreferrer" className="text-sm text-primary hover:underline">
          {String(value)}
        </a>
      ) : (
        <p className="text-sm font-medium">{String(value)}</p>
      )}
    </div>
  )
}

export function LeadDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: lead, isLoading } = useLead(id!)
  const { data: activities = [] } = useLeadActivities(id!)
  const { data: notes = [] } = useLeadNotes(id!)
  const { data: followUps = [] } = useLeadFollowUps(id!)
  const deleteLead = useDeleteLead()
  const deleteNote = useDeleteNote(id!)
  const createNote = useCreateNote(id!)
  const createFollowUp = useCreateFollowUp(id!)

  const generateEmail = useGenerateEmail(id!)

  const { data: allTags = [] } = useTags()
  const attachTag = useAttachTag(id!)
  const detachTag = useDetachTag(id!)
  const [showTagPicker, setShowTagPicker] = useState(false)

  const [newNote, setNewNote] = useState({ title: '', content: '', authorName: 'Utilisateur' })
  const [newFollowUp, setNewFollowUp] = useState({ scheduledAt: '', comment: '', userName: 'Utilisateur', priority: 'Medium' as const })
  const [showNoteForm, setShowNoteForm] = useState(false)
  const [showFollowUpForm, setShowFollowUpForm] = useState(false)
  const [showEmailModal, setShowEmailModal] = useState(false)
  const [emailReq, setEmailReq] = useState({
    style: 'Professional' as EmailStyle,
    length: 'Normal' as EmailLength,
    type: 'FirstContact' as EmailType,
    customInstructions: '',
  })
  const [generatedId, setGeneratedId] = useState<string | null>(null)
  const [showSmsModal, setShowSmsModal] = useState(false)

  const { data: smsJobs = [] } = useSmsJobs(id!)

  if (isLoading) return <div className="text-muted-foreground">Chargement...</div>
  if (!lead) return <div className="text-destructive">Prospect introuvable</div>


  const handleDelete = async () => {
    if (!confirm('Supprimer ce prospect ?')) return
    await deleteLead.mutateAsync(lead.id)
    navigate('/leads')
  }

  const handleCreateNote = async () => {
    if (!newNote.title || !newNote.content) return
    await createNote.mutateAsync(newNote)
    setNewNote({ title: '', content: '', authorName: 'Utilisateur' })
    setShowNoteForm(false)
  }

  const handleCreateFollowUp = async () => {
    if (!newFollowUp.scheduledAt) return
    await createFollowUp.mutateAsync({
      scheduledAt: new Date(newFollowUp.scheduledAt).toISOString(),
      comment: newFollowUp.comment,
      userName: newFollowUp.userName,
      priority: newFollowUp.priority,
    })
    setNewFollowUp({ scheduledAt: '', comment: '', userName: 'Utilisateur', priority: 'Medium' })
    setShowFollowUpForm(false)
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link to="/leads">
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">{lead.companyName}</h1>
            <StatusBadge status={lead.status} label={lead.statusLabel} />
            <PriorityBadge priority={lead.priority} label={lead.priorityLabel} />
            <Badge variant="outline" className="font-mono">Score : {lead.score}</Badge>
          </div>
          {lead.tradeName && <p className="text-muted-foreground">{lead.tradeName}</p>}
        </div>
        <div className="flex gap-2">
          {lead.website && (
            <Link to={`/leads/${lead.id}/analysis`}>
              <Button variant="outline" size="sm"><Globe className="mr-2 h-4 w-4" />Analyser</Button>
            </Link>
          )}
          <Button variant="outline" size="sm" onClick={() => setShowEmailModal(true)}>
            <Bot className="mr-2 h-4 w-4" />Générer email IA
          </Button>
          {lead.phone && (
            <Button variant="outline" size="sm" onClick={() => setShowSmsModal(true)}>
              <MessageSquare className="mr-2 h-4 w-4" />Envoyer SMS
            </Button>
          )}
          <Link to={`/leads/${lead.id}/edit`}>
            <Button variant="outline" size="sm"><Edit className="mr-2 h-4 w-4" />Modifier</Button>
          </Link>
          <Button variant="destructive" size="sm" onClick={handleDelete}>
            <Trash2 className="mr-2 h-4 w-4" />Supprimer
          </Button>
        </div>
      </div>

      {/* Tags */}
      <div className="flex flex-wrap items-center gap-2">
        {lead.tags.map(t => (
          <Badge key={t.id} style={{ backgroundColor: t.color + '20', color: t.color, borderColor: t.color }} className="gap-1">
            {t.name}
            <button
              type="button"
              onClick={() => detachTag.mutate(t.id)}
              className="rounded-full hover:bg-black/10"
              aria-label={`Retirer ${t.name}`}
            >
              <X className="h-3 w-3" />
            </button>
          </Badge>
        ))}

        <div className="relative">
          <Button variant="outline" size="sm" className="h-7" onClick={() => setShowTagPicker(v => !v)}>
            <Plus className="h-3.5 w-3.5 mr-1" />
            Tag
          </Button>

          {showTagPicker && (
            <div className="absolute z-20 mt-1 w-56 rounded-md border bg-popover p-2 shadow-md">
              {(() => {
                const attachedIds = new Set(lead.tags.map(t => t.id))
                const available = allTags.filter(t => !attachedIds.has(t.id))
                if (allTags.length === 0)
                  return <p className="text-xs text-muted-foreground px-1 py-2">Aucun tag. Créez-en dans le menu « Tags ».</p>
                if (available.length === 0)
                  return <p className="text-xs text-muted-foreground px-1 py-2">Tous les tags sont déjà appliqués.</p>
                return (
                  <div className="flex flex-wrap gap-1.5">
                    {available.map(t => (
                      <button
                        key={t.id}
                        type="button"
                        onClick={() => { attachTag.mutate(t.id); setShowTagPicker(false) }}
                        className="rounded-full px-2.5 py-0.5 text-xs font-medium hover:opacity-80"
                        style={{ backgroundColor: t.color + '20', color: t.color }}
                      >
                        {t.name}
                      </button>
                    ))}
                  </div>
                )
              })()}
            </div>
          )}
        </div>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="overview">
        <TabsList>
          <TabsTrigger value="overview">Vue d'ensemble</TabsTrigger>
          <TabsTrigger value="activities">
            Activités <Badge variant="secondary" className="ml-1">{lead.activitiesCount}</Badge>
          </TabsTrigger>
          <TabsTrigger value="notes">
            Notes <Badge variant="secondary" className="ml-1">{lead.notesCount}</Badge>
          </TabsTrigger>
          <TabsTrigger value="followups">
            Relances <Badge variant="secondary" className="ml-1">{lead.followUpsCount}</Badge>
          </TabsTrigger>
          <TabsTrigger value="sms">
            SMS {smsJobs.length > 0 && <Badge variant="secondary" className="ml-1">{smsJobs.length}</Badge>}
          </TabsTrigger>
        </TabsList>

        {/* OVERVIEW */}
        <TabsContent value="overview" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            <Card>
              <CardHeader><CardTitle className="text-sm flex items-center gap-2"><Building2 className="h-4 w-4" />Informations générales</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                <InfoField label="Secteur" value={lead.industry} />
                <InfoField label="Description" value={lead.description} />
                <InfoField label="Effectif" value={lead.employeeCount ? `${lead.employeeCount} employés` : null} />
                <InfoField label="Code NAF" value={lead.nafCode} />
                <InfoField label="SIREN" value={lead.siren} />
                <InfoField label="SIRET" value={lead.siret} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader><CardTitle className="text-sm flex items-center gap-2"><Mail className="h-4 w-4" />Contact</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                <InfoField label="Email" value={lead.email} link={lead.email ? `mailto:${lead.email}` : undefined} />
                <InfoField label="Téléphone" value={lead.phone} link={lead.phone ? `tel:${lead.phone}` : undefined} />
                <InfoField label="Site web" value={lead.website} link={lead.website} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader><CardTitle className="text-sm flex items-center gap-2"><MapPin className="h-4 w-4" />Localisation</CardTitle></CardHeader>
              <CardContent className="space-y-3">
                <InfoField label="Adresse" value={lead.address} />
                <InfoField label="Code postal" value={lead.postalCode} />
                <InfoField label="Ville" value={lead.city} />
                <InfoField label="Département" value={lead.department} />
                <InfoField label="Région" value={lead.region} />
                <InfoField label="Pays" value={lead.country} />
              </CardContent>
            </Card>
          </div>

          <EnrichmentPanel leadId={id!} />

          <div className="text-xs text-muted-foreground">
            Créé le {format(new Date(lead.createdAt), 'dd MMMM yyyy à HH:mm', { locale: fr })}
            {lead.updatedAt && ` · Modifié le ${format(new Date(lead.updatedAt), 'dd MMMM yyyy à HH:mm', { locale: fr })}`}
          </div>
        </TabsContent>

        {/* ACTIVITIES */}
        <TabsContent value="activities">
          <Card>
            <CardContent className="pt-6">
              {activities.length === 0 ? (
                <p className="text-sm text-muted-foreground text-center py-8">Aucune activité</p>
              ) : (
                <div className="space-y-4">
                  {(activities as { id: string; typeLabel: string; description: string; createdAt: string }[]).map((activity, i) => (
                    <div key={activity.id} className="flex gap-4">
                      <div className="flex flex-col items-center">
                        <div className="h-2 w-2 rounded-full bg-primary mt-1" />
                        {i < activities.length - 1 && <div className="w-0.5 flex-1 bg-border mt-2" />}
                      </div>
                      <div className="flex-1 pb-4">
                        <div className="flex items-center gap-2">
                          <Badge variant="outline" className="text-xs">{activity.typeLabel}</Badge>
                          <span className="text-xs text-muted-foreground">
                            {format(new Date(activity.createdAt), 'dd/MM/yyyy HH:mm')}
                          </span>
                        </div>
                        <p className="text-sm mt-1">{activity.description}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* NOTES */}
        <TabsContent value="notes" className="space-y-4">
          <div className="flex justify-end">
            <Button size="sm" onClick={() => setShowNoteForm(!showNoteForm)}>
              <Plus className="mr-2 h-4 w-4" />Ajouter une note
            </Button>
          </div>

          {showNoteForm && (
            <Card>
              <CardContent className="pt-6 space-y-3">
                <div>
                  <Label htmlFor="note-title">Titre</Label>
                  <Input id="note-title" value={newNote.title} onChange={e => setNewNote(p => ({ ...p, title: e.target.value }))} placeholder="Titre de la note" />
                </div>
                <div>
                  <Label htmlFor="note-content">Contenu</Label>
                  <textarea
                    id="note-content"
                    value={newNote.content}
                    onChange={e => setNewNote(p => ({ ...p, content: e.target.value }))}
                    placeholder="Contenu de la note..."
                    rows={4}
                    className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                  />
                </div>
                <div className="flex gap-2">
                  <Button size="sm" onClick={handleCreateNote} disabled={createNote.isPending}>Enregistrer</Button>
                  <Button size="sm" variant="outline" onClick={() => setShowNoteForm(false)}>Annuler</Button>
                </div>
              </CardContent>
            </Card>
          )}

          {notes.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Aucune note</p>
          ) : (
            <div className="space-y-3">
              {notes.map(note => (
                <Card key={note.id}>
                  <CardHeader className="pb-2">
                    <div className="flex items-start justify-between">
                      <div>
                        <CardTitle className="text-sm">{note.title}</CardTitle>
                        <p className="text-xs text-muted-foreground mt-1">
                          Par {note.authorName} · {format(new Date(note.createdAt), 'dd/MM/yyyy HH:mm')}
                        </p>
                      </div>
                      <Button
                        variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-destructive"
                        onClick={() => deleteNote.mutate(note.id)}
                      >
                        <X className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <p className="text-sm whitespace-pre-wrap">{note.content}</p>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </TabsContent>

        {/* FOLLOW-UPS */}
        <TabsContent value="followups" className="space-y-4">
          <div className="flex justify-end">
            <Button size="sm" onClick={() => setShowFollowUpForm(!showFollowUpForm)}>
              <Plus className="mr-2 h-4 w-4" />Planifier une relance
            </Button>
          </div>

          {showFollowUpForm && (
            <Card>
              <CardContent className="pt-6 space-y-3">
                <div>
                  <Label htmlFor="fu-date">Date</Label>
                  <Input
                    id="fu-date"
                    type="datetime-local"
                    value={newFollowUp.scheduledAt}
                    onChange={e => setNewFollowUp(p => ({ ...p, scheduledAt: e.target.value }))}
                  />
                </div>
                <div>
                  <Label htmlFor="fu-comment">Commentaire</Label>
                  <Input id="fu-comment" value={newFollowUp.comment} onChange={e => setNewFollowUp(p => ({ ...p, comment: e.target.value }))} placeholder="Optionnel" />
                </div>
                <div className="flex gap-2">
                  <Button size="sm" onClick={handleCreateFollowUp}>Enregistrer</Button>
                  <Button size="sm" variant="outline" onClick={() => setShowFollowUpForm(false)}>Annuler</Button>
                </div>
              </CardContent>
            </Card>
          )}

          {followUps.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Aucune relance</p>
          ) : (
            <div className="space-y-3">
              {followUps.map(fu => (
                <Card key={fu.id} className={fu.isOverdue ? 'border-destructive' : ''}>
                  <CardContent className="pt-4 flex items-center justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <Clock className="h-4 w-4 text-muted-foreground" />
                        <span className="text-sm font-medium">
                          {format(new Date(fu.scheduledAt), 'dd MMMM yyyy à HH:mm', { locale: fr })}
                        </span>
                        {fu.isOverdue && <Badge variant="destructive" className="text-xs">En retard</Badge>}
                      </div>
                      {fu.comment && <p className="text-sm text-muted-foreground mt-1">{fu.comment}</p>}
                      <p className="text-xs text-muted-foreground mt-1">Par {fu.userName}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant="outline">{fu.statusLabel}</Badge>
                      <PriorityBadge priority={fu.priority} label={fu.priorityLabel} />
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </TabsContent>

        {/* SMS */}
        <TabsContent value="sms" className="space-y-4">
          <div className="flex justify-end">
            {lead.phone && (
              <Button size="sm" onClick={() => setShowSmsModal(true)}>
                <MessageSquare className="mr-2 h-4 w-4" />Nouveau SMS
              </Button>
            )}
          </div>

          {smsJobs.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Aucun SMS envoyé</p>
          ) : (
            <div className="space-y-3">
              {smsJobs.map(job => (
                <div key={job.id} className="border rounded-lg p-4 space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">{job.toPhone}</span>
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                      job.status === 'Sent'      ? 'bg-green-100 text-green-700' :
                      job.status === 'Failed'    ? 'bg-red-100 text-red-700' :
                      job.status === 'Pending'   ? 'bg-yellow-100 text-yellow-700' :
                      job.status === 'Sending'   ? 'bg-blue-100 text-blue-700' :
                                                   'bg-muted text-muted-foreground'
                    }`}>
                      {job.status === 'Sent'      ? 'Envoyé' :
                       job.status === 'Failed'    ? 'Échoué' :
                       job.status === 'Pending'   ? 'En attente' :
                       job.status === 'Sending'   ? 'En cours' :
                                                    'Annulé'}
                    </span>
                  </div>
                  <p className="text-sm bg-muted rounded p-2">{job.message}</p>
                  <div className="flex items-center justify-between text-xs text-muted-foreground">
                    <span>{format(new Date(job.createdAt), 'dd/MM/yyyy HH:mm')}</span>
                    {job.sentAt && <span>Envoyé le {format(new Date(job.sentAt), 'dd/MM/yyyy HH:mm')}</span>}
                    {job.errorMessage && <span className="text-destructive">{job.errorMessage}</span>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </TabsContent>
      </Tabs>

      {/* Generate Email Modal */}
      {showEmailModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-md mx-4">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="text-base flex items-center gap-2">
                  <Bot className="h-5 w-5" />Générer un brouillon d'email
                </CardTitle>
                <button onClick={() => { setShowEmailModal(false); setGeneratedId(null) }} className="text-muted-foreground hover:text-foreground">
                  <X className="h-4 w-4" />
                </button>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {generatedId ? (
                <div className="text-center py-4 space-y-3">
                  <div className="text-green-600 font-medium flex items-center justify-center gap-2">
                    <Bot className="h-5 w-5" />Email généré avec succès !
                  </div>
                  <div className="flex gap-2 justify-center">
                    <Link to={`/emails/${generatedId}`}>
                      <Button size="sm">Ouvrir l'éditeur</Button>
                    </Link>
                    <Button size="sm" variant="outline" onClick={() => { setShowEmailModal(false); setGeneratedId(null) }}>
                      Fermer
                    </Button>
                  </div>
                </div>
              ) : (
                <>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <Label>Type</Label>
                      <select
                        className="w-full mt-1 px-2 py-1.5 rounded-md border border-input bg-transparent text-sm"
                        value={emailReq.type}
                        onChange={e => setEmailReq(r => ({ ...r, type: e.target.value as EmailType }))}
                      >
                        <option value="FirstContact">Premier contact</option>
                        <option value="FollowUp">Relance</option>
                        <option value="Reply">Réponse</option>
                        <option value="Proposal">Proposition</option>
                        <option value="AfterMeeting">Après RDV</option>
                        <option value="LastFollowUp">Dernière relance</option>
                      </select>
                    </div>
                    <div>
                      <Label>Style</Label>
                      <select
                        className="w-full mt-1 px-2 py-1.5 rounded-md border border-input bg-transparent text-sm"
                        value={emailReq.style}
                        onChange={e => setEmailReq(r => ({ ...r, style: e.target.value as EmailStyle }))}
                      >
                        <option value="Professional">Professionnel</option>
                        <option value="Friendly">Chaleureux</option>
                        <option value="Premium">Premium</option>
                        <option value="Short">Court</option>
                        <option value="Luxury">Luxe</option>
                        <option value="French">Français classique</option>
                        <option value="English">English</option>
                      </select>
                    </div>
                    <div className="col-span-2">
                      <Label>Longueur</Label>
                      <div className="flex gap-2 mt-1">
                        {(['Short', 'Normal', 'Long'] as EmailLength[]).map(l => (
                          <button
                            key={l}
                            onClick={() => setEmailReq(r => ({ ...r, length: l }))}
                            className={`flex-1 text-sm py-1.5 rounded border transition-colors ${
                              emailReq.length === l ? 'border-primary bg-primary/10' : 'border-border hover:bg-muted'
                            }`}
                          >
                            {l === 'Short' ? 'Court' : l === 'Normal' ? 'Normal' : 'Long'}
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                  <div>
                    <Label>Instructions personnalisées (optionnel)</Label>
                    <textarea
                      className="w-full mt-1 px-3 py-2 rounded-md border border-input bg-transparent text-sm resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                      rows={3}
                      placeholder="Ex: Mentionner notre nouveau service de réservation en ligne..."
                      value={emailReq.customInstructions}
                      onChange={e => setEmailReq(r => ({ ...r, customInstructions: e.target.value }))}
                    />
                  </div>
                  <div className="flex gap-2">
                    <Button
                      className="flex-1"
                      onClick={async () => {
                        const result = await generateEmail.mutateAsync(emailReq)
                        setGeneratedId(result.id)
                      }}
                      disabled={generateEmail.isPending}
                    >
                      {generateEmail.isPending
                        ? <><Loader2 className="h-4 w-4 mr-2 animate-spin" />Génération...</>
                        : <><Bot className="h-4 w-4 mr-2" />Générer</>
                      }
                    </Button>
                    <Button variant="outline" onClick={() => setShowEmailModal(false)}>Annuler</Button>
                  </div>
                  {generateEmail.isError && (
                    <p className="text-sm text-destructive">
                      Erreur : {(generateEmail.error as Error).message ?? 'Une erreur est survenue.'}
                    </p>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {showSmsModal && (
        <SmsComposeModal
          leadId={id!}
          defaultPhone={lead.phone ?? ''}
          companyName={lead.companyName}
          onClose={() => setShowSmsModal(false)}
        />
      )}
    </div>
  )
}

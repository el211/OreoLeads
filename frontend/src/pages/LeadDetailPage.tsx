import { useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { fr } from 'date-fns/locale'
import {
  ArrowLeft, Edit, Trash2, Mail, MapPin,
  Building2, Clock, Plus, X, Globe,
} from 'lucide-react'
import { useLead, useLeadActivities, useLeadNotes, useDeleteLead, useDeleteNote, useCreateNote } from '@/hooks/useLeads'
import { useLeadFollowUps, useCreateFollowUp } from '@/hooks/useFollowUps'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
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

  const [newNote, setNewNote] = useState({ title: '', content: '', authorName: 'Utilisateur' })
  const [newFollowUp, setNewFollowUp] = useState({ scheduledAt: '', comment: '', userName: 'Utilisateur', priority: 'Medium' as const })
  const [showNoteForm, setShowNoteForm] = useState(false)
  const [showFollowUpForm, setShowFollowUpForm] = useState(false)

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
          <Link to={`/leads/${lead.id}/edit`}>
            <Button variant="outline" size="sm"><Edit className="mr-2 h-4 w-4" />Modifier</Button>
          </Link>
          <Button variant="destructive" size="sm" onClick={handleDelete}>
            <Trash2 className="mr-2 h-4 w-4" />Supprimer
          </Button>
        </div>
      </div>

      {/* Tags */}
      {lead.tags.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {lead.tags.map(t => (
            <Badge key={t.id} style={{ backgroundColor: t.color + '20', color: t.color, borderColor: t.color }}>
              {t.name}
            </Badge>
          ))}
        </div>
      )}

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
      </Tabs>
    </div>
  )
}

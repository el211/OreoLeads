import { useState } from 'react'
import { Link } from 'react-router-dom'
import { format, isToday, isThisWeek, isPast } from 'date-fns'
import { fr } from 'date-fns/locale'
import { Bell, CheckCircle, Clock, AlertTriangle, ChevronRight, Trash2 } from 'lucide-react'
import { useFollowUps, useUpdateFollowUp, useDeleteFollowUp } from '@/hooks/useFollowUps'
import type { FollowUp, FollowUpStatus } from '@/types/lead'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'

function getGroup(followUp: FollowUp): 'overdue' | 'today' | 'week' | 'later' {
  const date = new Date(followUp.scheduledAt)
  if (followUp.status !== 'Pending') return 'later'
  if (isPast(date) && !isToday(date)) return 'overdue'
  if (isToday(date)) return 'today'
  if (isThisWeek(date, { weekStartsOn: 1 })) return 'week'
  return 'later'
}

const GROUP_CONFIG = {
  overdue: { label: 'En retard', icon: AlertTriangle, color: 'text-destructive', bg: 'bg-destructive/10' },
  today: { label: "Aujourd'hui", icon: Clock, color: 'text-warning', bg: 'bg-warning/10' },
  week: { label: 'Cette semaine', icon: Bell, color: 'text-primary', bg: 'bg-primary/10' },
  later: { label: 'Plus tard / Terminés', icon: CheckCircle, color: 'text-muted-foreground', bg: 'bg-muted/50' },
} as const

const PRIORITY_VARIANT: Record<string, 'destructive' | 'warning' | 'default' | 'secondary'> = {
  Urgent: 'destructive',
  High: 'warning',
  Medium: 'default',
  Low: 'secondary',
}

const STATUS_OPTIONS: { value: FollowUpStatus; label: string }[] = [
  { value: 'Pending', label: 'En attente' },
  { value: 'Done', label: 'Fait' },
  { value: 'Cancelled', label: 'Annulé' },
  { value: 'Rescheduled', label: 'Reprogrammé' },
]

function FollowUpCard({ followUp, onStatusChange, onDelete }: {
  followUp: FollowUp
  onStatusChange: (id: string, status: FollowUpStatus) => void
  onDelete: (id: string) => void
}) {
  return (
    <div className="flex items-start gap-3 p-4 rounded-lg border bg-card hover:bg-accent/5 transition-colors">
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          {followUp.companyName && (
            <Link to={`/leads/${followUp.leadId}`} className="font-medium hover:underline truncate flex items-center gap-1">
              {followUp.companyName}
              <ChevronRight className="h-3 w-3 inline opacity-50" />
            </Link>
          )}
          <Badge variant={PRIORITY_VARIANT[followUp.priority] ?? 'default'} className="text-xs">
            {followUp.priorityLabel}
          </Badge>
        </div>
        {followUp.comment && (
          <p className="text-sm text-muted-foreground mt-1 truncate">{followUp.comment}</p>
        )}
        <div className="flex items-center gap-3 mt-2 text-xs text-muted-foreground">
          <span>{format(new Date(followUp.scheduledAt), 'dd MMM yyyy à HH:mm', { locale: fr })}</span>
          {followUp.userName && <span>· {followUp.userName}</span>}
        </div>
      </div>

      <div className="flex items-center gap-2 shrink-0">
        <Select
          value={followUp.status}
          onValueChange={v => onStatusChange(followUp.id, v as FollowUpStatus)}
        >
          <SelectTrigger className="h-8 w-36 text-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map(o => (
              <SelectItem key={o.value} value={o.value} className="text-xs">{o.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 text-muted-foreground hover:text-destructive"
          onClick={() => onDelete(followUp.id)}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  )
}

export function FollowUpsPage() {
  const { data: followUps = [], isLoading } = useFollowUps()
  const updateFollowUp = useUpdateFollowUp()
  const deleteFollowUp = useDeleteFollowUp()
  const [statusFilter, setStatusFilter] = useState<FollowUpStatus | 'all'>('all')

  const filtered = statusFilter === 'all'
    ? followUps
    : followUps.filter(f => f.status === statusFilter)

  const grouped = {
    overdue: filtered.filter(f => getGroup(f) === 'overdue'),
    today: filtered.filter(f => getGroup(f) === 'today'),
    week: filtered.filter(f => getGroup(f) === 'week'),
    later: filtered.filter(f => getGroup(f) === 'later'),
  }

  const handleStatusChange = async (id: string, status: FollowUpStatus) => {
    const fu = followUps.find(f => f.id === id)
    if (!fu) return
    await updateFollowUp.mutateAsync({
      id,
      dto: {
        scheduledAt: fu.scheduledAt,
        comment: fu.comment,
        status,
        priority: fu.priority,
      },
    })
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Supprimer cette relance ?')) return
    await deleteFollowUp.mutateAsync(id)
  }

  const totalPending = followUps.filter(f => f.status === 'Pending').length
  const totalOverdue = followUps.filter(f => getGroup(f) === 'overdue').length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Relances</h1>
          <p className="text-sm text-muted-foreground mt-1">
            {totalPending} en attente
            {totalOverdue > 0 && <span className="text-destructive"> · {totalOverdue} en retard</span>}
          </p>
        </div>

        <Select value={statusFilter} onValueChange={v => setStatusFilter(v as FollowUpStatus | 'all')}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Tous les statuts" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tous</SelectItem>
            {STATUS_OPTIONS.map(o => (
              <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {isLoading && (
        <div className="text-center py-16 text-muted-foreground">Chargement...</div>
      )}

      {!isLoading && followUps.length === 0 && (
        <Card>
          <CardContent className="py-16 text-center text-muted-foreground">
            <Bell className="h-8 w-8 mx-auto mb-3 opacity-30" />
            <p>Aucune relance planifiée</p>
            <p className="text-xs mt-1">Créez des relances depuis la fiche d'un prospect</p>
          </CardContent>
        </Card>
      )}

      {(['overdue', 'today', 'week', 'later'] as const).map(groupKey => {
        const items = grouped[groupKey]
        if (items.length === 0) return null
        const config = GROUP_CONFIG[groupKey]
        const Icon = config.icon
        return (
          <Card key={groupKey}>
            <CardHeader className="pb-3">
              <CardTitle className={`text-sm flex items-center gap-2 ${config.color}`}>
                <span className={`inline-flex h-6 w-6 items-center justify-center rounded-full ${config.bg}`}>
                  <Icon className="h-3.5 w-3.5" />
                </span>
                {config.label}
                <Badge variant="secondary" className="ml-auto">{items.length}</Badge>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {items.map(fu => (
                <FollowUpCard
                  key={fu.id}
                  followUp={fu}
                  onStatusChange={handleStatusChange}
                  onDelete={handleDelete}
                />
              ))}
            </CardContent>
          </Card>
        )
      })}
    </div>
  )
}

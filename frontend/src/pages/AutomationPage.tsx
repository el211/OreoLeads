import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Play, Pause, Copy, Trash2, Zap, MoreVertical } from 'lucide-react'
import { useWorkflows, useCreateWorkflow, useActivateWorkflow, useDeactivateWorkflow, useDeleteWorkflow, useCloneWorkflow } from '@/hooks/useAutomation'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import type { WorkflowStatus } from '@/types/automation'

const statusColors: Record<WorkflowStatus, string> = {
  Draft: 'bg-gray-100 text-gray-700',
  Active: 'bg-green-100 text-green-700',
  Paused: 'bg-yellow-100 text-yellow-700',
  Archived: 'bg-red-100 text-red-700',
}

export function AutomationPage() {
  const { data: workflows, isLoading } = useWorkflows()
  const createWorkflow = useCreateWorkflow()
  const activateWorkflow = useActivateWorkflow()
  const deactivateWorkflow = useDeactivateWorkflow()
  const deleteWorkflow = useDeleteWorkflow()
  const cloneWorkflow = useCloneWorkflow()
  const [menuOpen, setMenuOpen] = useState<string | null>(null)

  const handleCreate = () => {
    createWorkflow.mutate({
      name: 'Nouveau workflow',
      description: '',
      concurrencyLimit: 1,
      timeoutSeconds: 300,
    })
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Automatisations</h1>
          <p className="text-muted-foreground">Gerez vos workflows d'automatisation</p>
        </div>
        <div className="flex gap-2">
          <Link to="/automation/monitoring">
            <Button variant="outline">Monitoring</Button>
          </Link>
          <Link to="/automation/history">
            <Button variant="outline">Historique</Button>
          </Link>
          <Button onClick={handleCreate} disabled={createWorkflow.isPending}>
            <Plus className="mr-2 h-4 w-4" />
            Nouveau workflow
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="text-center py-12 text-muted-foreground">Chargement...</div>
      ) : !workflows?.length ? (
        <Card>
          <CardContent className="py-12 text-center">
            <Zap className="mx-auto h-12 w-12 text-muted-foreground/50 mb-4" />
            <p className="text-muted-foreground">Aucun workflow. Creez-en un pour commencer.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4">
          {workflows.map(wf => (
            <Card key={wf.id} className="hover:border-primary/30 transition-colors">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <div className="flex items-center gap-3">
                  <Zap className="h-5 w-5 text-primary" />
                  <div>
                    <Link
                      to={`/automation/builder/${wf.id}`}
                      className="font-semibold text-foreground hover:underline"
                    >
                      {wf.name}
                    </Link>
                    {wf.description && (
                      <p className="text-sm text-muted-foreground">{wf.description}</p>
                    )}
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Badge className={statusColors[wf.status]}>{wf.status}</Badge>
                  <div className="relative">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setMenuOpen(menuOpen === wf.id ? null : wf.id)}
                    >
                      <MoreVertical className="h-4 w-4" />
                    </Button>
                    {menuOpen === wf.id && (
                      <div className="absolute right-0 top-full z-10 mt-1 w-48 rounded-md border bg-popover shadow-md">
                        {wf.isEnabled ? (
                          <button
                            className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent"
                            onClick={() => { deactivateWorkflow.mutate(wf.id); setMenuOpen(null) }}
                          >
                            <Pause className="h-4 w-4" /> Desactiver
                          </button>
                        ) : (
                          <button
                            className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent"
                            onClick={() => { activateWorkflow.mutate(wf.id); setMenuOpen(null) }}
                          >
                            <Play className="h-4 w-4" /> Activer
                          </button>
                        )}
                        <button
                          className="flex w-full items-center gap-2 px-3 py-2 text-sm hover:bg-accent"
                          onClick={() => { cloneWorkflow.mutate(wf.id); setMenuOpen(null) }}
                        >
                          <Copy className="h-4 w-4" /> Dupliquer
                        </button>
                        <button
                          className="flex w-full items-center gap-2 px-3 py-2 text-sm text-destructive hover:bg-accent"
                          onClick={() => { deleteWorkflow.mutate(wf.id); setMenuOpen(null) }}
                        >
                          <Trash2 className="h-4 w-4" /> Supprimer
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex gap-6 text-sm text-muted-foreground">
                  <span>{wf.executionCount} executions</span>
                  {wf.lastExecutedAt && (
                    <span>Derniere: {new Date(wf.lastExecutedAt).toLocaleDateString('fr-FR')}</span>
                  )}
                  <span>Cree le {new Date(wf.createdAt).toLocaleDateString('fr-FR')}</span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

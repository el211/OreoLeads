import { useState } from 'react'
import { RefreshCw, XCircle, RotateCcw } from 'lucide-react'
import { useWorkflowExecutions, useCancelExecution, useRetryExecution, useExecutionLogs } from '@/hooks/useAutomation'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import type { ExecutionStatus } from '@/types/automation'

const statusColors: Record<ExecutionStatus, string> = {
  Pending: 'bg-gray-100 text-gray-700',
  Running: 'bg-blue-100 text-blue-700',
  Waiting: 'bg-yellow-100 text-yellow-700',
  Completed: 'bg-green-100 text-green-700',
  Failed: 'bg-red-100 text-red-700',
  Cancelled: 'bg-gray-100 text-gray-700',
  TimedOut: 'bg-orange-100 text-orange-700',
  Skipped: 'bg-gray-100 text-gray-700',
}

export function AutomationHistoryPage() {
  const { data: executions, isLoading } = useWorkflowExecutions()
  const cancelExecution = useCancelExecution()
  const retryExecution = useRetryExecution()
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const { data: logs } = useExecutionLogs(selectedId ?? undefined)
  const [statusFilter, setStatusFilter] = useState<string>('')

  const filtered = executions?.filter(e =>
    !statusFilter || e.status === statusFilter
  )

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Historique des executions</h1>
          <p className="text-muted-foreground">{executions?.length ?? 0} executions</p>
        </div>
        <div className="flex gap-2">
          <select
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
            className="rounded-md border bg-background px-3 py-2 text-sm"
          >
            <option value="">Tous les statuts</option>
            <option value="Completed">Termine</option>
            <option value="Failed">Echoue</option>
            <option value="Running">En cours</option>
            <option value="Cancelled">Annule</option>
          </select>
        </div>
      </div>

      {isLoading ? (
        <div className="text-center py-12 text-muted-foreground">
          <RefreshCw className="mx-auto h-6 w-6 animate-spin mb-2" />
          Chargement...
        </div>
      ) : (
        <Card>
          <CardContent className="p-0">
            <table className="w-full">
              <thead>
                <tr className="border-b text-left text-sm text-muted-foreground">
                  <th className="px-4 py-3">Workflow</th>
                  <th className="px-4 py-3">Declencheur</th>
                  <th className="px-4 py-3">Statut</th>
                  <th className="px-4 py-3">Duree</th>
                  <th className="px-4 py-3">Demarre</th>
                  <th className="px-4 py-3">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered?.map(exec => (
                  <tr
                    key={exec.id}
                    className={`border-b hover:bg-accent/50 cursor-pointer ${selectedId === exec.id ? 'bg-accent' : ''}`}
                    onClick={() => setSelectedId(selectedId === exec.id ? null : exec.id)}
                  >
                    <td className="px-4 py-3 font-medium">{exec.workflowName}</td>
                    <td className="px-4 py-3 text-sm">{exec.triggerType}</td>
                    <td className="px-4 py-3">
                      <Badge className={statusColors[exec.status]}>{exec.status}</Badge>
                    </td>
                    <td className="px-4 py-3 text-sm">
                      {exec.durationMs != null ? `${exec.durationMs}ms` : '-'}
                    </td>
                    <td className="px-4 py-3 text-sm">
                      {exec.startedAt ? new Date(exec.startedAt).toLocaleString('fr-FR') : '-'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1" onClick={e => e.stopPropagation()}>
                        {exec.status === 'Failed' && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => retryExecution.mutate(exec.id)}
                          >
                            <RotateCcw className="h-4 w-4" />
                          </Button>
                        )}
                        {(exec.status === 'Running' || exec.status === 'Pending') && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => cancelExecution.mutate(exec.id)}
                          >
                            <XCircle className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {!filtered?.length && (
                  <tr>
                    <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                      Aucune execution
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}

      {/* Logs panel */}
      {selectedId && logs && (
        <Card>
          <CardHeader>
            <CardTitle>Logs de l'execution</CardTitle>
          </CardHeader>
          <CardContent>
            {logs.length === 0 ? (
              <p className="text-muted-foreground">Aucun log</p>
            ) : (
              <div className="space-y-2 font-mono text-sm">
                {logs.map(log => (
                  <div key={log.id} className="flex gap-3 rounded border px-3 py-2">
                    <span className="text-muted-foreground whitespace-nowrap">
                      {new Date(log.timestamp).toLocaleTimeString('fr-FR')}
                    </span>
                    <Badge variant="outline" className="text-xs">{log.level}</Badge>
                    {log.actionName && <span className="text-primary">[{log.actionName}]</span>}
                    <span>{log.message}</span>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  )
}

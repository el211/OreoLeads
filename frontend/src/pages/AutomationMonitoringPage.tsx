import { Activity, CheckCircle, XCircle, Clock, Inbox, AlertTriangle, RotateCcw } from 'lucide-react'
import { useAutomationMonitoring, useActiveJobs, useWorkflowExecutions, useRetryExecution } from '@/hooks/useAutomation'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'

export function AutomationMonitoringPage() {
  const { data: stats, isLoading } = useAutomationMonitoring()
  const { data: activeJobs } = useActiveJobs()
  const { data: executions } = useWorkflowExecutions()
  const retryExecution = useRetryExecution()

  const failedExecutions = executions?.filter(e => e.status === 'Failed') ?? []

  if (isLoading || !stats) {
    return (
      <div className="text-center py-12 text-muted-foreground">Chargement du monitoring...</div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-foreground">Monitoring Automatisation</h1>
        <p className="text-muted-foreground">Vue d'ensemble du moteur d'automatisation</p>
      </div>

      {/* Stats cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <Activity className="h-5 w-5 text-primary" />
              <div>
                <p className="text-2xl font-bold">{stats.totalWorkflows}</p>
                <p className="text-xs text-muted-foreground">Workflows</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <CheckCircle className="h-5 w-5 text-green-500" />
              <div>
                <p className="text-2xl font-bold">{stats.activeWorkflows}</p>
                <p className="text-xs text-muted-foreground">Actifs</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <Clock className="h-5 w-5 text-blue-500" />
              <div>
                <p className="text-2xl font-bold">{stats.totalExecutions}</p>
                <p className="text-xs text-muted-foreground">Executions</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <CheckCircle className="h-5 w-5 text-green-500" />
              <div>
                <p className="text-2xl font-bold">{stats.averageSuccessRate.toFixed(1)}%</p>
                <p className="text-xs text-muted-foreground">Taux succes</p>
              </div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3">
              <Inbox className="h-5 w-5 text-orange-500" />
              <div>
                <p className="text-2xl font-bold">{stats.queueDepth}</p>
                <p className="text-xs text-muted-foreground">File d'attente</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Active jobs */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" /> Jobs actifs ({activeJobs?.length ?? 0})
          </CardTitle>
        </CardHeader>
        <CardContent>
          {!activeJobs?.length ? (
            <p className="text-muted-foreground text-center py-4">Aucun job actif</p>
          ) : (
            <table className="w-full">
              <thead>
                <tr className="border-b text-left text-sm text-muted-foreground">
                  <th className="px-4 py-2">Workflow</th>
                  <th className="px-4 py-2">Statut</th>
                  <th className="px-4 py-2">Demarre</th>
                </tr>
              </thead>
              <tbody>
                {activeJobs.map(job => (
                  <tr key={job.id} className="border-b">
                    <td className="px-4 py-2">{job.workflowName}</td>
                    <td className="px-4 py-2">
                      <Badge className="bg-blue-100 text-blue-700">{job.status}</Badge>
                    </td>
                    <td className="px-4 py-2 text-sm">
                      {job.startedAt ? new Date(job.startedAt).toLocaleString('fr-FR') : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>

      {/* Failed jobs */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-red-500" /> Jobs echoues ({failedExecutions.length})
          </CardTitle>
        </CardHeader>
        <CardContent>
          {!failedExecutions.length ? (
            <p className="text-muted-foreground text-center py-4">Aucun job echoue</p>
          ) : (
            <table className="w-full">
              <thead>
                <tr className="border-b text-left text-sm text-muted-foreground">
                  <th className="px-4 py-2">Workflow</th>
                  <th className="px-4 py-2">Tentatives</th>
                  <th className="px-4 py-2">Demarre</th>
                  <th className="px-4 py-2">Action</th>
                </tr>
              </thead>
              <tbody>
                {failedExecutions.map(exec => (
                  <tr key={exec.id} className="border-b">
                    <td className="px-4 py-2">{exec.workflowName}</td>
                    <td className="px-4 py-2">{exec.retryCount}</td>
                    <td className="px-4 py-2 text-sm">
                      {exec.startedAt ? new Date(exec.startedAt).toLocaleString('fr-FR') : '-'}
                    </td>
                    <td className="px-4 py-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => retryExecution.mutate(exec.id)}
                      >
                        <RotateCcw className="mr-1 h-4 w-4" /> Relancer
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>

      {/* Dead letter */}
      {stats.deadLetterCount > 0 && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-3 text-red-600">
              <XCircle className="h-5 w-5" />
              <p className="font-semibold">{stats.deadLetterCount} message(s) en dead letter</p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

import { useState } from 'react'
import { RefreshCw, Loader2, X, ChevronDown, ChevronUp } from 'lucide-react'
import {
  useAirtableSyncJobs,
  useAirtableSyncLogs,
  useEnqueueSync,
  useEnqueueFullSync,
  useCancelSyncJob,
} from '@/hooks/useAirtable'
import type { AirtableSyncJob, AirtableSyncJobStatus } from '@/types/airtable'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const STATUS_COLORS: Record<AirtableSyncJobStatus, string> = {
  Pending:    'bg-gray-100 text-gray-700',
  Processing: 'bg-blue-100 text-blue-700',
  Completed:  'bg-green-100 text-green-700',
  Failed:     'bg-red-100 text-red-700',
  Conflict:   'bg-yellow-100 text-yellow-700',
  Cancelled:  'bg-gray-100 text-gray-500',
}

const STATUS_LABELS: Record<AirtableSyncJobStatus, string> = {
  Pending:    'En attente',
  Processing: 'En cours',
  Completed:  'Terminé',
  Failed:     'Échoué',
  Conflict:   'Conflit',
  Cancelled:  'Annulé',
}

const DIRECTION_LABELS: Record<string, string> = {
  OreoLeadsToAirtable: '→ Airtable',
  AirtableToOreoLeads: '← OreoLeads',
  Bidirectional:       '↔ Bidirectionnel',
}

function SyncJobLogs({ jobId }: { jobId: string }) {
  const { data: logs = [], isLoading } = useAirtableSyncLogs(jobId)

  if (isLoading) return <p className="text-xs text-muted-foreground">Chargement des logs...</p>
  if (logs.length === 0) return <p className="text-xs text-muted-foreground">Aucun log disponible.</p>

  return (
    <div className="mt-3 space-y-1 max-h-48 overflow-y-auto">
      {logs.map(log => (
        <div
          key={log.id}
          className={`flex items-start gap-2 text-xs px-2 py-1 rounded ${
            log.success ? 'bg-green-50' : 'bg-red-50'
          }`}
        >
          <span className="font-mono text-muted-foreground shrink-0">
            {new Date(log.occurredAt).toLocaleTimeString('fr-FR')}
          </span>
          <span className={`font-medium shrink-0 ${log.success ? 'text-green-700' : 'text-red-700'}`}>
            {log.action}
          </span>
          <span className="text-muted-foreground truncate">
            {log.airtableRecordId && <span className="mr-1 font-mono">[{log.airtableRecordId}]</span>}
            {log.details ?? log.errorMessage ?? ''}
          </span>
        </div>
      ))}
    </div>
  )
}

function SyncJobRow({ job }: { job: AirtableSyncJob }) {
  const [expanded, setExpanded] = useState(false)
  const cancelJob = useCancelSyncJob()

  return (
    <>
      <tr className="border-b last:border-0 hover:bg-muted/30 transition-colors">
        <td className="py-3 px-4">
          <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[job.status]}`}>
            {STATUS_LABELS[job.status]}
          </span>
        </td>
        <td className="py-3 px-4 text-sm">{DIRECTION_LABELS[job.direction] ?? job.direction}</td>
        <td className="py-3 px-4 text-sm">
          <span className="text-green-600">{job.successRecords}</span>
          {' / '}
          <span className="text-muted-foreground">{job.totalRecords}</span>
          {job.failedRecords > 0 && (
            <span className="text-red-500 ml-1">({job.failedRecords} échecs)</span>
          )}
          {job.conflictRecords > 0 && (
            <span className="text-yellow-500 ml-1">({job.conflictRecords} conflits)</span>
          )}
        </td>
        <td className="py-3 px-4 text-xs text-muted-foreground">
          {job.startedAt ? new Date(job.startedAt).toLocaleString('fr-FR') : '—'}
        </td>
        <td className="py-3 px-4 text-xs text-muted-foreground">
          {job.completedAt ? new Date(job.completedAt).toLocaleString('fr-FR') : '—'}
        </td>
        <td className="py-3 px-4">
          <div className="flex items-center gap-1">
            <button
              onClick={() => setExpanded(v => !v)}
              className="p-1 hover:bg-muted rounded"
            >
              {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
            </button>
            {job.status === 'Pending' && (
              <button
                onClick={() => cancelJob.mutate(job.id)}
                className="p-1 hover:bg-muted rounded text-destructive"
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>
        </td>
      </tr>

      {expanded && (
        <tr className="bg-muted/20">
          <td colSpan={6} className="px-4 pb-3">
            {job.errorMessage && (
              <p className="text-xs text-destructive mb-2">{job.errorMessage}</p>
            )}
            <SyncJobLogs jobId={job.id} />
          </td>
        </tr>
      )}
    </>
  )
}

export function AirtableSyncHistoryPage() {
  const { data: jobs = [], isLoading } = useAirtableSyncJobs()
  const enqueueSync     = useEnqueueSync()
  const enqueueFullSync = useEnqueueFullSync()

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Historique de synchronisation</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Suivez l'état des synchronisations Airtable.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            onClick={() => enqueueSync.mutate()}
            disabled={enqueueSync.isPending}
          >
            {enqueueSync.isPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : <RefreshCw className="h-4 w-4 mr-2" />}
            Sync rapide
          </Button>
          <Button
            onClick={() => enqueueFullSync.mutate()}
            disabled={enqueueFullSync.isPending}
          >
            {enqueueFullSync.isPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : null}
            Sync complète
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Jobs récents</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : jobs.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              Aucun job de synchronisation trouvé.
            </p>
          ) : (
            <table className="w-full">
              <thead className="border-b">
                <tr>
                  <th className="py-2 px-4 text-left text-xs font-medium text-muted-foreground">Statut</th>
                  <th className="py-2 px-4 text-left text-xs font-medium text-muted-foreground">Direction</th>
                  <th className="py-2 px-4 text-left text-xs font-medium text-muted-foreground">Enregistrements</th>
                  <th className="py-2 px-4 text-left text-xs font-medium text-muted-foreground">Démarré</th>
                  <th className="py-2 px-4 text-left text-xs font-medium text-muted-foreground">Terminé</th>
                  <th className="py-2 px-4 text-left text-xs font-medium text-muted-foreground">Actions</th>
                </tr>
              </thead>
              <tbody>
                {jobs.map(job => (
                  <SyncJobRow key={job.id} job={job} />
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

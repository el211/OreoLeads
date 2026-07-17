import { useState } from 'react'
import { Download, Plus, Trash2, Clock, FileText } from 'lucide-react'
import {
  useReports, useCreateReport, useExportReport,
  useScheduledReports, useSaveScheduledReport, useDeleteScheduledReport,
} from '@/hooks/useAnalytics'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { ReportFormat, ReportFrequency, DateRangePreset } from '@/types/analytics'

const statusColors: Record<string, string> = {
  Pending: 'bg-yellow-100 text-yellow-700',
  Running: 'bg-blue-100 text-blue-700',
  Completed: 'bg-green-100 text-green-700',
  Failed: 'bg-red-100 text-red-700',
}

export function AnalyticsReportsPage() {
  const { data: reports, isLoading } = useReports()
  const { data: scheduled } = useScheduledReports()
  const createReport = useCreateReport()
  const exportReport = useExportReport()
  const saveScheduled = useSaveScheduledReport()
  const deleteScheduled = useDeleteScheduledReport()

  const [showCreate, setShowCreate] = useState(false)
  const [showSchedule, setShowSchedule] = useState(false)
  const [reportName, setReportName] = useState('')
  const [reportType, setReportType] = useState('dashboard')
  const [format, setFormat] = useState<ReportFormat>('Csv')
  const [preset] = useState<DateRangePreset>('Last30Days')
  const [schedName, setSchedName] = useState('')
  const [schedFreq, setSchedFreq] = useState<ReportFrequency>('Weekly')
  const [schedRecipients, setSchedRecipients] = useState('')

  const handleCreate = () => {
    createReport.mutate({ name: reportName || 'Rapport', reportType, format })
    setReportName('')
    setShowCreate(false)
  }

  const handleExport = () => {
    exportReport.mutate({ reportType, preset, format })
  }

  const handleSaveSchedule = () => {
    saveScheduled.mutate({
      name: schedName || 'Rapport planifie',
      reportType,
      frequency: schedFreq,
      recipients: schedRecipients,
      format,
      isEnabled: true,
    })
    setSchedName('')
    setSchedRecipients('')
    setShowSchedule(false)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Rapports</h1>
          <p className="text-muted-foreground">Generez et planifiez des rapports</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={handleExport} disabled={exportReport.isPending}>
            <Download className="mr-2 h-4 w-4" />
            Exporter
          </Button>
          <Button onClick={() => setShowCreate(!showCreate)}>
            <Plus className="mr-2 h-4 w-4" />
            Creer un rapport
          </Button>
        </div>
      </div>

      {/* Create report form */}
      {showCreate && (
        <Card>
          <CardContent className="pt-6">
            <div className="grid gap-4 md:grid-cols-4 items-end">
              <div>
                <label className="text-sm font-medium">Nom</label>
                <input className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={reportName} onChange={e => setReportName(e.target.value)} placeholder="Nom du rapport" />
              </div>
              <div>
                <label className="text-sm font-medium">Type</label>
                <select className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={reportType} onChange={e => setReportType(e.target.value)}>
                  <option value="dashboard">Dashboard</option>
                  <option value="leads">Leads</option>
                  <option value="emails">Emails</option>
                  <option value="automation">Automation</option>
                </select>
              </div>
              <div>
                <label className="text-sm font-medium">Format</label>
                <select className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={format} onChange={e => setFormat(e.target.value as ReportFormat)}>
                  <option value="Csv">CSV</option>
                  <option value="Excel">Excel</option>
                  <option value="Pdf">PDF</option>
                </select>
              </div>
              <Button onClick={handleCreate} disabled={createReport.isPending}>Creer</Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Reports list */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Rapports generes</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Chargement...</p>
          ) : !reports?.length ? (
            <p className="text-sm text-muted-foreground">Aucun rapport genere</p>
          ) : (
            <div className="space-y-2">
              {reports.map(r => (
                <div key={r.id} className="flex items-center justify-between rounded-lg border p-3">
                  <div className="flex items-center gap-3">
                    <FileText className="h-4 w-4 text-muted-foreground" />
                    <div>
                      <p className="text-sm font-medium">{r.name}</p>
                      <p className="text-xs text-muted-foreground">
                        {r.reportType} - {r.format} - {r.generatedAt ? new Date(r.generatedAt).toLocaleDateString('fr-FR') : ''}
                      </p>
                    </div>
                  </div>
                  <Badge className={statusColors[r.status] ?? ''}>{r.status}</Badge>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Scheduled reports */}
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Rapports planifies</h2>
        <Button variant="outline" size="sm" onClick={() => setShowSchedule(!showSchedule)}>
          <Clock className="mr-2 h-4 w-4" />
          Planifier
        </Button>
      </div>

      {showSchedule && (
        <Card>
          <CardContent className="pt-6">
            <div className="grid gap-4 md:grid-cols-4 items-end">
              <div>
                <label className="text-sm font-medium">Nom</label>
                <input className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={schedName} onChange={e => setSchedName(e.target.value)} placeholder="Nom" />
              </div>
              <div>
                <label className="text-sm font-medium">Frequence</label>
                <select className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={schedFreq} onChange={e => setSchedFreq(e.target.value as ReportFrequency)}>
                  <option value="Daily">Quotidien</option>
                  <option value="Weekly">Hebdomadaire</option>
                  <option value="Monthly">Mensuel</option>
                </select>
              </div>
              <div>
                <label className="text-sm font-medium">Destinataires</label>
                <input className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={schedRecipients} onChange={e => setSchedRecipients(e.target.value)} placeholder="email@test.com" />
              </div>
              <Button onClick={handleSaveSchedule} disabled={saveScheduled.isPending}>Planifier</Button>
            </div>
          </CardContent>
        </Card>
      )}

      {scheduled && scheduled.length > 0 && (
        <div className="space-y-2">
          {scheduled.map(s => (
            <Card key={s.id}>
              <CardContent className="py-4 flex items-center justify-between">
                <div>
                  <p className="font-medium">{s.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {s.frequency} - {s.recipients} - Prochain envoi: {s.nextSendAt ? new Date(s.nextSendAt).toLocaleDateString('fr-FR') : 'N/A'}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <Badge variant={s.isEnabled ? 'default' : 'outline'}>
                    {s.isEnabled ? 'Actif' : 'Inactif'}
                  </Badge>
                  <Button variant="ghost" size="sm" className="text-destructive"
                    onClick={() => deleteScheduled.mutate(s.id)}>
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

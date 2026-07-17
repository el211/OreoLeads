import { Activity, Clock, Server, AlertTriangle, Layers } from 'lucide-react'
import { useSystemMonitoring } from '@/hooks/useAnalytics'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

function MonitorCard({ title, value, unit, icon: Icon, color = 'text-primary' }: {
  title: string; value: number | string; unit?: string; icon: React.ElementType; color?: string
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className={`h-4 w-4 ${color}`} />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">
          {typeof value === 'number' ? value.toLocaleString() : value}
          {unit && <span className="text-sm font-normal text-muted-foreground ml-1">{unit}</span>}
        </div>
      </CardContent>
    </Card>
  )
}

export function AnalyticsMonitoringPage() {
  const { data: stats, isLoading } = useSystemMonitoring()

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-48 bg-muted rounded animate-pulse" />
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[1, 2, 3, 4].map(i => <div key={i} className="h-28 bg-muted rounded-lg animate-pulse" />)}
        </div>
      </div>
    )
  }

  if (!stats) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        Impossible de charger les donnees de monitoring
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Monitoring systeme</h1>
        <p className="text-muted-foreground">
          Etat du systeme en temps reel — Mis a jour le {new Date(stats.generatedAt).toLocaleString('fr-FR')}
        </p>
      </div>

      {/* Performance cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <MonitorCard
          title="Temps API moyen"
          value={stats.averageApiResponseMs.toFixed(0)}
          unit="ms"
          icon={Clock}
          color="text-blue-500"
        />
        <MonitorCard
          title="Duree workflow moyenne"
          value={stats.averageWorkflowDurationMs.toFixed(0)}
          unit="ms"
          icon={Activity}
          color="text-purple-500"
        />
        <MonitorCard
          title="Duree sync moyenne"
          value={stats.averageSyncDurationMs.toFixed(0)}
          unit="ms"
          icon={Server}
          color="text-green-500"
        />
        <MonitorCard
          title="Services actifs"
          value={stats.activeBackgroundServices}
          icon={Layers}
          color="text-indigo-500"
        />
      </div>

      {/* Queue stats */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card className={stats.queueDepth > 10 ? 'border-yellow-500' : ''}>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2">
              <Layers className="h-5 w-5 text-blue-500" />
              <p className="text-sm text-muted-foreground">File d'attente</p>
            </div>
            <p className="text-3xl font-bold mt-2">{stats.queueDepth}</p>
            <p className="text-xs text-muted-foreground mt-1">elements en attente</p>
          </CardContent>
        </Card>
        <Card className={stats.activeJobs > 5 ? 'border-yellow-500' : ''}>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2">
              <Activity className="h-5 w-5 text-green-500" />
              <p className="text-sm text-muted-foreground">Jobs actifs</p>
            </div>
            <p className="text-3xl font-bold mt-2">{stats.activeJobs}</p>
            <p className="text-xs text-muted-foreground mt-1">en cours d'execution</p>
          </CardContent>
        </Card>
        <Card className={stats.failedJobs > 0 ? 'border-red-500' : ''}>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-red-500" />
              <p className="text-sm text-muted-foreground">Jobs echoues</p>
            </div>
            <p className="text-3xl font-bold mt-2">{stats.failedJobs}</p>
            <p className="text-xs text-muted-foreground mt-1">en erreur</p>
          </CardContent>
        </Card>
      </div>

      {/* Status overview */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Resume du systeme</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            <div className="flex items-center justify-between py-2 border-b">
              <span className="text-sm">Workflows (duree moyenne)</span>
              <span className="text-sm font-medium">{stats.averageWorkflowDurationMs.toFixed(0)} ms</span>
            </div>
            <div className="flex items-center justify-between py-2 border-b">
              <span className="text-sm">Synchronisations (duree moyenne)</span>
              <span className="text-sm font-medium">{stats.averageSyncDurationMs.toFixed(0)} ms</span>
            </div>
            <div className="flex items-center justify-between py-2 border-b">
              <span className="text-sm">Profondeur de file</span>
              <span className="text-sm font-medium">{stats.queueDepth}</span>
            </div>
            <div className="flex items-center justify-between py-2 border-b">
              <span className="text-sm">Jobs actifs</span>
              <span className="text-sm font-medium">{stats.activeJobs}</span>
            </div>
            <div className="flex items-center justify-between py-2">
              <span className="text-sm">Jobs echoues</span>
              <span className={`text-sm font-medium ${stats.failedJobs > 0 ? 'text-red-600' : ''}`}>{stats.failedJobs}</span>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

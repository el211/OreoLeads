import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  LineChart, Line, AreaChart, Area,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts'
import { Users, Mail, Zap, Database, Bell, TrendingUp, TrendingDown } from 'lucide-react'
import { useExecutiveDashboard, useLeadTimeSeries, useEmailTimeSeries } from '@/hooks/useAnalytics'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import type { DateRangePreset } from '@/types/analytics'

const presets: { value: DateRangePreset; label: string }[] = [
  { value: 'Today', label: "Aujourd'hui" },
  { value: 'Last7Days', label: '7 jours' },
  { value: 'Last30Days', label: '30 jours' },
  { value: 'Last90Days', label: '90 jours' },
  { value: 'ThisYear', label: 'Cette annee' },
]

function KpiCard({ title, value, sub, icon: Icon, color = 'text-primary', trend }: {
  title: string; value: number | string; sub?: string; icon: React.ElementType; color?: string; trend?: number
}) {
  return (
    <Card className="hover:border-primary/30 transition-colors">
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className={`h-4 w-4 ${color}`} />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{typeof value === 'number' ? value.toLocaleString() : value}</div>
        <div className="flex items-center gap-2 mt-1">
          {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
          {trend !== undefined && trend !== 0 && (
            <span className={`inline-flex items-center text-xs font-medium ${trend > 0 ? 'text-green-600' : 'text-red-600'}`}>
              {trend > 0 ? <TrendingUp className="h-3 w-3 mr-0.5" /> : <TrendingDown className="h-3 w-3 mr-0.5" />}
              {Math.abs(trend).toFixed(1)}%
            </span>
          )}
        </div>
      </CardContent>
    </Card>
  )
}

export function AnalyticsDashboardPage() {
  const [preset, setPreset] = useState<DateRangePreset>('Last30Days')
  const { data: dashboard, isLoading } = useExecutiveDashboard(preset)
  const { data: leadSeries } = useLeadTimeSeries(preset)
  const { data: emailSeries } = useEmailTimeSeries(preset)

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-48 bg-muted rounded animate-pulse" />
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[1, 2, 3, 4].map(i => <div key={i} className="h-28 bg-muted rounded-lg animate-pulse" />)}
        </div>
        <div className="grid gap-4 lg:grid-cols-2">
          {[1, 2].map(i => <div key={i} className="h-72 bg-muted rounded-lg animate-pulse" />)}
        </div>
      </div>
    )
  }

  const leadChartData = (leadSeries ?? []).map(p => ({
    date: new Date(p.date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short' }),
    leads: p.value,
  }))

  const emailChartData = (emailSeries ?? []).map(p => ({
    date: new Date(p.date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short' }),
    emails: p.value,
  }))

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Analytics</h1>
          <p className="text-muted-foreground">Vue executive de votre activite</p>
        </div>
        <div className="flex gap-1 rounded-lg border p-1">
          {presets.map(p => (
            <Button
              key={p.value}
              variant={preset === p.value ? 'default' : 'ghost'}
              size="sm"
              onClick={() => setPreset(p.value)}
            >
              {p.label}
            </Button>
          ))}
        </div>
      </div>

      {!dashboard ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-muted-foreground">Aucune donnee disponible pour cette periode.</p>
          </CardContent>
        </Card>
      ) : (
        <>
          {/* KPI Cards */}
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 xl:grid-cols-5">
            <Link to="/analytics/forecast">
              <KpiCard title="Leads ce mois" value={dashboard.leads.thisMonth} icon={Users}
                sub={`${dashboard.leads.newProspects} nouveaux`} color="text-blue-500" />
            </Link>
            <Link to="/analytics/reports">
              <KpiCard title="Emails envoyes" value={dashboard.emails.sent} icon={Mail}
                sub={`Taux ouverture: ${dashboard.emails.openRate}%`} color="text-purple-500" />
            </Link>
            <Link to="/analytics/monitoring">
              <KpiCard title="Automations" value={dashboard.automation.totalExecutions} icon={Zap}
                sub={`${dashboard.automation.successRate}% succes`} color="text-orange-500" />
            </Link>
            <KpiCard title="Syncs Airtable" value={dashboard.airtable.totalSyncs} icon={Database}
              sub={`${dashboard.airtable.successRate}% succes`} color="text-green-500" />
            <KpiCard title="Relances" value={dashboard.pendingFollowUps} icon={Bell}
              sub="En attente" color="text-yellow-500" />
          </div>

          {/* Conversion rate + reply rate cards */}
          <div className="grid gap-4 md:grid-cols-3">
            <Card>
              <CardContent className="pt-6">
                <p className="text-sm text-muted-foreground">Taux de conversion</p>
                <p className="text-3xl font-bold mt-1">{dashboard.leads.conversionRate}%</p>
                <p className="text-xs text-muted-foreground mt-1">{dashboard.leads.converted} convertis sur {dashboard.leads.thisMonth}</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-sm text-muted-foreground">Taux de reponse</p>
                <p className="text-3xl font-bold mt-1">{dashboard.emails.replyRate}%</p>
                <p className="text-xs text-muted-foreground mt-1">{dashboard.emails.replied} reponses</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6">
                <p className="text-sm text-muted-foreground">Taux de rebond</p>
                <p className="text-3xl font-bold mt-1">{dashboard.emails.bounceRate}%</p>
                <p className="text-xs text-muted-foreground mt-1">{dashboard.emails.bounced} rebonds</p>
              </CardContent>
            </Card>
          </div>

          {/* Charts */}
          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Evolution des leads</CardTitle>
              </CardHeader>
              <CardContent>
                {leadChartData.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-8">Aucune donnee</p>
                ) : (
                  <ResponsiveContainer width="100%" height={280}>
                    <LineChart data={leadChartData}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                      <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                      <YAxis tick={{ fontSize: 12 }} />
                      <Tooltip />
                      <Line type="monotone" dataKey="leads" stroke="#6366f1" strokeWidth={2} dot={false} />
                    </LineChart>
                  </ResponsiveContainer>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Emails envoyes</CardTitle>
              </CardHeader>
              <CardContent>
                {emailChartData.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-8">Aucune donnee</p>
                ) : (
                  <ResponsiveContainer width="100%" height={280}>
                    <AreaChart data={emailChartData}>
                      <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                      <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                      <YAxis tick={{ fontSize: 12 }} />
                      <Tooltip />
                      <Area type="monotone" dataKey="emails" stroke="#8b5cf6" fill="#8b5cf6" fillOpacity={0.2} />
                    </AreaChart>
                  </ResponsiveContainer>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Automation bar chart */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Executions d'automatisation</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 md:grid-cols-4 mb-4">
                <div className="text-center">
                  <p className="text-2xl font-bold text-green-600">{dashboard.automation.successful}</p>
                  <p className="text-xs text-muted-foreground">Reussis</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-red-600">{dashboard.automation.failed}</p>
                  <p className="text-xs text-muted-foreground">Echoues</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold text-yellow-600">{dashboard.automation.retried}</p>
                  <p className="text-xs text-muted-foreground">Retentes</p>
                </div>
                <div className="text-center">
                  <p className="text-2xl font-bold">{dashboard.automation.averageDurationMs.toFixed(0)}ms</p>
                  <p className="text-xs text-muted-foreground">Duree moyenne</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}

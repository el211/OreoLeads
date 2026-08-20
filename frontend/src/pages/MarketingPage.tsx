import { useState } from 'react'
import {
  AreaChart, Area, LineChart, Line,
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, Legend,
} from 'recharts'
import {
  Mail, MousePointerClick, MessageSquare, TrendingUp,
  TrendingDown, Users, AlertCircle, Clock, Calendar,
} from 'lucide-react'
import {
  useExecutiveDashboard,
  useEmailAnalytics,
  useLeadTimeSeries,
  useEmailTimeSeries,
  useSalesFunnel,
} from '@/hooks/useAnalytics'
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

function KpiCard({
  title, value, sub, icon: Icon, color = 'text-primary', trend,
}: {
  title: string
  value: number | string
  sub?: string
  icon: React.ElementType
  color?: string
  trend?: number
}) {
  return (
    <Card className="hover:border-primary/30 transition-colors">
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className={`h-4 w-4 ${color}`} />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">
          {typeof value === 'number' ? value.toLocaleString() : value}
        </div>
        <div className="flex items-center gap-2 mt-1">
          {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
          {trend !== undefined && trend !== 0 && (
            <span className={`inline-flex items-center text-xs font-medium ${trend > 0 ? 'text-green-600' : 'text-red-600'}`}>
              {trend > 0
                ? <TrendingUp className="h-3 w-3 mr-0.5" />
                : <TrendingDown className="h-3 w-3 mr-0.5" />}
              {Math.abs(trend).toFixed(1)}%
            </span>
          )}
        </div>
      </CardContent>
    </Card>
  )
}

const DAY_LABELS: Record<string, string> = {
  Monday: 'Lundi',
  Tuesday: 'Mardi',
  Wednesday: 'Mercredi',
  Thursday: 'Jeudi',
  Friday: 'Vendredi',
  Saturday: 'Samedi',
  Sunday: 'Dimanche',
}

export function MarketingPage() {
  const [preset, setPreset] = useState<DateRangePreset>('Last30Days')

  const { data: dashboard, isLoading: loadingDashboard } = useExecutiveDashboard(preset)
  const { data: emailAnalytics, isLoading: loadingEmail } = useEmailAnalytics(preset)
  const { data: leadSeries } = useLeadTimeSeries(preset)
  const { data: emailSeries } = useEmailTimeSeries(preset)
  const { data: funnel } = useSalesFunnel(preset)

  const isLoading = loadingDashboard || loadingEmail

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-64 bg-muted rounded animate-pulse" />
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[1, 2, 3, 4, 5, 6].map(i => (
            <div key={i} className="h-28 bg-muted rounded-lg animate-pulse" />
          ))}
        </div>
        <div className="grid gap-4 lg:grid-cols-2">
          {[1, 2].map(i => (
            <div key={i} className="h-72 bg-muted rounded-lg animate-pulse" />
          ))}
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

  const funnelChartData = (funnel?.stages ?? []).map(s => ({
    name: s.name,
    prospects: s.count,
    conversion: parseFloat(s.conversionRate.toFixed(1)),
  }))

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Marketing</h1>
          <p className="text-muted-foreground">Performance campagnes et acquisition</p>
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

      {!dashboard && !emailAnalytics ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-muted-foreground">Aucune donnee disponible pour cette periode.</p>
          </CardContent>
        </Card>
      ) : (
        <>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
            <KpiCard
              title="Emails envoyes"
              value={dashboard?.emails.sent ?? 0}
              sub={`${dashboard?.emails.delivered ?? 0} delivres`}
              icon={Mail}
              color="text-blue-500"
            />
            <KpiCard
              title="Taux d'ouverture"
              value={`${emailAnalytics?.openRate ?? dashboard?.emails.openRate ?? 0}%`}
              sub={`${dashboard?.emails.opened ?? 0} ouvertures`}
              icon={Mail}
              color="text-indigo-500"
            />
            <KpiCard
              title="Taux de clic"
              value={`${emailAnalytics?.clickRate ?? dashboard?.emails.clickRate ?? 0}%`}
              sub={`${dashboard?.emails.clicked ?? 0} clics`}
              icon={MousePointerClick}
              color="text-violet-500"
            />
            <KpiCard
              title="Taux de reponse"
              value={`${emailAnalytics?.replyRate ?? dashboard?.emails.replyRate ?? 0}%`}
              sub={`${dashboard?.emails.replied ?? 0} reponses`}
              icon={MessageSquare}
              color="text-green-500"
            />
            <KpiCard
              title="Rebonds"
              value={`${emailAnalytics?.bounceRate ?? dashboard?.emails.bounceRate ?? 0}%`}
              sub={`${dashboard?.emails.bounced ?? 0} rebonds`}
              icon={AlertCircle}
              color="text-red-500"
            />
            <KpiCard
              title="Nouveaux leads"
              value={dashboard?.leads.newProspects ?? 0}
              sub={`Taux conversion: ${dashboard?.leads.conversionRate ?? 0}%`}
              icon={Users}
              color="text-orange-500"
            />
          </div>

          {emailAnalytics && (emailAnalytics.bestHourOfDay !== null || emailAnalytics.bestDayOfWeek) && (
            <div className="grid gap-4 md:grid-cols-2">
              {emailAnalytics.bestHourOfDay !== null && (
                <Card>
                  <CardContent className="pt-6 flex items-center gap-4">
                    <Clock className="h-8 w-8 text-primary shrink-0" />
                    <div>
                      <p className="text-sm text-muted-foreground">Meilleure heure d'envoi</p>
                      <p className="text-2xl font-bold">{emailAnalytics.bestHourOfDay}h00</p>
                      <p className="text-xs text-muted-foreground">
                        Ouverture moy. en {emailAnalytics.averageMinutesToOpen} min
                      </p>
                    </div>
                  </CardContent>
                </Card>
              )}
              {emailAnalytics.bestDayOfWeek && (
                <Card>
                  <CardContent className="pt-6 flex items-center gap-4">
                    <Calendar className="h-8 w-8 text-primary shrink-0" />
                    <div>
                      <p className="text-sm text-muted-foreground">Meilleur jour d'envoi</p>
                      <p className="text-2xl font-bold">
                        {DAY_LABELS[emailAnalytics.bestDayOfWeek] ?? emailAnalytics.bestDayOfWeek}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        Reponse moy. en {emailAnalytics.averageMinutesToReply} min
                      </p>
                    </div>
                  </CardContent>
                </Card>
              )}
            </div>
          )}

          <div className="grid gap-6 lg:grid-cols-2">
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
                      <Area
                        type="monotone"
                        dataKey="emails"
                        name="Emails"
                        stroke="#6366f1"
                        fill="#6366f1"
                        fillOpacity={0.2}
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Acquisition de leads</CardTitle>
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
                      <Line
                        type="monotone"
                        dataKey="leads"
                        name="Leads"
                        stroke="#f97316"
                        strokeWidth={2}
                        dot={false}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                )}
              </CardContent>
            </Card>
          </div>

          {emailAnalytics && emailAnalytics.topCampaigns.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Top campagnes</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b text-left text-muted-foreground">
                        <th className="pb-2 font-medium">Campagne</th>
                        <th className="pb-2 font-medium text-right">Envoyes</th>
                        <th className="pb-2 font-medium text-right">Ouverts</th>
                        <th className="pb-2 font-medium text-right">Clics</th>
                        <th className="pb-2 font-medium text-right">Taux ouverture</th>
                      </tr>
                    </thead>
                    <tbody>
                      {emailAnalytics.topCampaigns.map(c => (
                        <tr key={c.name} className="border-b last:border-0 hover:bg-muted/30 transition-colors">
                          <td className="py-2 font-medium truncate max-w-[200px]">{c.name}</td>
                          <td className="py-2 text-right">{c.sent.toLocaleString()}</td>
                          <td className="py-2 text-right">{c.opened.toLocaleString()}</td>
                          <td className="py-2 text-right">{c.clicked.toLocaleString()}</td>
                          <td className="py-2 text-right">
                            <span className={`font-medium ${c.openRate >= 20 ? 'text-green-600' : c.openRate >= 10 ? 'text-yellow-600' : 'text-red-600'}`}>
                              {c.openRate.toFixed(1)}%
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}

          {funnelChartData.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Funnel de conversion</CardTitle>
              </CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={260}>
                  <BarChart data={funnelChartData} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                    <XAxis type="number" tick={{ fontSize: 11 }} />
                    <YAxis dataKey="name" type="category" width={120} tick={{ fontSize: 11 }} />
                    <Tooltip />
                    <Legend />
                    <Bar dataKey="prospects" name="Prospects" fill="#6366f1" radius={[0, 4, 4, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          <div className="grid gap-4 md:grid-cols-3">
            <Card>
              <CardContent className="pt-6 text-center">
                <p className="text-sm text-muted-foreground">Desabonnements</p>
                <p className="text-3xl font-bold mt-1 text-red-500">
                  {emailAnalytics?.unsubscribeRate ?? 0}%
                </p>
                <p className="text-xs text-muted-foreground mt-1">
                  {dashboard?.emails.unsubscribed ?? 0} contacts
                </p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6 text-center">
                <p className="text-sm text-muted-foreground">Spam</p>
                <p className="text-3xl font-bold mt-1 text-orange-500">
                  {emailAnalytics?.spamRate ?? 0}%
                </p>
                <p className="text-xs text-muted-foreground mt-1">Taux signalement spam</p>
              </CardContent>
            </Card>
            <Card>
              <CardContent className="pt-6 text-center">
                <p className="text-sm text-muted-foreground">Leads convertis</p>
                <p className="text-3xl font-bold mt-1 text-green-600">
                  {dashboard?.leads.converted ?? 0}
                </p>
                <p className="text-xs text-muted-foreground mt-1">
                  sur {dashboard?.leads.thisMonth ?? 0} ce mois
                </p>
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  )
}

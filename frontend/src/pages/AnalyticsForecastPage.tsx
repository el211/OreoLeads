import {
  LineChart, Line, AreaChart, Area,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts'
import { TrendingUp } from 'lucide-react'
import { useForecastSummary } from '@/hooks/useAnalytics'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function AnalyticsForecastPage() {
  const { data: forecast, isLoading } = useForecastSummary()

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 w-48 bg-muted rounded animate-pulse" />
        <div className="grid gap-4 md:grid-cols-2">
          {[1, 2].map(i => <div key={i} className="h-28 bg-muted rounded-lg animate-pulse" />)}
        </div>
        <div className="h-72 bg-muted rounded-lg animate-pulse" />
      </div>
    )
  }

  const leadData = (forecast?.leadsForecast ?? []).map(p => ({
    date: new Date(p.date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short' }),
    prevision: p.value,
    bas: p.confidenceLow,
    haut: p.confidenceHigh,
  }))

  const conversionData = (forecast?.conversionsForecast ?? []).map(p => ({
    date: new Date(p.date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short' }),
    prevision: p.value,
    bas: p.confidenceLow,
    haut: p.confidenceHigh,
  }))

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Previsions</h1>
        <p className="text-muted-foreground">Projections basees sur les tendances des 30 derniers jours</p>
      </div>

      {/* Summary cards */}
      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-blue-500" />
              <p className="text-sm text-muted-foreground">Leads prevus ce mois</p>
            </div>
            <p className="text-3xl font-bold mt-2">{forecast?.projectedLeadsNextMonth.toFixed(0) ?? 0}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-green-500" />
              <p className="text-sm text-muted-foreground">Conversions prevues ce mois</p>
            </div>
            <p className="text-3xl font-bold mt-2">{forecast?.projectedConversionsNextMonth.toFixed(0) ?? 0}</p>
          </CardContent>
        </Card>
      </div>

      {/* Lead forecast chart */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Prevision des leads (30 jours)</CardTitle>
        </CardHeader>
        <CardContent>
          {leadData.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Pas assez de donnees pour generer une prevision</p>
          ) : (
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={leadData}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis dataKey="date" tick={{ fontSize: 10 }} interval={4} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Line type="monotone" dataKey="prevision" stroke="#6366f1" strokeWidth={2} strokeDasharray="6 3" dot={false} />
                <Line type="monotone" dataKey="bas" stroke="#94a3b8" strokeWidth={1} strokeDasharray="3 3" dot={false} />
                <Line type="monotone" dataKey="haut" stroke="#94a3b8" strokeWidth={1} strokeDasharray="3 3" dot={false} />
              </LineChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      {/* Conversion forecast chart */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Prevision des conversions (30 jours)</CardTitle>
        </CardHeader>
        <CardContent>
          {conversionData.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Pas assez de donnees pour generer une prevision</p>
          ) : (
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart data={conversionData}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis dataKey="date" tick={{ fontSize: 10 }} interval={4} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip />
                <Area type="monotone" dataKey="haut" stroke="transparent" fill="#6366f1" fillOpacity={0.1} />
                <Area type="monotone" dataKey="bas" stroke="transparent" fill="#ffffff" fillOpacity={1} />
                <Line type="monotone" dataKey="prevision" stroke="#6366f1" strokeWidth={2} dot={false} />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      <p className="text-xs text-muted-foreground text-center">
        Les previsions sont basees sur une regression lineaire des 30 derniers jours.
        Les intervalles de confiance representent +/- 20% de la valeur predite.
      </p>
    </div>
  )
}

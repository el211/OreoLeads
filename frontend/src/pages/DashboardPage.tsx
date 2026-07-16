import { Link } from 'react-router-dom'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, Legend,
} from 'recharts'
import { Users, UserCheck, Mail, Bell, TrendingUp, Plus } from 'lucide-react'
import { useDashboard } from '@/hooks/useDashboard'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { StatusBadge } from '@/components/ui/status-badge'
import { Badge } from '@/components/ui/badge'

const CHART_COLORS = ['#6366f1', '#8b5cf6', '#a78bfa', '#c4b5fd', '#ddd6fe', '#ede9fe', '#f3f0ff']

function StatCard({ title, value, icon: Icon, description, color = 'text-primary' }: {
  title: string
  value: number
  icon: React.ElementType
  description?: string
  color?: string
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className={`h-4 w-4 ${color}`} />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value.toLocaleString()}</div>
        {description && <p className="text-xs text-muted-foreground mt-1">{description}</p>}
      </CardContent>
    </Card>
  )
}

export function DashboardPage() {
  const { data: stats, isLoading, isError } = useDashboard()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-muted-foreground">Chargement...</div>
      </div>
    )
  }

  if (isError || !stats) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-destructive">Erreur lors du chargement des statistiques</div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">Vue d'ensemble de votre activité commerciale</p>
        </div>
        <Link to="/leads/new">
          <Button>
            <Plus className="mr-2 h-4 w-4" />
            Nouveau prospect
          </Button>
        </Link>
      </div>

      {/* Stat cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
        <StatCard title="Total prospects" value={stats.totalLeads} icon={Users} />
        <StatCard title="Nouveaux" value={stats.newLeads} icon={TrendingUp} color="text-blue-500" />
        <StatCard title="Clients" value={stats.clients} icon={UserCheck} color="text-green-500"
          description="Convertis ce mois" />
        <StatCard title="Emails envoyés" value={stats.emailsSent} icon={Mail} color="text-purple-500" />
        <StatCard title="Relances" value={stats.pendingFollowUps} icon={Bell} color="text-yellow-500"
          description="En attente" />
        <StatCard title="Ce mois" value={stats.leadsThisMonth} icon={TrendingUp} color="text-indigo-500"
          description="Nouveaux prospects" />
      </div>

      {/* Charts */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Status distribution */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Répartition par statut</CardTitle>
          </CardHeader>
          <CardContent>
            {stats.statusDistribution.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-8">Aucune donnée</p>
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={stats.statusDistribution} margin={{ top: 0, right: 10, left: -20, bottom: 60 }}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                  <XAxis dataKey="statusLabel" tick={{ fontSize: 11 }} angle={-35} textAnchor="end" />
                  <YAxis tick={{ fontSize: 12 }} />
                  <Tooltip formatter={(v) => [v, 'Prospects']} />
                  <Bar dataKey="count" fill="#6366f1" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Industry pie */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Répartition par secteur (top 7)</CardTitle>
          </CardHeader>
          <CardContent>
            {stats.industryDistribution.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-8">Aucune donnée</p>
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie
                    data={stats.industryDistribution}
                    dataKey="count"
                    nameKey="industry"
                    cx="50%"
                    cy="50%"
                    outerRadius={100}
                    label={({ industry, percent }: { industry?: string; percent?: number }) =>
                      `${industry ?? ''} (${((percent ?? 0) * 100).toFixed(0)}%)`
                    }
                    labelLine={false}
                  >
                    {stats.industryDistribution.map((_, i) => (
                      <Cell key={i} fill={CHART_COLORS[i % CHART_COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(v) => [v, 'Prospects']} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Top cities + Recent leads */}
      <div className="grid gap-6 lg:grid-cols-2">
        {/* Top cities */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Top villes</CardTitle>
          </CardHeader>
          <CardContent>
            {stats.cityDistribution.length === 0 ? (
              <p className="text-sm text-muted-foreground">Aucune donnée</p>
            ) : (
              <div className="space-y-2">
                {stats.cityDistribution.slice(0, 8).map((c) => (
                  <div key={c.city} className="flex items-center justify-between">
                    <span className="text-sm">{c.city}</span>
                    <div className="flex items-center gap-2">
                      <div
                        className="h-2 rounded-full bg-primary"
                        style={{ width: `${(c.count / stats.cityDistribution[0].count) * 120}px` }}
                      />
                      <span className="text-sm font-medium w-6 text-right">{c.count}</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Recent leads */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-base">Derniers prospects</CardTitle>
            <Link to="/leads">
              <Button variant="ghost" size="sm">Voir tout</Button>
            </Link>
          </CardHeader>
          <CardContent>
            {stats.recentLeads.length === 0 ? (
              <p className="text-sm text-muted-foreground">Aucun prospect</p>
            ) : (
              <div className="space-y-3">
                {stats.recentLeads.map((lead) => (
                  <Link key={lead.id} to={`/leads/${lead.id}`}>
                    <div className="flex items-center justify-between rounded-lg border p-3 hover:bg-accent transition-colors">
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium truncate">{lead.companyName}</p>
                        <p className="text-xs text-muted-foreground">
                          {lead.city}{lead.industry ? ` · ${lead.industry}` : ''}
                        </p>
                      </div>
                      <div className="ml-4 flex items-center gap-2">
                        <StatusBadge status={lead.status} label={lead.statusLabel} />
                        <Badge variant="outline" className="text-xs">{lead.score}</Badge>
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

import { useState } from 'react'
import { Plus, Eye, EyeOff, Trash2, GripVertical } from 'lucide-react'
import { useDashboards, useWidgets, useAddWidget, useUpdateWidget, useDeleteWidget } from '@/hooks/useAnalytics'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { WidgetType } from '@/types/analytics'

const widgetTypes: { value: WidgetType; label: string }[] = [
  { value: 'KpiCard', label: 'Carte KPI' },
  { value: 'LineChart', label: 'Graphique ligne' },
  { value: 'AreaChart', label: 'Graphique aire' },
  { value: 'BarChart', label: 'Graphique barres' },
  { value: 'PieChart', label: 'Camembert' },
  { value: 'Table', label: 'Tableau' },
]

export function AnalyticsWidgetsPage() {
  const { data: dashboards, isLoading: loadingDashboards } = useDashboards()
  const selectedDashboardId = dashboards?.[0]?.id
  const { data: widgets, isLoading: loadingWidgets } = useWidgets(selectedDashboardId)
  const addWidget = useAddWidget()
  const updateWidget = useUpdateWidget()
  const deleteWidget = useDeleteWidget()
  const [showAdd, setShowAdd] = useState(false)
  const [newTitle, setNewTitle] = useState('')
  const [newType, setNewType] = useState<WidgetType>('KpiCard')

  const handleAdd = () => {
    if (!selectedDashboardId || !newTitle.trim()) return
    addWidget.mutate({
      dashboardId: selectedDashboardId,
      title: newTitle,
      type: newType,
      sortOrder: (widgets?.length ?? 0) + 1,
    })
    setNewTitle('')
    setShowAdd(false)
  }

  const toggleVisibility = (widget: { id: string; title: string; isVisible: boolean }) => {
    updateWidget.mutate({
      id: widget.id,
      dto: { title: widget.title, isVisible: !widget.isVisible },
    })
  }

  if (loadingDashboards || loadingWidgets) {
    return <div className="text-center py-12 text-muted-foreground">Chargement...</div>
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Widgets</h1>
          <p className="text-muted-foreground">
            Gerez les widgets de votre dashboard
            {dashboards?.[0] && <span className="font-medium"> — {dashboards[0].name}</span>}
          </p>
        </div>
        <Button onClick={() => setShowAdd(!showAdd)}>
          <Plus className="mr-2 h-4 w-4" />
          Ajouter un widget
        </Button>
      </div>

      {showAdd && (
        <Card>
          <CardContent className="pt-6">
            <div className="flex gap-4 items-end">
              <div className="flex-1">
                <label className="text-sm font-medium">Titre</label>
                <input
                  className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={newTitle}
                  onChange={e => setNewTitle(e.target.value)}
                  placeholder="Nom du widget"
                />
              </div>
              <div>
                <label className="text-sm font-medium">Type</label>
                <select
                  className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
                  value={newType}
                  onChange={e => setNewType(e.target.value as WidgetType)}
                >
                  {widgetTypes.map(t => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
              </div>
              <Button onClick={handleAdd} disabled={addWidget.isPending}>
                Ajouter
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {!widgets?.length ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-muted-foreground">Aucun widget. Ajoutez-en un pour commencer.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-3">
          {widgets.map(w => (
            <Card key={w.id} className={`${!w.isVisible ? 'opacity-50' : ''} transition-opacity`}>
              <CardContent className="py-4 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <GripVertical className="h-4 w-4 text-muted-foreground" />
                  <div>
                    <p className="font-medium">{w.title}</p>
                    <Badge variant="outline" className="text-xs mt-1">{w.type}</Badge>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => toggleVisibility(w)}
                  >
                    {w.isVisible ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-destructive"
                    onClick={() => deleteWidget.mutate(w.id)}
                  >
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

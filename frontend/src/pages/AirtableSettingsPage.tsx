import { useState, useEffect } from 'react'
import { CheckCircle2, XCircle, Loader2, Eye, EyeOff, RefreshCw } from 'lucide-react'
import {
  useAirtableConfig,
  useUpdateAirtableConfig,
  useTestAirtableConnection,
  useAirtableTables,
  useAirtableSyncStats,
} from '@/hooks/useAirtable'
import type { SyncDirection, ConflictStrategy, UpdateAirtableConfiguration } from '@/types/airtable'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'

const SYNC_DIRECTIONS: { value: SyncDirection; label: string }[] = [
  { value: 'OreoLeadsToAirtable', label: 'OreoLeads → Airtable' },
  { value: 'AirtableToOreoLeads', label: 'Airtable → OreoLeads' },
  { value: 'Bidirectional',       label: 'Bidirectionnel' },
]

const CONFLICT_STRATEGIES: { value: ConflictStrategy; label: string }[] = [
  { value: 'OreoLeadsWins',      label: 'OreoLeads gagne' },
  { value: 'AirtableWins',       label: 'Airtable gagne' },
  { value: 'MostRecentWins',     label: 'Plus récent gagne' },
  { value: 'ManualResolution',   label: 'Résolution manuelle' },
]

export function AirtableSettingsPage() {
  const { data: config }       = useAirtableConfig()
  const { data: stats }        = useAirtableSyncStats()
  const updateConfig           = useUpdateAirtableConfig()
  const testConnection         = useTestAirtableConnection()
  const { refetch: loadTables, data: tables, isFetching: loadingTables } = useAirtableTables()

  const [form, setForm] = useState<UpdateAirtableConfiguration>({
    connectionName:   '',
    baseId:           '',
    tableIdOrName:    '',
    isEnabled:        false,
    syncDirection:    'OreoLeadsToAirtable',
    conflictStrategy: 'OreoLeadsWins',
  })
  const [showToken, setShowToken] = useState(false)
  const [saved, setSaved]         = useState(false)

  useEffect(() => {
    if (config) {
      setForm(f => ({
        ...f,
        connectionName:   config.connectionName,
        baseId:           config.baseId,
        tableIdOrName:    config.tableIdOrName,
        isEnabled:        config.isEnabled,
        syncDirection:    config.syncDirection,
        conflictStrategy: config.conflictStrategy,
      }))
    }
  }, [config])

  const handleSave = async () => {
    await updateConfig.mutateAsync(form)
    setSaved(true)
    setTimeout(() => setSaved(false), 3000)
  }

  const handleTest = () => testConnection.mutate()

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Paramètres Airtable</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Configurez la synchronisation entre OreoLeads et Airtable.
        </p>
      </div>

      {/* Status section */}
      {config && config.id !== '00000000-0000-0000-0000-000000000000' && (
        <Card>
          <CardHeader><CardTitle className="text-sm">Statut</CardTitle></CardHeader>
          <CardContent className="space-y-2 text-sm">
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Connexion</span>
              <Badge variant={config.hasAccessToken ? 'success' : 'secondary'}>
                {config.hasAccessToken ? 'Token configuré' : 'Non configuré'}
              </Badge>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Webhook</span>
              <Badge variant={config.hasWebhook ? 'success' : 'secondary'}>
                {config.hasWebhook ? 'Actif' : 'Inactif'}
              </Badge>
            </div>
            {config.lastSyncAt && (
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Dernière sync</span>
                <span>{new Date(config.lastSyncAt).toLocaleString('fr-FR')}</span>
              </div>
            )}
            {stats && (
              <div className="flex items-center justify-between">
                <span className="text-muted-foreground">Conflits actifs</span>
                <Badge variant={stats.activeConflicts > 0 ? 'destructive' : 'secondary'}>
                  {stats.activeConflicts}
                </Badge>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Connection credentials */}
      <Card>
        <CardHeader><CardTitle className="text-sm">Connexion Airtable</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="connection-name">Nom de la connexion</Label>
            <Input
              id="connection-name"
              placeholder="Connexion principale"
              value={form.connectionName}
              onChange={e => setForm(f => ({ ...f, connectionName: e.target.value }))}
              className="mt-1"
            />
          </div>

          <div>
            <Label htmlFor="access-token">
              Access Token{' '}
              {config?.hasAccessToken && (
                <Badge variant="outline" className="ml-2 text-xs">Configuré</Badge>
              )}
            </Label>
            <div className="relative mt-1">
              <Input
                id="access-token"
                type={showToken ? 'text' : 'password'}
                placeholder={config?.hasAccessToken ? '••••••••••••••••••••' : 'pat_xxxxxx...'}
                value={form.accessToken ?? ''}
                onChange={e => setForm(f => ({ ...f, accessToken: e.target.value }))}
                className="pr-10"
              />
              <button
                type="button"
                onClick={() => setShowToken(v => !v)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground"
              >
                {showToken ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
            <p className="text-xs text-muted-foreground mt-1">
              Le token est chiffré en base de données et jamais retourné en clair.
            </p>
          </div>

          <div>
            <Label htmlFor="base-id">Base ID</Label>
            <Input
              id="base-id"
              placeholder="appXXXXXXXXXXXXXX"
              value={form.baseId}
              onChange={e => setForm(f => ({ ...f, baseId: e.target.value }))}
              className="mt-1"
            />
          </div>

          <div>
            <Label htmlFor="table">Table ID ou Nom</Label>
            <div className="flex gap-2 mt-1">
              <Input
                id="table"
                placeholder="Leads"
                value={form.tableIdOrName}
                onChange={e => setForm(f => ({ ...f, tableIdOrName: e.target.value }))}
              />
              <Button
                variant="outline"
                size="sm"
                onClick={() => loadTables()}
                disabled={loadingTables}
              >
                {loadingTables ? <Loader2 className="h-4 w-4 animate-spin" /> : <RefreshCw className="h-4 w-4" />}
              </Button>
            </div>
            {tables && tables.length > 0 && (
              <div className="flex flex-wrap gap-1 mt-2">
                {tables.map(t => (
                  <button
                    key={t.id}
                    onClick={() => setForm(f => ({ ...f, tableIdOrName: t.name }))}
                    className="text-[10px] px-2 py-0.5 rounded border hover:bg-muted transition-colors"
                  >
                    {t.name}
                  </button>
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Sync configuration */}
      <Card>
        <CardHeader><CardTitle className="text-sm">Configuration de synchronisation</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label>Direction de synchronisation</Label>
            <div className="grid grid-cols-1 gap-2 mt-2">
              {SYNC_DIRECTIONS.map(({ value, label }) => (
                <label key={value} className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="syncDirection"
                    value={value}
                    checked={form.syncDirection === value}
                    onChange={() => setForm(f => ({ ...f, syncDirection: value }))}
                  />
                  <span className="text-sm">{label}</span>
                </label>
              ))}
            </div>
          </div>

          <div>
            <Label htmlFor="conflict-strategy">Stratégie de conflit</Label>
            <select
              id="conflict-strategy"
              value={form.conflictStrategy}
              onChange={e => setForm(f => ({ ...f, conflictStrategy: e.target.value as ConflictStrategy }))}
              className="mt-1 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            >
              {CONFLICT_STRATEGIES.map(({ value, label }) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-3 pt-2">
            <label className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={form.isEnabled}
                onChange={e => setForm(f => ({ ...f, isEnabled: e.target.checked }))}
                className="rounded"
              />
              <span className="text-sm font-medium">Activer la synchronisation</span>
            </label>
            {form.isEnabled
              ? <Badge variant="success">Activée</Badge>
              : <Badge variant="secondary">Désactivée</Badge>
            }
          </div>
        </CardContent>
      </Card>

      {/* Actions */}
      <div className="flex items-center gap-3 flex-wrap">
        <Button onClick={handleSave} disabled={updateConfig.isPending}>
          {updateConfig.isPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : null}
          Enregistrer
        </Button>

        <Button
          variant="outline"
          onClick={handleTest}
          disabled={testConnection.isPending}
        >
          {testConnection.isPending
            ? <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            : null
          }
          Tester la connexion
        </Button>

        {saved && (
          <span className="flex items-center gap-1 text-sm text-green-600">
            <CheckCircle2 className="h-4 w-4" />Enregistré
          </span>
        )}
      </div>

      {/* Test result */}
      {testConnection.data && (
        <Card className={testConnection.data.success
          ? 'border-green-300 bg-green-50 dark:bg-green-950/20'
          : 'border-destructive bg-destructive/5'
        }>
          <CardContent className="pt-4 flex items-center gap-3">
            {testConnection.data.success
              ? <CheckCircle2 className="h-5 w-5 text-green-600 shrink-0" />
              : <XCircle className="h-5 w-5 text-destructive shrink-0" />
            }
            <div>
              <p className="text-sm font-medium">{testConnection.data.message}</p>
              {testConnection.data.baseName && (
                <p className="text-xs text-muted-foreground">
                  Base : {testConnection.data.baseName}
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

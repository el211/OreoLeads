import { useState, useEffect } from 'react'
import { CheckCircle2, XCircle, Loader2, Eye, EyeOff, Zap } from 'lucide-react'
import { useAiProviders, useAiConfig, useUpdateAiConfig, useTestAiConnection } from '@/hooks/useEmails'
import type { AiProviderType, UpdateAiConfiguration } from '@/types/emails'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'

const PROVIDER_COLORS: Record<AiProviderType, string> = {
  Claude: 'bg-orange-100 text-orange-800',
  OpenAI: 'bg-green-100 text-green-800',
  Ollama: 'bg-blue-100 text-blue-800',
  GenericOpenAI: 'bg-purple-100 text-purple-800',
}

export function AiSettingsPage() {
  const { data: providers = [] } = useAiProviders()
  const { data: config } = useAiConfig()
  const updateConfig = useUpdateAiConfig()
  const testConnection = useTestAiConnection()

  const [form, setForm] = useState<UpdateAiConfiguration>({
    providerType: 'Claude',
    temperature: 0.7,
    maxTokens: 1000,
    timeoutSeconds: 30,
    isEnabled: false,
  })
  const [showKey, setShowKey] = useState(false)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (config) {
      setForm(f => ({
        ...f,
        providerType: config.providerType,
        model: config.model,
        temperature: config.temperature,
        maxTokens: config.maxTokens,
        timeoutSeconds: config.timeoutSeconds,
        isEnabled: config.isEnabled,
        baseUrl: config.baseUrl,
      }))
    }
  }, [config])

  const selectedProvider = providers.find(p => p.type === form.providerType)

  const handleSave = async () => {
    await updateConfig.mutateAsync(form)
    setSaved(true)
    setTimeout(() => setSaved(false), 3000)
  }

  const handleTest = () => testConnection.mutate()

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Paramètres IA</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Configurez le provider IA pour la génération des brouillons d'emails.
        </p>
      </div>

      {/* Provider selection */}
      <Card>
        <CardHeader><CardTitle className="text-sm">Provider</CardTitle></CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-3">
            {providers.map(p => (
              <button
                key={p.type}
                onClick={() => setForm(f => ({ ...f, providerType: p.type }))}
                className={`text-left rounded-lg border-2 p-3 transition-colors ${
                  form.providerType === p.type
                    ? 'border-primary bg-primary/5'
                    : 'border-border hover:border-muted-foreground'
                }`}
              >
                <div className="flex items-start justify-between mb-1">
                  <span className="font-medium text-sm">{p.name}</span>
                  <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium ${PROVIDER_COLORS[p.type]}`}>
                    {p.requiresApiKey ? 'API Key' : 'Local'}
                  </span>
                </div>
                <p className="text-xs text-muted-foreground">{p.description}</p>
                {p.defaultModels.length > 0 && (
                  <p className="text-[10px] text-muted-foreground mt-1">
                    Modèles : {p.defaultModels.slice(0, 2).join(', ')}
                  </p>
                )}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Credentials */}
      <Card>
        <CardHeader><CardTitle className="text-sm">Accès & Modèle</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          {selectedProvider?.requiresApiKey && (
            <div>
              <Label htmlFor="api-key">
                Clé API {config?.hasApiKey && <Badge variant="outline" className="ml-2 text-xs">Configurée</Badge>}
              </Label>
              <div className="relative mt-1">
                <Input
                  id="api-key"
                  type={showKey ? 'text' : 'password'}
                  placeholder={config?.hasApiKey ? '••••••••••••••••••••' : 'Entrez votre clé API'}
                  value={form.apiKey ?? ''}
                  onChange={e => setForm(f => ({ ...f, apiKey: e.target.value }))}
                  className="pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowKey(v => !v)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground"
                >
                  {showKey ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              <p className="text-xs text-muted-foreground mt-1">La clé est chiffrée en base de données. Elle n'est jamais affichée en clair.</p>
            </div>
          )}

          {selectedProvider?.requiresBaseUrl && (
            <div>
              <Label htmlFor="base-url">URL de base</Label>
              <Input
                id="base-url"
                placeholder={form.providerType === 'Ollama' ? 'http://localhost:11434' : 'https://api.example.com/v1'}
                value={form.baseUrl ?? ''}
                onChange={e => setForm(f => ({ ...f, baseUrl: e.target.value }))}
                className="mt-1"
              />
            </div>
          )}

          <div>
            <Label htmlFor="model">Modèle</Label>
            <Input
              id="model"
              placeholder={selectedProvider?.defaultModels[0] ?? 'Nom du modèle'}
              value={form.model ?? ''}
              onChange={e => setForm(f => ({ ...f, model: e.target.value }))}
              className="mt-1"
            />
            {selectedProvider?.defaultModels && selectedProvider.defaultModels.length > 0 && (
              <div className="flex flex-wrap gap-1 mt-2">
                {selectedProvider.defaultModels.map(m => (
                  <button
                    key={m}
                    onClick={() => setForm(f => ({ ...f, model: m }))}
                    className="text-[10px] px-2 py-0.5 rounded border hover:bg-muted transition-colors"
                  >
                    {m}
                  </button>
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Parameters */}
      <Card>
        <CardHeader><CardTitle className="text-sm">Paramètres de génération</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-3 gap-4">
            <div>
              <Label>Température <span className="text-muted-foreground">({form.temperature})</span></Label>
              <input
                type="range"
                min="0" max="1" step="0.1"
                value={form.temperature}
                onChange={e => setForm(f => ({ ...f, temperature: parseFloat(e.target.value) }))}
                className="w-full mt-2"
              />
              <div className="flex justify-between text-[10px] text-muted-foreground">
                <span>Précis</span><span>Créatif</span>
              </div>
            </div>
            <div>
              <Label htmlFor="max-tokens">Max tokens</Label>
              <Input
                id="max-tokens"
                type="number"
                min={100} max={4000} step={100}
                value={form.maxTokens}
                onChange={e => setForm(f => ({ ...f, maxTokens: parseInt(e.target.value) }))}
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="timeout">Timeout (s)</Label>
              <Input
                id="timeout"
                type="number"
                min={10} max={120}
                value={form.timeoutSeconds}
                onChange={e => setForm(f => ({ ...f, timeoutSeconds: parseInt(e.target.value) }))}
                className="mt-1"
              />
            </div>
          </div>

          <div className="flex items-center gap-3 pt-2">
            <label className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={form.isEnabled}
                onChange={e => setForm(f => ({ ...f, isEnabled: e.target.checked }))}
                className="rounded"
              />
              <span className="text-sm font-medium">Activer l'IA</span>
            </label>
            {form.isEnabled
              ? <Badge variant="success">Activée</Badge>
              : <Badge variant="secondary">Désactivée</Badge>
            }
          </div>
        </CardContent>
      </Card>

      {/* Actions */}
      <div className="flex items-center gap-3">
        <Button onClick={handleSave} disabled={updateConfig.isPending}>
          {updateConfig.isPending ? <Loader2 className="h-4 w-4 mr-2 animate-spin" /> : null}
          Enregistrer
        </Button>

        <Button variant="outline" onClick={handleTest} disabled={testConnection.isPending || !form.isEnabled}>
          {testConnection.isPending
            ? <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            : <Zap className="h-4 w-4 mr-2" />
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
        <Card className={testConnection.data.success ? 'border-green-300 bg-green-50 dark:bg-green-950/20' : 'border-destructive bg-destructive/5'}>
          <CardContent className="pt-4 flex items-center gap-3">
            {testConnection.data.success
              ? <CheckCircle2 className="h-5 w-5 text-green-600 shrink-0" />
              : <XCircle className="h-5 w-5 text-destructive shrink-0" />
            }
            <div>
              <p className="text-sm font-medium">{testConnection.data.message}</p>
              {testConnection.data.success && (
                <p className="text-xs text-muted-foreground">
                  {testConnection.data.model} · {testConnection.data.latencyMs} ms
                </p>
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

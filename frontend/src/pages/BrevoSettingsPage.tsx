import { useState, useEffect } from 'react'
import { Loader2, CheckCircle2, XCircle, Eye, EyeOff, MessageSquare } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

interface BrevoConfig {
  id: string
  hasApiKey: boolean
  senderName: string
  senderEmail: string
  replyTo?: string
  contactPhone?: string
  isEnabled: boolean
  testMode: boolean
  testModeEmail?: string
  dailyLimit: number
}

interface UpdateBrevoConfig {
  apiKey?: string
  senderName: string
  senderEmail: string
  replyTo?: string
  contactPhone?: string
  isEnabled: boolean
  testMode: boolean
  testModeEmail?: string
  dailyLimit: number
  webhookSecret?: string
}

function useBrevoConfig() {
  return useQuery<BrevoConfig>({
    queryKey: ['brevo-config'],
    queryFn: async () => (await api.get('/brevo')).data,
  })
}

function useUpdateBrevoConfig() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: UpdateBrevoConfig) => (await api.put('/brevo', dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['brevo-config'] }),
  })
}

function useTestBrevoConnection() {
  return useMutation({
    mutationFn: async () => (await api.post('/brevo/test')).data,
  })
}

export function BrevoSettingsPage() {
  const { data: config } = useBrevoConfig()
  const update = useUpdateBrevoConfig()
  const test   = useTestBrevoConnection()
  const [showKey, setShowKey] = useState(false)
  const [saved, setSaved]     = useState(false)

  const [form, setForm] = useState<UpdateBrevoConfig>({
    senderName: '',
    senderEmail: '',
    isEnabled: false,
    testMode: false,
    dailyLimit: 300,
  })

  useEffect(() => {
    if (config) {
      setForm(f => ({
        ...f,
        senderName:    config.senderName,
        senderEmail:   config.senderEmail,
        replyTo:       config.replyTo,
        contactPhone:  config.contactPhone,
        isEnabled:     config.isEnabled,
        testMode:      config.testMode,
        testModeEmail: config.testModeEmail,
        dailyLimit:    config.dailyLimit,
      }))
    }
  }, [config])

  const set = (field: keyof UpdateBrevoConfig) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(f => ({ ...f, [field]: e.target.value }))

  const handleSave = async () => {
    await update.mutateAsync(form)
    setSaved(true)
    setTimeout(() => setSaved(false), 3000)
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Paramètres Brevo</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Configuration pour l'envoi d'emails et de SMS via Brevo (Sendinblue).
        </p>
      </div>

      {/* API & Sender */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Connexion & Expéditeur</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-1">
            <Label htmlFor="apiKey">Clé API Brevo</Label>
            <div className="relative">
              <Input
                id="apiKey"
                type={showKey ? 'text' : 'password'}
                placeholder={config?.hasApiKey ? '••••••••••••••••' : 'xkeysib-…'}
                value={form.apiKey ?? ''}
                onChange={set('apiKey')}
                className="pr-10"
              />
              <button
                type="button"
                onClick={() => setShowKey(v => !v)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
              >
                {showKey ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              </button>
            </div>
            {config?.hasApiKey && !form.apiKey && (
              <p className="text-xs text-muted-foreground">Clé existante — laissez vide pour ne pas la modifier.</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="senderName">Nom expéditeur</Label>
              <Input id="senderName" value={form.senderName} onChange={set('senderName')} placeholder="Oreo Studios" />
            </div>
            <div className="space-y-1">
              <Label htmlFor="senderEmail">Email expéditeur</Label>
              <Input id="senderEmail" type="email" value={form.senderEmail} onChange={set('senderEmail')} placeholder="contact@oreostudios.fr" />
            </div>
          </div>

          <div className="space-y-1">
            <Label htmlFor="replyTo">Reply-To (optionnel)</Label>
            <Input id="replyTo" type="email" value={form.replyTo ?? ''} onChange={set('replyTo')} placeholder="support@oreostudios.fr" />
          </div>

          <div className="flex items-center gap-3">
            <Button variant="outline" size="sm" onClick={() => test.mutate()} disabled={test.isPending}>
              {test.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
              Tester la connexion
            </Button>
            {test.data && (
              <span className={`text-sm flex items-center gap-1 ${test.data.success ? 'text-green-600' : 'text-destructive'}`}>
                {test.data.success
                  ? <><CheckCircle2 className="h-4 w-4" />{test.data.accountName ?? 'Connexion OK'}</>
                  : <><XCircle className="h-4 w-4" />{test.data.message}</>}
              </span>
            )}
          </div>
        </CardContent>
      </Card>

      {/* SMS Contact */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <MessageSquare className="h-4 w-4 text-primary" />
            Coordonnées de rappel SMS
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Ces informations sont incluses dans la fin de chaque SMS généré par l'IA, pour que les prospects sachent comment vous contacter (le numéro Brevo ne reçoit pas les réponses).
          </p>
          <div className="space-y-1">
            <Label htmlFor="contactPhone">Numéro de téléphone à afficher dans les SMS</Label>
            <Input
              id="contactPhone"
              type="tel"
              value={form.contactPhone ?? ''}
              onChange={set('contactPhone')}
              placeholder="06 12 34 56 78"
            />
            <p className="text-xs text-muted-foreground">
              Ex : "Appelez le 06 12 34 56 78 — Oreo Studios" sera ajouté à la fin du SMS.
            </p>
          </div>
          <p className="text-xs text-muted-foreground">
            L'email de contact utilisé dans les SMS est l'email expéditeur configuré ci-dessus&nbsp;: <strong>{form.senderEmail || '—'}</strong>
          </p>
        </CardContent>
      </Card>

      {/* Options */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Options</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Activer Brevo</p>
              <p className="text-xs text-muted-foreground">Active l'envoi d'emails et SMS via Brevo.</p>
            </div>
            <input
              type="checkbox"
              className="h-4 w-4"
              checked={form.isEnabled}
              onChange={e => setForm(f => ({ ...f, isEnabled: e.target.checked }))}
            />
          </div>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Mode test</p>
              <p className="text-xs text-muted-foreground">Redirige tous les emails vers l'adresse de test.</p>
            </div>
            <input
              type="checkbox"
              className="h-4 w-4"
              checked={form.testMode}
              onChange={e => setForm(f => ({ ...f, testMode: e.target.checked }))}
            />
          </div>
          {form.testMode && (
            <div className="space-y-1">
              <Label htmlFor="testModeEmail">Email de test</Label>
              <Input id="testModeEmail" type="email" value={form.testModeEmail ?? ''} onChange={set('testModeEmail')} placeholder="test@example.com" />
            </div>
          )}
        </CardContent>
      </Card>

      <div className="flex items-center gap-3">
        <Button onClick={handleSave} disabled={update.isPending}>
          {update.isPending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
          Enregistrer
        </Button>
        {saved && <span className="text-sm text-green-600 flex items-center gap-1"><CheckCircle2 className="h-4 w-4" />Enregistré</span>}
      </div>
    </div>
  )
}

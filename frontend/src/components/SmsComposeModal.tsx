import { useState } from 'react'
import { MessageSquare, Loader2, X, Send, Sparkles, RotateCcw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useSendSms, useGenerateSms } from '@/hooks/useSms'

interface SmsComposeModalProps {
  leadId: string
  defaultPhone?: string
  companyName: string
  onClose: () => void
}

const MAX_CHARS = 160

export function SmsComposeModal({ leadId, defaultPhone, companyName, onClose }: SmsComposeModalProps) {
  const [phone, setPhone]                   = useState(defaultPhone ?? '')
  const [message, setMessage]               = useState('')
  const [customInstructions, setCustom]     = useState('')
  const [showCustom, setShowCustom]         = useState(false)
  const [aiInfo, setAiInfo]                 = useState<{ provider: string; model: string; ms: number } | null>(null)

  const sendSms    = useSendSms(leadId)
  const generateSms = useGenerateSms(leadId)

  const remaining = MAX_CHARS - message.length
  const canSend   = phone.trim().length > 0 && message.trim().length > 0 && remaining >= 0

  const handleGenerate = async () => {
    const result = await generateSms.mutateAsync({ customInstructions: customInstructions || undefined })
    setMessage(result.message)
    setAiInfo({ provider: result.providerUsed, model: result.modelUsed, ms: result.generationMs })
  }

  const handleSend = async () => {
    if (!canSend) return
    await sendSms.mutateAsync({ toPhone: phone.trim(), message })
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-background border rounded-lg shadow-xl w-full max-w-lg mx-4">

        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b">
          <div className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-primary" />
            <h2 className="font-semibold">Envoyer un SMS</h2>
          </div>
          <button type="button" onClick={onClose} className="rounded-full p-1 hover:bg-muted transition-colors">
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Body */}
        <div className="p-4 space-y-4">
          <p className="text-sm text-muted-foreground">
            Destinataire&nbsp;: <span className="font-medium text-foreground">{companyName}</span>
          </p>

          {/* Phone */}
          <div className="space-y-1">
            <Label htmlFor="sms-phone">Numéro de téléphone</Label>
            <Input
              id="sms-phone"
              type="tel"
              placeholder="+33612345678"
              value={phone}
              onChange={e => setPhone(e.target.value)}
            />
            <p className="text-xs text-muted-foreground">Format international recommandé&nbsp;: +33…</p>
          </div>

          {/* AI generate section */}
          <div className="rounded-lg border bg-muted/30 p-3 space-y-3">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium flex items-center gap-1.5">
                <Sparkles className="h-4 w-4 text-primary" />
                Rédaction par IA
              </p>
              <button
                type="button"
                onClick={() => setShowCustom(v => !v)}
                className="text-xs text-muted-foreground hover:text-foreground transition-colors"
              >
                {showCustom ? 'Masquer options' : 'Personnaliser'}
              </button>
            </div>

            {showCustom && (
              <div className="space-y-1">
                <Label htmlFor="sms-custom" className="text-xs">Instructions personnalisées (optionnel)</Label>
                <textarea
                  id="sms-custom"
                  value={customInstructions}
                  onChange={e => setCustom(e.target.value)}
                  placeholder="Ex : Mettre en avant la refonte site, ton urgentiste…"
                  rows={2}
                  className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                />
              </div>
            )}

            <Button
              variant="outline"
              size="sm"
              className="w-full"
              onClick={handleGenerate}
              disabled={generateSms.isPending}
            >
              {generateSms.isPending ? (
                <><Loader2 className="mr-2 h-4 w-4 animate-spin" />Génération en cours…</>
              ) : message ? (
                <><RotateCcw className="mr-2 h-4 w-4" />Régénérer</>
              ) : (
                <><Sparkles className="mr-2 h-4 w-4" />Générer avec l'IA</>
              )}
            </Button>

            {aiInfo && (
              <p className="text-xs text-muted-foreground text-center">
                Généré par {aiInfo.provider} · {aiInfo.model} · {aiInfo.ms} ms
              </p>
            )}
          </div>

          {/* Message */}
          <div className="space-y-1">
            <div className="flex items-center justify-between">
              <Label htmlFor="sms-message">Message</Label>
              <span className={`text-xs font-medium ${remaining < 0 ? 'text-destructive' : remaining < 20 ? 'text-orange-500' : 'text-muted-foreground'}`}>
                {message.length} / {MAX_CHARS}
              </span>
            </div>
            <textarea
              id="sms-message"
              value={message}
              onChange={e => setMessage(e.target.value)}
              placeholder="Bonjour, je vous contacte au sujet de…"
              rows={5}
              className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            />
            {remaining < 0 && (
              <p className="text-xs text-destructive">{Math.abs(remaining)} caractère{Math.abs(remaining) > 1 ? 's' : ''} en trop</p>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-2 p-4 border-t">
          <Button variant="outline" onClick={onClose} disabled={sendSms.isPending}>
            Annuler
          </Button>
          <Button onClick={handleSend} disabled={!canSend || sendSms.isPending}>
            {sendSms.isPending ? (
              <><Loader2 className="mr-2 h-4 w-4 animate-spin" />Envoi…</>
            ) : (
              <><Send className="mr-2 h-4 w-4" />Envoyer</>
            )}
          </Button>
        </div>
      </div>
    </div>
  )
}

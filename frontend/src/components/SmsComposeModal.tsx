import { useState } from 'react'
import { MessageSquare, Loader2, X, Send } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useSendSms } from '@/hooks/useSms'

interface SmsComposeModalProps {
  leadId: string
  defaultPhone?: string
  companyName: string
  onClose: () => void
}

const MAX_CHARS = 160

export function SmsComposeModal({ leadId, defaultPhone, companyName, onClose }: SmsComposeModalProps) {
  const [phone, setPhone]     = useState(defaultPhone ?? '')
  const [message, setMessage] = useState('')
  const sendSms = useSendSms(leadId)

  const remaining = MAX_CHARS - message.length
  const canSend   = phone.trim().length > 0 && message.trim().length > 0 && remaining >= 0

  const handleSend = async () => {
    if (!canSend) return
    await sendSms.mutateAsync({ toPhone: phone.trim(), message })
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-background border rounded-lg shadow-xl w-full max-w-md mx-4">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b">
          <div className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5 text-primary" />
            <h2 className="font-semibold">Envoyer un SMS</h2>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-full p-1 hover:bg-muted transition-colors"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        {/* Body */}
        <div className="p-4 space-y-4">
          <p className="text-sm text-muted-foreground">
            Destinataire&nbsp;: <span className="font-medium text-foreground">{companyName}</span>
          </p>

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

          <div className="space-y-1">
            <div className="flex items-center justify-between">
              <Label htmlFor="sms-message">Message</Label>
              <span className={`text-xs ${remaining < 0 ? 'text-destructive font-medium' : remaining < 20 ? 'text-orange-500' : 'text-muted-foreground'}`}>
                {remaining} / {MAX_CHARS}
              </span>
            </div>
            <textarea
              id="sms-message"
              value={message}
              onChange={e => setMessage(e.target.value)}
              placeholder="Bonjour, je vous contacte au sujet de…"
              rows={5}
              maxLength={MAX_CHARS}
              className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            />
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

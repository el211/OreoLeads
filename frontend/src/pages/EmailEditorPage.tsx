import { useState, useEffect } from 'react'
import { useParams, Link } from 'react-router-dom'
import { format } from 'date-fns'
import { fr } from 'date-fns/locale'
import {
  ArrowLeft, CheckCircle2, XCircle, RefreshCw, Copy, Loader2,
  History, Eye, EyeOff, ChevronDown, ChevronUp, RotateCcw,
} from 'lucide-react'
import {
  useEmailDraft, useUpdateEmailDraft, useApproveEmail,
  useRejectEmail, useRegenerateEmail, useRestoreEmailVersion, useSendEmail,
} from '@/hooks/useEmails'
import type { EmailDraftStatus } from '@/types/emails'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

const STATUS_STYLES: Record<EmailDraftStatus, string> = {
  Generated: 'bg-blue-100 text-blue-800',
  Edited:    'bg-yellow-100 text-yellow-800',
  Approved:  'bg-green-100 text-green-800',
  Rejected:  'bg-red-100 text-red-800',
  Sent:      'bg-gray-100 text-gray-800',
}

export function EmailEditorPage() {
  const { id } = useParams<{ id: string }>()
  const { data, isLoading } = useEmailDraft(id!)
  const updateDraft = useUpdateEmailDraft(id!)
  const approve = useApproveEmail(id!)
  const reject = useRejectEmail(id!)
  const regenerate = useRegenerateEmail(id!)
  const restoreVersion = useRestoreEmailVersion(id!)
  const sendEmail = useSendEmail(id!)

  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [showHistory, setShowHistory] = useState(false)
  const [showPreview, setShowPreview] = useState(false)
  const [copyOk, setCopyOk] = useState(false)
  const [compareVersion, setCompareVersion] = useState<number | null>(null)

  const draft = data?.draft
  const versions = data?.versions ?? []

  useEffect(() => {
    if (draft) {
      setSubject(draft.subject)
      setBody(draft.body)
    }
  }, [draft])

  if (isLoading) return <div className="text-muted-foreground py-8 text-center">Chargement...</div>
  if (!draft) return <div className="text-destructive py-8 text-center">Brouillon introuvable.</div>

  const isEditable = draft.status !== 'Approved' && draft.status !== 'Sent'
  const isDirty = subject !== draft.subject || body !== draft.body

  const handleSave = async () => {
    if (isDirty) await updateDraft.mutateAsync({ subject, body })
  }

  const handleCopy = () => {
    navigator.clipboard.writeText(`Objet : ${subject}\n\n${body}`)
    setCopyOk(true)
    setTimeout(() => setCopyOk(false), 2000)
  }

  const handleDownload = () => {
    const blob = new Blob([`Objet : ${subject}\n\n${body}`], { type: 'text/plain' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `email-${draft.companyName.toLowerCase().replace(/\s+/g, '-')}.txt`
    a.click()
    URL.revokeObjectURL(url)
  }

  const compareV = compareVersion !== null
    ? versions.find(v => v.version === compareVersion)
    : null

  return (
    <div className="max-w-4xl space-y-4">
      {/* Header */}
      <div className="flex items-center gap-3 flex-wrap">
        <Link to="/emails">
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h1 className="text-xl font-bold truncate">{draft.companyName}</h1>
            <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${STATUS_STYLES[draft.status]}`}>
              {draft.statusLabel}
            </span>
            <Badge variant="outline" className="text-xs">v{draft.currentVersion}</Badge>
          </div>
          <p className="text-xs text-muted-foreground">
            {draft.typeLabel} · {draft.providerUsed} · {draft.totalTokens} tokens · {draft.generationMs} ms
          </p>
        </div>

        {/* Action buttons */}
        <div className="flex gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={handleCopy}>
            <Copy className="h-4 w-4 mr-1" />{copyOk ? 'Copié !' : 'Copier'}
          </Button>
          <Button variant="outline" size="sm" onClick={handleDownload}>
            Télécharger
          </Button>
          <Button
            variant="outline" size="sm"
            onClick={() => regenerate.mutate()}
            disabled={regenerate.isPending || draft.status === 'Sent'}
          >
            {regenerate.isPending
              ? <Loader2 className="h-4 w-4 mr-1 animate-spin" />
              : <RefreshCw className="h-4 w-4 mr-1" />
            }
            Régénérer
          </Button>
          {draft.status !== 'Approved' && draft.status !== 'Sent' && draft.status !== 'Rejected' && (
            <Button
              size="sm"
              className="bg-green-600 hover:bg-green-700 text-white"
              onClick={() => approve.mutate()}
              disabled={approve.isPending}
            >
              <CheckCircle2 className="h-4 w-4 mr-1" />Approuver
            </Button>
          )}
          {draft.status !== 'Sent' && draft.status !== 'Rejected' && (
            <Button
              size="sm"
              variant="destructive"
              onClick={() => reject.mutate()}
              disabled={reject.isPending}
            >
              <XCircle className="h-4 w-4 mr-1" />Rejeter
            </Button>
          )}
        </div>
      </div>

      {/* Approval notice + Send button */}
      {draft.status === 'Approved' && (
        <div className="rounded-md bg-green-50 dark:bg-green-950/20 border border-green-200 dark:border-green-800 px-4 py-3 text-sm text-green-700 dark:text-green-300 flex items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            Email approuvé — cliquez sur Envoyer pour l'expédier via votre boîte OVH.
          </div>
          <Button
            size="sm"
            className="bg-green-600 hover:bg-green-700 text-white shrink-0"
            disabled={sendEmail.isPending}
            onClick={async () => {
              try {
                await sendEmail.mutateAsync()
                alert('Email envoyé avec succès !')
              } catch (err: any) {
                const serverMsg =
                  typeof err?.response?.data === 'string'
                    ? err.response.data
                    : err?.response?.data?.message ?? err?.message
                alert(`Erreur lors de l'envoi : ${serverMsg ?? 'erreur inconnue'}`)
              }
            }}
          >
            {sendEmail.isPending ? <Loader2 className="h-4 w-4 animate-spin mr-1" /> : null}
            Envoyer
          </Button>
        </div>
      )}

      {/* Editor */}
      <div className={`grid gap-4 ${compareV ? 'md:grid-cols-2' : ''}`}>
        <Card>
          {compareV && <CardHeader className="pb-2"><CardTitle className="text-xs">Version actuelle (v{draft.currentVersion})</CardTitle></CardHeader>}
          <CardContent className="pt-4 space-y-3">
            <div>
              <Label htmlFor="subject">Objet</Label>
              <Input
                id="subject"
                value={subject}
                onChange={e => setSubject(e.target.value)}
                disabled={!isEditable}
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="body">Corps de l'email</Label>
              <textarea
                id="body"
                value={body}
                onChange={e => setBody(e.target.value)}
                disabled={!isEditable}
                rows={16}
                className="flex w-full mt-1 rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm resize-y focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:opacity-60 font-mono"
              />
            </div>
            {isDirty && isEditable && (
              <Button size="sm" onClick={handleSave} disabled={updateDraft.isPending}>
                {updateDraft.isPending ? <Loader2 className="h-4 w-4 mr-1 animate-spin" /> : null}
                Enregistrer les modifications
              </Button>
            )}
          </CardContent>
        </Card>

        {/* Compare pane */}
        {compareV && (
          <Card className="border-yellow-200 dark:border-yellow-800">
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <CardTitle className="text-xs">Version {compareV.version}</CardTitle>
                <Button
                  size="sm" variant="outline"
                  onClick={() => restoreVersion.mutate(compareV.version)}
                  disabled={restoreVersion.isPending}
                >
                  <RotateCcw className="h-3 w-3 mr-1" />Restaurer
                </Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                <Label>Objet</Label>
                <div className="mt-1 px-3 py-2 rounded-md border bg-muted text-sm">{compareV.subject}</div>
              </div>
              <div>
                <Label>Corps</Label>
                <div className="mt-1 px-3 py-2 rounded-md border bg-muted text-sm whitespace-pre-wrap font-mono" style={{ minHeight: '24rem' }}>
                  {compareV.body}
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Summary & CTA */}
      {(draft.summary || draft.callToAction) && (
        <div className="grid gap-3 md:grid-cols-2">
          {draft.summary && (
            <Card>
              <CardHeader className="pb-1"><CardTitle className="text-xs">Résumé IA</CardTitle></CardHeader>
              <CardContent><p className="text-sm text-muted-foreground">{draft.summary}</p></CardContent>
            </Card>
          )}
          {draft.callToAction && (
            <Card>
              <CardHeader className="pb-1"><CardTitle className="text-xs">Call To Action</CardTitle></CardHeader>
              <CardContent><p className="text-sm font-medium">{draft.callToAction}</p></CardContent>
            </Card>
          )}
        </div>
      )}

      {/* Preview */}
      <Card>
        <CardHeader className="pb-2 cursor-pointer" onClick={() => setShowPreview(v => !v)}>
          <div className="flex items-center justify-between">
            <CardTitle className="text-sm flex items-center gap-2">
              {showPreview ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
              Aperçu
            </CardTitle>
            {showPreview ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </div>
        </CardHeader>
        {showPreview && (
          <CardContent>
            <div className="rounded-md border p-4 bg-white text-gray-900">
              <div className="text-xs text-gray-500 mb-2">Objet : <span className="font-medium text-gray-900">{subject}</span></div>
              <hr className="mb-3 border-gray-200" />
              <div className="text-sm whitespace-pre-wrap font-sans text-gray-900">{body}</div>
            </div>
          </CardContent>
        )}
      </Card>

      {/* Version history */}
      {versions.length > 1 && (
        <Card>
          <CardHeader className="pb-2 cursor-pointer" onClick={() => setShowHistory(v => !v)}>
            <div className="flex items-center justify-between">
              <CardTitle className="text-sm flex items-center gap-2">
                <History className="h-4 w-4" />
                Historique ({versions.length} versions)
              </CardTitle>
              {showHistory ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
            </div>
          </CardHeader>
          {showHistory && (
            <CardContent>
              <div className="space-y-2">
                {versions.map(v => (
                  <div key={v.id}
                    className={`flex items-center justify-between text-sm py-2 px-3 rounded border cursor-pointer transition-colors ${
                      compareVersion === v.version ? 'border-primary bg-primary/5' : 'hover:bg-muted'
                    }`}
                    onClick={() => setCompareVersion(compareVersion === v.version ? null : v.version)}
                  >
                    <div className="flex items-center gap-3">
                      <Badge variant="outline" className="text-xs">v{v.version}</Badge>
                      <span className="text-muted-foreground text-xs">{v.providerUsed} · {v.modelUsed}</span>
                      <span className="text-muted-foreground text-xs">{v.generationMs} ms</span>
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {format(new Date(v.createdAt), 'dd/MM HH:mm', { locale: fr })}
                    </span>
                  </div>
                ))}
              </div>
              {compareVersion !== null && (
                <p className="text-xs text-muted-foreground mt-2">Cliquez à nouveau sur une version pour fermer la comparaison.</p>
              )}
            </CardContent>
          )}
        </Card>
      )}
    </div>
  )
}

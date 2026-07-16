import { useState } from 'react'
import { Link } from 'react-router-dom'
import { format } from 'date-fns'
import { fr } from 'date-fns/locale'
import { Mail, CheckCircle2, XCircle, Clock, Bot } from 'lucide-react'
import { useEmailDrafts, useAiStats } from '@/hooks/useEmails'
import type { EmailDraftStatus } from '@/types/emails'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'

const STATUS_STYLES: Record<EmailDraftStatus, string> = {
  Generated: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
  Edited:    'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
  Approved:  'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
  Rejected:  'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
  Sent:      'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
}

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: '', label: 'Tous les statuts' },
  { value: 'Generated', label: 'Générés' },
  { value: 'Edited', label: 'Modifiés' },
  { value: 'Approved', label: 'Approuvés' },
  { value: 'Rejected', label: 'Rejetés' },
  { value: 'Sent', label: 'Envoyés' },
]

export function EmailDraftsPage() {
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState('')
  const { data, isLoading } = useEmailDrafts(page, 20, statusFilter || undefined)
  const { data: stats } = useAiStats()

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold">Brouillons d'emails</h1>
          {stats && (
            <p className="text-sm text-muted-foreground mt-1">
              {stats.totalGenerations} générations · {stats.totalTokens.toLocaleString()} tokens · {Math.round(stats.averageGenerationMs)} ms/email
            </p>
          )}
        </div>
        <Link to="/settings/ai">
          <Button variant="outline" size="sm">
            <Bot className="h-4 w-4 mr-2" />Paramètres IA
          </Button>
        </Link>
      </div>

      {/* Filters */}
      <div className="flex gap-2 flex-wrap">
        {STATUS_OPTIONS.map(opt => (
          <button
            key={opt.value}
            onClick={() => { setStatusFilter(opt.value); setPage(1) }}
            className={`text-sm px-3 py-1.5 rounded-full border transition-colors ${
              statusFilter === opt.value
                ? 'bg-primary text-primary-foreground border-primary'
                : 'border-border hover:bg-muted'
            }`}
          >
            {opt.label}
          </button>
        ))}
      </div>

      {/* Stats by provider */}
      {stats && Object.keys(stats.generationsByProvider).length > 0 && (
        <div className="flex gap-3 flex-wrap">
          {Object.entries(stats.generationsByProvider).map(([provider, count]) => (
            <Badge key={provider} variant="outline">
              <Bot className="h-3 w-3 mr-1" />{provider} ({count})
            </Badge>
          ))}
        </div>
      )}

      {/* List */}
      {isLoading ? (
        <p className="text-muted-foreground text-center py-8">Chargement...</p>
      ) : !data?.items.length ? (
        <Card>
          <CardContent className="py-12 text-center text-muted-foreground">
            <Mail className="h-8 w-8 mx-auto mb-3 opacity-30" />
            <p>Aucun brouillon. Générez un email depuis la fiche d'un prospect.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {data.items.map(draft => (
            <Link key={draft.id} to={`/emails/${draft.id}`}>
              <Card className="hover:bg-muted/50 transition-colors cursor-pointer">
                <CardContent className="py-3 px-4">
                  <div className="flex items-start gap-3">
                    <Mail className="h-4 w-4 mt-0.5 text-muted-foreground shrink-0" />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-medium text-sm truncate">{draft.subject}</span>
                        <span className={`text-[10px] px-2 py-0.5 rounded-full font-medium ${STATUS_STYLES[draft.status]}`}>
                          {draft.statusLabel}
                        </span>
                      </div>
                      <div className="flex items-center gap-3 mt-0.5 text-xs text-muted-foreground">
                        <span>{draft.companyName}</span>
                        {draft.businessScore !== undefined && draft.businessScore !== null && (
                          <span>Score {draft.businessScore}/100</span>
                        )}
                        <span>{draft.providerUsed}</span>
                        <span>{draft.totalTokens} tokens</span>
                      </div>
                    </div>
                    <div className="flex flex-col items-end gap-1 shrink-0">
                      <span className="text-xs text-muted-foreground">
                        {format(new Date(draft.createdAt), 'dd MMM HH:mm', { locale: fr })}
                      </span>
                      <div className="flex items-center gap-1">
                        {draft.status === 'Approved' && <CheckCircle2 className="h-3.5 w-3.5 text-green-500" />}
                        {draft.status === 'Rejected' && <XCircle className="h-3.5 w-3.5 text-destructive" />}
                        {(draft.status === 'Generated' || draft.status === 'Edited') && (
                          <Clock className="h-3.5 w-3.5 text-yellow-500" />
                        )}
                        <span className="text-[10px] text-muted-foreground">v{draft.currentVersion}</span>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>
            Précédent
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {data.page} / {data.totalPages}
          </span>
          <Button variant="outline" size="sm" disabled={page >= data.totalPages} onClick={() => setPage(p => p + 1)}>
            Suivant
          </Button>
        </div>
      )}
    </div>
  )
}

import { useState } from 'react'
import { Link } from 'react-router-dom'
import { format } from 'date-fns'
import { fr } from 'date-fns/locale'
import { ArrowLeft, Search, Clock, TrendingUp } from 'lucide-react'
import { useSearchHistory } from '@/hooks/useSearch'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import type { SearchHistory } from '@/types/search'

function buildCriteriaSummary(h: SearchHistory): string {
  const parts: string[] = []
  if (h.keywords) parts.push(`"${h.keywords}"`)
  if (h.nafCode) parts.push(`NAF ${h.nafCode}`)
  if (h.department) parts.push(`Dép. ${h.department}`)
  if (h.postalCode) parts.push(h.postalCode)
  if (h.city) parts.push(h.city)
  if (h.region) parts.push(h.region)
  if (h.industry) parts.push(h.industry)
  return parts.length > 0 ? parts.join(' · ') : 'Tous les critères'
}

function StatusBadge({ status }: { status: string }) {
  if (status === 'Imported') return <Badge variant="success">Importé</Badge>
  if (status === 'Searched') return <Badge variant="secondary">Recherché</Badge>
  return <Badge variant="outline">{status}</Badge>
}

export function SearchHistoryPage() {
  const [page, setPage] = useState(1)
  const { data, isLoading } = useSearchHistory(page, 20)

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link to="/search">
          <Button variant="ghost" size="icon"><ArrowLeft className="h-4 w-4" /></Button>
        </Link>
        <div>
          <h1 className="text-2xl font-bold">Historique des recherches</h1>
          {data && (
            <p className="text-sm text-muted-foreground mt-1">
              {data.totalCount} recherche{data.totalCount !== 1 ? 's' : ''}
            </p>
          )}
        </div>
        <Link to="/search" className="ml-auto">
          <Button size="sm">
            <Search className="h-4 w-4 mr-2" />Nouvelle recherche
          </Button>
        </Link>
      </div>

      {isLoading && (
        <div className="text-center py-16 text-muted-foreground">Chargement...</div>
      )}

      {!isLoading && data?.items.length === 0 && (
        <Card>
          <CardContent className="py-16 text-center text-muted-foreground">
            <Search className="h-8 w-8 mx-auto mb-3 opacity-30" />
            <p>Aucune recherche effectuée</p>
            <Link to="/search">
              <Button variant="outline" size="sm" className="mt-4">Lancer une recherche</Button>
            </Link>
          </CardContent>
        </Card>
      )}

      {data && data.items.length > 0 && (
        <>
          <div className="space-y-3">
            {data.items.map(h => (
              <Card key={h.id} className="hover:bg-accent/5 transition-colors">
                <CardContent className="pt-4">
                  <div className="flex items-start justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-medium text-sm truncate">
                          {buildCriteriaSummary(h)}
                        </span>
                        <StatusBadge status={h.status} />
                        <Badge variant="outline" className="text-xs">{h.provider}</Badge>
                      </div>

                      {/* Stats */}
                      <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
                        <span className="flex items-center gap-1">
                          <TrendingUp className="h-3 w-3" />
                          {h.totalFound.toLocaleString()} trouvé{h.totalFound !== 1 ? 's' : ''}
                        </span>
                        {h.newLeads > 0 && (
                          <span className="text-green-600 dark:text-green-400">
                            +{h.newLeads} nouveau{h.newLeads !== 1 ? 'x' : ''}
                          </span>
                        )}
                        {h.updatedLeads > 0 && (
                          <span className="text-blue-600 dark:text-blue-400">
                            {h.updatedLeads} enrichi{h.updatedLeads !== 1 ? 's' : ''}
                          </span>
                        )}
                        {h.duplicates > 0 && <span>{h.duplicates} doublon{h.duplicates !== 1 ? 's' : ''}</span>}
                        {h.errors > 0 && <span className="text-destructive">{h.errors} erreur{h.errors !== 1 ? 's' : ''}</span>}
                        <span className="flex items-center gap-1">
                          <Clock className="h-3 w-3" />
                          {h.durationMs} ms
                        </span>
                      </div>

                      {/* Search params summary */}
                      <div className="mt-1.5 flex flex-wrap gap-2">
                        {h.activeOnly && <Badge variant="secondary" className="text-xs">Actives seulement</Badge>}
                        <Badge variant="outline" className="text-xs">Max {h.maxResults}</Badge>
                      </div>
                    </div>

                    <div className="text-right text-xs text-muted-foreground shrink-0">
                      {format(new Date(h.createdAt), 'dd MMM yyyy', { locale: fr })}
                      <br />
                      {format(new Date(h.createdAt), 'HH:mm', { locale: fr })}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>

          {/* Pagination */}
          {data.totalPages > 1 && (
            <div className="flex items-center justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={!data.hasPreviousPage}
                onClick={() => setPage(p => p - 1)}
              >
                Précédent
              </Button>
              <span className="text-sm text-muted-foreground">
                Page {data.page} / {data.totalPages}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={!data.hasNextPage}
                onClick={() => setPage(p => p + 1)}
              >
                Suivant
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  )
}

import { useState } from 'react'
import { ChevronDown, ChevronUp, Loader2 } from 'lucide-react'
import {
  useAirtableConflicts,
  useResolveConflict,
} from '@/hooks/useAirtable'
import type { AirtableConflict } from '@/types/airtable'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

function ConflictRow({ conflict }: { conflict: AirtableConflict }) {
  const [expanded, setExpanded]   = useState(false)
  const resolveConflict           = useResolveConflict()

  const handleResolve = (winner: 'oreoleads' | 'airtable') => {
    resolveConflict.mutate({ id: conflict.id, winnerSource: winner })
  }

  const parseJson = (data: string | null) => {
    if (!data) return null
    try { return JSON.parse(data) } catch { return data }
  }

  const oreoData     = parseJson(conflict.conflictOreoLeadsData)
  const airtableData = parseJson(conflict.conflictAirtableData)

  return (
    <div className="border rounded-lg overflow-hidden">
      <div className="flex items-center justify-between p-4 hover:bg-muted/30 cursor-pointer"
           onClick={() => setExpanded(v => !v)}>
        <div className="space-y-0.5">
          <p className="text-sm font-medium">
            {conflict.leadName ?? 'Lead sans nom'}
          </p>
          <p className="text-xs text-muted-foreground font-mono">
            Record Airtable : {conflict.airtableRecordId}
          </p>
          {conflict.conflictDetectedAt && (
            <p className="text-xs text-muted-foreground">
              Détecté le {new Date(conflict.conflictDetectedAt).toLocaleString('fr-FR')}
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          <Badge variant="destructive">Conflit</Badge>
          {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
        </div>
      </div>

      {expanded && (
        <div className="border-t bg-muted/10 p-4 space-y-4">
          <div className="grid grid-cols-2 gap-4">
            {/* OreoLeads data */}
            <div>
              <p className="text-xs font-semibold text-blue-700 mb-2">OreoLeads</p>
              <pre className="text-xs bg-white border rounded p-2 overflow-auto max-h-40 whitespace-pre-wrap">
                {oreoData ? JSON.stringify(oreoData, null, 2) : 'Aucune donnée'}
              </pre>
            </div>

            {/* Airtable data */}
            <div>
              <p className="text-xs font-semibold text-orange-700 mb-2">Airtable</p>
              <pre className="text-xs bg-white border rounded p-2 overflow-auto max-h-40 whitespace-pre-wrap">
                {airtableData ? JSON.stringify(airtableData, null, 2) : 'Aucune donnée'}
              </pre>
            </div>
          </div>

          <div className="flex gap-2">
            <Button
              size="sm"
              variant="outline"
              onClick={() => handleResolve('oreoleads')}
              disabled={resolveConflict.isPending}
              className="border-blue-300 text-blue-700 hover:bg-blue-50"
            >
              {resolveConflict.isPending ? <Loader2 className="h-3 w-3 mr-1 animate-spin" /> : null}
              OreoLeads gagne
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => handleResolve('airtable')}
              disabled={resolveConflict.isPending}
              className="border-orange-300 text-orange-700 hover:bg-orange-50"
            >
              {resolveConflict.isPending ? <Loader2 className="h-3 w-3 mr-1 animate-spin" /> : null}
              Airtable gagne
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

export function AirtableConflictsPage() {
  const { data: conflicts = [], isLoading } = useAirtableConflicts()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Conflits de synchronisation</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Résolvez les conflits entre les données OreoLeads et Airtable.
        </p>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      ) : conflicts.length === 0 ? (
        <Card>
          <CardContent className="pt-6 text-center text-muted-foreground text-sm py-12">
            Aucun conflit actif. Toutes les synchronisations sont harmonieuses.
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          <p className="text-sm text-muted-foreground">
            {conflicts.length} conflit{conflicts.length > 1 ? 's' : ''} en attente de résolution
          </p>
          {conflicts.map(conflict => (
            <ConflictRow key={conflict.id} conflict={conflict} />
          ))}
        </div>
      )}
    </div>
  )
}

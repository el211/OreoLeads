import { useState } from 'react'
import { Tag as TagIcon, Plus, Trash2, Loader2 } from 'lucide-react'
import { useTags, useCreateTag, useDeleteTag } from '@/hooks/useTags'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const PRESET_COLORS = [
  '#ef4444', '#f97316', '#f59e0b', '#eab308', '#84cc16', '#22c55e',
  '#10b981', '#06b6d4', '#3b82f6', '#6366f1', '#8b5cf6', '#ec4899',
  '#64748b', '#0f172a',
]

/** Choisit une couleur de texte lisible (noir/blanc) selon la luminance du fond. */
function readableText(hex: string): string {
  const c = hex.replace('#', '')
  if (c.length !== 6) return '#fff'
  const r = parseInt(c.slice(0, 2), 16)
  const g = parseInt(c.slice(2, 4), 16)
  const b = parseInt(c.slice(4, 6), 16)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  return luminance > 0.6 ? '#0f172a' : '#ffffff'
}

export function TagsPage() {
  const { data: tags = [], isLoading } = useTags()
  const createTag = useCreateTag()
  const deleteTag = useDeleteTag()

  const [name, setName] = useState('')
  const [color, setColor] = useState(PRESET_COLORS[8])

  const handleCreate = async () => {
    const trimmed = name.trim()
    if (!trimmed) return
    await createTag.mutateAsync({ name: trimmed, color })
    setName('')
  }

  const handleDelete = async (id: string, tagName: string) => {
    if (!confirm(`Supprimer le tag « ${tagName} » ? Il sera retiré de tous les prospects.`)) return
    await deleteTag.mutateAsync(id)
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold flex items-center gap-2">
          <TagIcon className="h-6 w-6" />
          Tags
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          Créez des étiquettes (ex : « Pas d'e-mail pro », « À rappeler ») pour classer vos prospects.
        </p>
      </div>

      {/* Création */}
      <Card>
        <CardHeader><CardTitle className="text-base">Nouveau tag</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-col sm:flex-row gap-3 sm:items-end">
            <div className="flex-1">
              <Label htmlFor="tag-name">Nom</Label>
              <Input
                id="tag-name"
                value={name}
                onChange={e => setName(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleCreate()}
                placeholder="Ex : Pas d'adresse e-mail pro"
                maxLength={50}
              />
            </div>
            <Button onClick={handleCreate} disabled={createTag.isPending || !name.trim()}>
              {createTag.isPending
                ? <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                : <Plus className="h-4 w-4 mr-2" />}
              Créer
            </Button>
          </div>

          <div>
            <Label className="mb-1 block">Couleur</Label>
            <div className="flex flex-wrap gap-2">
              {PRESET_COLORS.map(c => (
                <button
                  key={c}
                  type="button"
                  onClick={() => setColor(c)}
                  className={`h-7 w-7 rounded-full border-2 transition ${color === c ? 'border-foreground scale-110' : 'border-transparent'}`}
                  style={{ backgroundColor: c }}
                  aria-label={c}
                />
              ))}
            </div>
          </div>

          {/* Aperçu */}
          <div className="text-sm text-muted-foreground">
            Aperçu :{' '}
            <span
              className="inline-block rounded-full px-2.5 py-0.5 text-xs font-medium align-middle"
              style={{ backgroundColor: color, color: readableText(color) }}
            >
              {name.trim() || 'Nom du tag'}
            </span>
          </div>
        </CardContent>
      </Card>

      {/* Liste */}
      <Card>
        <CardHeader><CardTitle className="text-base">Tags existants ({tags.length})</CardTitle></CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-muted-foreground">Chargement…</p>
          ) : tags.length === 0 ? (
            <p className="text-sm text-muted-foreground">Aucun tag pour l'instant.</p>
          ) : (
            <div className="flex flex-wrap gap-2">
              {tags.map(tag => (
                <span
                  key={tag.id}
                  className="inline-flex items-center gap-1.5 rounded-full pl-3 pr-1.5 py-1 text-sm font-medium"
                  style={{ backgroundColor: tag.color, color: readableText(tag.color) }}
                >
                  {tag.name}
                  <button
                    type="button"
                    onClick={() => handleDelete(tag.id, tag.name)}
                    className="rounded-full p-0.5 hover:bg-black/20"
                    aria-label={`Supprimer ${tag.name}`}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </span>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

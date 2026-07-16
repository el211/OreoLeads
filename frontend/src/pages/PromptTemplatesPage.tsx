import { useState } from 'react'
import { format } from 'date-fns'
import { fr } from 'date-fns/locale'
import { Save, Loader2, Info } from 'lucide-react'
import { usePromptTemplates, useUpdatePromptTemplate } from '@/hooks/useEmails'
import type { PromptTemplate } from '@/types/emails'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'

function TemplateEditor({ template, onClose }: { template: PromptTemplate; onClose: () => void }) {
  const [content, setContent] = useState(template.content)
  const [name, setName] = useState(template.name)
  const update = useUpdatePromptTemplate(template.id)

  const handleSave = async () => {
    await update.mutateAsync({ content, name })
    onClose()
  }

  return (
    <Card className="border-primary/30">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-sm">{template.name}</CardTitle>
            <p className="text-xs text-muted-foreground mt-0.5">Clé : <code className="font-mono">{template.key}</code></p>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose}>Fermer</Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div>
          <Label>Nom du template</Label>
          <input
            className="w-full mt-1 px-3 py-1.5 rounded-md border border-input bg-transparent text-sm focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            value={name}
            onChange={e => setName(e.target.value)}
          />
        </div>
        <div>
          <Label>Contenu du prompt</Label>
          <div className="flex items-center gap-2 mt-1 mb-1 text-xs text-muted-foreground">
            <Info className="h-3 w-3" />
            Variables disponibles : aucune (le contenu est injecté dans le prompt de manière automatique)
          </div>
          <textarea
            value={content}
            onChange={e => setContent(e.target.value)}
            rows={12}
            className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-sm resize-y focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring font-mono"
          />
        </div>
        <div className="flex items-center gap-2">
          <Button size="sm" onClick={handleSave} disabled={update.isPending}>
            {update.isPending ? <Loader2 className="h-4 w-4 mr-1 animate-spin" /> : <Save className="h-4 w-4 mr-1" />}
            Enregistrer
          </Button>
          {update.isSuccess && <span className="text-xs text-green-600">Enregistré !</span>}
        </div>
      </CardContent>
    </Card>
  )
}

export function PromptTemplatesPage() {
  const { data: templates = [], isLoading } = usePromptTemplates()
  const [editing, setEditing] = useState<string | null>(null)

  return (
    <div className="max-w-3xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold">Templates de prompts IA</h1>
        <p className="text-sm text-muted-foreground mt-1">
          Tous les prompts sont modifiables. Les modifications sont immédiatement appliquées aux prochaines générations.
        </p>
      </div>

      {isLoading ? (
        <p className="text-muted-foreground text-center py-8">Chargement...</p>
      ) : (
        <div className="space-y-3">
          {templates.map(t => (
            editing === t.id ? (
              <TemplateEditor key={t.id} template={t} onClose={() => setEditing(null)} />
            ) : (
              <Card key={t.id} className="cursor-pointer hover:bg-muted/50 transition-colors" onClick={() => setEditing(t.id)}>
                <CardContent className="py-3 px-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-medium text-sm">{t.name}</span>
                        {t.isSystem && <Badge variant="secondary" className="text-[10px]">Système</Badge>}
                        {t.emailType && <Badge variant="outline" className="text-[10px]">{t.emailType}</Badge>}
                      </div>
                      <code className="text-xs text-muted-foreground">{t.key}</code>
                      {t.description && <p className="text-xs text-muted-foreground mt-1">{t.description}</p>}
                      <p className="text-xs text-muted-foreground mt-1 truncate italic">{t.content.substring(0, 100)}…</p>
                    </div>
                    <div className="text-xs text-muted-foreground shrink-0">
                      <p>Modifié</p>
                      <p>{format(new Date(t.updatedAt), 'dd/MM/yy', { locale: fr })}</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )
          ))}
        </div>
      )}
    </div>
  )
}

import { useState } from 'react'
import { Plus, Trash2, Save, Loader2 } from 'lucide-react'
import {
  useAirtableMappings,
  useSaveAirtableMappings,
  useAirtableFields,
} from '@/hooks/useAirtable'
import type { AirtableFieldType, SaveAirtableFieldMapping, SyncDirection } from '@/types/airtable'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

const OREOLEADS_FIELDS = [
  'CompanyName', 'Email', 'Phone', 'Website', 'Address',
  'PostalCode', 'City', 'Country', 'Industry', 'Status',
  'Score', 'Siren', 'Siret',
]

const FIELD_TYPES: AirtableFieldType[] = [
  'SingleLineText', 'MultilineText', 'Email', 'PhoneNumber', 'Url',
  'Number', 'Checkbox', 'SingleSelect', 'MultipleSelects', 'Date', 'DateTime',
]

const DIRECTIONS: { value: SyncDirection; label: string }[] = [
  { value: 'OreoLeadsToAirtable', label: '→ Airtable' },
  { value: 'AirtableToOreoLeads', label: '← OreoLeads' },
  { value: 'Bidirectional',       label: '↔ Bidirectionnel' },
]

type EditableMapping = SaveAirtableFieldMapping & { _key: string }

export function AirtableMappingPage() {
  const { data: existingMappings = [] } = useAirtableMappings()
  const saveMappings = useSaveAirtableMappings()
  const [tableSearch] = useState('')
  useAirtableFields(tableSearch)  // preload fields if table is set

  const [mappings, setMappings] = useState<EditableMapping[]>(() =>
    existingMappings.map((m, i) => ({
      _key:             `existing-${i}`,
      oreoLeadsField:   m.oreoLeadsField,
      airtableFieldName:m.airtableFieldName,
      airtableFieldType:m.airtableFieldType,
      direction:        m.direction,
      isRequired:       m.isRequired,
      defaultValue:     m.defaultValue ?? undefined,
      transformation:   m.transformation ?? undefined,
      sortOrder:        m.sortOrder,
    }))
  )
  const [saved, setSaved] = useState(false)

  const addMapping = () => {
    setMappings(prev => [
      ...prev,
      {
        _key:             `new-${Date.now()}`,
        oreoLeadsField:   'CompanyName',
        airtableFieldName:'',
        airtableFieldType:'SingleLineText',
        direction:        'Bidirectional',
        isRequired:       false,
        sortOrder:        prev.length,
      },
    ])
  }

  const removeMapping = (key: string) => {
    setMappings(prev => prev.filter(m => m._key !== key))
  }

  const updateMapping = (key: string, updates: Partial<EditableMapping>) => {
    setMappings(prev => prev.map(m => m._key === key ? { ...m, ...updates } : m))
  }

  const handleSave = async () => {
    const toSave: SaveAirtableFieldMapping[] = mappings.map((m, i) => ({
      oreoLeadsField:   m.oreoLeadsField,
      airtableFieldName:m.airtableFieldName,
      airtableFieldType:m.airtableFieldType,
      direction:        m.direction,
      isRequired:       m.isRequired,
      defaultValue:     m.defaultValue,
      transformation:   m.transformation,
      sortOrder:        i,
    }))
    await saveMappings.mutateAsync(toSave)
    setSaved(true)
    setTimeout(() => setSaved(false), 3000)
  }

  return (
    <div className="max-w-4xl space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Mapping des champs Airtable</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Configurez la correspondance entre les champs OreoLeads et Airtable.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={addMapping}>
            <Plus className="h-4 w-4 mr-2" />Ajouter
          </Button>
          <Button onClick={handleSave} disabled={saveMappings.isPending}>
            {saveMappings.isPending
              ? <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              : <Save className="h-4 w-4 mr-2" />
            }
            Enregistrer
          </Button>
        </div>
      </div>

      {saved && (
        <div className="rounded-md bg-green-50 border border-green-300 px-4 py-2 text-sm text-green-700">
          Mappings enregistrés avec succès.
        </div>
      )}

      {mappings.length === 0 ? (
        <Card>
          <CardContent className="pt-6 text-center text-muted-foreground text-sm">
            Aucun mapping configuré. Cliquez sur "Ajouter" pour commencer.
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {mappings.map((m) => (
            <Card key={m._key}>
              <CardContent className="pt-4">
                <div className="grid grid-cols-12 gap-3 items-end">
                  {/* OreoLeads field */}
                  <div className="col-span-3">
                    <Label className="text-xs">Champ OreoLeads</Label>
                    <select
                      value={m.oreoLeadsField}
                      onChange={e => updateMapping(m._key, { oreoLeadsField: e.target.value })}
                      className="mt-1 w-full rounded-md border border-input bg-background px-2 py-1.5 text-sm"
                    >
                      {OREOLEADS_FIELDS.map(f => (
                        <option key={f} value={f}>{f}</option>
                      ))}
                    </select>
                  </div>

                  {/* Direction */}
                  <div className="col-span-2">
                    <Label className="text-xs">Direction</Label>
                    <select
                      value={m.direction}
                      onChange={e => updateMapping(m._key, { direction: e.target.value as SyncDirection })}
                      className="mt-1 w-full rounded-md border border-input bg-background px-2 py-1.5 text-sm"
                    >
                      {DIRECTIONS.map(d => (
                        <option key={d.value} value={d.value}>{d.label}</option>
                      ))}
                    </select>
                  </div>

                  {/* Airtable field name */}
                  <div className="col-span-3">
                    <Label className="text-xs">Champ Airtable</Label>
                    <Input
                      value={m.airtableFieldName}
                      onChange={e => updateMapping(m._key, { airtableFieldName: e.target.value })}
                      placeholder="Nom du champ..."
                      className="mt-1 h-8 text-sm"
                    />
                  </div>

                  {/* Field type */}
                  <div className="col-span-2">
                    <Label className="text-xs">Type</Label>
                    <select
                      value={m.airtableFieldType}
                      onChange={e => updateMapping(m._key, { airtableFieldType: e.target.value as AirtableFieldType })}
                      className="mt-1 w-full rounded-md border border-input bg-background px-2 py-1.5 text-sm"
                    >
                      {FIELD_TYPES.map(t => (
                        <option key={t} value={t}>{t}</option>
                      ))}
                    </select>
                  </div>

                  {/* Required */}
                  <div className="col-span-1 flex flex-col items-center">
                    <Label className="text-xs">Requis</Label>
                    <input
                      type="checkbox"
                      checked={m.isRequired}
                      onChange={e => updateMapping(m._key, { isRequired: e.target.checked })}
                      className="mt-2"
                    />
                  </div>

                  {/* Delete */}
                  <div className="col-span-1 flex justify-end">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => removeMapping(m._key)}
                      className="text-destructive hover:text-destructive"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

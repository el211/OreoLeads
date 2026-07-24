import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Search, History, Download, CheckSquare, Square, ExternalLink, AlertCircle, Loader2, ChevronDown, ChevronRight, Sparkles } from 'lucide-react'
import { useCompanySearch, useSearchImport, useAiSearchParse } from '@/hooks/useSearch'
import type { CompanySearchRequest, CompanySearchResponse, CompanySearchResult } from '@/types/search'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

const DEFAULT_REQUEST: CompanySearchRequest = {
  activeOnly: true,
  maxResults: 50,
  includeIndividualEntrepreneurs: true,
  includeNoEmployees: true,
}

export function SearchPage() {
  const [form, setForm] = useState<CompanySearchRequest>(DEFAULT_REQUEST)
  const [showAdvanced, setShowAdvanced] = useState(false)
  const [response, setResponse] = useState<CompanySearchResponse | null>(null)
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [importResult, setImportResult] = useState<{ newLeads: number; updatedLeads: number; duplicates: number; errors: number } | null>(null)

  const [aiPrompt, setAiPrompt] = useState('')
  const [aiNote, setAiNote] = useState<string | null>(null)

  const search = useCompanySearch()
  const importMutation = useSearchImport()
  const aiParse = useAiSearchParse()

  const set = (field: keyof CompanySearchRequest) =>
    (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm(prev => ({ ...prev, [field]: e.target.value || undefined }))

  const runSearch = async (req: CompanySearchRequest) => {
    setSelected(new Set())
    setImportResult(null)
    const result = await search.mutateAsync(req)
    setResponse(result)
  }

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault()
    await runSearch(form)
  }

  const handleAiSearch = async () => {
    if (!aiPrompt.trim()) return
    setAiNote(null)
    try {
      const parsed = await aiParse.mutateAsync(aiPrompt)
      const merged: CompanySearchRequest = {
        ...DEFAULT_REQUEST,
        ...parsed.request,
        maxResults: form.maxResults,
      }
      setForm(merged)
      // Note sur les critères que la recherche officielle ne peut pas filtrer
      const notes: string[] = []
      if (parsed.interpretation) notes.push(parsed.interpretation)
      if (parsed.wantsWebsite === false) notes.push("filtre « sans site web » : à appliquer après import (liste Prospects → Site web : Non)")
      if (parsed.wantsWebsite === true) notes.push("filtre « avec site web » : à appliquer après enrichissement")
      if (parsed.wantsEmail === false) notes.push("filtre « sans e-mail » : à appliquer après import")
      setAiNote(notes.length ? notes.join(' · ') : null)
      await runSearch(merged)
    } catch (err: any) {
      setAiNote(err?.response?.data ?? "L'IA n'a pas pu interpréter cette demande.")
    }
  }

  const toggleSelect = (i: number) =>
    setSelected(prev => {
      const next = new Set(prev)
      next.has(i) ? next.delete(i) : next.add(i)
      return next
    })

  const toggleAll = () => {
    if (!response) return
    setSelected(prev =>
      prev.size === response.results.length
        ? new Set()
        : new Set(response.results.map((_, i) => i))
    )
  }

  const handleImport = async () => {
    if (!response) return
    const companies = response.results.filter((_, i) => selected.has(i))
    if (companies.length === 0) return

    const result = await importMutation.mutateAsync({
      searchId: response.searchId,
      companies,
    })
    setImportResult(result)
    setSelected(new Set())
  }

  const selectedCount = selected.size
  const allSelected = response ? selectedCount === response.results.length : false

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Recherche de prospects</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Source officielle : recherche-entreprises.api.gouv.fr (données INSEE/SIRENE)
          </p>
        </div>
        <Link to="/search/history">
          <Button variant="outline" size="sm">
            <History className="h-4 w-4 mr-2" />
            Historique
          </Button>
        </Link>
      </div>

      {/* Recherche IA — phrase → filtres (vraies données) */}
      <Card className="border-primary/30">
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-primary" />
            Recherche assistée par IA
          </CardTitle>
          <p className="text-xs text-muted-foreground">
            Décris ta cible en une phrase. L'IA en déduit les filtres, puis la recherche officielle
            ramène de vraies entreprises. Ex : « coiffeurs à Strasbourg », « restaurants dans le 67 sans site web ».
          </p>
        </CardHeader>
        <CardContent>
          <div className="flex gap-2">
            <Input
              value={aiPrompt}
              onChange={e => setAiPrompt(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleAiSearch()}
              placeholder="Ex : plombiers indépendants à Lyon…"
            />
            <Button onClick={handleAiSearch} disabled={aiParse.isPending || search.isPending}>
              {aiParse.isPending || search.isPending
                ? <><Loader2 className="h-4 w-4 mr-2 animate-spin" />…</>
                : <><Sparkles className="h-4 w-4 mr-2" />Rechercher</>}
            </Button>
          </div>
          {aiNote && (
            <p className="mt-2 text-xs text-muted-foreground">
              <span className="font-medium text-foreground">IA :</span> {aiNote}
            </p>
          )}
        </CardContent>
      </Card>

      {/* Search Form */}
      <Card>
        <CardHeader><CardTitle className="text-base">Critères de recherche</CardTitle></CardHeader>
        <CardContent>
          <form onSubmit={handleSearch} className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="lg:col-span-3">
              <Label htmlFor="keywords">Mots-clés</Label>
              <Input
                id="keywords"
                value={form.keywords ?? ''}
                onChange={set('keywords')}
                placeholder="Nom d'entreprise, SIREN..."
              />
            </div>
            <div>
              <Label htmlFor="region">Région</Label>
              <Input id="region" value={form.region ?? ''} onChange={set('region')} placeholder="Île-de-France..." />
            </div>
            <div>
              <Label htmlFor="department">Département</Label>
              <Input id="department" value={form.department ?? ''} onChange={set('department')} placeholder="75, 69, 13..." />
            </div>
            <div>
              <Label htmlFor="city">Ville</Label>
              <Input id="city" value={form.city ?? ''} onChange={set('city')} placeholder="Paris, Lyon..." />
            </div>
            <div>
              <Label htmlFor="postalCode">Code postal</Label>
              <Input id="postalCode" value={form.postalCode ?? ''} onChange={set('postalCode')} placeholder="75001" />
            </div>
            <div>
              <Label htmlFor="nafCode">Code NAF</Label>
              <Input id="nafCode" value={form.nafCode ?? ''} onChange={set('nafCode')} placeholder="56.10A" />
            </div>
            <div>
              <Label htmlFor="industry">Secteur d'activité</Label>
              <Input id="industry" value={form.industry ?? ''} onChange={set('industry')} placeholder="Restauration..." />
            </div>

            {/* Filtres avancés (EI, sans salarié, dates de création) */}
            <div className="sm:col-span-2 lg:col-span-3">
              <button
                type="button"
                onClick={() => setShowAdvanced(s => !s)}
                className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1"
              >
                {showAdvanced ? <ChevronDown className="h-3.5 w-3.5" /> : <ChevronRight className="h-3.5 w-3.5" />}
                Filtres avancés
              </button>
            </div>

            {showAdvanced && (
              <div className="sm:col-span-2 lg:col-span-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-3 rounded-lg border bg-muted/30 p-4">
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.includeIndividualEntrepreneurs ?? true}
                    onChange={e => setForm(p => ({ ...p, includeIndividualEntrepreneurs: e.target.checked }))}
                    className="h-4 w-4 rounded border-input"
                  />
                  Inclure les entrepreneurs individuels
                </label>
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.onlyIndividualEntrepreneurs ?? false}
                    onChange={e => setForm(p => ({ ...p, onlyIndividualEntrepreneurs: e.target.checked }))}
                    className="h-4 w-4 rounded border-input"
                  />
                  Uniquement les entrepreneurs individuels
                </label>
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.includeNoEmployees ?? true}
                    onChange={e => setForm(p => ({ ...p, includeNoEmployees: e.target.checked }))}
                    className="h-4 w-4 rounded border-input"
                  />
                  Inclure les établissements sans salarié
                </label>
                <div>
                  <Label htmlFor="createdAfter">Créée après</Label>
                  <Input
                    id="createdAfter"
                    type="date"
                    value={form.createdAfter ?? ''}
                    onChange={e => setForm(p => ({ ...p, createdAfter: e.target.value || undefined }))}
                  />
                </div>
                <div>
                  <Label htmlFor="createdBefore">Créée avant</Label>
                  <Input
                    id="createdBefore"
                    type="date"
                    value={form.createdBefore ?? ''}
                    onChange={e => setForm(p => ({ ...p, createdBefore: e.target.value || undefined }))}
                  />
                </div>
              </div>
            )}

            <div className="sm:col-span-2 lg:col-span-3 flex items-center justify-between gap-4 pt-2">
              <div className="flex items-center gap-4">
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <input
                    type="checkbox"
                    checked={form.activeOnly}
                    onChange={e => setForm(p => ({ ...p, activeOnly: e.target.checked }))}
                    className="h-4 w-4 rounded border-input"
                  />
                  Entreprises actives uniquement
                </label>
                <div className="flex items-center gap-2">
                  <Label htmlFor="maxResults">Max résultats</Label>
                  <Input
                    id="maxResults"
                    type="number"
                    min={1}
                    max={200}
                    value={form.maxResults}
                    onChange={e => setForm(p => ({ ...p, maxResults: Number(e.target.value) || 50 }))}
                    className="w-20"
                  />
                </div>
              </div>
              <Button type="submit" disabled={search.isPending}>
                {search.isPending
                  ? <><Loader2 className="h-4 w-4 mr-2 animate-spin" />Recherche...</>
                  : <><Search className="h-4 w-4 mr-2" />Lancer la recherche</>
                }
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Import Result Banner */}
      {importResult && (
        <Card className="border-green-200 bg-green-50 dark:bg-green-950/20">
          <CardContent className="pt-4 flex items-center gap-6 flex-wrap text-sm">
            <span className="font-semibold text-green-700 dark:text-green-400">Import terminé</span>
            <span><strong>{importResult.newLeads}</strong> nouveau{importResult.newLeads > 1 ? 'x' : ''} prospect{importResult.newLeads > 1 ? 's' : ''}</span>
            <span><strong>{importResult.updatedLeads}</strong> enrichi{importResult.updatedLeads > 1 ? 's' : ''}</span>
            <span><strong>{importResult.duplicates}</strong> doublon{importResult.duplicates > 1 ? 's' : ''}</span>
            {importResult.errors > 0 && <span className="text-destructive"><strong>{importResult.errors}</strong> erreur{importResult.errors > 1 ? 's' : ''}</span>}
          </CardContent>
        </Card>
      )}

      {/* Results */}
      {response && (
        <div className="space-y-3">
          {/* Results header */}
          <div className="flex items-center justify-between">
            <div className="text-sm text-muted-foreground">
              <strong>{response.results.length}</strong> résultat{response.results.length !== 1 ? 's' : ''}
              {response.totalFound > response.results.length && (
                <span> sur {response.totalFound.toLocaleString()} trouvés</span>
              )}
              <span className="ml-2">· {response.durationMs} ms · {response.provider}</span>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={toggleAll}
                className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1"
              >
                {allSelected
                  ? <><CheckSquare className="h-3.5 w-3.5" />Tout désélectionner</>
                  : <><Square className="h-3.5 w-3.5" />Tout sélectionner</>
                }
              </button>
              {selectedCount > 0 && (
                <Button
                  size="sm"
                  onClick={handleImport}
                  disabled={importMutation.isPending}
                >
                  {importMutation.isPending
                    ? <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                    : <Download className="h-4 w-4 mr-1" />
                  }
                  Importer ({selectedCount})
                </Button>
              )}
            </div>
          </div>

          {response.results.length === 0 ? (
            <Card>
              <CardContent className="py-12 text-center text-muted-foreground">
                <AlertCircle className="h-8 w-8 mx-auto mb-3 opacity-30" />
                <p>Aucun résultat pour ces critères</p>
              </CardContent>
            </Card>
          ) : (
            <div className="space-y-2">
              {response.results.map((company, i) => (
                <SearchResultCard
                  key={`${company.siren ?? company.companyName}-${i}`}
                  company={company}
                  selected={selected.has(i)}
                  onToggle={() => toggleSelect(i)}
                />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

function SearchResultCard({
  company,
  selected,
  onToggle,
}: {
  company: CompanySearchResult
  selected: boolean
  onToggle: () => void
}) {
  return (
    <div
      className={`flex items-start gap-3 p-4 rounded-lg border bg-card transition-colors cursor-pointer hover:bg-accent/5 ${
        selected ? 'border-primary/50 bg-primary/5' : ''
      }`}
      onClick={onToggle}
    >
      <div className="pt-0.5">
        {selected
          ? <CheckSquare className="h-4 w-4 text-primary" />
          : <Square className="h-4 w-4 text-muted-foreground" />
        }
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <span className="font-medium truncate">{company.companyName}</span>
          {company.isIndividualEntrepreneur && (
            <Badge variant="outline" className="text-xs border-primary/40 text-primary">EI</Badge>
          )}
          {company.nafCode && (
            <Badge variant="outline" className="text-xs font-mono">{company.nafCode}</Badge>
          )}
          {company.alreadyExists && (
            <Badge variant="secondary" className="text-xs">
              Déjà importé
            </Badge>
          )}
          {company.isNonDiffusible && (
            <Badge variant="secondary" className="text-xs">Non diffusible</Badge>
          )}
          {!company.isActive && (
            <Badge variant="destructive" className="text-xs">Inactive</Badge>
          )}
        </div>

        <div className="mt-1 flex flex-wrap gap-x-4 gap-y-0.5 text-xs text-muted-foreground">
          {company.tradeName && company.tradeName !== company.companyName && (
            <span className="italic">{company.tradeName}</span>
          )}
          {company.industry && <span>{company.industry}</span>}
          {(company.city || company.postalCode) && (
            <span>{[company.postalCode, company.city].filter(Boolean).join(' ')}</span>
          )}
          {company.department && <span>Dép. {company.department}</span>}
          {company.siren && <span className="font-mono">SIREN: {company.siren}</span>}
          {company.employeeCount != null && <span>{company.employeeCount}+ salariés</span>}
        </div>
      </div>

      {company.alreadyExists && company.existingLeadId && (
        <Link
          to={`/leads/${company.existingLeadId}`}
          onClick={e => e.stopPropagation()}
          className="shrink-0 text-muted-foreground hover:text-primary"
        >
          <ExternalLink className="h-4 w-4" />
        </Link>
      )}
    </div>
  )
}

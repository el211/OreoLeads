import { useState, useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  useReactTable, getCoreRowModel, getSortedRowModel,
  flexRender, type ColumnDef, type SortingState,
} from '@tanstack/react-table'
import { Plus, Search, Download, Upload, Trash2, ExternalLink, MailX, Pencil, Sparkles } from 'lucide-react'
import { useLeads, useDeleteLead, useLeadsMeta, useDeleteUncontactedLeads, fetchUncontactedCount, useBulkUpdateIndustry, useAutofillIndustry } from '@/hooks/useLeads'
import type { LeadFilter, LeadSummary, LeadStatus, LeadPriority } from '@/types/lead'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { StatusBadge, PriorityBadge } from '@/components/ui/status-badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import api from '@/lib/api'

const STATUS_OPTIONS: { value: LeadStatus | 'all'; label: string }[] = [
  { value: 'all', label: 'Tous les statuts' },
  { value: 'New', label: 'Nouveau' },
  { value: 'Qualified', label: 'Qualifié' },
  { value: 'ReadyToContact', label: 'Prêt à contacter' },
  { value: 'EmailPrepared', label: 'Email préparé' },
  { value: 'EmailSent', label: 'Email envoyé' },
  { value: 'FollowUp1', label: 'Relance 1' },
  { value: 'FollowUp2', label: 'Relance 2' },
  { value: 'Meeting', label: 'Rendez-vous' },
  { value: 'ProposalSent', label: 'Devis envoyé' },
  { value: 'Client', label: 'Client' },
  { value: 'Rejected', label: 'Refusé' },
  { value: 'DoNotContact', label: 'Ne pas contacter' },
]

const PRIORITY_OPTIONS: { value: LeadPriority | 'all'; label: string }[] = [
  { value: 'all', label: 'Toutes priorités' },
  { value: 'Low', label: 'Basse' },
  { value: 'Medium', label: 'Moyenne' },
  { value: 'High', label: 'Haute' },
  { value: 'Urgent', label: 'Urgente' },
]

// Filtre relance : prospects dont le dernier e-mail date d'au moins N jours
const RELANCE_OPTIONS: { value: string; label: string }[] = [
  { value: '0', label: 'Tous' },
  { value: '7', label: 'À relancer (+7 j)' },
  { value: '14', label: 'À relancer (+14 j)' },
  { value: '30', label: 'À relancer (+30 j)' },
]

// value = "<sortBy>:<asc|desc>" (sortBy correspond aux clés du back)
const SORT_OPTIONS: { value: string; label: string }[] = [
  { value: 'createdat:desc', label: 'Plus récents' },
  { value: 'updatedat:desc', label: 'Dernière modification' },
  { value: 'lastemailsent:desc', label: 'Dernier e-mail envoyé' },
  { value: 'score:desc', label: 'Score le plus élevé' },
  { value: 'companyname:asc', label: 'Nom (A → Z)' },
]

export function LeadsPage() {
  const navigate = useNavigate()
  const [filter, setFilter] = useState<LeadFilter>({ page: 1, pageSize: 20 })
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<LeadStatus | 'all'>('all')
  const [priority, setPriority] = useState<LeadPriority | 'all'>('all')
  const [industry, setIndustry] = useState('all')
  const [legalForm, setLegalForm] = useState('all')
  const [sort, setSort] = useState('createdat:desc')
  const [relance, setRelance] = useState('0')
  const [contact, setContact] = useState('all')
  const [sorting, setSorting] = useState<SortingState>([])

  const { data, isLoading, isFetching } = useLeads(filter)
  const { data: meta } = useLeadsMeta()
  const deleteLead = useDeleteLead()
  const deleteUncontacted = useDeleteUncontactedLeads()
  const bulkIndustry = useBulkUpdateIndustry()
  const autofillIndustry = useAutofillIndustry()

  // Apply filter on enter or debounce
  const applyFilter = (overrides: Partial<LeadFilter> = {}) => {
    setFilter(prev => ({
      ...prev,
      ...overrides,
      search: overrides.search ?? search,
      status: (status !== 'all' ? status : undefined) ?? overrides.status,
      priority: (priority !== 'all' ? priority : undefined) ?? overrides.priority,
      industry: (industry !== 'all' ? industry : undefined) ?? overrides.industry,
      legalForm: (legalForm !== 'all' ? legalForm : undefined) ?? overrides.legalForm,
      minDaysSinceEmail: overrides.minDaysSinceEmail ?? (relance !== '0' ? Number(relance) : undefined),
      hasPhone:       overrides.hasPhone       ?? (contact === 'phone'    ? true  : undefined),
      hasMobilePhone: overrides.hasMobilePhone ?? (contact === 'mobile'   ? true  : contact === 'landline' ? false : undefined),
      hasEmail:       overrides.hasEmail       ?? (contact === 'email'    ? true  : undefined),
      hasContact:     overrides.hasContact     ?? (contact === 'contact'  ? true  : undefined),
      page: 1,
    }))
  }

  const columns = useMemo<ColumnDef<LeadSummary>[]>(() => [
    {
      id: 'select',
      header: ({ table }) => (
        <input
          type="checkbox"
          className="h-4 w-4"
          checked={table.getIsAllRowsSelected()}
          onChange={table.getToggleAllRowsSelectedHandler()}
        />
      ),
      cell: ({ row }) => (
        <input
          type="checkbox"
          className="h-4 w-4"
          checked={row.getIsSelected()}
          onChange={row.getToggleSelectedHandler()}
          onClick={e => e.stopPropagation()}
        />
      ),
      size: 36,
    },
    {
      accessorKey: 'companyName',
      header: 'Entreprise',
      cell: ({ row }) => (
        <div>
          <p className="font-medium text-sm">{row.original.companyName}</p>
          {row.original.tradeName && (
            <p className="text-xs text-muted-foreground">{row.original.tradeName}</p>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'industry',
      header: 'Secteur',
      cell: ({ getValue }) => <span className="text-sm">{getValue() as string ?? '—'}</span>,
    },
    {
      accessorKey: 'city',
      header: 'Ville',
      cell: ({ getValue }) => <span className="text-sm">{getValue() as string ?? '—'}</span>,
    },
    {
      accessorKey: 'status',
      header: 'Statut',
      cell: ({ row }) => <StatusBadge status={row.original.status} label={row.original.statusLabel} />,
    },
    {
      accessorKey: 'priority',
      header: 'Priorité',
      cell: ({ row }) => <PriorityBadge priority={row.original.priority} label={row.original.priorityLabel} />,
    },
    {
      accessorKey: 'score',
      header: 'Score',
      cell: ({ getValue }) => {
        const score = getValue() as number
        return (
          <div className="flex items-center gap-2">
            <div className="h-1.5 w-16 rounded-full bg-muted overflow-hidden">
              <div className="h-full rounded-full bg-primary" style={{ width: `${score}%` }} />
            </div>
            <span className="text-xs font-medium">{score}</span>
          </div>
        )
      },
    },
    {
      id: 'contact',
      header: 'Contact',
      cell: ({ row }) => (
        <div className="text-xs space-y-0.5">
          {row.original.email && <p className="text-muted-foreground truncate max-w-[140px]">{row.original.email}</p>}
          {row.original.phone && <p>{row.original.phone}</p>}
        </div>
      ),
    },
    {
      id: 'lastEmail',
      header: 'Dernier e-mail',
      cell: ({ row }) => {
        const d = row.original.lastEmailSentAt
        if (!d) return <span className="text-muted-foreground text-xs">—</span>
        const days = Math.floor((Date.now() - new Date(d).getTime()) / 86400000)
        const label = days === 0 ? "aujourd'hui" : days === 1 ? 'hier' : `il y a ${days} j`
        // Vert < 7 j, orange 7-14 j, rouge > 14 j (candidat à relance)
        const color = days > 14 ? 'text-red-600' : days >= 7 ? 'text-amber-600' : 'text-green-600'
        return <span className={`text-xs font-medium ${color}`}>{label}</span>
      },
    },
    {
      id: 'tags',
      header: 'Tags',
      cell: ({ row }) => (
        <div className="flex flex-wrap gap-1">
          {row.original.tags.slice(0, 3).map(t => (
            <Badge key={t.id} variant="outline" style={{ borderColor: t.color, color: t.color }} className="text-xs">
              {t.name}
            </Badge>
          ))}
          {row.original.tags.length > 3 && (
            <Badge variant="outline" className="text-xs">+{row.original.tags.length - 3}</Badge>
          )}
        </div>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className="flex items-center gap-1" onClick={e => e.stopPropagation()}>
          {row.original.website && (
            <a href={row.original.website} target="_blank" rel="noopener noreferrer">
              <Button variant="ghost" size="icon" className="h-7 w-7">
                <ExternalLink className="h-3.5 w-3.5" />
              </Button>
            </a>
          )}
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 text-destructive"
            onClick={() => {
              if (confirm('Supprimer ce prospect ?'))
                deleteLead.mutate(row.original.id)
            }}
          >
            <Trash2 className="h-3.5 w-3.5" />
          </Button>
        </div>
      ),
    },
  ], [deleteLead])

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    enableRowSelection: true,
  })

  const downloadFile = async (path: string, filename: string) => {
    const params = new URLSearchParams()
    if (filter.search) params.set('search', filter.search)
    if (filter.status) params.set('status', filter.status)
    const response = await api.get(`${path}?${params}`, { responseType: 'blob' })
    const url = URL.createObjectURL(new Blob([response.data]))
    const a = document.createElement('a')
    a.href = url
    a.download = filename
    a.click()
    URL.revokeObjectURL(url)
  }

  const handleExportCsv = () => downloadFile('/leads/export/csv', 'prospects.csv')
  const handleExportExcel = () => downloadFile('/leads/export/excel', 'prospects.xlsx')

  const handleImport = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    const formData = new FormData()
    formData.append('file', file)
    await api.post('/leads/import', formData, { headers: { 'Content-Type': 'multipart/form-data' } })
    setFilter(prev => ({ ...prev })) // trigger refetch
    e.target.value = ''
  }

  const deleteSelected = async () => {
    const ids = table.getSelectedRowModel().rows.map(r => r.original.id)
    if (!ids.length) return
    if (!confirm(`Supprimer ${ids.length} prospect(s) ?`)) return
    await Promise.all(ids.map(id => deleteLead.mutateAsync(id)))
    table.resetRowSelection()
  }

  const editIndustrySelected = async () => {
    const ids = table.getSelectedRowModel().rows.map(r => r.original.id)
    if (!ids.length) return
    const value = prompt(`Nouveau secteur d'activité pour les ${ids.length} prospect(s) sélectionné(s) :`)
    if (value === null) return // annulé
    const updated = await bulkIndustry.mutateAsync({ leadIds: ids, industry: value.trim() })
    table.resetRowSelection()
    alert(`Secteur mis à jour pour ${updated} prospect(s).`)
  }

  const autofillIndustrySelected = async () => {
    const ids = table.getSelectedRowModel().rows.map(r => r.original.id)
    if (!ids.length) return
    if (!confirm(`Laisser l'IA déduire le secteur des ${ids.length} prospect(s) sélectionné(s) (d'après leur nom, description et code NAF) ?`)) return
    try {
      const updated = await autofillIndustry.mutateAsync(ids)
      table.resetRowSelection()
      alert(`Secteur renseigné par l'IA pour ${updated} prospect(s).`)
    } catch (err: any) {
      const msg = typeof err?.response?.data === 'string' ? err.response.data : err?.message
      alert(`Erreur : ${msg ?? 'IA indisponible.'}`)
    }
  }

  const handlePurgeUncontacted = async () => {
    const count = await fetchUncontactedCount()
    if (count === 0) {
      alert('Aucun prospect sans e-mail envoyé.')
      return
    }
    if (!confirm(
      `Retirer les ${count} prospect(s) auxquels aucun e-mail n'a été envoyé ?\n\n` +
      `Ils seront supprimés de vos prospects mais réapparaîtront si vous relancez la Recherche.`
    )) return
    const deleted = await deleteUncontacted.mutateAsync()
    alert(`${deleted} prospect(s) retiré(s).`)
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Prospects</h1>
          <p className="text-muted-foreground">
            {data?.totalCount ?? 0} prospect{(data?.totalCount ?? 0) > 1 ? 's' : ''}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <label>
            <Button variant="outline" size="sm" asChild>
              <span>
                <Upload className="mr-2 h-4 w-4" />
                Importer CSV
                <input type="file" accept=".csv" className="hidden" onChange={handleImport} />
              </span>
            </Button>
          </label>
          <Button variant="outline" size="sm" onClick={handleExportCsv}>
            <Download className="mr-2 h-4 w-4" />
            CSV
          </Button>
          <Button variant="outline" size="sm" onClick={handleExportExcel}>
            <Download className="mr-2 h-4 w-4" />
            Excel
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={handlePurgeUncontacted}
            disabled={deleteUncontacted.isPending}
            className="text-destructive hover:text-destructive"
            title="Supprime les prospects sans e-mail envoyé (ils réapparaîtront dans la Recherche)"
          >
            <MailX className="mr-2 h-4 w-4" />
            Sans e-mail
          </Button>
          <Link to="/leads/new">
            <Button size="sm">
              <Plus className="mr-2 h-4 w-4" />
              Nouveau
            </Button>
          </Link>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Rechercher..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && applyFilter({ search })}
            className="pl-9"
          />
        </div>

        <Select value={status} onValueChange={v => {
          setStatus(v as LeadStatus | 'all')
          applyFilter({ status: v !== 'all' ? v as LeadStatus : undefined })
        }}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Statut" />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map(o => <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>)}
          </SelectContent>
        </Select>

        <Select value={priority} onValueChange={v => {
          setPriority(v as LeadPriority | 'all')
          applyFilter({ priority: v !== 'all' ? v as LeadPriority : undefined })
        }}>
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Priorité" />
          </SelectTrigger>
          <SelectContent>
            {PRIORITY_OPTIONS.map(o => <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>)}
          </SelectContent>
        </Select>

        {(meta?.legalForms?.length ?? 0) > 0 && (
          <Select value={legalForm} onValueChange={v => {
            setLegalForm(v)
            applyFilter({ legalForm: v !== 'all' ? v : undefined })
          }}>
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="Forme juridique" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Toutes formes</SelectItem>
              {meta!.legalForms.map(f => <SelectItem key={f} value={f}>{f}</SelectItem>)}
            </SelectContent>
          </Select>
        )}

        {(meta?.industries?.length ?? 0) > 0 && (
          <Select value={industry} onValueChange={v => {
            setIndustry(v)
            applyFilter({ industry: v !== 'all' ? v : undefined })
          }}>
            <SelectTrigger className="w-[200px]">
              <SelectValue placeholder="Secteur d'activité" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tous les secteurs</SelectItem>
              {meta!.industries.map(i => <SelectItem key={i} value={i}>{i}</SelectItem>)}
            </SelectContent>
          </Select>
        )}

        <Select value={contact} onValueChange={v => {
          setContact(v)
          applyFilter({
            hasPhone:        v === 'phone'    ? true  : undefined,
            hasMobilePhone:  v === 'mobile'   ? true  : v === 'landline' ? false : undefined,
            hasEmail:        v === 'email'    ? true  : undefined,
            hasContact:      v === 'contact'  ? true  : undefined,
          })
        }}>
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="Contact" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tous les contacts</SelectItem>
            <SelectItem value="mobile">📱 Portable (06/07)</SelectItem>
            <SelectItem value="landline">☎️ Fixe uniquement</SelectItem>
            <SelectItem value="phone">Avec téléphone</SelectItem>
            <SelectItem value="email">Avec email</SelectItem>
            <SelectItem value="contact">Téléphone ou email</SelectItem>
          </SelectContent>
        </Select>

        <Select value={relance} onValueChange={v => {
          setRelance(v)
          applyFilter({ minDaysSinceEmail: v !== '0' ? Number(v) : undefined })
        }}>
          <SelectTrigger className="w-[170px]">
            <SelectValue placeholder="Relance" />
          </SelectTrigger>
          <SelectContent>
            {RELANCE_OPTIONS.map(o => <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>)}
          </SelectContent>
        </Select>

        <Select value={sort} onValueChange={v => {
          setSort(v)
          const [sortBy, dir] = v.split(':')
          applyFilter({ sortBy, sortDesc: dir === 'desc' })
        }}>
          <SelectTrigger className="w-[210px]">
            <SelectValue placeholder="Trier par" />
          </SelectTrigger>
          <SelectContent>
            {SORT_OPTIONS.map(o => <SelectItem key={o.value} value={o.value}>{o.label}</SelectItem>)}
          </SelectContent>
        </Select>

        <Button variant="outline" size="sm" onClick={() => applyFilter({ search })}>
          Filtrer
        </Button>

        {table.getSelectedRowModel().rows.length > 0 && (
          <>
            <Button variant="outline" size="sm" onClick={autofillIndustrySelected} disabled={autofillIndustry.isPending}>
              <Sparkles className="mr-2 h-4 w-4" />
              Secteur IA ({table.getSelectedRowModel().rows.length})
            </Button>
            <Button variant="outline" size="sm" onClick={editIndustrySelected} disabled={bulkIndustry.isPending}>
              <Pencil className="mr-2 h-4 w-4" />
              Modifier secteur ({table.getSelectedRowModel().rows.length})
            </Button>
            <Button variant="destructive" size="sm" onClick={deleteSelected}>
              <Trash2 className="mr-2 h-4 w-4" />
              Supprimer ({table.getSelectedRowModel().rows.length})
            </Button>
          </>
        )}
      </div>

      {/* Table */}
      <div className={`rounded-lg border overflow-hidden transition-opacity ${isFetching ? 'opacity-60' : 'opacity-100'}`}>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              {table.getHeaderGroups().map(hg => (
                <tr key={hg.id}>
                  {hg.headers.map(header => (
                    <th
                      key={header.id}
                      className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide cursor-pointer select-none"
                      onClick={header.column.getToggleSortingHandler()}
                    >
                      {flexRender(header.column.columnDef.header, header.getContext())}
                      {header.column.getIsSorted() === 'asc' ? ' ↑' : header.column.getIsSorted() === 'desc' ? ' ↓' : ''}
                    </th>
                  ))}
                </tr>
              ))}
            </thead>
            <tbody className="divide-y">
              {isLoading ? (
                <tr>
                  <td colSpan={columns.length} className="px-4 py-8 text-center text-muted-foreground">
                    Chargement...
                  </td>
                </tr>
              ) : table.getRowModel().rows.length === 0 ? (
                <tr>
                  <td colSpan={columns.length} className="px-4 py-8 text-center text-muted-foreground">
                    Aucun prospect trouvé
                  </td>
                </tr>
              ) : (
                table.getRowModel().rows.map(row => (
                  <tr
                    key={row.id}
                    className="hover:bg-muted/50 cursor-pointer transition-colors"
                    onClick={() => navigate(`/leads/${row.original.id}`)}
                  >
                    {row.getVisibleCells().map(cell => (
                      <td key={cell.id} className="px-4 py-3">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Page {data.page} sur {data.totalPages} ({data.totalCount} résultats)
            {isFetching && <span className="ml-2 text-primary">Chargement...</span>}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline" size="sm"
              disabled={!data.hasPreviousPage || isFetching}
              onClick={() => setFilter(prev => ({ ...prev, page: prev.page - 1 }))}
            >
              Précédent
            </Button>
            <Button
              variant="outline" size="sm"
              disabled={!data.hasNextPage || isFetching}
              onClick={() => setFilter(prev => ({ ...prev, page: prev.page + 1 }))}
            >
              Suivant
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

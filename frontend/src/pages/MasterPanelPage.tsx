import { useState } from 'react'
import { Copy, Trash2, Plus, RefreshCw, CheckCircle2, Lock } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'

const BASE = '/api/master'

function masterApi(password: string) {
  return axios.create({
    baseURL: BASE,
    headers: { 'X-Master-Password': password },
  })
}

interface InviteCode {
  id: string
  code: string
  note: string | null
  isUsed: boolean
  usedBy: string | null
  usedAt: string | null
  expiresAt: string | null
  createdAt: string
}

export default function MasterPanelPage() {
  const [password, setPassword]     = useState('')
  const [inputPw, setInputPw]       = useState('')
  const [pwError, setPwError]       = useState(false)
  const [authenticated, setAuthenticated] = useState(false)

  const [count, setCount]           = useState(1)
  const [note, setNote]             = useState('')
  const [expiresInDays, setExpires] = useState('')
  const [copied, setCopied]         = useState<string | null>(null)

  const qc = useQueryClient()

  // Verify password
  async function handleVerify(e: React.FormEvent) {
    e.preventDefault()
    try {
      await axios.post(`${BASE}/verify`, {}, { headers: { 'X-Master-Password': inputPw } })
      setPassword(inputPw)
      setAuthenticated(true)
      setPwError(false)
    } catch {
      setPwError(true)
    }
  }

  const api = masterApi(password)

  const { data: codes = [], isLoading } = useQuery<InviteCode[]>({
    queryKey: ['master-invite-codes', password],
    queryFn: async () => (await api.get('/invite-codes')).data,
    enabled: authenticated,
  })

  const generate = useMutation({
    mutationFn: async () => (await api.post('/invite-codes/generate', {
      count,
      note: note || null,
      expiresInDays: expiresInDays ? parseInt(expiresInDays) : null,
    })).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-invite-codes'] }),
  })

  const remove = useMutation({
    mutationFn: async (id: string) => api.delete(`/invite-codes/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-invite-codes'] }),
  })

  function copyCode(code: string) {
    navigator.clipboard.writeText(code)
    setCopied(code)
    setTimeout(() => setCopied(null), 2000)
  }

  if (!authenticated) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center px-4">
        <div className="w-full max-w-sm">
          <div className="text-center mb-8">
            <div className="inline-flex items-center justify-center w-12 h-12 bg-red-600 rounded-xl mb-3">
              <Lock className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-xl font-bold text-white">Master Panel</h1>
            <p className="text-gray-500 text-sm mt-1">Accès restreint — administrateur uniquement</p>
          </div>

          <form onSubmit={handleVerify} className="bg-gray-900 rounded-2xl border border-gray-800 p-8 space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-1">Mot de passe master</label>
              <input
                type="password"
                value={inputPw}
                onChange={e => setInputPw(e.target.value)}
                required
                autoFocus
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
                placeholder="••••••••"
              />
              {pwError && <p className="text-red-400 text-xs mt-1">Mot de passe incorrect.</p>}
            </div>
            <button
              type="submit"
              className="w-full py-2.5 bg-red-600 text-white font-medium rounded-lg hover:bg-red-700 transition-colors text-sm"
            >
              Accéder
            </button>
          </form>
        </div>
      </div>
    )
  }

  const unused = codes.filter(c => !c.isUsed)
  const used   = codes.filter(c => c.isUsed)

  return (
    <div className="min-h-screen bg-gray-950 text-white p-6">
      <div className="max-w-3xl mx-auto space-y-8">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold flex items-center gap-2">
              <Lock className="h-5 w-5 text-red-400" /> Master Panel
            </h1>
            <p className="text-gray-400 text-sm mt-1">Gestion des codes d'invitation</p>
          </div>
          <span className="text-xs text-gray-600 bg-gray-900 px-3 py-1 rounded-full border border-gray-800">
            {unused.length} disponible{unused.length !== 1 ? 's' : ''} · {used.length} utilisé{used.length !== 1 ? 's' : ''}
          </span>
        </div>

        {/* Generator */}
        <div className="bg-gray-900 rounded-2xl border border-gray-800 p-6 space-y-4">
          <h2 className="text-base font-semibold">Générer des codes</h2>
          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-xs text-gray-400 mb-1">Quantité</label>
              <input
                type="number"
                min={1} max={50}
                value={count}
                onChange={e => setCount(Number(e.target.value))}
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-400 mb-1">Note (optionnel)</label>
              <input
                type="text"
                value={note}
                onChange={e => setNote(e.target.value)}
                placeholder="ex: client X"
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-400 mb-1">Expire dans (jours)</label>
              <input
                type="number"
                min={1}
                value={expiresInDays}
                onChange={e => setExpires(e.target.value)}
                placeholder="jamais"
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
              />
            </div>
          </div>
          <button
            onClick={() => generate.mutate()}
            disabled={generate.isPending}
            className="flex items-center gap-2 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-60"
          >
            {generate.isPending
              ? <RefreshCw className="h-4 w-4 animate-spin" />
              : <Plus className="h-4 w-4" />}
            Générer {count} code{count !== 1 ? 's' : ''}
          </button>
        </div>

        {/* Unused codes */}
        <div className="space-y-2">
          <h2 className="text-base font-semibold text-gray-300">Codes disponibles ({unused.length})</h2>
          {isLoading && <p className="text-gray-500 text-sm">Chargement…</p>}
          {!isLoading && unused.length === 0 && (
            <p className="text-gray-600 text-sm">Aucun code disponible — générez-en ci-dessus.</p>
          )}
          {unused.map(c => (
            <div key={c.id} className="flex items-center justify-between bg-gray-900 border border-gray-800 rounded-xl px-4 py-3">
              <div className="flex items-center gap-4">
                <span className="font-mono text-lg font-bold tracking-widest text-green-400">{c.code}</span>
                {c.note && <span className="text-xs text-gray-500 bg-gray-800 px-2 py-0.5 rounded">{c.note}</span>}
                {c.expiresAt && (
                  <span className="text-xs text-yellow-600">
                    expire {new Date(c.expiresAt).toLocaleDateString('fr-FR')}
                  </span>
                )}
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => copyCode(c.code)}
                  className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-white transition-colors"
                  title="Copier"
                >
                  {copied === c.code
                    ? <CheckCircle2 className="h-4 w-4 text-green-400" />
                    : <Copy className="h-4 w-4" />}
                </button>
                <button
                  onClick={() => remove.mutate(c.id)}
                  className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-red-400 transition-colors"
                  title="Supprimer"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Used codes */}
        {used.length > 0 && (
          <div className="space-y-2">
            <h2 className="text-base font-semibold text-gray-500">Codes utilisés ({used.length})</h2>
            {used.map(c => (
              <div key={c.id} className="flex items-center justify-between bg-gray-900 border border-gray-800 rounded-xl px-4 py-3 opacity-50">
                <div className="flex items-center gap-4">
                  <span className="font-mono text-lg font-bold tracking-widest text-gray-500 line-through">{c.code}</span>
                  {c.note && <span className="text-xs text-gray-600 bg-gray-800 px-2 py-0.5 rounded">{c.note}</span>}
                </div>
                <div className="text-right">
                  <p className="text-xs text-gray-500">{c.usedBy}</p>
                  <p className="text-xs text-gray-600">{c.usedAt ? new Date(c.usedAt).toLocaleDateString('fr-FR') : ''}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

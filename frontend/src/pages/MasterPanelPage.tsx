import { useState } from 'react'
import { Copy, Trash2, Plus, RefreshCw, CheckCircle2, Lock, Users, BarChart3, ShieldOff, ShieldCheck, KeyRound, LogIn } from 'lucide-react'
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

interface MasterUser {
  id: string
  email: string
  firstName: string
  lastName: string
  isActive: boolean
  organizationName: string | null
  roles: string[]
}

interface MasterStats {
  totalUsers: number
  activeUsers: number
  bannedUsers: number
  totalLeads: number
  totalInviteCodes: number
}

type Tab = 'codes' | 'users'

export default function MasterPanelPage() {
  const [password, setPassword]     = useState('')
  const [inputPw, setInputPw]       = useState('')
  const [pwError, setPwError]       = useState(false)
  const [authenticated, setAuthenticated] = useState(false)
  const [tab, setTab]               = useState<Tab>('codes')

  const [count, setCount]           = useState(1)
  const [note, setNote]             = useState('')
  const [expiresInDays, setExpires] = useState('')
  const [copied, setCopied]         = useState<string | null>(null)
  const [newPasswordMap, setNewPasswordMap] = useState<Record<string, string>>({})
  const [impersonateMap, setImpersonateMap] = useState<Record<string, string>>({})

  const qc = useQueryClient()

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

  const { data: stats } = useQuery<MasterStats>({
    queryKey: ['master-stats', password],
    queryFn: async () => (await api.get('/stats')).data,
    enabled: authenticated,
  })

  const { data: codes = [], isLoading: codesLoading } = useQuery<InviteCode[]>({
    queryKey: ['master-invite-codes', password],
    queryFn: async () => (await api.get('/invite-codes')).data,
    enabled: authenticated,
  })

  const { data: users = [], isLoading: usersLoading } = useQuery<MasterUser[]>({
    queryKey: ['master-users', password],
    queryFn: async () => (await api.get('/users')).data,
    enabled: authenticated,
  })

  const generate = useMutation({
    mutationFn: async () => (await api.post('/invite-codes/generate', {
      count,
      note: note || null,
      expiresInDays: expiresInDays ? parseInt(expiresInDays) : null,
    })).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['master-invite-codes'] })
      qc.invalidateQueries({ queryKey: ['master-stats'] })
    },
  })

  const removeCode = useMutation({
    mutationFn: async (id: string) => api.delete(`/invite-codes/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['master-invite-codes'] }),
  })

  const ban = useMutation({
    mutationFn: async (id: string) => api.post(`/users/${id}/ban`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['master-users'] })
      qc.invalidateQueries({ queryKey: ['master-stats'] })
    },
  })

  const unban = useMutation({
    mutationFn: async (id: string) => api.post(`/users/${id}/unban`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['master-users'] })
      qc.invalidateQueries({ queryKey: ['master-stats'] })
    },
  })

  const deleteUser = useMutation({
    mutationFn: async (id: string) => api.delete(`/users/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['master-users'] })
      qc.invalidateQueries({ queryKey: ['master-stats'] })
    },
  })

  const resetPassword = useMutation({
    mutationFn: async (id: string) => (await api.post(`/users/${id}/reset-password`)).data,
    onSuccess: (data: { newPassword: string }, id: string) => {
      setNewPasswordMap(prev => ({ ...prev, [id]: data.newPassword }))
    },
  })

  const impersonate = useMutation({
    mutationFn: async (id: string) => (await api.post(`/users/${id}/impersonate`)).data,
    onSuccess: (data: { token: string }, id: string) => {
      setImpersonateMap(prev => ({ ...prev, [id]: data.token }))
    },
  })

  function copyCode(code: string) {
    navigator.clipboard.writeText(code)
    setCopied(code)
    setTimeout(() => setCopied(null), 2000)
  }

  function openImpersonation(userId: string, token: string) {
    const url = `${window.location.origin}/impersonate?token=${encodeURIComponent(token)}`
    window.open(url, '_blank')
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
                required autoFocus
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
                placeholder="••••••••"
              />
              {pwError && <p className="text-red-400 text-xs mt-1">Mot de passe incorrect.</p>}
            </div>
            <button type="submit" className="w-full py-2.5 bg-red-600 text-white font-medium rounded-lg hover:bg-red-700 transition-colors text-sm">
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
      <div className="max-w-4xl mx-auto space-y-8">

        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold flex items-center gap-2">
              <Lock className="h-5 w-5 text-red-400" /> Master Panel
            </h1>
            <p className="text-gray-400 text-sm mt-1">Administration complète du CRM</p>
          </div>
        </div>

        {/* Stats */}
        {stats && (
          <div className="grid grid-cols-5 gap-3">
            {[
              { label: 'Comptes', value: stats.totalUsers,      color: 'text-blue-400' },
              { label: 'Actifs',  value: stats.activeUsers,     color: 'text-green-400' },
              { label: 'Bannis',  value: stats.bannedUsers,     color: 'text-red-400' },
              { label: 'Prospects', value: stats.totalLeads,    color: 'text-purple-400' },
              { label: 'Codes',   value: stats.totalInviteCodes,color: 'text-yellow-400' },
            ].map(s => (
              <div key={s.label} className="bg-gray-900 rounded-xl border border-gray-800 p-4 text-center">
                <div className={`text-2xl font-bold ${s.color}`}>{s.value}</div>
                <div className="text-xs text-gray-500 mt-1">{s.label}</div>
              </div>
            ))}
          </div>
        )}

        {/* Tabs */}
        <div className="flex gap-2 border-b border-gray-800">
          <button
            onClick={() => setTab('codes')}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${tab === 'codes' ? 'border-red-500 text-white' : 'border-transparent text-gray-500 hover:text-gray-300'}`}
          >
            Codes d'invitation
          </button>
          <button
            onClick={() => setTab('users')}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${tab === 'users' ? 'border-red-500 text-white' : 'border-transparent text-gray-500 hover:text-gray-300'}`}
          >
            Comptes ({users.length})
          </button>
        </div>

        {/* ── Invite codes tab ── */}
        {tab === 'codes' && (
          <div className="space-y-6">
            {/* Generator */}
            <div className="bg-gray-900 rounded-2xl border border-gray-800 p-6 space-y-4">
              <h2 className="text-base font-semibold">Générer des codes</h2>
              <div className="grid grid-cols-3 gap-3">
                <div>
                  <label className="block text-xs text-gray-400 mb-1">Quantité</label>
                  <input type="number" min={1} max={50} value={count} onChange={e => setCount(Number(e.target.value))}
                    className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500" />
                </div>
                <div>
                  <label className="block text-xs text-gray-400 mb-1">Note (optionnel)</label>
                  <input type="text" value={note} onChange={e => setNote(e.target.value)} placeholder="ex: client X"
                    className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500" />
                </div>
                <div>
                  <label className="block text-xs text-gray-400 mb-1">Expire dans (jours)</label>
                  <input type="number" min={1} value={expiresInDays} onChange={e => setExpires(e.target.value)} placeholder="jamais"
                    className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-red-500" />
                </div>
              </div>
              <button onClick={() => generate.mutate()} disabled={generate.isPending}
                className="flex items-center gap-2 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-60">
                {generate.isPending ? <RefreshCw className="h-4 w-4 animate-spin" /> : <Plus className="h-4 w-4" />}
                Générer {count} code{count !== 1 ? 's' : ''}
              </button>
            </div>

            {/* Unused */}
            <div className="space-y-2">
              <h2 className="text-base font-semibold text-gray-300">Codes disponibles ({unused.length})</h2>
              {codesLoading && <p className="text-gray-500 text-sm">Chargement…</p>}
              {!codesLoading && unused.length === 0 && <p className="text-gray-600 text-sm">Aucun code disponible.</p>}
              {unused.map(c => (
                <div key={c.id} className="flex items-center justify-between bg-gray-900 border border-gray-800 rounded-xl px-4 py-3">
                  <div className="flex items-center gap-4">
                    <span className="font-mono text-lg font-bold tracking-widest text-green-400">{c.code}</span>
                    {c.note && <span className="text-xs text-gray-500 bg-gray-800 px-2 py-0.5 rounded">{c.note}</span>}
                    {c.expiresAt && <span className="text-xs text-yellow-600">expire {new Date(c.expiresAt).toLocaleDateString('fr-FR')}</span>}
                  </div>
                  <div className="flex items-center gap-2">
                    <button onClick={() => copyCode(c.code)} className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-white transition-colors">
                      {copied === c.code ? <CheckCircle2 className="h-4 w-4 text-green-400" /> : <Copy className="h-4 w-4" />}
                    </button>
                    <button onClick={() => removeCode.mutate(c.id)} className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-red-400 transition-colors">
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Used */}
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
        )}

        {/* ── Users tab ── */}
        {tab === 'users' && (
          <div className="space-y-3">
            {usersLoading && <p className="text-gray-500 text-sm">Chargement…</p>}
            {users.map(u => (
              <div key={u.id} className={`bg-gray-900 border rounded-xl px-4 py-4 ${!u.isActive ? 'border-red-900 opacity-70' : 'border-gray-800'}`}>
                <div className="flex items-start justify-between gap-4">
                  <div className="min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-medium text-white">{u.firstName} {u.lastName}</span>
                      {u.roles.map(r => (
                        <span key={r} className="text-[10px] bg-gray-700 text-gray-300 px-1.5 py-0.5 rounded">{r}</span>
                      ))}
                      {!u.isActive && <span className="text-[10px] bg-red-900 text-red-300 px-1.5 py-0.5 rounded">BANNI</span>}
                    </div>
                    <p className="text-sm text-gray-400 mt-0.5">{u.email}</p>
                    {u.organizationName && <p className="text-xs text-gray-600 mt-0.5">{u.organizationName}</p>}

                    {/* New password reveal */}
                    {newPasswordMap[u.id] && (
                      <div className="mt-2 flex items-center gap-2 bg-gray-800 rounded-lg px-3 py-1.5">
                        <span className="text-xs text-gray-400">Nouveau mdp :</span>
                        <span className="font-mono text-sm text-green-400">{newPasswordMap[u.id]}</span>
                        <button onClick={() => navigator.clipboard.writeText(newPasswordMap[u.id])} className="text-gray-500 hover:text-white">
                          <Copy className="h-3 w-3" />
                        </button>
                      </div>
                    )}

                    {/* Impersonate link */}
                    {impersonateMap[u.id] && (
                      <div className="mt-2 flex items-center gap-2">
                        <button
                          onClick={() => openImpersonation(u.id, impersonateMap[u.id])}
                          className="text-xs text-indigo-400 hover:text-indigo-300 underline"
                        >
                          Ouvrir en tant que {u.firstName} →
                        </button>
                      </div>
                    )}
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-1 flex-shrink-0">
                    {u.isActive ? (
                      <button
                        onClick={() => ban.mutate(u.id)}
                        title="Bannir"
                        className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-red-400 transition-colors"
                      >
                        <ShieldOff className="h-4 w-4" />
                      </button>
                    ) : (
                      <button
                        onClick={() => unban.mutate(u.id)}
                        title="Débannir"
                        className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-green-400 transition-colors"
                      >
                        <ShieldCheck className="h-4 w-4" />
                      </button>
                    )}
                    <button
                      onClick={() => resetPassword.mutate(u.id)}
                      title="Réinitialiser le mot de passe"
                      className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-yellow-400 transition-colors"
                    >
                      <KeyRound className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => impersonate.mutate(u.id)}
                      title="Se connecter en tant que"
                      className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-indigo-400 transition-colors"
                    >
                      <LogIn className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => {
                        if (confirm(`Supprimer le compte de ${u.firstName} ${u.lastName} ? Cette action est irréversible.`))
                          deleteUser.mutate(u.id)
                      }}
                      title="Supprimer le compte"
                      className="p-1.5 rounded-lg hover:bg-gray-800 text-gray-400 hover:text-red-500 transition-colors"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

      </div>
    </div>
  )
}

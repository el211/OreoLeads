import { useState, useEffect, useRef } from 'react'
import { Send } from 'lucide-react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '../lib/api'
import { useAuth } from '../context/AuthContext'

interface ChatMessage {
  id: string
  userId: string
  authorName: string
  content: string
  createdAt: string
}

export default function ChatPage() {
  const { user } = useAuth()
  const qc = useQueryClient()
  const [text, setText] = useState('')
  const bottomRef = useRef<HTMLDivElement>(null)
  // Initial load — last 100 messages
  const { data: messages = [] } = useQuery<ChatMessage[]>({
    queryKey: ['chat-messages'],
    queryFn: async () => (await api.get('/chat/messages')).data,
    refetchInterval: 3000, // poll every 3 s
    staleTime: 0,
  })

  // Scroll to bottom when messages change
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const send = useMutation({
    mutationFn: async (content: string) => (await api.post('/chat/messages', { content })).data,
    onSuccess: (msg: ChatMessage) => {
      qc.setQueryData<ChatMessage[]>(['chat-messages'], prev => [...(prev ?? []), msg])
      setText('')
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const trimmed = text.trim()
    if (!trimmed) return
    send.mutate(trimmed)
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      const trimmed = text.trim()
      if (trimmed) send.mutate(trimmed)
    }
  }

  function formatTime(iso: string) {
    const d = new Date(iso)
    return d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' })
  }

  function formatDate(iso: string) {
    const d = new Date(iso)
    return d.toLocaleDateString('fr-FR', { weekday: 'short', day: 'numeric', month: 'short' })
  }

  // Group messages by date
  const grouped: { date: string; msgs: ChatMessage[] }[] = []
  for (const msg of messages) {
    const date = formatDate(msg.createdAt)
    const last = grouped[grouped.length - 1]
    if (last && last.date === date) last.msgs.push(msg)
    else grouped.push({ date, msgs: [msg] })
  }

  const initials = (name: string) =>
    name.split(' ').map(p => p[0]?.toUpperCase()).join('').slice(0, 2)

  const colors = ['bg-indigo-500', 'bg-emerald-500', 'bg-rose-500', 'bg-amber-500', 'bg-violet-500', 'bg-cyan-500']
  const colorFor = (id: string) => colors[id.charCodeAt(0) % colors.length]

  return (
    <div className="flex flex-col h-[calc(100vh-6rem)] max-w-3xl mx-auto">
      {/* Header */}
      <div className="mb-4">
        <h1 className="text-2xl font-bold">Chat d'équipe</h1>
        <p className="text-muted-foreground text-sm mt-0.5">Messagerie commune — tous les membres du CRM</p>
      </div>

      {/* Messages area */}
      <div className="flex-1 overflow-y-auto bg-card border rounded-xl p-4 space-y-4">
        {grouped.map(({ date, msgs }) => (
          <div key={date}>
            {/* Date separator */}
            <div className="flex items-center gap-3 my-3">
              <div className="flex-1 h-px bg-border" />
              <span className="text-xs text-muted-foreground px-2">{date}</span>
              <div className="flex-1 h-px bg-border" />
            </div>

            {msgs.map((msg, i) => {
              const isMine = msg.userId === user?.id
              const showAvatar = i === 0 || msgs[i - 1].userId !== msg.userId

              return (
                <div key={msg.id} className={`flex gap-3 ${isMine ? 'flex-row-reverse' : ''}`}>
                  {/* Avatar */}
                  <div className="w-8 flex-shrink-0">
                    {showAvatar && (
                      <div className={`w-8 h-8 rounded-full ${colorFor(msg.userId)} flex items-center justify-center text-white text-xs font-bold`}>
                        {initials(msg.authorName)}
                      </div>
                    )}
                  </div>

                  {/* Bubble */}
                  <div className={`max-w-[70%] ${isMine ? 'items-end' : 'items-start'} flex flex-col`}>
                    {showAvatar && (
                      <span className={`text-xs text-muted-foreground mb-1 ${isMine ? 'text-right' : ''}`}>
                        {isMine ? 'Vous' : msg.authorName}
                      </span>
                    )}
                    <div className={`px-3 py-2 rounded-2xl text-sm leading-relaxed break-words ${
                      isMine
                        ? 'bg-primary text-primary-foreground rounded-tr-sm'
                        : 'bg-muted text-foreground rounded-tl-sm'
                    }`}>
                      {msg.content}
                    </div>
                    <span className="text-[10px] text-muted-foreground mt-1">{formatTime(msg.createdAt)}</span>
                  </div>
                </div>
              )
            })}
          </div>
        ))}

        {messages.length === 0 && (
          <div className="flex items-center justify-center h-full text-muted-foreground text-sm">
            Aucun message pour l'instant. Soyez le premier à écrire !
          </div>
        )}

        <div ref={bottomRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSubmit} className="mt-3 flex gap-2 items-end">
        <textarea
          value={text}
          onChange={e => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Écrivez un message… (Entrée pour envoyer, Maj+Entrée pour nouvelle ligne)"
          rows={2}
          maxLength={2000}
          className="flex-1 px-4 py-3 bg-card border rounded-xl text-sm resize-none focus:outline-none focus:ring-2 focus:ring-primary"
        />
        <button
          type="submit"
          disabled={!text.trim() || send.isPending}
          className="p-3 bg-primary text-primary-foreground rounded-xl hover:opacity-90 disabled:opacity-40 transition-opacity"
        >
          <Send className="h-5 w-5" />
        </button>
      </form>
    </div>
  )
}

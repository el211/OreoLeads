import { useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import api from '../lib/api'

export default function ImpersonatePage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()

  useEffect(() => {
    const token = params.get('token')
    if (!token) { navigate('/login'); return }

    // Set the token and fetch user info
    localStorage.setItem('access_token', token)
    api.get('/auth/me')
      .then(({ data }) => {
        localStorage.setItem('user', JSON.stringify(data))
        // Hard reload so AuthContext picks up the new user
        window.location.href = '/'
      })
      .catch(() => {
        localStorage.removeItem('access_token')
        navigate('/login')
      })
  }, [])

  return (
    <div className="min-h-screen flex items-center justify-center">
      <p className="text-muted-foreground text-sm">Connexion en cours…</p>
    </div>
  )
}

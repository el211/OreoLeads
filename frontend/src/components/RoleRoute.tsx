import { Navigate } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'

interface Props {
  role: string
  children: React.ReactNode
}

export default function RoleRoute({ role, children }: Props) {
  const { user, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
      </div>
    )
  }

  if (!user?.roles.includes(role)) {
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}

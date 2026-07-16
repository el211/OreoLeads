import { NavLink } from 'react-router-dom'
import { LayoutDashboard, Users, Bell, Tag, Building2, Search, Mail, Bot, FileText, Settings, LogOut } from 'lucide-react'
import { cn } from '@/lib/utils'
import { useAuth } from '@/context/AuthContext'

const navItems = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/leads', label: 'Prospects', icon: Users },
  { to: '/search', label: 'Recherche', icon: Search },
  { to: '/followups', label: 'Relances', icon: Bell },
  { to: '/emails', label: 'Brouillons IA', icon: Mail },
  { to: '/tags', label: 'Tags', icon: Tag },
]

const settingsItems = [
  { to: '/settings/ai', label: 'IA & Modèle', icon: Bot },
  { to: '/settings/prompts', label: 'Prompts', icon: FileText },
]

export function Sidebar() {
  const { user, logout } = useAuth()

  return (
    <aside className="flex h-screen w-64 flex-col border-r bg-card">
      {/* Logo */}
      <div className="flex h-16 items-center gap-3 border-b px-6">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary">
          <Building2 className="h-4 w-4 text-primary-foreground" />
        </div>
        <span className="font-semibold text-foreground">OreoLeads</span>
      </div>

      {/* Navigation */}
      <nav className="flex-1 space-y-1 p-3 overflow-y-auto">
        {navItems.map(({ to, label, icon: Icon, end }) => (
          <NavLink
            key={to}
            to={to}
            end={end}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )
            }
          >
            <Icon className="h-4 w-4" />
            {label}
          </NavLink>
        ))}

        <div className="pt-4 pb-1">
          <p className="px-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider flex items-center gap-1">
            <Settings className="h-3 w-3" />Paramètres
          </p>
        </div>

        {settingsItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )
            }
          >
            <Icon className="h-4 w-4" />
            {label}
          </NavLink>
        ))}
      </nav>

      {/* Footer */}
      <div className="border-t p-4 space-y-2">
        {user && (
          <div className="text-xs text-muted-foreground truncate">
            {user.fullName || user.email}
          </div>
        )}
        <button
          onClick={logout}
          className="flex items-center gap-2 text-xs text-muted-foreground hover:text-foreground transition-colors w-full"
        >
          <LogOut className="h-3.5 w-3.5" />
          Se déconnecter
        </button>
      </div>
    </aside>
  )
}

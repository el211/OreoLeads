import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AppLayout } from '@/components/layout/AppLayout'
import { DashboardPage } from '@/pages/DashboardPage'
import { LeadsPage } from '@/pages/LeadsPage'
import { LeadDetailPage } from '@/pages/LeadDetailPage'
import { CreateLeadPage } from '@/pages/CreateLeadPage'
import { EditLeadPage } from '@/pages/EditLeadPage'
import { FollowUpsPage } from '@/pages/FollowUpsPage'
import { SearchPage } from '@/pages/SearchPage'
import { SearchHistoryPage } from '@/pages/SearchHistoryPage'
import { LeadAnalysisPage } from '@/pages/LeadAnalysisPage'
import { AiSettingsPage } from '@/pages/AiSettingsPage'
import { EmailDraftsPage } from '@/pages/EmailDraftsPage'
import { EmailEditorPage } from '@/pages/EmailEditorPage'
import { PromptTemplatesPage } from '@/pages/PromptTemplatesPage'
import { AuthProvider } from '@/context/AuthContext'
import PrivateRoute from '@/components/PrivateRoute'
import LoginPage from '@/pages/LoginPage'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
      retry: (failureCount, error: unknown) => {
        // Don't retry on 401/403
        const status = (error as { response?: { status?: number } })?.response?.status
        if (status === 401 || status === 403) return false
        return failureCount < 1
      },
    },
  },
})

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            {/* Public */}
            <Route path="/login" element={<LoginPage />} />

            {/* Protected */}
            <Route element={
              <PrivateRoute>
                <AppLayout />
              </PrivateRoute>
            }>
              <Route index element={<DashboardPage />} />
              <Route path="leads" element={<LeadsPage />} />
              <Route path="leads/new" element={<CreateLeadPage />} />
              <Route path="leads/:id" element={<LeadDetailPage />} />
              <Route path="leads/:id/edit" element={<EditLeadPage />} />
              <Route path="leads/:id/analysis" element={<LeadAnalysisPage />} />
              <Route path="followups" element={<FollowUpsPage />} />
              <Route path="search" element={<SearchPage />} />
              <Route path="search/history" element={<SearchHistoryPage />} />
              <Route path="emails" element={<EmailDraftsPage />} />
              <Route path="emails/:id" element={<EmailEditorPage />} />
              <Route path="settings/ai" element={<AiSettingsPage />} />
              <Route path="settings/prompts" element={<PromptTemplatesPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}

export default App

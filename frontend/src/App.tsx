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

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
      retry: 1,
    },
  },
})

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<AppLayout />}>
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
    </QueryClientProvider>
  )
}

export default App

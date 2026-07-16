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
            <Route path="followups" element={<FollowUpsPage />} />
            <Route path="search" element={<SearchPage />} />
            <Route path="search/history" element={<SearchHistoryPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App

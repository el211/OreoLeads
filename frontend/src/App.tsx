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
import { TagsPage } from '@/pages/TagsPage'
import { LeadAnalysisPage } from '@/pages/LeadAnalysisPage'
import { AiSettingsPage } from '@/pages/AiSettingsPage'
import { BrevoSettingsPage } from '@/pages/BrevoSettingsPage'
import { AirtableSettingsPage } from '@/pages/AirtableSettingsPage'
import { AirtableMappingPage } from '@/pages/AirtableMappingPage'
import { AirtableSyncHistoryPage } from '@/pages/AirtableSyncHistoryPage'
import { AirtableConflictsPage } from '@/pages/AirtableConflictsPage'
import { EmailDraftsPage } from '@/pages/EmailDraftsPage'
import { EmailEditorPage } from '@/pages/EmailEditorPage'
import { PromptTemplatesPage } from '@/pages/PromptTemplatesPage'
import { AutomationPage } from '@/pages/AutomationPage'
import { AutomationBuilderPage } from '@/pages/AutomationBuilderPage'
import { AutomationHistoryPage } from '@/pages/AutomationHistoryPage'
import { AutomationMonitoringPage } from '@/pages/AutomationMonitoringPage'
import { AnalyticsDashboardPage } from '@/pages/AnalyticsDashboardPage'
import { AnalyticsWidgetsPage } from '@/pages/AnalyticsWidgetsPage'
import { AnalyticsReportsPage } from '@/pages/AnalyticsReportsPage'
import { AnalyticsForecastPage } from '@/pages/AnalyticsForecastPage'
import { AnalyticsMonitoringPage } from '@/pages/AnalyticsMonitoringPage'
import { AuthProvider } from '@/context/AuthContext'
import PrivateRoute from '@/components/PrivateRoute'
import LoginPage from '@/pages/LoginPage'
import ForgotPasswordPage from '@/pages/ForgotPasswordPage'
import ResetPasswordPage from '@/pages/ResetPasswordPage'

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
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />

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
              <Route path="tags" element={<TagsPage />} />
              <Route path="search" element={<SearchPage />} />
              <Route path="search/history" element={<SearchHistoryPage />} />
              <Route path="emails" element={<EmailDraftsPage />} />
              <Route path="emails/:id" element={<EmailEditorPage />} />
              <Route path="settings/ai" element={<AiSettingsPage />} />
              <Route path="settings/brevo" element={<BrevoSettingsPage />} />
              <Route path="settings/prompts" element={<PromptTemplatesPage />} />
              <Route path="settings/airtable" element={<AirtableSettingsPage />} />
              <Route path="airtable/mapping" element={<AirtableMappingPage />} />
              <Route path="airtable/sync-history" element={<AirtableSyncHistoryPage />} />
              <Route path="airtable/conflicts" element={<AirtableConflictsPage />} />
              <Route path="analytics" element={<AnalyticsDashboardPage />} />
              <Route path="analytics/widgets" element={<AnalyticsWidgetsPage />} />
              <Route path="analytics/reports" element={<AnalyticsReportsPage />} />
              <Route path="analytics/forecast" element={<AnalyticsForecastPage />} />
              <Route path="analytics/monitoring" element={<AnalyticsMonitoringPage />} />
              <Route path="automation" element={<AutomationPage />} />
              <Route path="automation/builder/:id" element={<AutomationBuilderPage />} />
              <Route path="automation/history" element={<AutomationHistoryPage />} />
              <Route path="automation/monitoring" element={<AutomationMonitoringPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}

export default App

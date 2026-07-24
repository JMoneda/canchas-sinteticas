import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { Layout } from './components/Layout';
import { MarketplacePage } from './pages/MarketplacePage';
import { VenueDetailPage } from './pages/VenueDetailPage';
import { OpenMatchesPage } from './pages/OpenMatchesPage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { MyReservationsPage } from './pages/MyReservationsPage';
import { OwnerDashboardPage } from './pages/OwnerDashboardPage';
import { OwnerVenuesPage } from './pages/OwnerVenuesPage';
import { OwnerVenueDetailPage } from './pages/OwnerVenueDetailPage';
import { OwnerAgendaPage } from './pages/OwnerAgendaPage';
import { NotFoundPage } from './pages/NotFoundPage';

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<MarketplacePage />} />
            <Route path="partidos" element={<OpenMatchesPage />} />
            <Route path="sedes/:venueId" element={<VenueDetailPage />} />
            <Route path="login" element={<LoginPage />} />
            <Route path="registro" element={<RegisterPage />} />
            <Route
              path="mis-reservas"
              element={
                <ProtectedRoute>
                  <MyReservationsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="panel"
              element={
                <ProtectedRoute role="Owner">
                  <OwnerDashboardPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="panel/sedes"
              element={
                <ProtectedRoute role="Owner">
                  <OwnerVenuesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="panel/sedes/:venueId"
              element={
                <ProtectedRoute role="Owner">
                  <OwnerVenueDetailPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="panel/agenda"
              element={
                <ProtectedRoute role="Owner">
                  <OwnerAgendaPage />
                </ProtectedRoute>
              }
            />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

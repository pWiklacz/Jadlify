import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { RequireAuth } from './auth/RequireAuth'
import { LandingPage } from './routes/LandingPage'
import { LoginPage } from './routes/LoginPage'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public placeholder; real auth UI is slice S-01. */}
        <Route path="/login" element={<LoginPage />} />

        {/* Protected area. Phase 4 layers the responsive shell + section routes here. */}
        <Route element={<RequireAuth />}>
          <Route path="/" element={<LandingPage />} />
        </Route>

        {/* Unknown paths fall back to the protected landing (guard handles redirect). */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App

import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { RequireAuth } from './auth/RequireAuth'
import { AppShell } from './layout/AppShell'
import { LandingPage } from './routes/LandingPage'
import { LoginPage } from './routes/LoginPage'
import { ProductsPage } from './routes/sections/ProductsPage'
import { RecipesPage } from './routes/sections/RecipesPage'
import { MealPlanPage } from './routes/sections/MealPlanPage'
import { GoalsPage } from './routes/sections/GoalsPage'
import { ShoppingListPage } from './routes/sections/ShoppingListPage'

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public placeholder; real auth UI is slice S-01. */}
        <Route path="/login" element={<LoginPage />} />

        {/* Protected area: the guard gates access, the shell frames it. */}
        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route path="/" element={<LandingPage />} />
            <Route path="/products" element={<ProductsPage />} />
            <Route path="/recipes" element={<RecipesPage />} />
            <Route path="/meal-plan" element={<MealPlanPage />} />
            <Route path="/goals" element={<GoalsPage />} />
            <Route path="/shopping-list" element={<ShoppingListPage />} />
          </Route>
        </Route>

        {/* Unknown paths fall back to the protected home (guard handles redirect). */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App

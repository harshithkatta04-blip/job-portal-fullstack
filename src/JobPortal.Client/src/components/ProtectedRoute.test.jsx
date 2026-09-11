import { render, screen } from '@testing-library/react'
import {
  MemoryRouter,
  Route,
  Routes,
} from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import AuthContext from '../context/AuthContext'
import ProtectedRoute from './ProtectedRoute'

function renderProtectedRoute(user) {
  return render(
    <AuthContext.Provider value={{ user }}>
      <MemoryRouter initialEntries={['/candidate']}>
        <Routes>
          <Route path="/login" element={<p>Login page</p>} />
          <Route path="/jobs" element={<p>Jobs page</p>} />
          <Route
            path="/candidate"
            element={
              <ProtectedRoute allowedRole="Candidate">
                <p>Candidate dashboard</p>
              </ProtectedRoute>
            }
          />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

describe('ProtectedRoute', () => {
  it('redirects a user who is not logged in to login', () => {
    renderProtectedRoute(null)

    expect(screen.getByText('Login page')).toBeInTheDocument()
  })

  it('redirects a user with the wrong role to jobs', () => {
    renderProtectedRoute({
      fullName: 'Test Employer',
      role: 'Employer',
    })

    expect(screen.getByText('Jobs page')).toBeInTheDocument()
  })

  it('renders children for a user with the allowed role', () => {
    renderProtectedRoute({
      fullName: 'Test Candidate',
      role: 'Candidate',
    })

    expect(
      screen.getByText('Candidate dashboard'),
    ).toBeInTheDocument()
  })
})
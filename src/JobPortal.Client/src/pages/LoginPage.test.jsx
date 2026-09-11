import {
  fireEvent,
  render,
  screen,
} from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'
import AuthContext from '../context/AuthContext'
import api from '../services/api'
import LoginPage from './LoginPage'

vi.mock('../services/api', () => ({
  default: {
    post: vi.fn(),
  },
}))

describe('LoginPage', () => {
  const login = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows the API error message when login fails', async () => {
    api.post.mockRejectedValueOnce({
      response: {
        data: {
          message: 'Invalid email or password.',
        },
      },
    })

    render(
      <AuthContext.Provider
        value={{
          user: null,
          login,
          logout: vi.fn(),
        }}
      >
        <MemoryRouter>
          <LoginPage />
        </MemoryRouter>
      </AuthContext.Provider>,
    )

    fireEvent.change(screen.getByLabelText('Email'), {
      target: {
        value: 'candidate@example.com',
      },
    })

    fireEvent.change(screen.getByLabelText('Password'), {
      target: {
        value: 'wrong-password',
      },
    })

    fireEvent.click(
      screen.getByRole('button', { name: 'Login' }),
    )

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Invalid email or password.',
    )

    expect(api.post).toHaveBeenCalledWith('/auth/login', {
      email: 'candidate@example.com',
      password: 'wrong-password',
    })

    expect(login).not.toHaveBeenCalled()
  })
})
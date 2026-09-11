import { fireEvent, render, screen } from '@testing-library/react'
import {
  MemoryRouter,
  Route,
  Routes,
} from 'react-router-dom'
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'
import AuthContext from '../context/AuthContext'
import api from '../services/api'
import JobDetailsPage from './JobDetailsPage'

vi.mock('../services/api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
}))

describe('JobDetailsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('loads job details and asks a visitor to log in', async () => {
    api.get.mockResolvedValueOnce({
      data: {
        jobId: 1,
        title: 'Junior .NET Developer',
        companyName: 'Test Technologies',
        description: 'Develop backend APIs.',
        location: 'Hyderabad',
        jobType: 'FullTime',
        experienceRequiredYears: 1,
        salaryRange: '3-5 LPA',
        applicationDeadline: '2026-10-01T00:00:00Z',
        skills: [
          {
            skillId: 1,
            name: 'C#',
          },
        ],
      },
    })

    render(
      <AuthContext.Provider
        value={{
          user: null,
          login: vi.fn(),
          logout: vi.fn(),
        }}
      >
        <MemoryRouter initialEntries={['/jobs/1']}>
          <Routes>
            <Route
              path="/jobs/:jobId"
              element={<JobDetailsPage />}
            />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    )

    expect(
      await screen.findByRole('heading', {
        name: 'Junior .NET Developer',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByText('Develop backend APIs.'),
    ).toBeInTheDocument()

    expect(screen.getByText('C#')).toBeInTheDocument()

    expect(api.get).toHaveBeenCalledWith('/jobs/1')

    expect(
      screen.getByRole('link', { name: 'Login to apply' }),
    ).toHaveAttribute('href', '/login')
  })

    it('submits an application using the backend apply endpoint', async () => {
    api.get.mockResolvedValueOnce({
      data: {
        jobId: 2,
        title: 'Associate .NET Developer',
        companyName: 'Test Technologies',
        description: 'Develop ASP.NET Core APIs.',
        location: 'Bengaluru',
        jobType: 'FullTime',
        experienceRequiredYears: 2,
        salaryRange: '4-6 LPA',
        applicationDeadline: '2026-10-30T00:00:00Z',
        skills: [],
      },
    })

    api.post.mockResolvedValueOnce({
      data: {},
    })

    render(
      <AuthContext.Provider
        value={{
          user: {
            userId: 1,
            fullName: 'Test Candidate',
            role: 'Candidate',
          },
          login: vi.fn(),
          logout: vi.fn(),
        }}
      >
        <MemoryRouter initialEntries={['/jobs/2']}>
          <Routes>
            <Route
              path="/jobs/:jobId"
              element={<JobDetailsPage />}
            />
          </Routes>
        </MemoryRouter>
      </AuthContext.Provider>,
    )

    fireEvent.click(
      await screen.findByRole('button', {
        name: 'Apply now',
      }),
    )

    expect(
      await screen.findByRole('status'),
    ).toHaveTextContent('Application submitted successfully.')

    expect(api.post).toHaveBeenCalledWith(
      '/jobs/2/apply',
      {},
    )
  })
})
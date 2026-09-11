import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import {
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest'
import api from '../services/api'
import JobsPage from './JobsPage'

vi.mock('../services/api', () => ({
  default: {
    get: vi.fn(),
  },
}))

describe('JobsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('loads and displays available jobs', async () => {
    api.get.mockResolvedValueOnce({
      data: {
        items: [
          {
            jobId: 1,
            title: 'Junior .NET Developer',
            companyName: 'Test Technologies',
            location: 'Hyderabad',
            jobType: 'FullTime',
            experienceRequiredYears: 1,
            salaryRange: '3-5 LPA',
          },
        ],
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
      },
    })

    render(
      <MemoryRouter>
        <JobsPage />
      </MemoryRouter>,
    )

    expect(
      await screen.findByText('Junior .NET Developer'),
    ).toBeInTheDocument()

    expect(
      screen.getByText('Test Technologies'),
    ).toBeInTheDocument()

    expect(screen.getByText('1 job found')).toBeInTheDocument()

    expect(api.get).toHaveBeenCalledWith('/jobs', {
      params: {
        page: 1,
        pageSize: 10,
      },
    })

    expect(
      screen.getByRole('link', { name: 'View details' }),
    ).toHaveAttribute('href', '/jobs/1')
  })
})
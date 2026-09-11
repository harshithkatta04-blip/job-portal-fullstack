import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import api from '../services/api'

const pageSize = 10

const initialFilters = {
  title: '',
  location: '',
  jobType: '',
  experienceYears: '',
}

function formatJobType(jobType) {
  return jobType.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function JobsPage() {
  const [filters, setFilters] = useState(initialFilters)
  const [appliedFilters, setAppliedFilters] = useState({})
  const [jobs, setJobs] = useState([])
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let ignore = false

    async function loadJobs() {
      setIsLoading(true)
      setError('')

      try {
        const response = await api.get('/jobs', {
          params: {
            ...appliedFilters,
            page,
            pageSize,
          },
        })

        if (!ignore) {
          setJobs(response.data.items)
          setTotalCount(response.data.totalCount)
          setTotalPages(response.data.totalPages)
        }
      } catch (requestError) {
        if (!ignore) {
          setJobs([])
          setTotalCount(0)
          setTotalPages(0)
          setError(
            requestError.response?.data?.message ??
              'Unable to load jobs. Please try again.',
          )
        }
      } finally {
        if (!ignore) {
          setIsLoading(false)
        }
      }
    }

    loadJobs()

    return () => {
      ignore = true
    }
  }, [appliedFilters, page])

  function handleChange(event) {
    const { name, value } = event.target

    setFilters((currentFilters) => ({
      ...currentFilters,
      [name]: value,
    }))
  }

  function handleSubmit(event) {
    event.preventDefault()

    setAppliedFilters({
      title: filters.title.trim() || undefined,
      location: filters.location.trim() || undefined,
      jobType: filters.jobType || undefined,
      experienceYears: filters.experienceYears
        ? Number(filters.experienceYears)
        : undefined,
    })

    setPage(1)
  }

  function handleClear() {
    setFilters(initialFilters)
    setAppliedFilters({})
    setPage(1)
  }

  return (
    <main>
      <h1>Available Jobs</h1>

      <form onSubmit={handleSubmit}>
        <label htmlFor="title">Job title</label>
        <input
          id="title"
          name="title"
          type="search"
          value={filters.title}
          onChange={handleChange}
          maxLength="150"
        />

        <label htmlFor="location">Location</label>
        <input
          id="location"
          name="location"
          type="search"
          value={filters.location}
          onChange={handleChange}
          maxLength="150"
        />

        <label htmlFor="jobType">Job type</label>
        <select
          id="jobType"
          name="jobType"
          value={filters.jobType}
          onChange={handleChange}
        >
          <option value="">All job types</option>
          <option value="FullTime">Full time</option>
          <option value="PartTime">Part time</option>
          <option value="Contract">Contract</option>
          <option value="Internship">Internship</option>
        </select>

        <label htmlFor="experienceYears">
          Maximum experience required
        </label>
        <input
          id="experienceYears"
          name="experienceYears"
          type="number"
          min="0"
          max="100"
          value={filters.experienceYears}
          onChange={handleChange}
        />

        <button type="submit">Search jobs</button>
        <button type="button" onClick={handleClear}>
          Clear filters
        </button>
      </form>

      {isLoading && <p>Loading jobs...</p>}

      {error && <p role="alert">{error}</p>}

      {!isLoading && !error && (
        <>
          <p>
            {totalCount} {totalCount === 1 ? 'job' : 'jobs'} found
          </p>

          {jobs.length === 0 ? (
            <p>No jobs match your search.</p>
          ) : (
            <section aria-label="Job listings">
              {jobs.map((job) => (
                <article key={job.jobId}>
                  <h2>
                    <Link to={`/jobs/${job.jobId}`}>
                      {job.title}
                    </Link>
                  </h2>

                  <p>{job.companyName}</p>
                  <p>Location: {job.location}</p>
                  <p>Type: {formatJobType(job.jobType)}</p>
                  <p>
                    Experience required:{' '}
                    {job.experienceRequiredYears} years
                  </p>

                  {job.salaryRange && (
                    <p>Salary: {job.salaryRange}</p>
                  )}

                  <Link to={`/jobs/${job.jobId}`}>
                    View details
                  </Link>
                </article>
              ))}
            </section>
          )}

          {totalPages > 1 && (
            <nav aria-label="Job result pages">
              <button
                type="button"
                disabled={page === 1}
                onClick={() => setPage((current) => current - 1)}
              >
                Previous
              </button>

              <span>
                Page {page} of {totalPages}
              </span>

              <button
                type="button"
                disabled={page === totalPages}
                onClick={() => setPage((current) => current + 1)}
              >
                Next
              </button>
            </nav>
          )}
        </>
      )}
    </main>
  )
}

export default JobsPage
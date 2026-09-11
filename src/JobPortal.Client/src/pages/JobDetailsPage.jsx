import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import useAuth from '../context/useAuth'
import api from '../services/api'

function formatJobType(jobType) {
  return jobType.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function formatDate(value) {
  return new Date(value).toLocaleDateString()
}

function JobDetailsPage() {
  const { jobId } = useParams()
  const { user } = useAuth()

  const [job, setJob] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [isApplying, setIsApplying] = useState(false)
  const [applicationError, setApplicationError] = useState('')
  const [applicationMessage, setApplicationMessage] = useState('')

  useEffect(() => {
    let ignore = false

    async function loadJob() {
      setIsLoading(true)
      setError('')

      try {
        const response = await api.get(`/jobs/${jobId}`)

        if (!ignore) {
          setJob(response.data)
        }
      } catch (requestError) {
        if (!ignore) {
          setJob(null)
          setError(
            requestError.response?.data?.message ??
              'Unable to load this job. Please try again.',
          )
        }
      } finally {
        if (!ignore) {
          setIsLoading(false)
        }
      }
    }

    loadJob()

    return () => {
      ignore = true
    }
  }, [jobId])

  async function handleApply() {
    setIsApplying(true)
    setApplicationError('')
    setApplicationMessage('')

    try {
      await api.post(`/jobs/${jobId}/apply`, {})
      setApplicationMessage('Application submitted successfully.')
    } catch (requestError) {
      setApplicationError(
        requestError.response?.data?.message ??
          'Unable to submit your application. Please try again.',
      )
    } finally {
      setIsApplying(false)
    }
  }

  if (isLoading) {
    return (
      <main>
        <p>Loading job details...</p>
      </main>
    )
  }

  if (error) {
    return (
      <main>
        <p role="alert">{error}</p>
        <Link to="/jobs">Back to jobs</Link>
      </main>
    )
  }

  return (
    <main>
      <Link to="/jobs">Back to jobs</Link>

      <article>
        <h1>{job.title}</h1>
        <p>{job.companyName}</p>

        <dl>
          <dt>Location</dt>
          <dd>{job.location}</dd>

          <dt>Job type</dt>
          <dd>{formatJobType(job.jobType)}</dd>

          <dt>Experience required</dt>
          <dd>{job.experienceRequiredYears} years</dd>

          <dt>Salary</dt>
          <dd>{job.salaryRange ?? 'Not specified'}</dd>

          <dt>Application deadline</dt>
          <dd>{formatDate(job.applicationDeadline)}</dd>
        </dl>

        <section>
          <h2>Description</h2>
          <p>{job.description}</p>
        </section>

        <section>
          <h2>Required skills</h2>

          {job.skills.length === 0 ? (
            <p>No specific skills listed.</p>
          ) : (
            <ul>
              {job.skills.map((skill) => (
                <li key={skill.skillId}>{skill.name}</li>
              ))}
            </ul>
          )}
        </section>
      </article>

      {!user && <Link to="/login">Login to apply</Link>}

      {user?.role === 'Candidate' && (
        <section>
          <button
            type="button"
            disabled={isApplying || Boolean(applicationMessage)}
            onClick={handleApply}
          >
            {isApplying ? 'Applying...' : 'Apply now'}
          </button>

          {applicationMessage && (
            <p role="status">{applicationMessage}</p>
          )}

          {applicationError && (
            <p role="alert">{applicationError}</p>
          )}
        </section>
      )}

      {user?.role === 'Employer' && (
        <p>Employers can view jobs but cannot apply.</p>
      )}
    </main>
  )
}

export default JobDetailsPage
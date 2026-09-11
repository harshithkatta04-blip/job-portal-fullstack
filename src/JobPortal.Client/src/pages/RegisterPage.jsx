import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import useAuth from '../context/useAuth'
import api from '../services/api'

function RegisterPage() {
  const navigate = useNavigate()
  const { login } = useAuth()

  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: '',
    role: 'Candidate',
    companyName: '',
    companyLocation: '',
    companyDescription: '',
    companyWebsite: '',
  })

  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  function handleChange(event) {
    const { name, value } = event.target

    setFormData((currentData) => ({
      ...currentData,
      [name]: value,
    }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    const request = {
      fullName: formData.fullName,
      email: formData.email,
      password: formData.password,
      role: formData.role,
    }

    if (formData.role === 'Employer') {
      request.companyName = formData.companyName
      request.companyLocation = formData.companyLocation
      request.companyDescription = formData.companyDescription
      request.companyWebsite = formData.companyWebsite || null
    }

    try {
      const response = await api.post('/auth/register', request)

      login(response.data)
      navigate('/jobs')
    } catch (requestError) {
      setError(
        requestError.response?.data?.message ??
          'Unable to register. Please check your details.',
      )
    } finally {
      setIsSubmitting(false)
    }
  }

  const isEmployer = formData.role === 'Employer'

  return (
    <main>
      <h1>Create Account</h1>

      <form onSubmit={handleSubmit}>
        <label htmlFor="fullName">Full name</label>
        <input
          id="fullName"
          name="fullName"
          value={formData.fullName}
          onChange={handleChange}
          maxLength="100"
          required
        />

        <label htmlFor="email">Email</label>
        <input
          id="email"
          name="email"
          type="email"
          value={formData.email}
          onChange={handleChange}
          maxLength="255"
          required
        />

        <label htmlFor="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          value={formData.password}
          onChange={handleChange}
          minLength="8"
          maxLength="100"
          required
        />

        <label htmlFor="role">Register as</label>
        <select
          id="role"
          name="role"
          value={formData.role}
          onChange={handleChange}
        >
          <option value="Candidate">Candidate</option>
          <option value="Employer">Employer</option>
        </select>

        {isEmployer && (
          <>
            <label htmlFor="companyName">Company name</label>
            <input
              id="companyName"
              name="companyName"
              value={formData.companyName}
              onChange={handleChange}
              maxLength="150"
              required
            />

            <label htmlFor="companyLocation">Company location</label>
            <input
              id="companyLocation"
              name="companyLocation"
              value={formData.companyLocation}
              onChange={handleChange}
              maxLength="150"
              required
            />

            <label htmlFor="companyDescription">
              Company description
            </label>
            <textarea
              id="companyDescription"
              name="companyDescription"
              value={formData.companyDescription}
              onChange={handleChange}
              maxLength="2000"
              required
            />

            <label htmlFor="companyWebsite">
              Company website (optional)
            </label>
            <input
              id="companyWebsite"
              name="companyWebsite"
              type="url"
              value={formData.companyWebsite}
              onChange={handleChange}
              maxLength="500"
            />
          </>
        )}

        {error && <p role="alert">{error}</p>}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating account...' : 'Register'}
        </button>
      </form>

      <p>
        Already have an account? <Link to="/login">Login</Link>
      </p>
    </main>
  )
}

export default RegisterPage
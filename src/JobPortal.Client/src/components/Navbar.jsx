import { Link, useNavigate } from 'react-router-dom'
import useAuth from '../context/useAuth'

function Navbar() {
  const navigate = useNavigate()
  const { user, logout } = useAuth()

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <header>
      <nav>
        <Link to="/">Job Portal</Link>

        <div>
          <Link to="/jobs">Jobs</Link>

          {user?.role === 'Candidate' && (
            <Link to="/candidate">Candidate Dashboard</Link>
          )}

          {user?.role === 'Employer' && (
            <Link to="/employer">Employer Dashboard</Link>
          )}

          {user ? (
            <>
              <span>
                {user.fullName} ({user.role})
              </span>

              <button type="button" onClick={handleLogout}>
                Logout
              </button>
            </>
          ) : (
            <>
              <Link to="/login">Login</Link>
              <Link to="/register">Register</Link>
            </>
          )}
        </div>
      </nav>
    </header>
  )
}

export default Navbar
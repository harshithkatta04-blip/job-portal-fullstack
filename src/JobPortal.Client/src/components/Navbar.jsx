import { Link } from 'react-router-dom'

function Navbar() {
  return (
    <header>
      <nav>
        <Link to="/">Job Portal</Link>

        <div>
          <Link to="/jobs">Jobs</Link>
          <Link to="/login">Login</Link>
          <Link to="/register">Register</Link>
        </div>
      </nav>
    </header>
  )
}

export default Navbar
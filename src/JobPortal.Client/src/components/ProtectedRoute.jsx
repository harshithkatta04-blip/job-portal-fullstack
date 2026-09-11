import { Navigate } from 'react-router-dom'
import useAuth from '../context/useAuth'

function ProtectedRoute({ children, allowedRole }) {
  const { user } = useAuth()

  if (!user) {
    return <Navigate to="/login" replace />
  }

  if (allowedRole && user.role !== allowedRole) {
    return <Navigate to="/jobs" replace />
  }

  return children
}

export default ProtectedRoute
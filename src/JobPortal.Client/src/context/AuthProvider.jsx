import { useState } from 'react'
import AuthContext from './AuthContext'

function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const savedUser = localStorage.getItem('user')

    return savedUser ? JSON.parse(savedUser) : null
  })

  function login(authResponse) {
    const authenticatedUser = {
      userId: authResponse.userId,
      fullName: authResponse.fullName,
      email: authResponse.email,
      role: authResponse.role,
      expiresAt: authResponse.expiresAt,
    }

    localStorage.setItem('token', authResponse.token)
    localStorage.setItem('user', JSON.stringify(authenticatedUser))
    setUser(authenticatedUser)
  }

  function logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export default AuthProvider
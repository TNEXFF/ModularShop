import { useState, type FormEvent } from 'react'
import { useAuth } from '../../auth/AuthContext'

/**
 * The sign-in / register gate. The whole app sits behind it: authentication is a kernel concern and
 * every module endpoint requires a signed-in user. Cookie auth means there is nothing to store here.
 */
export function AuthScreen() {
  const { login, register } = useAuth()
  const [mode, setMode] = useState<'login' | 'register'>('login')
  const [email, setEmail] = useState('admin@modularshop.local')
  const [password, setPassword] = useState('Passw0rd!')
  const [displayName, setDisplayName] = useState('')
  const [error, setError] = useState<string>()
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true); setError(undefined)
    try {
      if (mode === 'login') await login(email, password)
      else await register(email, password, displayName || email)
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-wrap">
      <div className="auth-card card">
        <div className="brand"><span className="logo">▦</span> ModularShop</div>
        <p className="muted small">
          Modular Monolith demo. Authentication lives in the <b>kernel</b> (ASP.NET Core Identity) and is
          shared by every module — sign in to continue.
        </p>

        <div className="auth-tabs">
          <button className={mode === 'login' ? 'active' : ''} onClick={() => { setMode('login'); setError(undefined) }}>Sign in</button>
          <button className={mode === 'register' ? 'active' : ''} onClick={() => { setMode('register'); setError(undefined) }}>Register</button>
        </div>

        <form onSubmit={submit}>
          {mode === 'register' && (
            <label className="field"><span>Display name</span>
              <input value={displayName} onChange={e => setDisplayName(e.target.value)} placeholder="Jane Doe" />
            </label>
          )}
          <label className="field"><span>Email</span>
            <input type="email" value={email} onChange={e => setEmail(e.target.value)} required />
          </label>
          <label className="field"><span>Password</span>
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} required />
          </label>

          {error && <p className="error small">{error}</p>}

          <button className="btn primary auth-submit" disabled={busy} type="submit">
            {busy ? 'Please wait…' : (mode === 'login' ? 'Sign in' : 'Create account')}
          </button>
        </form>

        <div className="auth-demo">
          <strong>Demo accounts</strong> — password <code>Passw0rd!</code>:
          <ul>
            <li><code>admin@modularshop.local</code> · Admin</li>
            <li><code>agent@modularshop.local</code> · Agent</li>
          </ul>
        </div>
      </div>
    </div>
  )
}

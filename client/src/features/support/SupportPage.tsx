import { useEffect, useState } from 'react'
import { api, dateTime, type Customer, type Ticket, type TicketListItem } from '../../api'

const STATUSES = ['Open', 'Pending', 'Resolved', 'Closed']

export function SupportPage() {
  const [tickets, setTickets] = useState<TicketListItem[]>([])
  const [selected, setSelected] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [error, setError] = useState<string>()
  const [loading, setLoading] = useState(true)

  function refresh() {
    setLoading(true)
    api.tickets().then(setTickets).catch(e => setError(e.message)).finally(() => setLoading(false))
  }
  useEffect(refresh, [])

  if (selected) return <TicketDetail id={selected} onBack={() => { setSelected(null); refresh() }} />
  if (creating) return <NewTicket onDone={() => { setCreating(false); refresh() }} onCancel={() => setCreating(false)} />

  return (
    <section>
      <h2>Support <span className="owner">Support module</span></h2>
      <p className="muted">
        A <strong>genuinely unrelated</strong> module: it takes no part in the order → stock → ship flow
        and publishes/consumes no events. It uses only the <b>kernel</b> — the shared <code>Customer</code>
        and the signed-in Identity user. Its tables live in the <code>support</code> schema.
      </p>

      <div className="toolbar">
        <button className="btn primary" onClick={() => setCreating(true)}>New ticket</button>
      </div>

      {loading ? <p className="muted">Loading tickets…</p>
        : error ? <p className="error">{error}</p>
        : tickets.length === 0 ? <div className="card"><p className="muted">No tickets yet.</p></div>
        : (
          <div className="card">
            <table>
              <thead><tr><th>Subject</th><th>Customer</th><th>Status</th><th className="num">Messages</th><th></th></tr></thead>
              <tbody>
                {tickets.map(t => (
                  <tr key={t.id} className="clickable" onClick={() => setSelected(t.id)}>
                    <td><strong>{t.subject}</strong></td>
                    <td>{t.customerName}</td>
                    <td><span className={`status ${t.status.toLowerCase()}`}>{t.status}</span></td>
                    <td className="num">{t.messageCount}</td>
                    <td className="chev">▸</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
    </section>
  )
}

function TicketDetail({ id, onBack }: { id: string; onBack: () => void }) {
  const [ticket, setTicket] = useState<Ticket | null>(null)
  const [reply, setReply] = useState('')
  const [error, setError] = useState<string>()
  const [busy, setBusy] = useState(false)

  useEffect(() => { api.ticket(id).then(setTicket).catch(e => setError(e.message)) }, [id])

  async function send() {
    if (!reply.trim()) return
    setBusy(true); setError(undefined)
    try { setTicket(await api.addTicketMessage(id, reply)); setReply('') }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function setStatus(status: string) {
    setBusy(true); setError(undefined)
    try { setTicket(await api.changeTicketStatus(id, status)) }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  if (!ticket) {
    return <section><button className="btn small back-btn" onClick={onBack}>← All tickets</button>
      {error ? <p className="error">{error}</p> : <p className="muted">Loading…</p>}</section>
  }

  return (
    <section>
      <button className="btn small back-btn" onClick={onBack}>← All tickets</button>
      <h2>{ticket.subject} <span className={`status ${ticket.status.toLowerCase()}`}>{ticket.status}</span></h2>
      <p className="muted small">
        For <strong>{ticket.customerName}</strong> · opened by {ticket.createdByName} · {dateTime(ticket.createdOnUtc)}
      </p>

      <div className="card">
        <p className="ticket-desc">{ticket.description || <span className="muted">No description.</span>}</p>

        <div className="thread">
          {ticket.messages.length === 0 && <p className="muted small">No replies yet.</p>}
          {ticket.messages.map((m, i) => (
            <div key={i} className="msg">
              <div className="msg-head"><strong>{m.authorName}</strong><span className="muted small">{dateTime(m.sentOnUtc)}</span></div>
              <div className="msg-body">{m.body}</div>
            </div>
          ))}
        </div>

        <div className="reply">
          <textarea value={reply} onChange={e => setReply(e.target.value)} placeholder="Write a reply…" rows={3} />
          <div className="reply-bar">
            <div className="status-actions">
              <span className="muted small">Status:</span>
              {STATUSES.map(s => (
                <button key={s} className={`btn small ${s === ticket.status ? 'primary' : ''}`}
                  disabled={busy || s === ticket.status} onClick={() => setStatus(s)}>{s}</button>
              ))}
            </div>
            <button className="btn primary" disabled={busy || !reply.trim()} onClick={send}>
              {busy ? 'Sending…' : 'Reply'}
            </button>
          </div>
        </div>
        {error && <p className="error small">{error}</p>}
      </div>
    </section>
  )
}

function NewTicket({ onDone, onCancel }: { onDone: () => void; onCancel: () => void }) {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [customerId, setCustomerId] = useState('')
  const [subject, setSubject] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<string>()
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    api.customers().then(cs => { setCustomers(cs); if (cs[0]) setCustomerId(cs[0].id) }).catch(() => {})
  }, [])

  async function submit() {
    setBusy(true); setError(undefined)
    try { await api.createTicket(customerId, subject, description); onDone() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  return (
    <section>
      <button className="btn small back-btn" onClick={onCancel}>← Cancel</button>
      <h2>New support ticket <span className="owner">Support module</span></h2>
      <div className="card">
        <label className="field"><span>Customer (shared kernel entity)</span>
          <select value={customerId} onChange={e => setCustomerId(e.target.value)}>
            {customers.map(c => <option key={c.id} value={c.id}>{c.name} — {c.email}</option>)}
          </select>
        </label>
        <label className="field field-wide"><span>Subject</span>
          <input value={subject} onChange={e => setSubject(e.target.value)} placeholder="Short summary" />
        </label>
        <label className="field field-wide"><span>Description</span>
          <textarea value={description} onChange={e => setDescription(e.target.value)} rows={4} placeholder="Describe the issue…" />
        </label>
        {error && <p className="error small">{error}</p>}
        <div className="order-bar">
          <span className="muted small">Opened as the signed-in Identity user.</span>
          <button className="btn primary" disabled={busy || !subject.trim() || !customerId} onClick={submit}>
            {busy ? 'Creating…' : 'Create ticket'}
          </button>
        </div>
      </div>
    </section>
  )
}

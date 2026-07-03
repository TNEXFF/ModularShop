import { useEffect, useState } from 'react'
import { api, date, type Shipment } from '../../api'

export function ShipmentsPage() {
  const [shipments, setShipments] = useState<Shipment[]>([])
  const [error, setError] = useState<string>()
  const [actionError, setActionError] = useState<string>()
  const [loading, setLoading] = useState(true)

  function refresh() {
    api.shipments().then(setShipments).catch(e => setError(e.message)).finally(() => setLoading(false))
  }
  useEffect(refresh, [])

  async function advance(id: string, kind: 'ship' | 'deliver') {
    setActionError(undefined)
    try {
      kind === 'ship' ? await api.shipShipment(id) : await api.deliverShipment(id)
      refresh()
    } catch (e) {
      setActionError((e as Error).message)
    }
  }

  if (loading) return <p className="muted">Loading shipments…</p>
  if (error) return <p className="error">{error}</p>

  return (
    <section>
      <h2>Shipments <span className="owner">Shipping module</span></h2>
      <p className="muted">Shipments live in the <code>shipping</code> schema. Each was created by the Shipping module reacting to an <code>OrderPlaced</code> event — Shipping never reads the Sales tables.</p>

      {actionError && <p className="error">{actionError}</p>}

      <div className="card">
        {shipments.length === 0 ? <p className="muted">No shipments yet.</p> : (
        <table>
          <thead>
            <tr><th>Shipment #</th><th>Order #</th><th>Customer</th><th>Items</th><th>Status</th><th>Carrier / Tracking</th><th>Created</th><th></th></tr>
          </thead>
          <tbody>
            {shipments.map(s => (
              <tr key={s.id}>
                <td className="mono">{s.shipmentNumber}</td>
                <td className="mono">{s.orderNumber}</td>
                <td>{s.customerName}</td>
                <td className="muted small">{s.items.map(i => `${i.productName} ×${i.quantity}`).join(', ')}</td>
                <td><span className={`status ${s.status.toLowerCase()}`}>{s.status}</span></td>
                <td className="muted small">{s.carrier ? `${s.carrier} · ${s.trackingNumber}` : '—'}</td>
                <td>{date(s.createdOnUtc)}</td>
                <td>
                  {s.status === 'Pending' && <button className="btn small" onClick={() => advance(s.id, 'ship')}>Mark shipped</button>}
                  {s.status === 'Shipped' && <button className="btn small" onClick={() => advance(s.id, 'deliver')}>Mark delivered</button>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        )}
      </div>
    </section>
  )
}

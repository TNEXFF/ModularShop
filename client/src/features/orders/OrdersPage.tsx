import React, { useEffect, useState } from 'react'
import { api, date, money, type Order } from '../../api'

export function OrdersPage() {
  const [orders, setOrders] = useState<Order[]>([])
  const [open, setOpen] = useState<Set<string>>(new Set())
  const [error, setError] = useState<string>()
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.orders().then(setOrders).catch(e => setError(e.message)).finally(() => setLoading(false))
  }, [])

  function toggle(id: string) {
    setOpen(s => { const n = new Set(s); n.has(id) ? n.delete(id) : n.add(id); return n })
  }

  if (loading) return <p className="muted">Loading orders…</p>
  if (error) return <p className="error">{error}</p>

  return (
    <section>
      <h2>Orders <span className="owner">Sales module</span></h2>
      <p className="muted">Orders live in the <code>sales</code> schema. Each line stores a <em>snapshot</em> of the product name &amp; price captured at order time — Sales does not read the Warehouse tables.</p>

      <div className="card">
        {orders.length === 0 ? <p className="muted">No orders yet.</p> : (
        <table>
          <thead>
            <tr><th></th><th>Order #</th><th>Customer</th><th>Placed</th><th>By</th><th>Status</th><th className="num">Total</th></tr>
          </thead>
          <tbody>
            {orders.map(o => (
              <React.Fragment key={o.id}>
                <tr className="clickable" onClick={() => toggle(o.id)}>
                  <td className="chev">{open.has(o.id) ? '▾' : '▸'}</td>
                  <td className="mono">{o.orderNumber}</td>
                  <td>{o.customerName}</td>
                  <td>{date(o.placedOnUtc)}</td>
                  <td className="muted small">{o.placedBy}</td>
                  <td><span className={`status ${o.status.toLowerCase()}`}>{o.status}</span></td>
                  <td className="num">{money(o.total, o.currencyCode)}</td>
                </tr>
                {open.has(o.id) && (
                  <tr className="detail">
                    <td></td>
                    <td colSpan={6}>
                      <table className="inner">
                        <thead><tr><th>Product</th><th className="num">Unit</th><th className="num">Qty</th><th className="num">Line</th></tr></thead>
                        <tbody>
                          {o.lines.map((l, i) => (
                            <tr key={i}><td>{l.productName}</td><td className="num">{money(l.unitPrice, o.currencyCode)}</td><td className="num">{l.quantity}</td><td className="num">{money(l.lineTotal, o.currencyCode)}</td></tr>
                          ))}
                        </tbody>
                      </table>
                    </td>
                  </tr>
                )}
              </React.Fragment>
            ))}
          </tbody>
        </table>
        )}
      </div>
    </section>
  )
}

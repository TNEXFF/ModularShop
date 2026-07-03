import { useEffect, useState } from 'react'
import { api, money, type Customer, type Order, type Product } from '../../api'

export function PlaceOrderPage() {
  const [customers, setCustomers] = useState<Customer[]>([])
  const [products, setProducts] = useState<Product[]>([])
  const [customerId, setCustomerId] = useState('')
  const [qty, setQty] = useState<Record<string, number>>({})
  const [placed, setPlaced] = useState<Order | null>(null)
  const [errors, setErrors] = useState<string[]>([])
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string>()

  function load() {
    setLoadError(undefined)
    Promise.all([
      api.products().then(setProducts),
      api.customers().then(cs => { setCustomers(cs); if (cs[0] && !customerId) setCustomerId(cs[0].id) }),
    ]).catch(e => setLoadError(e.message)).finally(() => setLoading(false))
  }
  useEffect(load, [])

  const lines = Object.entries(qty).filter(([, q]) => q > 0).map(([productId, quantity]) => ({ productId, quantity }))
  const total = lines.reduce((sum, l) => {
    const p = products.find(x => x.id === l.productId)
    return sum + (p ? p.price * l.quantity : 0)
  }, 0)

  function setQuantity(id: string, value: number) {
    setQty(q => ({ ...q, [id]: Math.max(0, value || 0) }))
  }

  async function submit() {
    setBusy(true); setErrors([]); setPlaced(null)
    try {
      const order = await api.placeOrder(customerId, lines)
      setPlaced(order)
      setQty({})
      load() // reload catalogue to show the decremented stock
    } catch (e) {
      setErrors(String((e as Error).message).split(', '))
    } finally {
      setBusy(false)
    }
  }

  if (loading) return <p className="muted">Loading…</p>
  if (loadError) return <p className="error">{loadError}</p>

  return (
    <section>
      <h2>Place an order <span className="owner">Sales module</span></h2>

      <div className="explain">
        <strong>What happens when you place this order:</strong>
        <ol>
          <li><b>Sales</b> calls the <b>Warehouse</b> module's public API (a <em>synchronous</em> call) to fetch the current price &amp; stock.</li>
          <li>Sales saves the order in its own <code>sales</code> schema and publishes an <code>OrderPlaced</code> <em>integration event</em>.</li>
          <li><b>Warehouse</b> handles the event and decrements stock; <b>Shipping</b> handles the same event and creates a shipment.</li>
        </ol>
      </div>

      <div className="card">
        <label className="field">
          <span>Customer</span>
          <select value={customerId} onChange={e => setCustomerId(e.target.value)}>
            {customers.map(c => <option key={c.id} value={c.id}>{c.name} — {c.email}</option>)}
          </select>
        </label>

        <table>
          <thead>
            <tr><th>Product</th><th className="num">Price</th><th className="num">In stock</th><th className="num">Quantity</th></tr>
          </thead>
          <tbody>
            {products.map(p => (
              <tr key={p.id}>
                <td>{p.name}<div className="muted small mono">{p.sku}</div></td>
                <td className="num">{money(p.price, p.currencyCode)}</td>
                <td className="num">{p.stockQuantity}</td>
                <td className="num">
                  <input type="number" min={0} max={p.stockQuantity} value={qty[p.id] ?? 0}
                    onChange={e => setQuantity(p.id, parseInt(e.target.value, 10))} className="qty" />
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="order-bar">
          <div>{lines.length} line(s) · <strong>{money(total)}</strong></div>
          <button className="btn primary" disabled={busy || lines.length === 0 || !customerId} onClick={submit}>
            {busy ? 'Placing…' : 'Place order'}
          </button>
        </div>
      </div>

      {errors.length > 0 && (
        <div className="card error-card">
          <strong>Order rejected</strong>
          <ul>{errors.map((e, i) => <li key={i}>{e}</li>)}</ul>
        </div>
      )}

      {placed && (
        <div className="card success-card">
          <strong>Order {placed.orderNumber} placed ✔</strong>
          <p className="muted small">
            Stock was decremented in Warehouse and a shipment was created in Shipping (see the Catalogue, Orders and Shipments tabs).
          </p>
          <table>
            <thead><tr><th>Product</th><th className="num">Unit</th><th className="num">Qty</th><th className="num">Line</th></tr></thead>
            <tbody>
              {placed.lines.map((l, i) => (
                <tr key={i}><td>{l.productName}</td><td className="num">{money(l.unitPrice, placed.currencyCode)}</td><td className="num">{l.quantity}</td><td className="num">{money(l.lineTotal, placed.currencyCode)}</td></tr>
              ))}
            </tbody>
            <tfoot><tr><td colSpan={3} className="num"><strong>Total</strong></td><td className="num"><strong>{money(placed.total, placed.currencyCode)}</strong></td></tr></tfoot>
          </table>
        </div>
      )}
    </section>
  )
}

import { useEffect, useState } from 'react'
import { api, money, type Product } from '../../api'

export function CatalogPage() {
  const [products, setProducts] = useState<Product[]>([])
  const [error, setError] = useState<string>()
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.products().then(setProducts).catch(e => setError(e.message)).finally(() => setLoading(false))
  }, [])

  if (loading) return <p className="muted">Loading catalogue…</p>
  if (error) return <p className="error">{error}</p>

  const categories = [...new Set(products.map(p => p.category))].sort()

  return (
    <section>
      <h2>Catalogue <span className="owner">Warehouse module</span></h2>
      <p className="muted">Products and live stock. This data lives in the <code>warehouse</code> schema and is served by the Warehouse module.</p>

      {categories.map(category => (
        <div key={category} className="card">
          <h3>{category}</h3>
          <table>
            <thead>
              <tr><th>SKU</th><th>Product</th><th className="num">Price</th><th className="num">In stock</th></tr>
            </thead>
            <tbody>
              {products.filter(p => p.category === category).map(p => (
                <tr key={p.id}>
                  <td className="mono">{p.sku}</td>
                  <td><strong>{p.name}</strong><div className="muted small">{p.description}</div></td>
                  <td className="num">{money(p.price)}</td>
                  <td className="num">
                    {p.stockQuantity <= 30
                      ? <span className="pill pill-low">{p.stockQuantity} low</span>
                      : p.stockQuantity}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ))}
    </section>
  )
}

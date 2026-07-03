import { useState } from 'react'
import { useAuth } from './auth/AuthContext'
import { AuthScreen } from './features/auth/AuthScreen'
import { CatalogPage } from './features/catalog/CatalogPage'
import { PlaceOrderPage } from './features/orders/PlaceOrderPage'
import { OrdersPage } from './features/orders/OrdersPage'
import { ShipmentsPage } from './features/shipments/ShipmentsPage'
import { SupportPage } from './features/support/SupportPage'

type Tab = 'catalog' | 'place' | 'orders' | 'shipments' | 'support'

const TABS: { id: Tab; label: string; module: string }[] = [
  { id: 'catalog', label: 'Catalogue', module: 'Warehouse' },
  { id: 'place', label: 'Place order', module: 'Sales' },
  { id: 'orders', label: 'Orders', module: 'Sales' },
  { id: 'shipments', label: 'Shipments', module: 'Shipping' },
  { id: 'support', label: 'Support', module: 'Support' },
]

export default function App() {
  const { user, loading } = useAuth()
  if (loading) return <div className="app"><p className="muted center-pad">Loading…</p></div>
  if (!user) return <AuthScreen />
  return <Shop />
}

function Shop() {
  const { user, logout } = useAuth()
  const [tab, setTab] = useState<Tab>('catalog')

  return (
    <div className="app">
      <header className="topbar">
        <div className="topbar-row">
          <div>
            <div className="brand"><span className="logo">▦</span> ModularShop</div>
            <div className="subtitle">A Modular Monolith — one app, four modules + a shared kernel, one database with a schema each</div>
          </div>
          <div className="userbar">
            <div className="userbar-id">
              <span className="user-name">{user!.displayName}</span>
              <span className="user-roles">{user!.roles.length ? user!.roles.join(' · ') : 'user'}</span>
            </div>
            <button className="btn small" onClick={() => logout()}>Sign out</button>
          </div>
        </div>
        <div className="modules">
          <span className="chip chip-sales">Sales</span>
          <span className="chip chip-warehouse">Warehouse</span>
          <span className="chip chip-shipping">Shipping</span>
          <span className="chip chip-support">Support</span>
          <span className="chip chip-kernel">Kernel</span>
        </div>
      </header>

      <nav className="tabs">
        {TABS.map(t => (
          <button key={t.id} className={tab === t.id ? 'tab active' : 'tab'} onClick={() => setTab(t.id)}>
            {t.label}<span className="tab-module">{t.module}</span>
          </button>
        ))}
      </nav>

      <main className="content">
        {tab === 'catalog' && <CatalogPage />}
        {tab === 'place' && <PlaceOrderPage />}
        {tab === 'orders' && <OrdersPage />}
        {tab === 'shipments' && <ShipmentsPage />}
        {tab === 'support' && <SupportPage />}
      </main>

      <footer className="foot">
        Single deployable unit · in-process modules · schema-per-module on MSSQL · shared kernel (Identity + Customer + Currency) · sync calls + async integration events
      </footer>
    </div>
  )
}

import { useState } from 'react'
import { CatalogPage } from './features/catalog/CatalogPage'
import { PlaceOrderPage } from './features/orders/PlaceOrderPage'
import { OrdersPage } from './features/orders/OrdersPage'
import { ShipmentsPage } from './features/shipments/ShipmentsPage'

type Tab = 'catalog' | 'place' | 'orders' | 'shipments'

const TABS: { id: Tab; label: string; module: string }[] = [
  { id: 'catalog', label: 'Catalogue', module: 'Warehouse' },
  { id: 'place', label: 'Place order', module: 'Sales' },
  { id: 'orders', label: 'Orders', module: 'Sales' },
  { id: 'shipments', label: 'Shipments', module: 'Shipping' },
]

export default function App() {
  const [tab, setTab] = useState<Tab>('catalog')
  return (
    <div className="app">
      <header className="topbar">
        <div className="brand"><span className="logo">▦</span> ModularShop</div>
        <div className="subtitle">A Modular Monolith — one app, three modules, one database with a schema each</div>
        <div className="modules">
          <span className="chip chip-sales">Sales</span>
          <span className="chip chip-warehouse">Warehouse</span>
          <span className="chip chip-shipping">Shipping</span>
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
      </main>

      <footer className="foot">
        Single deployable unit · in-process modules · schema-per-module on MSSQL · sync calls + async integration events
      </footer>
    </div>
  )
}

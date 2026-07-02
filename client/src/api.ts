// Thin API client. Every endpoint returns the shared ApiResponse<T> envelope, so we unwrap it once
// here. The client always calls relative "/api" URLs — in dev Vite proxies them to the ASP.NET
// host; in production the host serves this SPA, so it is the same origin.

export interface ApiResponse<T> {
  isSuccess: boolean
  message: string | null
  errors: string[]
  data: T | null
}

export interface Product {
  id: string; sku: string; name: string; description: string
  category: string; price: number; stockQuantity: number
}
export interface Customer { id: string; name: string; email: string }
export interface OrderLine {
  productId: string; productName: string; unitPrice: number; quantity: number; lineTotal: number
}
export interface Order {
  id: string; orderNumber: string; customerId: string; customerName: string
  status: string; placedOnUtc: string; placedBy: string; total: number; lines: OrderLine[]
}
export interface ShipmentItem { productName: string; quantity: number }
export interface Shipment {
  id: string; shipmentNumber: string; orderId: string; orderNumber: string; customerName: string
  status: string; createdOnUtc: string; shippedOnUtc: string | null; deliveredOnUtc: string | null
  carrier: string | null; trackingNumber: string | null; totalUnits: number; items: ShipmentItem[]
}

async function unwrap<T>(res: Response): Promise<T> {
  const body = (await res.json()) as ApiResponse<T>
  if (!body.isSuccess) {
    throw new Error(body.errors?.length ? body.errors.join(', ') : (body.message ?? 'Request failed'))
  }
  return body.data as T
}

const json = { 'Content-Type': 'application/json', 'X-User-Id': 'web-user' }

export const api = {
  products: () => fetch('/api/products').then(r => unwrap<Product[]>(r)),
  customers: () => fetch('/api/customers').then(r => unwrap<Customer[]>(r)),
  orders: () => fetch('/api/orders').then(r => unwrap<Order[]>(r)),
  shipments: () => fetch('/api/shipments').then(r => unwrap<Shipment[]>(r)),
  placeOrder: (customerId: string, lines: { productId: string; quantity: number }[]) =>
    fetch('/api/orders', { method: 'POST', headers: json, body: JSON.stringify({ customerId, lines }) })
      .then(r => unwrap<Order>(r)),
  shipShipment: (id: string) => fetch(`/api/shipments/${id}/ship`, { method: 'POST' }).then(r => unwrap<Shipment>(r)),
  deliverShipment: (id: string) => fetch(`/api/shipments/${id}/deliver`, { method: 'POST' }).then(r => unwrap<Shipment>(r)),
}

export const money = (n: number) => `$${n.toFixed(2)}`
export const date = (s: string) => new Date(s).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })

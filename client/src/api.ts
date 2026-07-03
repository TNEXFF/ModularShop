// Thin API client. Every endpoint returns the shared ApiResponse<T> envelope, so we unwrap it once
// here. Authentication is a cookie set by /api/auth/login, so every request sends credentials. The
// client always calls relative "/api" URLs — in dev Vite proxies them to the ASP.NET host; in
// production the host serves this SPA, so it is the same origin.

export interface ApiResponse<T> {
  isSuccess: boolean
  message: string | null
  errors: string[]
  data: T | null
}

export interface AuthUser { id: string; email: string; displayName: string; roles: string[] }

export interface Product {
  id: string; sku: string; name: string; description: string
  category: string; price: number; currencyCode: string; stockQuantity: number
}
export interface Customer { id: string; name: string; email: string }
export interface OrderLine {
  productId: string; productName: string; unitPrice: number; quantity: number; lineTotal: number
}
export interface Order {
  id: string; orderNumber: string; customerId: string; customerName: string; currencyCode: string
  status: string; placedOnUtc: string; placedBy: string; total: number; lines: OrderLine[]
}
export interface ShipmentItem { productName: string; quantity: number }
export interface Shipment {
  id: string; shipmentNumber: string; orderId: string; orderNumber: string; customerId: string; customerName: string
  status: string; createdOnUtc: string; shippedOnUtc: string | null; deliveredOnUtc: string | null
  carrier: string | null; trackingNumber: string | null; totalUnits: number; items: ShipmentItem[]
}
export interface TicketMessage { authorName: string; body: string; sentOnUtc: string }
export interface TicketListItem {
  id: string; subject: string; customerName: string; status: string; createdOnUtc: string; messageCount: number
}
export interface Ticket {
  id: string; subject: string; description: string; customerId: string; customerName: string
  status: string; createdByName: string; createdOnUtc: string; resolvedOnUtc: string | null
  messages: TicketMessage[]
}

/** Error carrying the HTTP status, so callers (e.g. the auth layer) can react to 401 specifically. */
export class ApiError extends Error {
  constructor(message: string, public status: number) { super(message) }
}

async function unwrap<T>(res: Response): Promise<T> {
  let body: ApiResponse<T> | null = null
  try { body = (await res.json()) as ApiResponse<T> } catch { /* non-JSON / empty */ }

  if (!res.ok || !body || !body.isSuccess) {
    const msg = body?.errors?.length ? body.errors.join(', ') : (body?.message ?? `Request failed (${res.status})`)
    throw new ApiError(msg, res.status)
  }
  return body.data as T
}

const jsonHeaders = { 'Content-Type': 'application/json' }
const withCreds = (init?: RequestInit): RequestInit => ({ credentials: 'include', ...init })

const get = <T>(url: string) => fetch(url, withCreds()).then(r => unwrap<T>(r))
const post = <T>(url: string, body?: unknown) =>
  fetch(url, withCreds({
    method: 'POST',
    headers: body === undefined ? undefined : jsonHeaders,
    body: body === undefined ? undefined : JSON.stringify(body),
  })).then(r => unwrap<T>(r))

export const api = {
  // ── Authentication (kernel) ──────────────────────────────────────────────
  me: () => get<AuthUser>('/api/auth/me'),
  login: (email: string, password: string) => post<AuthUser>('/api/auth/login', { email, password }),
  register: (email: string, password: string, displayName: string) =>
    post<AuthUser>('/api/auth/register', { email, password, displayName }),
  logout: () => post<unknown>('/api/auth/logout'),

  // ── Warehouse / Sales / Shipping ─────────────────────────────────────────
  products: () => get<Product[]>('/api/products'),
  customers: () => get<Customer[]>('/api/customers'),
  orders: () => get<Order[]>('/api/orders'),
  shipments: () => get<Shipment[]>('/api/shipments'),
  placeOrder: (customerId: string, lines: { productId: string; quantity: number }[]) =>
    post<Order>('/api/orders', { customerId, lines }),
  shipShipment: (id: string) => post<Shipment>(`/api/shipments/${id}/ship`),
  deliverShipment: (id: string) => post<Shipment>(`/api/shipments/${id}/deliver`),

  // ── Support (the unrelated module) ───────────────────────────────────────
  tickets: () => get<TicketListItem[]>('/api/tickets'),
  ticket: (id: string) => get<Ticket>(`/api/tickets/${id}`),
  createTicket: (customerId: string, subject: string, description: string) =>
    post<Ticket>('/api/tickets', { customerId, subject, description }),
  addTicketMessage: (id: string, body: string) => post<Ticket>(`/api/tickets/${id}/messages`, { body }),
  changeTicketStatus: (id: string, status: string) => post<Ticket>(`/api/tickets/${id}/status`, { status }),
}

const SYMBOLS: Record<string, string> = { USD: '$', EUR: '€', GBP: '£' }
export const money = (n: number, currency: string = 'USD') => `${SYMBOLS[currency] ?? currency + ' '}${n.toFixed(2)}`
export const date = (s: string) => new Date(s).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
export const dateTime = (s: string) => new Date(s).toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })

import type {
  ApiList,
  CitizenApplicationView,
  DashboardView,
  DocumentDetail,
  DocumentSummary,
  FolderView,
  NotificationView,
  SessionResponse,
  UserView,
  WorkflowView,
} from './types';

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8081/api';

async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const headers = new Headers(options.headers || {});
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  const response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  const contentType = response.headers.get('Content-Type') || '';
  const payload = contentType.includes('application/json') ? await response.json() : await response.text();
  if (!response.ok) {
    const message = typeof payload === 'object' && payload && 'error' in payload ? String((payload as { error: string }).error) : response.statusText;
    throw new Error(message);
  }
  return payload as T;
}

export const api = {
  health: () => request<{ status: string; service: string }>('/health'),
  login: (email: string, password: string) => request<SessionResponse>('/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  me: (token: string) => request<{ user: UserView }>('/auth/me', {}, token),
  dashboard: (token: string) => request<DashboardView>('/dashboard', {}, token),
  folders: (token: string) => request<ApiList<FolderView>>('/folders', {}, token),
  createFolder: (token: string, body: Record<string, unknown>) => request<{ folder: FolderView }>('/folders', { method: 'POST', body: JSON.stringify(body) }, token),
  documents: (token: string, query?: Record<string, string | number | boolean | undefined>) => {
    const search = new URLSearchParams();
    Object.entries(query || {}).forEach(([key, value]) => {
      if (value !== undefined && value !== null && `${value}`.length > 0) {
        search.set(key, String(value));
      }
    });
    return request<ApiList<DocumentSummary>>(`/documents${search.toString() ? `?${search.toString()}` : ''}`, {}, token);
  },
  document: (token: string, id: number) => request<{ document: DocumentDetail }>(`/documents/${id}`, {}, token),
  uploadDocument: (token: string, body: Record<string, unknown>) => request<{ document: DocumentDetail }>('/documents', { method: 'POST', body: JSON.stringify(body) }, token),
  addVersion: (token: string, id: number, body: Record<string, unknown>) => request<{ version: unknown }>(`/documents/${id}/versions`, { method: 'POST', body: JSON.stringify(body) }, token),
  addComment: (token: string, id: number, body: Record<string, unknown>) => request(`/documents/${id}/comments`, { method: 'POST', body: JSON.stringify(body) }, token),
  shareDocument: (token: string, id: number, body: Record<string, unknown>) => request(`/documents/${id}/share`, { method: 'POST', body: JSON.stringify(body) }, token),
  trashDocument: (token: string, id: number) => request(`/documents/${id}/trash`, { method: 'POST' }, token),
  restoreDocument: (token: string, id: number) => request(`/documents/${id}/restore`, { method: 'POST' }, token),
  submitDocument: (token: string, id: number, body: Record<string, unknown>) => request<{ workflow: WorkflowView }>(`/documents/${id}/submit`, { method: 'POST', body: JSON.stringify(body) }, token),
  workflows: (token: string) => request<ApiList<WorkflowView>>('/workflows', {}, token),
  reviewWorkflow: (token: string, id: number, approve: boolean, body: Record<string, unknown>) => request<{ workflow: WorkflowView }>(`/workflows/${id}/${approve ? 'approve' : 'reject'}`, { method: 'POST', body: JSON.stringify(body) }, token),
  notifications: (token: string) => request<ApiList<NotificationView>>('/notifications', {}, token),
  markNotificationRead: (token: string, id: number) => request(`/notifications/${id}`, { method: 'POST' }, token),
  users: (token: string) => request<ApiList<UserView>>('/users', {}, token),
  createUser: (token: string, body: Record<string, unknown>) => request<{ user: UserView }>('/users', { method: 'POST', body: JSON.stringify(body) }, token),
  updateUser: (token: string, id: number, body: Record<string, unknown>) => request<{ user: UserView }>(`/users/${id}`, { method: 'PATCH', body: JSON.stringify(body) }, token),
  applications: (token: string) => request<ApiList<CitizenApplicationView>>('/citizen/applications', {}, token),
  submitApplication: (token: string, body: Record<string, unknown>) => request<{ application: CitizenApplicationView }>('/citizen/applications', { method: 'POST', body: JSON.stringify(body) }, token),
  downloadDocument: async (token: string, id: number) => {
    const response = await fetch(`${API_BASE}/documents/${id}/download`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!response.ok) {
      const payload = await response.json().catch(() => null);
      throw new Error(payload?.error || response.statusText);
    }
    return response.json() as Promise<{ fileName: string; mimeType: string; contentBase64: string }>;
  },
};

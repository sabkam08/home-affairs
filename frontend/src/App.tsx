import { useEffect, useMemo, useState } from 'react';
import { BrowserRouter, Link, Navigate, Outlet, Route, Routes, useLocation, useNavigate, useOutletContext } from 'react-router-dom';
import { api } from './api';
import type {
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

type SessionContext = {
  token: string;
  user: UserView;
  refreshSession: () => Promise<void>;
  logout: () => void;
};

function useSession() {
  return useOutletContext<SessionContext>();
}

function App() {
  const [session, setSession] = useState<SessionResponse | null>(null);
  const [booting, setBooting] = useState(true);

  useEffect(() => {
    const savedToken = localStorage.getItem('ha_token');
    if (!savedToken) {
      setBooting(false);
      return;
    }
    api.me(savedToken)
      .then((data) => {
        setSession({ token: savedToken, expiresAt: '', sessionTimeoutMinutes: 3, user: data.user });
      })
      .catch(() => localStorage.removeItem('ha_token'))
      .finally(() => setBooting(false));
  }, []);

  const handleLogin = (payload: SessionResponse) => {
    localStorage.setItem('ha_token', payload.token);
    setSession(payload);
  };

  const handleLogout = () => {
    localStorage.removeItem('ha_token');
    setSession(null);
  };

  const contextValue = useMemo<SessionContext | null>(() => {
    if (!session) {
      return null;
    }
    return {
      token: session.token,
      user: session.user,
      refreshSession: async () => {
        const data = await api.me(session.token);
        setSession((current) => current ? { ...current, user: data.user } : current);
      },
      logout: handleLogout,
    };
  }, [session]);

  if (booting) {
    return <LoadingScreen message="Preparing the secure document workspace" />;
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/login"
          element={session ? <Navigate to="/" replace /> : <LoginScreen onSuccess={handleLogin} />}
        />
        <Route
          element={session && contextValue ? <ShellLayout contextValue={contextValue} /> : <Navigate to="/login" replace />}
        >
          <Route index element={<DashboardPage />} />
          <Route path="documents" element={<DocumentsPage />} />
          <Route path="approvals" element={<ApprovalsPage />} />
          <Route path="citizen" element={<CitizenPortalPage />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
        </Route>
        <Route path="*" element={<Navigate to={session ? '/' : '/login'} replace />} />
      </Routes>
    </BrowserRouter>
  );
}

function ShellLayout({ contextValue }: { contextValue: SessionContext }) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-block">
          <div className="brand-mark">HA</div>
          <div>
            <div className="eyebrow">Home Affairs</div>
            <h1>Document Control</h1>
          </div>
        </div>
        <nav className="nav-list">
          <NavItem to="/" label="Dashboard" />
          <NavItem to="/documents" label="Documents" />
          <NavItem to="/approvals" label="Approvals" />
          <NavItem to="/citizen" label="Citizen Portal" />
          <NavItem to="/users" label="Users" />
          <NavItem to="/notifications" label="Notifications" />
        </nav>
        <div className="sidebar-footer">
          <div>
            <strong>{contextValue.user.fullName}</strong>
            <p>{contextValue.user.email}</p>
          </div>
          <button className="secondary-button" onClick={contextValue.logout}>Log out</button>
        </div>
      </aside>
      <main className="content-area">
        <header className="topbar">
          <div>
            <p className="eyebrow">Secure Scalable DMS</p>
            <PageTitle />
          </div>
          <div className="pill-row">
            <span className="pill">{contextValue.user.role}</span>
            <span className="pill accent">Session active</span>
          </div>
        </header>
        <Outlet context={contextValue} />
      </main>
    </div>
  );
}

function PageTitle() {
  const location = useLocation();
  const titleMap: Record<string, string> = {
    '/': 'Operations dashboard',
    '/documents': 'Document workspace',
    '/approvals': 'Workflow approvals',
    '/citizen': 'Citizen applications',
    '/users': 'User administration',
    '/notifications': 'Notifications',
  };
  return <h2>{titleMap[location.pathname] || 'Home Affairs Document Management'}</h2>;
}

function NavItem({ to, label }: { to: string; label: string }) {
  const location = useLocation();
  const active = location.pathname === to || (to !== '/' && location.pathname.startsWith(to));
  return (
    <Link className={`nav-item ${active ? 'active' : ''}`} to={to}>
      {label}
    </Link>
  );
}

function LoginScreen({ onSuccess }: { onSuccess: (session: SessionResponse) => void }) {
  const navigate = useNavigate();
  const [email, setEmail] = useState('admin@homeaffairs.gov.za');
  const [password, setPassword] = useState('ChangeMe123!');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setMessage('');
    try {
      const session = await api.login(email, password);
      onSuccess(session);
      navigate('/');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Sign in failed');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="login-screen">
      <div className="login-panel">
        <div className="login-hero">
          <span className="eyebrow">Government Document Management</span>
          <h1>One secure workspace for documents, approvals, and citizen services.</h1>
          <p>
            Track versions, control access, review approvals, and support citizen submissions from a single interface.
          </p>
          <div className="login-badges">
            <span>RBAC</span>
            <span>Versioning</span>
            <span>Audit trail</span>
            <span>Citizen portal</span>
          </div>
        </div>
        <form className="card form-card" onSubmit={submit}>
          <h2>Sign in</h2>
          <label>
            <span>Email</span>
            <input value={email} onChange={(event) => setEmail(event.target.value)} placeholder="admin@homeaffairs.gov.za" />
          </label>
          <label>
            <span>Password</span>
            <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="ChangeMe123!" />
          </label>
          <button disabled={busy} className="primary-button" type="submit">
            {busy ? 'Signing in...' : 'Enter workspace'}
          </button>
          {message ? <p className="error-text">{message}</p> : null}
          <p className="helper-text">Demo credentials: admin@homeaffairs.gov.za / ChangeMe123!</p>
        </form>
      </div>
    </div>
  );
}

function DashboardPage() {
  const { token } = useSession();
  const [dashboard, setDashboard] = useState<DashboardView | null>(null);
  const [notifications, setNotifications] = useState<NotificationView[]>([]);

  useEffect(() => {
    api.dashboard(token).then(setDashboard).catch(console.error);
    api.notifications(token).then((data) => setNotifications(data.items)).catch(console.error);
  }, [token]);

  if (!dashboard) {
    return <LoadingScreen message="Loading dashboard" />;
  }

  return (
    <section className="stack gap-large">
      <div className="metric-grid">
        <MetricCard label="Documents" value={dashboard.totalDocuments} hint="Active records in circulation" />
        <MetricCard label="Pending approvals" value={dashboard.pendingWorkflows} hint="Waiting on reviewer action" />
        <MetricCard label="Unread notifications" value={dashboard.unreadNotifications} hint="Activity needing attention" />
        <MetricCard label="Citizen applications" value={dashboard.citizenApplications} hint="Portal submissions received" />
      </div>
      <div className="two-column-grid">
        <Panel title="Recent documents" subtitle="Latest items moving through the system">
          <div className="list-stack">
            {dashboard.recentDocuments.map((document) => (
              <DocumentRow key={document.id} document={document} />
            ))}
          </div>
        </Panel>
        <Panel title="Notifications" subtitle="Latest system activity">
          <div className="list-stack">
            {notifications.slice(0, 5).map((notification) => (
              <div key={notification.id} className={`notification-card ${notification.read ? 'read' : ''}`}>
                <div className="row-between">
                  <strong>{notification.title}</strong>
                  <span className="pill small">{notification.type}</span>
                </div>
                <p>{notification.message}</p>
              </div>
            ))}
          </div>
        </Panel>
      </div>
    </section>
  );
}

function DocumentsPage() {
  const { token } = useSession();
  const [folders, setFolders] = useState<FolderView[]>([]);
  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [query, setQuery] = useState('');
  const [folderId, setFolderId] = useState('');
  const [fileType, setFileType] = useState('');
  const [selectedDocument, setSelectedDocument] = useState<DocumentDetail | null>(null);
  const [statusMessage, setStatusMessage] = useState('');
  const [upload, setUpload] = useState({ title: '', description: '', folderId: '', fileName: '', mimeType: '', file: null as File | null });
  const [commentBody, setCommentBody] = useState('');
  const [shareEmail, setShareEmail] = useState('');
  const [versionFile, setVersionFile] = useState<File | null>(null);

  const loadDocuments = async () => {
    const data = await api.documents(token, { query, folderId: folderId || undefined, fileType: fileType || undefined });
    setDocuments(data.items);
  };

  useEffect(() => {
    api.folders(token).then((data) => setFolders(data.items)).catch(console.error);
    loadDocuments().catch(console.error);
  }, [token]);

  const selectDocument = async (id: number) => {
    const data = await api.document(token, id);
    setSelectedDocument(data.document);
    setCommentBody('');
    setShareEmail('');
  };

  const handleUpload = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!upload.file) {
      setStatusMessage('Choose a file before uploading.');
      return;
    }
    const contentBase64 = await fileToBase64(upload.file);
    await api.uploadDocument(token, {
      title: upload.title,
      description: upload.description,
      folderId: upload.folderId ? Number(upload.folderId) : undefined,
      fileName: upload.fileName || upload.file.name,
      mimeType: upload.mimeType || upload.file.type || 'application/octet-stream',
      contentBase64,
    });
    setUpload({ title: '', description: '', folderId: '', fileName: '', mimeType: '', file: null });
    setStatusMessage('Document uploaded successfully.');
    await loadDocuments();
  };

  const handleAddVersion = async () => {
    if (!selectedDocument || !versionFile) {
      return;
    }
    await api.addVersion(token, selectedDocument.id, {
      fileName: versionFile.name,
      mimeType: versionFile.type || 'application/octet-stream',
      contentBase64: await fileToBase64(versionFile),
    });
    setVersionFile(null);
    setSelectedDocument((await api.document(token, selectedDocument.id)).document);
    setStatusMessage('New version saved.');
    await loadDocuments();
  };

  const handleComment = async () => {
    if (!selectedDocument || !commentBody.trim()) {
      return;
    }
    await api.addComment(token, selectedDocument.id, { body: commentBody });
    setCommentBody('');
    setSelectedDocument((await api.document(token, selectedDocument.id)).document);
  };

  const handleShare = async () => {
    if (!selectedDocument || !shareEmail.trim()) {
      return;
    }
    await api.shareDocument(token, selectedDocument.id, {
      email: shareEmail,
      canView: true,
      canEdit: true,
      canComment: true,
    });
    setShareEmail('');
    setSelectedDocument((await api.document(token, selectedDocument.id)).document);
  };

  return (
    <section className="stack gap-large">
      <Panel title="Search and upload" subtitle="Find records or add a new document">
        <div className="two-column-grid">
          <div className="stack">
            <div className="inline-form">
              <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search by keyword" />
              <select value={folderId} onChange={(event) => setFolderId(event.target.value)}>
                <option value="">All folders</option>
                {folders.map((folder) => <option key={folder.id} value={folder.id}>{folder.name}</option>)}
              </select>
              <input value={fileType} onChange={(event) => setFileType(event.target.value)} placeholder="file type" />
              <button className="primary-button" onClick={() => loadDocuments().catch(console.error)}>Search</button>
            </div>
            <div className="list-stack document-list">
              {documents.map((document) => <DocumentRow key={document.id} document={document} onSelect={() => selectDocument(document.id)} />)}
            </div>
          </div>
          <form className="stack card form-card" onSubmit={handleUpload}>
            <h3>Upload document</h3>
            <label><span>Title</span><input value={upload.title} onChange={(event) => setUpload({ ...upload, title: event.target.value })} /></label>
            <label><span>Description</span><textarea rows={3} value={upload.description} onChange={(event) => setUpload({ ...upload, description: event.target.value })} /></label>
            <label><span>Folder</span><select value={upload.folderId} onChange={(event) => setUpload({ ...upload, folderId: event.target.value })}><option value="">General</option>{folders.map((folder) => <option key={folder.id} value={folder.id}>{folder.name}</option>)}</select></label>
            <label><span>File</span><input type="file" onChange={(event) => setUpload({ ...upload, file: event.target.files?.[0] || null, fileName: event.target.files?.[0]?.name || '', mimeType: event.target.files?.[0]?.type || '' })} /></label>
            <button className="primary-button" type="submit">Upload</button>
            {statusMessage ? <p className="helper-text">{statusMessage}</p> : null}
          </form>
        </div>
      </Panel>

      {selectedDocument ? (
        <Panel title={selectedDocument.title} subtitle={`Version ${selectedDocument.currentVersionNumber} • ${selectedDocument.status}`}>
          <div className="document-detail-grid">
            <div className="stack">
              <div className="detail-grid">
                <DetailItem label="Folder" value={selectedDocument.folderName || 'General'} />
                <DetailItem label="Owner" value={selectedDocument.ownerName || 'Unknown'} />
                <DetailItem label="Checksum" value={selectedDocument.checksum} />
                <DetailItem label="Status" value={selectedDocument.status} />
              </div>
              <div className="action-row">
                <button className="secondary-button" onClick={() => api.trashDocument(token, selectedDocument.id).then(() => loadDocuments().then(() => setSelectedDocument(null)))}>Move to bin</button>
                <button className="secondary-button" onClick={() => api.restoreDocument(token, selectedDocument.id).then(() => selectDocument(selectedDocument.id))}>Restore</button>
                <button className="secondary-button" onClick={() => api.submitDocument(token, selectedDocument.id, { notes: 'Please review this version.' }).then(() => selectDocument(selectedDocument.id))}>Submit for approval</button>
                <button className="secondary-button" onClick={async () => {
                  const file = await api.downloadDocument(token, selectedDocument.id);
                  const bytes = Uint8Array.from(atob(file.contentBase64), (char) => char.charCodeAt(0));
                  const blob = new Blob([bytes], { type: file.mimeType });
                  const url = URL.createObjectURL(blob);
                  const anchor = document.createElement('a');
                  anchor.href = url;
                  anchor.download = file.fileName;
                  anchor.click();
                  URL.revokeObjectURL(url);
                }}>Download</button>
              </div>
              <div className="stack">
                <h4>Add version</h4>
                <div className="inline-form">
                  <input type="file" onChange={(event) => setVersionFile(event.target.files?.[0] || null)} />
                  <button className="primary-button" onClick={handleAddVersion}>Save version</button>
                </div>
              </div>
            </div>
            <div className="stack">
              <div className="stack">
                <h4>Share access</h4>
                <div className="inline-form">
                  <input value={shareEmail} onChange={(event) => setShareEmail(event.target.value)} placeholder="collaborator@email.gov.za" />
                  <button className="primary-button" onClick={handleShare}>Grant rights</button>
                </div>
              </div>
              <div className="stack">
                <h4>Comments</h4>
                <div className="inline-form">
                  <input value={commentBody} onChange={(event) => setCommentBody(event.target.value)} placeholder="Leave a note" />
                  <button className="primary-button" onClick={handleComment}>Post</button>
                </div>
                <div className="list-stack">
                  {selectedDocument.comments.map((comment) => <div key={comment.id} className="notification-card"><strong>{comment.authorName}</strong><p>{comment.body}</p></div>)}
                </div>
              </div>
              <div className="stack">
                <h4>Versions</h4>
                <div className="list-stack">
                  {selectedDocument.versions.map((version) => <div key={version.id} className="notification-card"><strong>v{version.versionNumber}</strong><p>{version.fileName}</p></div>)}
                </div>
              </div>
              {selectedDocument.workflow ? (
                <div className="stack">
                  <h4>Workflow</h4>
                  <p>{selectedDocument.workflow.status} by {selectedDocument.workflow.reviewer || 'pending review'}</p>
                </div>
              ) : null}
            </div>
          </div>
        </Panel>
      ) : null}
    </section>
  );
}

function ApprovalsPage() {
  const { token } = useSession();
  const [workflows, setWorkflows] = useState<WorkflowView[]>([]);
  const [notes, setNotes] = useState<Record<number, string>>({});

  const load = () => api.workflows(token).then((data) => setWorkflows(data.items)).catch(console.error);

  useEffect(() => {
    load().catch(console.error);
  }, [token]);

  return (
    <section className="stack gap-large">
      <Panel title="Pending workflows" subtitle="Approve or reject document submissions">
        <div className="list-stack">
          {workflows.map((workflow) => (
            <div key={workflow.id} className="card workflow-card">
              <div className="row-between">
                <div>
                  <strong>{workflow.documentTitle}</strong>
                  <p>{workflow.requestedBy} • {workflow.status}</p>
                </div>
                <span className="pill small">{workflow.currentStep}</span>
              </div>
              <textarea rows={2} placeholder="Decision notes" value={notes[workflow.id] || workflow.notes || ''} onChange={(event) => setNotes({ ...notes, [workflow.id]: event.target.value })} />
              <div className="action-row">
                <button className="primary-button" onClick={() => api.reviewWorkflow(token, workflow.id, true, { notes: notes[workflow.id] || '' }).then(load)}>Approve</button>
                <button className="secondary-button" onClick={() => api.reviewWorkflow(token, workflow.id, false, { notes: notes[workflow.id] || '' }).then(load)}>Reject</button>
              </div>
            </div>
          ))}
        </div>
      </Panel>
    </section>
  );
}

function CitizenPortalPage() {
  const { token } = useSession();
  const [applications, setApplications] = useState<CitizenApplicationView[]>([]);
  const [form, setForm] = useState({ applicationType: 'ID Renewal', fullName: '', email: '', idNumber: '', phoneNumber: '', notes: '' });
  const [file, setFile] = useState<File | null>(null);

  const load = () => api.applications(token).then((data) => setApplications(data.items)).catch(console.error);

  useEffect(() => {
    load().catch(console.error);
  }, [token]);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    const attachment = file ? { fileName: file.name, mimeType: file.type || 'application/octet-stream', contentBase64: await fileToBase64(file) } : undefined;
    await api.submitApplication(token, { ...form, attachment });
    setForm({ applicationType: 'ID Renewal', fullName: '', email: '', idNumber: '', phoneNumber: '', notes: '' });
    setFile(null);
    await load();
  };

  return (
    <section className="stack gap-large">
      <Panel title="New application" subtitle="Citizens can submit applications from the same platform">
        <form className="stack form-grid" onSubmit={submit}>
          <input value={form.applicationType} onChange={(event) => setForm({ ...form, applicationType: event.target.value })} placeholder="Application type" />
          <input value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} placeholder="Full name" />
          <input value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} placeholder="Email" />
          <input value={form.idNumber} onChange={(event) => setForm({ ...form, idNumber: event.target.value })} placeholder="ID number" />
          <input value={form.phoneNumber} onChange={(event) => setForm({ ...form, phoneNumber: event.target.value })} placeholder="Phone number" />
          <textarea rows={4} value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} placeholder="Notes" />
          <input type="file" onChange={(event) => setFile(event.target.files?.[0] || null)} />
          <button className="primary-button" type="submit">Submit application</button>
        </form>
      </Panel>
      <Panel title="Submitted applications" subtitle="Portal activity and status tracking">
        <div className="list-stack">
          {applications.map((application) => (
            <div key={application.id} className="notification-card">
              <div className="row-between">
                <strong>{application.applicationNumber}</strong>
                <span className="pill small">{application.status}</span>
              </div>
              <p>{application.fullName} • {application.applicationType}</p>
              <p>{application.attachedDocumentTitle || 'No attachment'}</p>
            </div>
          ))}
        </div>
      </Panel>
    </section>
  );
}

function UsersPage() {
  const { token, user } = useSession();
  const [users, setUsers] = useState<UserView[]>([]);
  const [form, setForm] = useState({ fullName: '', email: '', role: 'EMPLOYEE', password: '' });

  const load = () => api.users(token).then((data) => setUsers(data.items)).catch(console.error);

  useEffect(() => {
    if (user.role === 'IT_SUPERUSER') {
      load().catch(console.error);
    }
  }, [token, user.role]);

  if (user.role !== 'IT_SUPERUSER') {
    return <Panel title="Access limited" subtitle="Only the IT superuser can manage user accounts."><p>Sign in with the admin account to view this section.</p></Panel>;
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    await api.createUser(token, form);
    setForm({ fullName: '', email: '', role: 'EMPLOYEE', password: '' });
    await load();
  };

  return (
    <section className="stack gap-large">
      <Panel title="Create user" subtitle="Add, lock, unlock, or deactivate accounts">
        <form className="inline-form" onSubmit={submit}>
          <input value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} placeholder="Full name" />
          <input value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} placeholder="Email" />
          <select value={form.role} onChange={(event) => setForm({ ...form, role: event.target.value })}>
            <option value="EMPLOYEE">EMPLOYEE</option>
            <option value="REVIEWER">REVIEWER</option>
            <option value="CITIZEN">CITIZEN</option>
            <option value="IT_SUPERUSER">IT_SUPERUSER</option>
          </select>
          <input value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} placeholder="Password" />
          <button className="primary-button" type="submit">Create user</button>
        </form>
      </Panel>
      <Panel title="Accounts" subtitle="Current account state and controls">
        <div className="list-stack">
          {users.map((account) => (
            <div key={account.id} className="card workflow-card">
              <div className="row-between">
                <div>
                  <strong>{account.fullName}</strong>
                  <p>{account.email} • {account.role}</p>
                </div>
                <span className={`pill small ${account.active ? 'accent' : 'danger'}`}>{account.active ? 'Active' : 'Inactive'}</span>
              </div>
              <div className="action-row">
                <button className="secondary-button" onClick={() => api.updateUser(token, account.id, { unlock: true }).then(load)}>Unlock</button>
                <button className="secondary-button" onClick={() => api.updateUser(token, account.id, { lock: true }).then(load)}>Lock</button>
                <button className="secondary-button" onClick={() => api.updateUser(token, account.id, { active: !account.active }).then(load)}>{account.active ? 'Deactivate' : 'Activate'}</button>
              </div>
            </div>
          ))}
        </div>
      </Panel>
    </section>
  );
}

function NotificationsPage() {
  const { token } = useSession();
  const [notifications, setNotifications] = useState<NotificationView[]>([]);

  const load = () => api.notifications(token).then((data) => setNotifications(data.items)).catch(console.error);

  useEffect(() => {
    load().catch(console.error);
  }, [token]);

  return (
    <section className="stack gap-large">
      <Panel title="Notifications" subtitle="Read system updates and collaboration alerts">
        <div className="list-stack">
          {notifications.map((notification) => (
            <div key={notification.id} className={`notification-card ${notification.read ? 'read' : ''}`}>
              <div className="row-between">
                <div>
                  <strong>{notification.title}</strong>
                  <p>{notification.message}</p>
                </div>
                <button className="secondary-button" onClick={() => api.markNotificationRead(token, notification.id).then(load)}>{notification.read ? 'Read' : 'Mark read'}</button>
              </div>
            </div>
          ))}
        </div>
      </Panel>
    </section>
  );
}

function MetricCard({ label, value, hint }: { label: string; value: number; hint: string }) {
  return (
    <div className="metric-card">
      <span className="eyebrow">{label}</span>
      <strong>{value}</strong>
      <p>{hint}</p>
    </div>
  );
}

function Panel({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <section className="card panel">
      <div className="panel-heading">
        <div>
          <span className="eyebrow">{subtitle}</span>
          <h3>{title}</h3>
        </div>
      </div>
      {children}
    </section>
  );
}

function DocumentRow({ document, onSelect }: { document: DocumentSummary; onSelect?: () => void }) {
  return (
    <button className="document-row" onClick={onSelect}>
      <div>
        <strong>{document.title}</strong>
        <p>{document.folderName || 'General'} • {document.ownerName || 'Unknown'}</p>
      </div>
      <div className="row-meta">
        <span className="pill small">v{document.currentVersionNumber}</span>
        <span className="pill small">{document.status}</span>
      </div>
    </button>
  );
}

function DetailItem({ label, value }: { label: string; value: string | number | null }) {
  return (
    <div className="detail-item">
      <span>{label}</span>
      <strong>{value === null || value === '' ? '—' : value}</strong>
    </div>
  );
}

function LoadingScreen({ message }: { message: string }) {
  return (
    <div className="loading-screen">
      <div className="loading-card">
        <div className="brand-mark">HA</div>
        <p>{message}</p>
      </div>
    </div>
  );
}

function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = String(reader.result || '');
      resolve(result.includes(',') ? result.split(',')[1] : result);
    };
    reader.onerror = () => reject(reader.error);
    reader.readAsDataURL(file);
  });
}

export default App;
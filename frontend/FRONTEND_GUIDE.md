# Home Affairs frontend guide

## Start the application

From the `frontend` folder:

```powershell
npm install
npm start
```

Open [http://localhost:4200](http://localhost:4200). The development build calls the API at
`http://localhost:5086/api/v1`; start the ASP.NET backend on that URL first.

Useful commands:

```powershell
npm run build
npm test
```

## Navigation

1. **Sign in** — `/login`
   - Sign in with an account returned by the backend.
   - The frontend loads `/auth/me` and `/auth/permissions` after login.
2. **Applications** — `/applications`
   - View applications owned by the signed-in citizen.
   - Select **New application** to create a draft.
3. **Application details** — `/applications/{applicationId}`
   - Change the service while the application is a draft.
   - Submit or cancel a draft.
   - Upload, replace, download, and remove supporting documents.
   - Review document readiness and application status history.
4. **My profile** — `/profile`
   - View the signed-in citizen profile from `/citizens/me`.
5. **Permission denied** — `/forbidden`
   - Shown when the API returns `403`.
6. **Not found** — `/not-found`
   - Shown for unknown routes and unavailable resources.

## Backend integration notes

- The API base URL is configured in `src/environments/environment.development.ts`.
- Bearer tokens are attached by the auth interceptor; do not add `Authorization` headers in feature services.
- Document uploads use `FormData`; do not set the multipart `Content-Type` header manually.
- Application and document mutations send the latest API `rowVersion`. Successful responses replace the local model.
- A `409` response means the resource changed or the requested state transition is invalid. Reload the latest application before retrying.
- The backend is authoritative for permissions and lifecycle transitions. UI visibility is only a usability layer.

## Current screen coverage

Implemented citizen screens cover authentication, applications, draft editing, submission/cancellation, document readiness and versioning, status history, and profile viewing. Staff work queue, supervisor approval, assignment, and global search screens should be added when those operational workflows are enabled for the frontend.

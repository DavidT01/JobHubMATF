# Applications UI preview

From this directory, with Node/npm installed:

```powershell
npm ci
npm run preview
```

Open http://127.0.0.1:4201. Stop with Ctrl+C.

- **Company view**: 24 sample applications, pagination, all five read-only statuses and all four CV states. Refresh re-reads the list. The second job starts empty.
- **Apply to a job**: optional cover letter and a simulated submission. It appears under **My applications** and the company's second job. Reload the browser to reset the demo.
- **Narrow layout**: constrain the component to 390px to inspect wrapping. Also test a narrow browser window for actual viewport behavior.
- **Open current CV**: opens an explicitly marked sample text file, not an actual candidate CV.

This is an isolated visual/interaction preview with in-memory sample data. It does not make Application API requests, store tokens, create accounts, or persist data. It is **not** an authentication bypass or a backend end-to-end test. Never deploy the preview configuration as the real application.

`npm start` and `npm run build` still use the normal app entry point and existing profile routes. The preview entry point, sample provider and sample CV asset are selected only by `--configuration preview`; they are not imported by the normal app.

## Production integration (deferred by agreement)

The application form accepts a Catalog `jobId` and emits `applied` on success. The company list accepts the selected Catalog `jobId`; the candidate list gets identity from the authenticated API call. Import these standalone components into the team's future job/dashboard screens.

Configure `APPLICATIONS_API_URL` (default `/api/applications`) through the gateway/proxy and supply the team's authenticated HttpClient interceptor. Application Service requires a valid Candidate/Employer JWT. Do not place JWT secrets in Angular, manually pass candidate/company IDs, or use the preview provider for real calls. Auth/dashboard integration is intentionally not implemented here.

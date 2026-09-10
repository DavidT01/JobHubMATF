# WebSPA

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 21.1.2.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

### Application API through the Gateway

Signed-in candidates can open `/applications`; employers can open
`/applications-received` from the home page and select one of their company's
jobs (including inactive jobs). The employer page resolves the company profile
from the signed-in Identity user before querying Catalog by the profile ID.
`/api/company-profiles/**` is also proxied through the Gateway to Profile Service
at `http://localhost:5213`. Production hosting must forward this path as well.

Admins can open `/admin/statistics` from the home page or user-management page.
This dashboard shows application totals, status distribution and daily counts,
with an optional date range. It uses the Application database; it does not count
users, companies or Catalog matches. Access is checked against the signed-in
Identity role and again by the Application API.

The default development server proxies `/api/applications` and its subpaths to
the Gateway at `http://localhost:5107`. The Gateway forwards these requests to
Application Service at `http://localhost:5020`. The existing authentication
interceptor supplies the signed-in user's bearer token.

Start Application Service and its dependencies using its README, then run the
Gateway from `src/Gateways/Gateway` with `dotnet run --launch-profile http` and
start this frontend with `npm start`. Configure the same `JwtSettings__Secret`
for Identity, Gateway, and Application Service. Application Service additionally
validates the Identity issuer, audience, and role.

The proxy is development-only and does not affect `npm run preview`, which uses
in-memory application data. Production hosting must forward `/api/applications`
and its subpaths to the Gateway on the frontend's origin. For containers, replace
the Gateway's Application downstream `localhost:5020` with the Application
container's DNS name and internal port.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.

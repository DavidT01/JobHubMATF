# SETUP

This document describes what you need to run the project locally and start developing.

## 1) Prerequisites

### .NET SDK 10

1. Download and install the **.NET 10 SDK**:  
   https://dotnet.microsoft.com/en-us/download
2. Verify the installation:

```powershell
dotnet --version
```

Expected: the version starts with `10.`

---

### Node.js (LTS 24.x.x)

1. Download and install **Node.js 24.x.x (LTS)**:  
   https://nodejs.org/en
2. Verify the installation:

```powershell
node -v
npm -v
```

---

## 2) Frontend setup (Angular SPA)

The Angular application is in the `src\Web\WebSPA` folder.

1. Open a terminal in the repository root.
2. Go to the SPA folder:

```powershell
cd .\src\Web\WebSPA
```

3. Install the dependencies:

```powershell
npm install
```

> This installs the packages from `package.json` (including the Angular packages and themes).

---

## 3) Angular CLI (optional)

You can use the local CLI through `npx` (recommended) or install it globally.

### Option A — without a global install (recommended)

```powershell
npx ng version
```

### Option B — global install

```powershell
npm install -g @angular/cli
ng version
```

> Even with the global CLI, you still need to run `npm install` in `src\Web\WebSPA`.

---

## 4) Quick check that the environment is ready

Run the following commands and confirm they complete without errors:

```powershell
dotnet --version
node -v
npm -v
cd .\src\Web\WebSPA
npm install
npx ng version
```

If everything passes, the environment is ready.

---

## 5) Working on the project

### Microservices (`src\Services`)

- In the `src\Services` folder, each team member generates and maintains their own microservice.
- New microservices are added as separate projects/folders inside `src\Services`.


### Angular development (`src\Web\WebSPA`)

- The frontend is already initialized; create components for new parts of the application.
- Create components with:

```powershell
ng generate component component-name
```

- This command generates:
   - `.html` (layout),
   - `.scss` (styles),
   - `.ts` (behavior),
   - `.spec.ts` (tests).

### Git ignore rules

- .NET microservices use `src\Services\.gitignore` (Visual Studio/.NET rules).
- The Angular frontend uses `src\Web\WebSPA\.gitignore` (Angular/Node rules).

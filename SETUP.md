# SETUP

Ovaj dokument opisuje šta je potrebno da bi se lokalno pokrenuo projekat i krenulo sa razvojem.

## 1) Preduslovi

### .NET SDK 10

1. Preuzmite i instalirajte **.NET 10 SDK**:  
   https://dotnet.microsoft.com/en-us/download
2. Proverite instalaciju:

```powershell
dotnet --version
```

Očekivano: verzija počinje sa `10.`

---

### Node.js (LTS 24.x.x)

1. Preuzmite i instalirajte **Node.js 24.x.x (LTS)**:  
   https://nodejs.org/en
2. Proverite instalaciju:

```powershell
node -v
npm -v
```

---

## 2) Frontend setup (Angular SPA)

Angular aplikacija je u folderu `src\Web\WebSPA`.

1. Otvorite terminal u root-u repozitorijuma.
2. Pozicionirajte se u SPA folder:

```powershell
cd .\src\Web\WebSPA
```

3. Instalirajte zavisnosti:

```powershell
npm install
```

> Ovo instalira pakete iz `package.json` (uključujući Angular pakete/teme).

---

## 3) Angular CLI (opciono)

Možete koristiti lokalni CLI preko `npx` (preporučeno), ili ga instalirati globalno.

### Opcija A — bez globalne instalacije (preporučeno)

```powershell
npx ng version
```

### Opcija B — globalna instalacija

```powershell
npm install -g @angular/cli
ng version
```

> Čak i sa globalnim CLI-jem, i dalje morate uraditi `npm install` u `src\Web\WebSPA`.

---

## 4) Brza provera da je okruženje spremno

Pokrenite sledeće komande i potvrdite da rade bez greške:

```powershell
dotnet --version
node -v
npm -v
cd .\src\Web\WebSPA
npm install
npx ng version
```

Ako sve prolazi, okruženje je spremno za rad.

---

## 5) Uputstvo za rad na projektu

### Mikroservisi (`src\Services`)

- U folderu `src\Services` svaki član tima samostalno generiše i održava svoj mikroservis.
- Novi mikroservisi se dodaju kao zasebni projekti/folderi unutar `src\Services`.


### Angular razvoj (`src\Web\WebSPA`)

- Frontend je inicijalizovan; za nove delove aplikacije kreirajte komponente.
- Komponente kreirajte komandom:

```powershell
ng generate component ime-komponente
```

- Ova komanda generiše:
   - `.html` (izgled),
   - `.scss` (stil),
   - `.ts` (funkcionalnost),
   - `.spec.ts` (testiranje).

### Git ignore pravila

- Za .NET mikroservise koristi se `src\Services\.gitignore` (Visual Studio/.NET pravila).
- Za Angular frontend koristi se `src\Web\WebSPA\.gitignore` (Angular/Node pravila).
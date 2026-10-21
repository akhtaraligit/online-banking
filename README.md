# Feather Bank — Online Banking Prototype

Demo retail online banking app for the Talent Suite coding assessment.

Customers can sign in, view checking/savings balances, browse transaction history (filter + pagination), and transfer money to their own accounts or saved payees — with confirmation and idempotency so accidental double-submit does not move money twice.

## Stack

| Layer | Choice |
|---|---|
| Frontend | React + TypeScript (Vite) |
| Backend | ASP.NET Core 10 Web API |
| Database | SQLite (EF Core) |
| Auth | JWT bearer tokens |

Optional AI banking assistant was **not** implemented (bonus left out on purpose).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm

## How to run the app

Open two terminals from the repo root.

### 1) API

```bash
cd backend/OnlineBanking.Api
dotnet restore
dotnet run --launch-profile http
```

- API: http://localhost:5080  
- Swagger: http://localhost:5080/swagger  

SQLite file `onlinebanking.db` is created automatically and seeded on first run.

### 2) Frontend

```bash
cd frontend
npm install
npm run dev
```

- App: http://localhost:5173  

Vite proxies `/api` to the backend, so no extra CORS setup is needed for local demo.

### Demo users

| Username | Password |
|---|---|
| `alice` | `Password123!` |
| `bob` | `Password123!` |

Each customer has checking + savings accounts, sample transactions, and saved payees.

## How to run the tests

Focused API tests cover auth, account isolation, transfer validation, and transfer idempotency:

```bash
cd backend/OnlineBanking.Api.Tests
dotnet test
```

Frontend typecheck (optional smoke check):

```bash
cd frontend
npm run build
```

## Typical demo path

1. Sign in as `alice`
2. Open **Everyday Checking** and scan / filter history
3. Go to **Transfer** → move money to savings → confirm
4. Retry / double-click is safe (same idempotency key returns the original result)
5. Send a small payment to a saved payee
6. Confirm balances and new debit/credit rows in history
7. Sign out — protected pages require login again

## Key design decisions

- **SQLite for speed of demo** — zero external DB setup; good for a prototype. Not meant as a production banking datastore.
- **JWT sessions** — simple protected API for a SPA. Logout is client-side token discard (plus a logout endpoint for a clean UX contract). No refresh-token rotation.
- **Customer-scoped queries** — every accounts/transactions/transfers/payees call resolves the customer from the token and filters ownership in the service layer.
- **Transfer idempotency** — unique `(CustomerId, IdempotencyKey)` on a `Transfers` table. Retries return the original transfer with `alreadyProcessed: true` and do not move money again. Matching debit/credit rows share a `TransferGroupId`.
- **Confirm step in the UI** — form → review → submit. The idempotency key is created when the transfer flow starts and reused for that attempt.
- **Transaction filters** — date range, debit/credit, text search, pagination; running balance included as a small bonus on returned pages.
- **Enums as strings in JSON** — clearer API contract for the React client.

## Trade-offs / what we left out

- Optional **AI banking assistant**
- EF Core migrations / multi-environment deployment story
- Refresh tokens, MFA, lockout, audit logging
- True distributed locking (SQLite + unique constraint is enough for this prototype)
- External payee ledger (payee transfers debit the customer only)
- Full frontend test suite / E2E (API tests cover the highest-risk money path)
- Production secrets management (JWT key is in `appsettings.json` for local demo only)

## Project layout

```text
online-banking/
  backend/
    OnlineBanking.Api/           # Web API
    OnlineBanking.Api.Tests/     # xUnit integration tests
  frontend/                      # React SPA
  OnlineBanking.slnx
  README.md
```

## How AI tools were used

This prototype was built with **Cursor** (AI-assisted coding) under human direction: requirements breakdown, stack choices (including SQLite and skipping optional AI), and step-by-step delivery (auth → accounts/history → transfers → submission).

AI accelerated scaffolding, boilerplate (JWT/EF wiring, React auth context), and first-pass UI. Human review kept the money path strict: ownership checks, transfer validation, confirm UX, and idempotency.

### Example where AI output was corrected / rejected

Early API setup, AI suggested a Swagger security scheme using `Microsoft.OpenApi.Models` types (`OpenApiInfo`, `OpenApiSecurityScheme`, etc.). On .NET 10 / Swashbuckle 10 that namespace/API no longer matched the installed OpenAPI package and the build failed (`CS0234`).

**Correction:** drop the custom OpenAPI security diagram for the prototype and use plain `AddSwaggerGen()` + Swagger UI. Auth was verified with real HTTP calls and automated tests instead of fighting package churn in the Swagger config.

Another small reject: an AI slip used `ClaimRegisteredClaimNames` instead of `JwtRegisteredClaimNames` — caught at compile time and fixed before shipping.

## License / data

Fictional demo data only. Not a real bank product.

# CFIP-PRO

CFIP-PRO is a clean greenfield, Python-first financial market intelligence platform. **CForex is the sole external capability reference.** CFIP-PRO does not inherit CForex implementation structure or technical debt.

## Foundation

- Python 3.14 + FastAPI + Pydantic 2 + SQLAlchemy 2 + Alembic
- PostgreSQL as transactional system of record
- NATS JetStream for durable event workflows
- Redis for cache/ephemeral coordination
- Next.js 16 + React 19.3 + TypeScript 6.0.3 + Tailwind CSS 4.3
- TradingView Lightweight Charts 5.2.1
- `uv` for Python dependency/environment management
- npm for the frontend foundation

The repository is a modular monolith with explicit boundaries. Domain code does not depend on transport, persistence, or infrastructure clients.

## Native development

Normal development is native Windows and does not require Docker. **Port 8000 is the single CFIP-PRO application entrypoint.** The native launcher builds the static Next.js application when needed and serves the web terminal and FastAPI API from the same origin.

### Start the complete application

From the repository root, with the project `.venv` available:

```powershell
.\.venv\Scripts\python.exe scripts\run_cfip.py
```

Then open `http://127.0.0.1:8000`. No second frontend terminal is required.

The launcher rebuilds the frontend export only when it is missing or older than tracked frontend source/config files. It does not rebuild unconditionally, reinstall dependencies, reset PostgreSQL, or require Docker/WSL.

### Backend verification

```powershell
.\.venv\Scripts\python.exe -m pytest
.\.venv\Scripts\python.exe -m ruff check .
.\.venv\Scripts\python.exe -m mypy apps/api/src
```

### Frontend development/build

```powershell
cd apps/web
npm run build
npm run lint
npm run typecheck
```

The API defaults to `http://127.0.0.1:8000/api`; the production-style native entrypoint serves both API and web UI on port `8000`.

## Architecture

```text
apps/
├── api/
│   ├── src/cfip/
│   │   ├── api/            # HTTP transport and routers
│   │   ├── application/    # use cases and orchestration
│   │   ├── domain/         # framework-independent business contracts
│   │   ├── infrastructure/ # DB, messaging, cache, providers
│   │   └── worker/         # asynchronous process entry points
│   └── tests/               # API-local tests when needed
└── web/
    └── src/
        ├── app/            # Next.js App Router
        ├── components/     # reusable terminal UI
        ├── features/       # capability-owned UI modules
        └── lib/            # API/config/client utilities

scripts/
└── run_cfip.py              # single native application launcher
```

## Project state

The canonical implementation and progress record is `docs/CFIP-PROGRESS.md`. Every structural change, dependency decision, verification result, blocker, and next step must be recorded there.

## Scope

The foundation intentionally does not pretend that market intelligence, trading, payment, OAuth, or autonomous intelligence are already implemented. Those capabilities are introduced as vertical slices after their contracts, OSS evaluations, tests, and operational boundaries are established.

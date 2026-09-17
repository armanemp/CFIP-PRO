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

Normal development is native Windows and does not require Docker. Docker configuration is kept for later integration/production validation and must not become a hidden development dependency.

### Backend

```powershell
uv sync
uv run pytest
uv run ruff check .
uv run mypy apps/api/src
uv run uvicorn cfip.main:app --app-dir apps/api/src --reload
```

### Frontend

```powershell
cd apps/web
npm install
npm run dev
```

The API defaults to `http://127.0.0.1:8000`; the web app defaults to `http://localhost:3000`.

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
```

## Project state

The canonical implementation and progress record is `docs/CFIP-PROGRESS.md`. Every structural change, dependency decision, verification result, blocker, and next step must be recorded there.

## Scope

The foundation intentionally does not pretend that market intelligence, trading, payment, OAuth, or autonomous intelligence are already implemented. Those capabilities are introduced as vertical slices after their contracts, OSS evaluations, tests, and operational boundaries are established.

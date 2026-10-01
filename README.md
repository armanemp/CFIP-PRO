> MIOSAI is the canonical name of the entire platform. The historical GitHub slug armanemp/CFIP-PRO remains until the repository rename is applied.

# MIOSAI

MIOSAI is a Python-first financial market intelligence platform designed to evolve into a complete evidence-grounded market analyst with governed learning, self-healing and self-development. MIOSAI owns contracts, safety policy and UX; mature OSS engines provide implementation behind adapters.

## Foundation

- Python 3.14, FastAPI, Pydantic 2, SQLAlchemy 2, Alembic
- PostgreSQL, NATS JetStream and Redis
- Next.js, React, TypeScript, Tailwind and TradingView Lightweight Charts
- OSS-first adapters for TA-Lib, CCXT, OpenBB, NautilusTrader and the wider research/ML/observability fabric
- Native Windows development; Docker/WSL are not required for development

## Native entrypoint

From the repository root:

    .\.venv\Scripts\python.exe scripts\run_miosai.py

Open http://127.0.0.1:8000. The launcher builds the web export only when stale and never resets the database or unconditionally rebuilds dependencies.

scripts/run_cfip.py remains as a compatibility wrapper during the naming migration.

## Verification

    .\.venv\Scripts\python.exe -m pytest
    .\.venv\Scripts\python.exe -m ruff check .
    .\.venv\Scripts\python.exe -m mypy apps/api/src
    cd apps/web
    npm run lint
    npm run typecheck
    npm run build

## Architecture rule

OSS engine -> MIOSAI adapter -> MIOSAI domain contract -> application orchestration -> API/UI

OSS libraries are optional where practical, lazy-loaded, license-reviewed, resource-bounded and observable. The domain never imports vendor APIs directly.

## Intelligence lifecycle

evidence -> research -> analysis -> proposal -> risk gate -> validate -> apply -> observe outcome -> learn -> rollback/revise

Low/medium-risk changes may be autonomously prepared and committed on isolated branches when tests, rollback evidence and safety invariants pass. High/critical changes, merge and deployment remain approval-gated.

## Git control plane

MIOSAI is building a governed Git control plane covering local state, branches, commits, pull requests, reviews, checks, merge gates, rollback and provenance. Direct commits to main remain forbidden.

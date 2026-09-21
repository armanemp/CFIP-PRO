"""Runtime discovery for optional OSS engines.

Discovery reports installation only. Installed never implies configured,
reachable, or operational.
"""
from __future__ import annotations
import importlib.util

_CAPABILITIES = (
    ("openbb", "openbb", "market-data/research"),
    ("ta-lib", "talib", "technical-analysis"),
    ("ccxt", "ccxt", "crypto-market-data/execution"),
    ("nautilus-trader", "nautilus_trader", "backtest/execution"),
    ("polars", "polars", "columnar-analytics"),
    ("pydantic-ai", "pydantic_ai", "agent-orchestration"),
    ("mlflow", "mlflow", "experiment-lifecycle"),
    ("evidently", "evidently", "drift-monitoring"),
    ("opentelemetry", "opentelemetry", "observability"),
    ("trafilatura", "trafilatura", "web-acquisition"),
    ("qdrant-client", "qdrant_client", "vector-retrieval"),
    ("opensearch-py", "opensearchpy", "search"),
    ("feast", "feast", "feature-store"),
)

def capability_snapshot() -> list[dict[str, object]]:
    return [
        {
            "id": ident,
            "package": package,
            "capability": capability,
            "installed": importlib.util.find_spec(package) is not None,
            "configuration_state": "unknown",
            "connection_state": "unknown",
            "operational": False,
        }
        for ident, package, capability in _CAPABILITIES
    ]

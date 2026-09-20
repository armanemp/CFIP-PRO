"""Optional NautilusTrader trading-engine adapter.

The adapter intentionally accepts Nautilus objects only at this infrastructure
boundary. CFIP domain/application code remains vendor-neutral. The request
supports the low-level BacktestEngine path so callers can provide instruments,
venues, data, and strategies assembled with the currently installed
NautilusTrader version.
"""
from __future__ import annotations

import asyncio
from collections.abc import Mapping
from typing import Any


class NautilusTraderEngineAdapter:
    provider_id = "nautilus-trader"

    def _engine(self, request: Mapping[str, Any]) -> Any:
        try:
            from nautilus_trader.backtest import BacktestEngine
            from nautilus_trader.config import BacktestEngineConfig
        except ImportError as exc:
            raise RuntimeError("oss_dependency_missing:nautilus-trader") from exc

        config = request.get("engine_config")
        if config is None:
            config = BacktestEngineConfig()
        return BacktestEngine(config)

    @staticmethod
    def _add_optional(engine: Any, request: Mapping[str, Any]) -> None:
        for venue in request.get("venues", ()):
            engine.add_venue(venue)
        for instrument in request.get("instruments", ()):
            engine.add_instrument(instrument)
        for actor in request.get("actors", ()):
            engine.add_actor(actor)
        for strategy in request.get("strategies", ()):
            engine.add_strategy(strategy)
        for execution_algorithm in request.get("execution_algorithms", ()):
            engine.add_exec_algorithm(execution_algorithm)
        for batch in request.get("data_batches", ()):
            engine.add_data(
                batch,
                client_id=request.get("client_id"),
                validate=request.get("validate_data", True),
                sort=request.get("sort_data", True),
            )

    @staticmethod
    def _result_payload(result: Any) -> dict[str, Any]:
        payload: dict[str, Any] = {"provider": "nautilus-trader"}
        if result is None:
            return payload
        for name in ("stats_pnls", "stats_returns"):
            value = getattr(result, name, None)
            if value is not None:
                payload[name] = value
        return payload

    async def backtest(self, request: dict[str, Any]) -> dict[str, Any]:
        if not request:
            raise ValueError("nautilus_backtest_request_required")
        if not request.get("data_batches"):
            raise ValueError("nautilus_backtest_data_required")
        if not request.get("instruments"):
            raise ValueError("nautilus_backtest_instrument_required")
        if not request.get("venues"):
            raise ValueError("nautilus_backtest_venue_required")
        if not request.get("strategies") and not request.get("actors"):
            raise ValueError("nautilus_backtest_component_required")

        def _run() -> dict[str, Any]:
            engine = self._engine(request)
            try:
                self._add_optional(engine, request)
                engine.run(
                    start=request.get("start"),
                    end=request.get("end"),
                    run_config_id=request.get("run_config_id"),
                    streaming=request.get("streaming", False),
                )
                result = engine.get_result()
                return self._result_payload(result)
            finally:
                dispose = getattr(engine, "dispose", None)
                if callable(dispose):
                    dispose()

        return await asyncio.to_thread(_run)

    async def reconcile(self, account_id: str) -> dict[str, Any]:
        raise NotImplementedError(
            "nautilus_live_reconciliation_requires_live_node_adapter"
        )

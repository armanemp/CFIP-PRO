"""Optional Evidently evaluation adapter for MIOS drift/evaluation.

The dependency is lazy so the native CFIP installation remains lightweight.
Vendor report objects are converted to plain dictionaries at the boundary.
"""

import asyncio
from typing import Any


class EvidentlyEvaluationAdapter:
    provider_id = "evidently"

    async def evaluate_drift(self, current_data: Any, reference_data: Any) -> dict[str, Any]:
        return await asyncio.to_thread(
            self._evaluate_sync,
            current_data,
            reference_data,
        )

    @staticmethod
    def _evaluate_sync(current_data: Any, reference_data: Any) -> dict[str, Any]:
        try:
            from evidently import Report
            from evidently.presets import DataDriftPreset
        except ImportError as exc:
            raise RuntimeError(
                "evidently_optional_dependency_missing:install the Evidently extra to enable this adapter"
            ) from exc

        report = Report([DataDriftPreset()], include_tests=True)
        snapshot = report.run(current_data, reference_data)
        if hasattr(snapshot, "dict"):
            result = snapshot.dict()
        elif hasattr(snapshot, "json"):
            import json

            result = json.loads(snapshot.json())
        else:
            raise TypeError("evidently_snapshot_missing_serialization")
        if not isinstance(result, dict):
            raise TypeError("evidently_snapshot_must_be_mapping")
        return result

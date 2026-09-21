"""Runtime-editable platform identity registry.

The registry removes product branding from hardcoded UI/runtime constants. A durable
database-backed settings store can replace this process-local state without changing
the API contract.
"""
from threading import RLock
from cfip.domain.intelligence_identity import DEFAULT_INTELLIGENCE_IDENTITY, IntelligenceIdentity

class RuntimeIdentityRegistry:
    def __init__(self) -> None:
        self._lock = RLock()
        self._identity = DEFAULT_INTELLIGENCE_IDENTITY

    def get(self) -> IntelligenceIdentity:
        with self._lock:
            return self._identity.model_copy(deep=True)

    def set_name(self, name: str) -> IntelligenceIdentity:
        identity = self.get().model_copy(update={"name": name, "short_name": name[:12]})
        with self._lock:
            self._identity = identity
            return identity.model_copy(deep=True)

platform_identity = RuntimeIdentityRegistry()

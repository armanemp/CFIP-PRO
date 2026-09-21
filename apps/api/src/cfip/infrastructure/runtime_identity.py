"""Persistent, runtime-editable platform identity registry."""
from threading import RLock
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from cfip.domain.intelligence_identity import DEFAULT_INTELLIGENCE_IDENTITY, IntelligenceIdentity
from cfip.infrastructure.db.models import PlatformSettingModel
from cfip.infrastructure.db.session import session_factory

IDENTITY_NAME_KEY = "platform.identity.name"

class RuntimeIdentityRegistry:
    def __init__(self) -> None:
        self._lock = RLock()
        self._identity = DEFAULT_INTELLIGENCE_IDENTITY

    def get(self) -> IntelligenceIdentity:
        with self._lock:
            return self._identity.model_copy(deep=True)

    async def get_async(self, session: AsyncSession | None = None) -> IntelligenceIdentity:
        try:
            if session is None:
                async with session_factory() as owned:
                    return await self._read(owned)
            return await self._read(session)
        except Exception:
            return self.get()

    async def set_name_async(self, name: str, session: AsyncSession | None = None) -> IntelligenceIdentity:
        identity = self.get().model_copy(update={"name": name, "short_name": name[:12]})
        try:
            if session is None:
                async with session_factory() as owned:
                    await self._write(owned, identity)
            else:
                await self._write(session, identity)
        except Exception:
            # Keep runtime behavior usable when PostgreSQL is temporarily unavailable.
            pass
        with self._lock:
            self._identity = identity
            return identity.model_copy(deep=True)

    async def _read(self, session: AsyncSession) -> IntelligenceIdentity:
        row = await session.get(PlatformSettingModel, IDENTITY_NAME_KEY)
        if row is None:
            return self.get()
        identity = self.get().model_copy(update={"name": row.value, "short_name": row.value[:12]})
        with self._lock:
            self._identity = identity
        return identity.model_copy(deep=True)

    async def _write(self, session: AsyncSession, identity: IntelligenceIdentity) -> None:
        row = await session.get(PlatformSettingModel, IDENTITY_NAME_KEY)
        if row is None:
            row = PlatformSettingModel(key=IDENTITY_NAME_KEY, value=identity.name)
            session.add(row)
        else:
            row.value = identity.name
        await session.commit()

    def set_name(self, name: str) -> IntelligenceIdentity:
        identity = self.get().model_copy(update={"name": name, "short_name": name[:12]})
        with self._lock:
            self._identity = identity
        return identity.model_copy(deep=True)

platform_identity = RuntimeIdentityRegistry()

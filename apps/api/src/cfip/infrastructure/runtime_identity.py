"""Persistent, runtime-editable platform identity registry."""

from threading import RLock

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
            # Availability fallback: reads must not take the terminal offline.
            return self.get()

    async def set_name_async(
        self, name: str, session: AsyncSession | None = None
    ) -> IntelligenceIdentity:
        normalized = IntelligenceIdentity(name=name).name
        current = self.get()
        identity = current.model_copy(update={"name": normalized, "short_name": normalized[:12]})
        # Revalidate the complete aggregate after deriving its short name.
        identity = IntelligenceIdentity.model_validate(identity.model_dump())

        if session is None:
            async with session_factory() as owned:
                await self._write(owned, identity)
        else:
            await self._write(session, identity)

        with self._lock:
            self._identity = identity
            return identity.model_copy(deep=True)

    async def _read(self, session: AsyncSession) -> IntelligenceIdentity:
        row = await session.get(PlatformSettingModel, IDENTITY_NAME_KEY)
        if row is None:
            return self.get()
        identity = IntelligenceIdentity.model_validate(
            {**self.get().model_dump(), "name": row.value, "short_name": row.value[:12]}
        )
        with self._lock:
            self._identity = identity
        return identity.model_copy(deep=True)

    async def _write(self, session: AsyncSession, identity: IntelligenceIdentity) -> None:
        row = await session.get(PlatformSettingModel, IDENTITY_NAME_KEY)
        if row is None:
            session.add(PlatformSettingModel(key=IDENTITY_NAME_KEY, value=identity.name))
        else:
            row.value = identity.name
        await session.commit()

    def set_name(self, name: str) -> IntelligenceIdentity:
        normalized = IntelligenceIdentity(name=name).name
        current = self.get()
        identity = IntelligenceIdentity.model_validate(
            {**current.model_dump(), "name": normalized, "short_name": normalized[:12]}
        )
        with self._lock:
            self._identity = identity
        return identity.model_copy(deep=True)


platform_identity = RuntimeIdentityRegistry()

import pytest

from cfip.infrastructure.runtime_identity import platform_identity

@pytest.mark.asyncio
async def test_identity_cache_changes() -> None:
    original = platform_identity.get()
    try:
        changed = platform_identity.set_name("MIOSAI")
        assert changed.name == "MIOSAI"
        assert platform_identity.get().name == "MIOSAI"
    finally:
        platform_identity.set_name(original.name)

@pytest.mark.asyncio
async def test_identity_async_falls_back_without_database() -> None:
    original = platform_identity.get()
    try:
        platform_identity.set_name("MIOSAI")
        identity = await platform_identity.get_async()
        assert identity.name in {"MIOSAI", original.name}
    finally:
        platform_identity.set_name(original.name)

from cfip.infrastructure.runtime_identity import platform_identity

def test_identity_is_dynamic() -> None:
    original = platform_identity.get()
    try:
        changed = platform_identity.set_name("MIOSAI")
        assert changed.name == "MIOSAI"
        assert platform_identity.get().name == "MIOSAI"
    finally:
        platform_identity._identity = original

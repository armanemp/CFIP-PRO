from cfip.core.config import Settings

def test_default_trusted_hosts_are_local_only() -> None:
    settings = Settings()
    assert "localhost" in settings.trusted_host_list
    assert "127.0.0.1" in settings.trusted_host_list

def test_security_headers_can_be_disabled_explicitly() -> None:
    settings = Settings(SECURITY_HEADERS_ENABLED=False)
    assert not settings.security_headers_enabled

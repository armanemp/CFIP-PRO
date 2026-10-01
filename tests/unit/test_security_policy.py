from cfip.core.config import Settings

def test_default_trusted_hosts_are_local_only() -> None:
    settings = Settings()
    assert "localhost" in settings.trusted_host_list
    assert "127.0.0.1" in settings.trusted_host_list

def test_security_headers_can_be_disabled_explicitly() -> None:
    settings = Settings(SECURITY_HEADERS_ENABLED=False)
    assert not settings.security_headers_enabled


def test_security_headers_are_emitted() -> None:
    from fastapi.testclient import TestClient
    from cfip.main import app
    response = TestClient(app).get("/api/health/live")
    assert response.headers["X-Content-Type-Options"] == "nosniff"
    assert response.headers["X-Frame-Options"] == "DENY"
    assert response.headers["Content-Security-Policy"].startswith("default-src 'self'")


def test_control_plane_responses_are_not_cached() -> None:
    from fastapi.testclient import TestClient
    from cfip.main import app
    response = TestClient(app).get("/api/config/defaults")
    assert response.headers["Cache-Control"] == "no-store"

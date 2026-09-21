"""Security boundary regression tests."""

import pytest
from pydantic import ValidationError
from fastapi.testclient import TestClient

from cfip.core.config import Settings
from cfip.main import app


def test_cors_wildcard_is_rejected_when_credentials_are_enabled() -> None:
    with pytest.raises(ValidationError, match="cors_wildcard_forbidden_with_credentials"):
        Settings(CORS_ORIGINS="*")


def test_request_id_is_propagated_and_security_headers_are_present() -> None:
    client = TestClient(app)
    response = client.get("/api/health", headers={"X-Request-ID": "req-security-1"})
    assert response.status_code == 200
    assert response.headers["X-Request-ID"] == "req-security-1"
    assert response.headers["X-Content-Type-Options"] == "nosniff"
    assert response.headers["X-Frame-Options"] == "DENY"


def test_oversized_request_id_is_replaced() -> None:
    client = TestClient(app)
    response = client.get("/api/health", headers={"X-Request-ID": "x" * 129})
    assert response.status_code == 200
    assert response.headers["X-Request-ID"] != "x" * 129


def test_untrusted_host_is_rejected() -> None:
    client = TestClient(app)
    response = client.get("/api/health", headers={"Host": "evil.example"})
    assert response.status_code == 400

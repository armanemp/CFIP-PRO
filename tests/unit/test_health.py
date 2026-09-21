from fastapi.testclient import TestClient

from cfip.main import app


def test_health() -> None:
    response = TestClient(app).get("/api/health")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    assert body["service"] == "api"


def test_health_evaluation_is_fail_closed_after_repeated_failure() -> None:
    client = TestClient(app)
    response = client.post(
        "/api/health/evaluate",
        json={
            "health": {
                "component": "market-data",
                "status": "degraded",
                "observed_at": 1000,
                "error_rate": 0.20,
                "latency_ms": 8000,
                "freshness_seconds": 600,
            },
            "circuit": {
                "component": "market-data",
                "state": "closed",
                "consecutive_failures": 2,
            },
            "policy": {
                "max_consecutive_failures": 3,
            },
        },
    )
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "unhealthy"
    assert body["should_open_circuit"] is True
    assert body["next_state"] == "open"


def test_health_invariants_require_complete_evidence() -> None:
    client = TestClient(app)
    response = client.post(
        "/api/health/invariants",
        json={
            "observations": [
                {
                    "invariant_id": "tests_green",
                    "passed": True,
                    "observed_at": 1000,
                    "detail": "unit suite passed",
                }
            ]
        },
    )
    assert response.status_code == 200
    body = response.json()
    assert body["healthy"] is False
    assert "security_clean" in body["blocking_invariants"]


def test_security_headers() -> None:
    response = TestClient(app).get("/api/health")
    assert response.headers["x-content-type-options"] == "nosniff"
    assert response.headers["x-frame-options"] == "DENY"
    assert response.headers["referrer-policy"] == "strict-origin-when-cross-origin"
    assert response.headers["permissions-policy"] == "camera=(), microphone=(), geolocation=()"
    assert response.headers["content-security-policy"] == "frame-ancestors 'none'"

from fastapi.testclient import TestClient

from cfip.main import app


def test_health() -> None:
    response = TestClient(app).get("/api/health", headers={"Host": "localhost"})
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    assert body["service"] == "api"

from cfip.infrastructure.oss.openbb_research import OpenBBResearchAdapter


def test_openbb_serialization_adds_deterministic_evidence_metadata() -> None:
    result = OpenBBResearchAdapter._serialize(
        [{"title": "Gold", "url": "https://example.test/gold", "published": "2026-09-20"}]
    )
    record = result[0]
    assert record["source_provider"] == "openbb"
    assert record["source_url"] == "https://example.test/gold"
    assert record["published_at"] == "2026-09-20"
    assert len(record["evidence_id"]) == 64
    again = OpenBBResearchAdapter._serialize(
        [{"title": "Gold", "url": "https://example.test/gold", "published": "2026-09-20"}]
    )
    assert record["evidence_id"] == again[0]["evidence_id"]

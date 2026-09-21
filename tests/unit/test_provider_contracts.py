from cfip.domain.provider_contracts import PROVIDER_CATALOG

def test_provider_catalog_has_distinct_ids() -> None:
    ids = [item.id for item in PROVIDER_CATALOG]
    assert len(ids) == len(set(ids))
    assert "oanda" in ids
    assert "ctrader" in ids
    assert "github" in ids

def test_catalog_does_not_expose_credentials() -> None:
    for provider in PROVIDER_CATALOG:
        assert "key" not in provider.model_dump()
        assert "secret" not in provider.model_dump()

from cfip.domain.provider_health import FailoverPolicy, ProviderHealth, eligible_providers

def test_failover_preserves_configured_priority() -> None:
    health=(ProviderHealth(provider_id="a",state="degraded",capabilities=("forex",)),ProviderHealth(provider_id="b",state="healthy",capabilities=("forex",)))
    assert eligible_providers(health, FailoverPolicy(provider_ids=("a","b"),require_capability="forex")) == ("a","b")

def test_failover_skips_unavailable_and_incompatible_providers() -> None:
    health=(ProviderHealth(provider_id="a",state="down",capabilities=("forex",)),ProviderHealth(provider_id="b",state="healthy",capabilities=("crypto",)),ProviderHealth(provider_id="c",state="healthy",capabilities=("forex",)))
    assert eligible_providers(health, FailoverPolicy(provider_ids=("a","b","c"),require_capability="forex")) == ("c",)

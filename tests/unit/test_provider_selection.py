from cfip.domain.provider_contracts import ProviderDescriptor
from cfip.domain.provider_health import ProviderHealth
from cfip.domain.provider_selection import ProviderSelectionPolicy, select_providers

def test_selection_preserves_configured_priority_and_capability() -> None:
    catalog = (
        ProviderDescriptor(id="a", name="A", kind="market-data", capabilities=("forex", "realtime")),
        ProviderDescriptor(id="b", name="B", kind="market-data", capabilities=("forex",)),
    )
    health = (
        ProviderHealth(provider_id="a", state="healthy", capabilities=("forex", "realtime")),
        ProviderHealth(provider_id="b", state="healthy", capabilities=("forex",)),
    )
    selected = select_providers(
        catalog,
        health,
        ProviderSelectionPolicy(provider_ids=("b", "a"), required_capabilities=("realtime",)),
    )
    assert tuple(item.id for item in selected) == ("a",)

def test_degraded_provider_requires_explicit_opt_in() -> None:
    catalog = (ProviderDescriptor(id="a", name="A", kind="market-data", capabilities=("forex",)),)
    health = (ProviderHealth(provider_id="a", state="degraded", capabilities=("forex",)),)
    policy = ProviderSelectionPolicy(provider_ids=("a",), required_capabilities=("forex",))
    assert select_providers(catalog, health, policy) == ()
    assert tuple(item.id for item in select_providers(catalog, health, policy.model_copy(update={"allow_degraded": True}))) == ("a",)

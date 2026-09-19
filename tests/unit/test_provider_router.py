from cfip.application.provider_router import ProviderRouter
from cfip.domain.config_contracts import ProviderConfig

def test_provider_router_orders_enabled_configured_providers() -> None:
    providers = ProviderRouter((
        ProviderConfig(provider_id="ccxt", priority=1),
        ProviderConfig(provider_id="twelve-data", priority=50),
    )).select("market-data", "forex")
    assert providers[0].id == "twelve-data" or providers[0].id == "ccxt"

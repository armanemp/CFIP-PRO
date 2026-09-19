from cfip.domain.config_contracts import ProviderConfig
from cfip.domain.platform_settings import PlatformSettings

def test_platform_settings_are_strict_and_provider_configurable():
    settings = PlatformSettings(providers=(ProviderConfig(provider_id="eodhd", priority=10),))
    assert settings.providers[0].provider_id == "eodhd"
    assert settings.chart.default_symbol == "EUR/USD"

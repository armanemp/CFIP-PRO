from cfip.domain.platform_settings import PlatformSettings, default_provider_configs

def test_platform_settings_exposes_every_catalog_provider_without_claiming_connectivity():
    settings = PlatformSettings()
    assert len(settings.providers) >= 20
    assert all(item.enabled is False for item in settings.providers)
    assert {item.provider_id for item in settings.providers} >= {"eodhd", "ccxt", "oanda", "openai", "github"}

def test_platform_settings_allows_explicit_provider_configuration():
    settings = PlatformSettings(providers=(default_provider_configs()[0].model_copy(update={"enabled": True, "priority": 10}),))
    assert settings.providers[0].enabled is True
    assert settings.providers[0].priority == 10

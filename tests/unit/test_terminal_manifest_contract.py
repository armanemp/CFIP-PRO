from cfip.domain.platform_settings import PlatformSettings

def test_terminal_feature_manifest_has_core_surface() -> None:
    settings = PlatformSettings()
    required = {"indicators","analysis","risk","alerts","workspaces","intelligence"}
    assert required <= set(settings.enabled_modules)

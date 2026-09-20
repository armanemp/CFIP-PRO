from cfip.domain.config_contracts import IntelligenceConfig, ProviderConfig, RiskConfig
from cfip.domain.intelligence_identity import DEFAULT_INTELLIGENCE_NAME, IntelligenceIdentity


def test_risk_bounds_are_validated() -> None:
    assert RiskConfig(max_account_risk_percent=2).max_account_risk_percent == 2


def test_invalid_risk_is_rejected() -> None:
    import pytest

    with pytest.raises(ValueError):
        RiskConfig(max_account_risk_percent=101)


def test_provider_secrets_are_references() -> None:
    config = ProviderConfig(provider_id="oanda", secret_refs=({"ref": "secret/oanda"},))
    assert config.secret_refs[0].ref == "secret/oanda"
    assert "secret" not in config.model_dump(exclude={"secret_refs"})


def test_intelligence_identity_has_finance_default_and_is_overridable() -> None:
    assert DEFAULT_INTELLIGENCE_NAME == "Aurevex"
    identity = IntelligenceIdentity(name="NorthstarFX", short_name="NSF")
    assert identity.name == "NorthstarFX"
    assert identity.short_name == "NSF"
    assert identity.domain == "finance.forex"

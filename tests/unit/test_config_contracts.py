from cfip.domain.config_contracts import IntelligenceConfig, ProviderConfig, RiskConfig

def test_risk_bounds_are_validated() -> None:
    assert RiskConfig(max_account_risk_percent=2).max_account_risk_percent == 2

def test_invalid_risk_is_rejected() -> None:
    import pytest
    with pytest.raises(ValueError):
        RiskConfig(max_account_risk_percent=101)

def test_provider_secrets_are_references() -> None:
    config = ProviderConfig(provider_id="oanda", secret_refs=({"ref":"secret/oanda"},))
    assert config.secret_refs[0].ref == "secret/oanda"
    assert "secret" not in config.model_dump(exclude={"secret_refs"})

from cfip.domain.execution_contracts import OrderRequest
from cfip.domain.intelligence_governance import IntelligenceProposal
from cfip.domain.payment_contracts import PaymentIntent
from cfip.domain.platform_settings import PlatformSettings

def test_platform_settings_are_typed_and_composed() -> None:
    settings = PlatformSettings()
    assert "indicators" in settings.enabled_modules
    assert settings.git.allow_direct_main_commit is False

def test_execution_order_has_client_key() -> None:
    order = OrderRequest(account_id="paper", symbol="EUR/USD", side="buy", order_type="market", quantity=1, client_order_id="order-2026-09-19-001")
    assert order.client_order_id

def test_intelligence_proposal_defaults_to_approval() -> None:
    proposal = IntelligenceProposal(id="p1", objective="improve indicator cache")
    assert proposal.requires_human_approval is True

def test_payment_is_crypto_only() -> None:
    intent = PaymentIntent(id="pi1", user_id="u1", plan_id="pro", asset="USDC", amount_atomic=4900, checkout_reference="checkout-1", idempotency_key="idem-20260919-1")
    assert intent.asset in {"BTC","ETH","USDC","USDT"}

from cfip.domain.research_contracts import ResearchRequest
from cfip.domain.research_security import ResearchURLPolicy
from cfip.infrastructure.providers.ccxt_adapter import CCXTMarketDataAdapter


def test_research_policy_rejects_credentials_and_non_https() -> None:
    policy = ResearchURLPolicy()
    for url in ("http://example.com", "https://user:pass@example.com"):
        try:
            policy.validate(url)
        except ValueError:
            pass
        else:
            raise AssertionError("unsafe research URL was accepted")


def test_ccxt_adapter_is_lazy() -> None:
    adapter = CCXTMarketDataAdapter("binance")
    assert adapter.id == "ccxt"
    assert adapter.version


def test_research_request_is_typed() -> None:
    request = ResearchRequest(url="https://example.com")
    assert request.timeout_seconds == 15

from cfip.domain.oss_contracts import ResearchRetriever
from cfip.infrastructure.oss.openbb_research import OpenBBResearchAdapter


def test_openbb_adapter_implements_research_contract() -> None:
    adapter = OpenBBResearchAdapter(provider="yfinance")
    assert isinstance(adapter, ResearchRetriever)
    assert adapter.provider_id == "openbb"

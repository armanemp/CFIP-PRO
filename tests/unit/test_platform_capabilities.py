from cfip.domain.platform_capabilities import capabilities

def test_capability_registry_has_single_contract_for_major_domains() -> None:
    items = capabilities()
    ids = {item.id for item in items}
    assert "terminal.chart" in ids
    assert "analysis.unified" in ids
    assert "trading.risk" in ids
    assert "intelligence.pipvara" in ids
    assert "platform.git-governance" in ids

def test_capability_contracts_are_unique() -> None:
    contracts = [item.contract for item in capabilities()]
    assert len(contracts) == len(set(contracts))

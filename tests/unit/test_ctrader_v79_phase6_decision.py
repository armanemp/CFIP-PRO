from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
V78 = ROOT / "integrations" / "ctrader" / "calude-edit-v78.cs"
V79 = ROOT / "integrations" / "ctrader" / "calude-edit-v79.cs"

def read(p): return p.read_text(encoding="utf-8")

def params(s):
    return re.findall(
        r"\[Parameter\([\s\S]*?\)\]\s*public\s+[\w<>\?\.]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}",
        s)

def test_v79_preserves_v78_parameters():
    a, b = params(read(V78)), params(read(V79))
    assert len(a) == 512
    assert b == a
    assert len(set(b)) == 512

def test_phase6_has_authoritative_decision_engine():
    s = read(V79)
    assert "public sealed class CFIPClean79DecisionEngine" in s
    assert "CFIPClean79StructureSnapshot structure" in s
    assert "_decisionEngine.Evaluate(" in s
    assert 'CFIPClean79Provenance.Direct("DECISION_ENGINE"' in s

def test_phase6_decision_is_canonical_and_structural():
    s = read(V79)
    for token in [
        "MinimumConfidence", "MinimumEdge", "MinimumSmartQuality",
        "MinimumStructuralConfirmations", "MinimumIndependentEvidence",
        "MinimumTimeframeAgreement", "SmartConsensusThreshold",
        "RequireSmartConsensus", "SmartStrongSetupQuality",
        "SmartStrongSetupEdge", "UseSmartEntryQualityFilter",
        "UseRegimeNoTradeGuard", "BlockCompressionRegime",
        "BlockWeakRangeTransition", "EnableAggressiveAutoEntry"
    ]:
        assert token in s

def test_phase6_consumes_structure_zones_and_liquidity():
    s = read(V79)
    for token in [
        "structure.Events", "structure.Zones", "structure.Liquidity",
        "z.ExecutionEligible", "z.FvgConfluence",
        "z.LiquidityConfluence", "l.Swept", "e.Structural"
    ]:
        assert token in s

def test_phase6_has_no_broker_or_ui_authority():
    s = read(V79)
    for token in [
        ".ExecuteMarketOrder(", ".PlaceStopOrder(", ".PlaceLimitOrder(",
        ".ModifyStopLossPrice(", ".ModifyTakeProfitPrice(",
        ".ClosePosition(", ".CancelPendingOrder(", "Chart.", "CreatePanel("
    ]:
        assert token not in s

def test_v79_is_version_isolated_and_types_unique():
    s = read(V79)
    assert "CFIPClean78" not in s
    assert "Clean_v78" not in s
    types = re.findall(
        r"public\s+(?:sealed\s+)?(?:class|enum|interface|static class)\s+(\w+)", s)
    assert len(types) == len(set(types))

def test_v79_braces_are_balanced():
    s = read(V79)
    stripped = re.sub(r"//.*$", "", s, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)
    assert stripped.count("{") == stripped.count("}")

def test_decision_snapshot_contract_matches_engine():
    s = read(V79)
    sig = re.search(
        r"public\s+CFIPClean79DecisionSnapshot\s+Evaluate\([\s\S]*?\)",
        s)
    assert sig
    block = sig.group(0)
    for token in [
        "CFIPClean79RuntimeSnapshot runtime",
        "CFIPClean79MtfSnapshot mtf",
        "CFIPClean79MarketModel market",
        "CFIPClean79StructureSnapshot structure",
        "CFIPClean79ConfigSnapshot configuration"
    ]:
        assert token in block

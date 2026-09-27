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

def test_phase6_has_retest_quality_and_pending_policy():
    s = read(V79)
    assert "Retested" in s
    assert "BullRetest" in s and "BearRetest" in s
    assert "CFIPClean79DecisionPolicyMode.Pending" in s
    assert "ResolvePolicy(" in s

def test_phase6_weighted_quality_domains_sum_to_one():
    s = read(V79)
    assert "marketQuality * 0.28" in s
    assert "mtfAgreement * 0.18" in s
    assert "structureQuality * 0.18" in s
    assert "zoneQuality * 0.12" in s
    assert "liquidityQuality * 0.07" in s
    assert "confluenceQuality * 0.05" in s
    assert "retestQuality * 0.06" in s
    assert "regimeQuality * 0.06" in s
    assert "Weighted domains intentionally sum to 1.00" in s


def test_phase6_state_exposes_authoritative_decision_setter():
    s = read(V79)
    assert "public void SetDecision(CFIPClean79DecisionSnapshot value)" in s
    assert "Decision = value;" in s

def test_phase6_deduplicates_market_evidence_across_timeframes():
    s = read(V79)
    assert "AddMarketEvidence(" in s
    assert "Dictionary<CFIPClean79MarketFeature, FeatureAggregate>" in s
    assert "feature.Value * Math.Max(0, feature.Weight)" in s
    assert "aggregate.Score = feature.Value * Math.Max(0, feature.Weight)" not in s
    assert "e.Bull += aggregate.Score" in s
    assert "e.Bear += aggregate.Score" in s
    assert "AddMarket(e, market.FindFrame" not in s

def test_phase6_does_not_add_confluence_as_directional_evidence():
    s = read(V79)
    assert "Confluence is a quality modifier" in s
    assert "e.Bull += e.BullConfluence * 0.04" not in s
    assert "e.Bear += e.BearConfluence * 0.04" not in s

def test_phase6_collapses_structure_event_families():
    s = read(V79)
    assert "bullStructureBreak" in s
    assert "bearStructureBreak" in s
    assert "bullDisplacement" in s
    assert "bearDisplacement" in s


def test_phase6_cycle_reset_preserves_reference_gated_market_state():
    s = read(V79)
    block = re.search(
        r"public void ResetCycleOutputs\(\)[\s\S]*?\n\s*\}",
        s)
    assert block
    body = block.group(0)
    assert "Market = null;" not in body
    assert "Structure = null;" not in body
    assert "Decision = null;" in body

def test_phase6_reuses_persisted_market_structure_for_decision():
    s = read(V79)
    assert "if (mtf.IsPrimaryDecisionReady &&" in s
    assert "_state.Market != null" in s
    assert "_state.Structure != null" in s
    assert "_state.SetDecision(" in s


def test_phase6_evidence_dedup_preserves_market_feature_weights():
    s = read(V79)
    assert "feature.Value * Math.Max(0, feature.Weight)" in s
    assert "aggregate.Direction = feature.Direction" in s
    assert "e.Bull += aggregate.Score" in s
    assert "e.Bear += aggregate.Score" in s
    assert "aggregate.Bull = Math.Max" not in s
    assert "aggregate.Bear = Math.Max" not in s


def test_phase6_uses_decision_level_eligibility():
    s = read(V79)
    assert "DecisionEligible" in s
    assert "EntryEligible" not in s
    assert "Decision-level eligibility. Entry/Trigger eligibility belongs to Phase 7." in s


def test_phase6_policy_snapshot_invariants_are_enforced():
    s = read(V79)
    assert "Eligible decision cannot be WAIT or blocked." in s
    assert "Confirmed/Aggressive policy requires decision eligibility." in s
    assert "Pending policy requires a directional blocked setup." in s
    assert "CFIPClean79DecisionPolicyMode.Soft" in s


def test_phase6_blocked_snapshot_is_not_marked_confirmed():
    s = read(V79)
    engine = s[s.index("public sealed class CFIPClean79DecisionEngine"):
               s.index("// ------------------------------------------------------------------------
    // Trade identity / idempotency")]
    assert "CFIPClean79Direction.Wait" in engine
    assert "CFIPClean79DecisionPolicyMode.Soft" in engine


def test_phase6_has_single_decision_evaluation_authority():
    s = read(V79)
    assert s.count("_decisionEngine.Evaluate(") == 1
    engine = s[s.index("public sealed class CFIPClean79DecisionEngine"):
               s.index("// ------------------------------------------------------------------------
    // Trade identity / idempotency")]
    assert engine.count("new CFIPClean79DecisionSnapshot(") == 2


def test_phase6_structural_confirmations_are_directional_and_family_deduplicated():
    s = read(V79)
    assert "BullStructuralConfirmations" in s
    assert "BearStructuralConfirmations" in s
    assert 'string.Equals(\n                    x.Timeframe, "M5", StringComparison.OrdinalIgnoreCase)' in s
    assert 'string.Equals(\n                    x.Timeframe, "M15", StringComparison.OrdinalIgnoreCase)' in s
    assert 'string.Equals(\n                    x.Timeframe, "H1", StringComparison.OrdinalIgnoreCase)' in s
    assert 'string.Equals(\n                    x.Timeframe, "H4", StringComparison.OrdinalIgnoreCase)' in s
    assert "e.Structural =" in s
    assert "e.BullStructuralConfirmations" in s
    assert "e.BearStructuralConfirmations" in s
    assert "bullM5Break" in s and "bullM5Displacement" in s
    assert "bearM5Break" in s and "bearM5Displacement" in s


def test_phase6_market_dedup_is_order_independent():
    s = read(V79)
    assert "private sealed class FeatureAggregate" in s
    assert "public double BullScore;" in s
    assert "public double BearScore;" in s
    assert "aggregate.BullScore = Math.Max" in s
    assert "aggregate.BearScore = Math.Max" in s
    assert "aggregate.BullScore > aggregate.BearScore" in s
    assert "aggregate.BearScore > aggregate.BullScore" in s
    assert "exact tie is neutral" in s


def test_phase6_restores_adaptive_directional_threshold_semantics():
    s = read(V79)
    assert '"MinimumSmartDirectionShare", 57' in s
    assert '"AdaptiveSmartThresholds", true' in s
    assert '"SmartRegimeBuffer", 6' in s
    assert '"SmartScoreTemperature", 12.0' in s
    assert "GetAdaptiveSmartThresholds(" in s
    assert "CalculateDirectionalShares(" in s
    assert "strongestShare >= adaptiveShareThreshold" not in s
    assert "strongestShare < adaptiveShareThreshold" in s


def test_phase6_never_creates_direction_from_zero_or_tied_evidence():
    s = read(V79)
    assert "e.Bull <= 0 &&" in s
    assert "e.Bear <= 0" in s
    assert "bullShare > bearShare" in s
    assert "bearShare > bullShare" in s


def test_phase6_uses_adaptive_smart_quality_and_edge_gates():
    s = read(V79)
    assert "edge < adaptiveEdgeThreshold" in s
    assert "quality < adaptiveQualityThreshold" in s
    assert 'Math.Max(\n                        configuration.Get("SmartConsensusThreshold", 57),\n                        adaptiveShareThreshold)' in s


def test_phase6_reconciles_legacy_location_and_htf_confidence_semantics():
    s = read(V79)
    assert "CollectEvidence(" in s
    assert "UsePremiumDiscount" in s
    assert "structure.PremiumDiscount.IsDiscount" in s
    assert "structure.PremiumDiscount.IsPremium" in s
    assert "e.Bull += 6.0" in s
    assert "e.Bear += 6.0" in s
    assert "ApplyHigherTimeframePenalty(" in s
    assert 'cfg.Get("HigherTfPenalty", 7)' in s
    assert "market.FindFrame("H1")" in s
    assert "market.FindFrame("H4")" in s
    assert "market.FindFrame("D1")" in s


def test_phase6_softmax_share_calculation_is_temperature_controlled():
    s = read(V79)
    assert "double safeTemperature = Math.Max(1.0, temperature);" in s
    assert "double centered = (bull - bear) / safeTemperature;" in s
    assert "Math.Exp(" in s
    assert "bullShare = Clamp(" in s
    assert "bearShare = 100 - bullShare;" in s


def test_phase6_snapshot_requires_coherent_wait_and_eligibility_states():
    s = read(V79)
    assert "WAIT decision requires a NoDirection block." in s
    assert "Blocked directional decision requires at least one block." in s
    assert "Eligible decision requires Confirmed or Aggressive policy." in s


def test_phase6_advanced_confluence_is_a_quality_switch():
    s = read(V79)
    assert 'configuration.Get("UseAdvancedConfluence", true)' in s
    assert "confluenceQuality" in s
    assert "e.BullConfluence" in s and "e.BearConfluence" in s


def test_phase6_parameter_ownership_does_not_reintroduce_entry_logic():
    s = read(V79)
    assert "Compatibility parameter retained for preset parity. The clean Decision" in s
    assert "Phase 7 owns it." in s
    assert "execution confirmation belong to the Phase 7 Entry/Trigger owner." in s
    assert "v79 replaces the old" in s
    assert "additive regime-weighting layer with deduplicated evidence" in s


def test_phase6_decision_quality_filter_is_not_entry_trigger_authority():
    s = read(V79)
    assert "Legacy name retained for preset parity. In Phase 6 this is" in s
    assert "only a Decision-quality policy floor" in s
    assert "actual Entry/Trigger eligibility" in s


def test_phase6_engine_has_no_final_entry_or_broker_authority():
    s = read(V79)
    a = s.index("public sealed class CFIPClean79DecisionEngine")
    b = s.index("// ------------------------------------------------------------------------\n    // Trade identity / idempotency", a)
    engine = s[a:b]
    assert engine.count("CFIPClean79Direction direction =") == 1
    for token in [
        ".ExecuteMarketOrder(", ".PlaceStopOrder(", ".PlaceLimitOrder(",
        ".ModifyStopLossPrice(", ".ModifyTakeProfitPrice(",
        ".ClosePosition(", ".CancelPendingOrder(", "Chart.", "CreatePanel("
    ]:
        assert token not in engine
    assert "actual Entry/Trigger eligibility" in engine


def test_phase6_deduplicates_zones_by_family_and_direction():
    s = read(V79)
    assert "private sealed class ZoneAggregate" in s
    assert "Dictionary<CFIPClean79ZoneKind, ZoneAggregate>" in s
    assert "bullZones" in s and "bearZones" in s
    assert "z.Quality > aggregate.Quality" in s
    assert "aggregate.Quality * 0.08" in s
    assert "Multiple timeframes or repeated instances of the same family" in s


def test_phase6_deduplicates_liquidity_by_pool_family_and_direction():
    s = read(V79)
    assert "private sealed class LiquidityAggregate" in s
    assert "Dictionary<CFIPClean79LiquidityKind, LiquidityAggregate>" in s
    assert "bullLiquidity" in s and "bearLiquidity" in s
    assert "aggregate.Quality = Math.Max" in s
    assert "aggregate.Quality * 0.06" in s
    assert "Liquidity sweeps are deduplicated by liquidity-pool family." in s

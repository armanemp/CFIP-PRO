import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V80 = ROOT / "integrations" / "ctrader" / "calude-edit-v80.cs"
V81 = ROOT / "integrations" / "ctrader" / "calude-edit-v81.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def parameters(source: str):
    return re.findall(
        r'\[Parameter\("([^"]+)".*?\)\]\s*\r?\n\s*public\s+[^\s]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}',
        source,
    )


def csharp_braces_balanced(source: str) -> bool:
    depth = 0
    in_string = False
    in_char = False
    in_line_comment = False
    in_block_comment = False
    escape = False
    i = 0
    while i < len(source):
        ch = source[i]
        nxt = source[i + 1] if i + 1 < len(source) else ""
        if in_line_comment:
            if ch == "\n":
                in_line_comment = False
            i += 1
            continue
        if in_block_comment:
            if ch == "*" and nxt == "/":
                in_block_comment = False
                i += 2
                continue
            i += 1
            continue
        if in_string:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == '"':
                in_string = False
            i += 1
            continue
        if in_char:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == "'":
                in_char = False
            i += 1
            continue
        if ch == "/" and nxt == "/":
            in_line_comment = True
            i += 2
            continue
        if ch == "/" and nxt == "*":
            in_block_comment = True
            i += 2
            continue
        if ch == '"':
            in_string = True
        elif ch == "'":
            in_char = True
        elif ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth < 0:
                return False
        i += 1
    return depth == 0 and not in_string and not in_char and not in_block_comment


def phase8_engine(source: str) -> str:
    start = source.index("public sealed class CFIPClean81TradePlanBuilder")
    end = source.index("// ------------------------------------------------------------------------\n    // Execution intent / result", start)
    return source[start:end]


def test_v81_preserves_v80_parameter_surface():
    s80 = read(V80)
    s81 = read(V81)
    assert len(parameters(s80)) == 513
    assert len(parameters(s81)) == 513
    assert parameters(s80) == parameters(s81)


def test_v81_is_version_isolated():
    s = read(V81)
    assert "CFIPClean80" not in s
    assert "CFIPClean79" not in s
    assert "calude-edit-v80" not in s
    assert "CFIP-PRO-v81" in s
    assert "CFIP_MTF_LiveEntryEngine_Clean_v81" in s


def test_v81_braces_are_balanced():
    assert csharp_braces_balanced(read(V81))


def test_phase8_has_one_authoritative_trade_plan_builder():
    s = read(V81)
    assert s.count("class CFIPClean81TradePlanBuilder") == 1
    assert "ICFIPClean81TradePlanBuilder" in s
    assert "_tradePlanBuilder.Build(" in s
    assert s.count("_tradePlanBuilder.Build(") == 1


def test_phase8_consumes_exact_decision_and_entry_snapshots():
    e = phase8_engine(read(V81))
    assert "CFIPClean81DecisionSnapshot decision" in e
    assert "CFIPClean81EntrySnapshot entry" in e
    assert "entry.Decision != decision" in e
    assert "entry.Direction != decision.Direction" in e


def test_phase8_structural_stop_precedes_fallback():
    e = phase8_engine(read(V81))
    assert "FindStructuralStop(" in e
    assert "ENTRY_INVALIDATION" in e
    assert "M5_FVG_STOP" in e
    assert "M5_OB_STOP" in e
    assert "M5_SWING_STOP" in e
    assert "HTF_SWING_STOP" in e
    assert "FallbackSlAtr" in e
    assert "CFIPClean81FallbackKind.Atr" in e


def test_phase8_stop_risk_bounds_are_explicit():
    e = phase8_engine(read(V81))
    assert "MinimumSlAtr" in e
    assert "MaximumSlAtr" in e
    assert "STRUCTURAL_STOP_EXCEEDS_MAX_RISK" in e
    assert "STRUCTURAL_STOP_EXPANDED_TO_MINIMUM_RISK" in e


def test_phase8_target_ladder_is_single_source():
    e = phase8_engine(read(V81))
    assert "BuildTargetLadder(" in e
    assert "Tp1MinimumRR" in e
    assert "Tp2MinimumRR" in e
    assert "Tp3MinimumRR" in e
    assert "Tp4MinimumRR" in e
    assert "MinimumTpSpacingAtr" in e
    assert "MaximumTargetExtensionAtr" in e
    assert "TargetClearanceAtr" in e


def test_phase8_uses_liquidity_and_explicit_synthetic_fallback():
    e = phase8_engine(read(V81))
    assert "FindLiquidityTarget(" in e
    assert "LIQUIDITY_TARGET_" in e
    assert "SYNTHETIC_RR_TARGET" in e
    assert "AllowSyntheticTargetFallback" in e
    assert "CFIPClean81FallbackKind.Synthetic" in e


def test_phase8_target_obstacle_policy_is_explicit():
    e = phase8_engine(read(V81))
    assert "RejectTargetObstacle" in e
    assert "HasObstacle(" in e
    assert "CFIPClean81Direction opposing" in e


def test_phase8_target_ordering_and_plan_validity_are_enforced():
    e = phase8_engine(read(V81))
    assert "ladder.ValidateForDirection(decision.Direction)" in e
    assert "ladder.Levels.Count == 0" in e
    assert "rr1 > 0" in e


def test_phase8_has_no_broker_or_ui_authority():
    e = phase8_engine(read(V81))
    forbidden = (
        "ExecuteMarketOrder",
        "PlaceStopOrder",
        "PlaceLimitOrder",
        "CancelPendingOrder",
        "ModifyPosition",
        "ModifyPendingOrder",
        "ClosePosition",
        "Chart.Draw",
        "Chart.RemoveObject",
        "Chart.DrawText",
    )
    assert not any(token in e for token in forbidden)


def test_phase8_plan_is_persisted_downstream_of_entry():
    s = read(V81)
    assert "CFIPClean81TradePlan Plan" in s
    assert "public void SetPlan(CFIPClean81TradePlan value)" in s
    assert "Plan = null;" in s
    assert "_state.SetPlan(" in s
    assert s.index("_state.SetEntry(") < s.index("_state.SetPlan(")


def test_phase8_target_candidates_must_progress_in_trade_direction():
    e = phase8_engine(read(V81))
    assert "progressesBeyondPrevious" in e
    assert "x.Price > previousTarget + spacing" in e
    assert "x.Price < previousTarget - spacing" in e


def test_phase8_plan_and_signal_ids_are_distinct():
    e = phase8_engine(read(V81))
    assert "BuildPlanId(" in e
    assert "BuildSignalId(" in e
    assert '"CFIP81|PLAN|"' in e
    assert '"CFIP81|SIGNAL|"' in e
    assert "stop.Price" in e


def test_phase8_valid_trade_plan_has_constructor_invariants():
    s = read(V81)
    plan = s[s.index("public sealed class CFIPClean81TradePlan"):
               s.index("// ------------------------------------------------------------------------\n// Phase 8", s.index("public sealed class CFIPClean81TradePlan"))]
    assert "A valid TradePlan must be directional." in plan
    assert "TradePlan direction must match Entry direction." in plan
    assert "TradePlan structural stop must protect the selected direction." in plan
    assert "positive RR" in plan

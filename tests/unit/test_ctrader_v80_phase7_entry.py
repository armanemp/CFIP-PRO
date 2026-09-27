import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V79 = ROOT / "integrations" / "ctrader" / "calude-edit-v79.cs"
V80 = ROOT / "integrations" / "ctrader" / "calude-edit-v80.cs"


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


def entry_engine(source: str) -> str:
    start = source.index("public sealed class CFIPClean80EntryTriggerEngine")
    end = source.index("// Trade identity / idempotency", start)
    return source[start:end]


def test_v80_preserves_v79_parameter_surface():
    s79 = read(V79)
    s80 = read(V80)
    assert len(parameters(s79)) == 513
    assert len(parameters(s80)) == 513
    assert parameters(s79) == parameters(s80)


def test_v80_is_version_isolated():
    s = read(V80)
    assert "CFIPClean79" not in s
    assert "CFIPClean78" not in s
    assert "calude-edit-v79" not in s
    assert "CFIP-PRO-v80" in s
    assert "CFIP_MTF_LiveEntryEngine_Clean_v80" in s


def test_v80_braces_are_balanced():
    assert csharp_braces_balanced(read(V80))


def test_phase7_has_canonical_entry_snapshot():
    s = read(V80)
    assert "class CFIPClean80EntrySnapshot" in s
    assert "CFIPClean80DecisionSnapshot Decision" in s
    assert "CFIPClean80Direction Direction" in s
    assert "CFIPClean80EntryMode Mode" in s
    assert "CFIPClean80EntryTriggerState State" in s
    assert "CFIPClean80EntryModel Model" in s
    assert "CFIPClean80PriceLevel RequestedEntry" in s
    assert "bool TriggerReached" in s
    assert "bool Eligible" in s
    assert "DateTime? ExpiresUtc" in s


def test_phase7_separates_ideal_entry_trigger_requested_entry():
    s = read(V80)
    model = s[s.index("public sealed class CFIPClean80EntryModel"):
               s.index("public sealed class CFIPClean80EntrySnapshot")]
    snapshot = s[s.index("public sealed class CFIPClean80EntrySnapshot"):
                  s.index("public sealed class CFIPClean80EntryTriggerEngine")]

    assert "IdealEntry" in model
    assert "EntryZone" in model
    assert "Trigger" in model
    assert "Invalidation" in model
    assert "RequestedEntry" in snapshot
    assert "TriggerReached" in snapshot
    assert "Eligible" in snapshot
    assert "ActualFill" not in snapshot


def test_phase7_uses_decision_direction_as_single_authority():
    e = entry_engine(read(V80))
    assert "CFIPClean80Direction direction = decision.Direction;" in e
    assert "direction = m5.BiasDirection" not in e
    assert "direction = m1.BiasDirection" not in e
    assert "direction = structure.CurrentStructureDirection" not in e


def test_phase7_consumes_the_exact_host_decision_snapshot():
    s = read(V80)
    assert "_entryTriggerEngine.Evaluate(" in s
    assert "_state.Decision," in s
    assert s.count("_decisionEngine.Evaluate(") == 1
    assert s.count("_entryTriggerEngine.Evaluate(") == 1


def test_phase7_entry_engine_has_no_broker_authority():
    e = entry_engine(read(V80))
    forbidden = (
        "ExecuteMarketOrder",
        "PlaceStopOrder",
        "PlaceLimitOrder",
        "CancelPendingOrder",
        "ModifyPosition",
        "ModifyPendingOrder",
        "ClosePosition",
    )
    assert not any(token in e for token in forbidden)


def test_phase7_entry_engine_has_no_chart_or_ui_authority():
    e = entry_engine(read(V80))
    forbidden = (
        "Chart.Draw",
        "Chart.RemoveObject",
        "Chart.DrawText",
        "Button",
        "Grid",
        "StackPanel",
    )
    assert not any(token in e for token in forbidden)


def test_phase7_has_retest_path():
    e = entry_engine(read(V80))
    assert "TryFindRetestZone(" in e
    assert "CFIPClean80EntryMode.RetestMarket" in e
    assert "CFIPClean80EntryTriggerState.Ready" in e
    assert "RETEST_READY" in e
    assert "RequireRetestCloseConfirmation" in e
    assert "RetestRejectionBodyAtr" in e
    assert "RetestZoneToleranceAtr" in e


def test_phase7_has_breakout_trigger_path():
    e = entry_engine(read(V80))
    assert "TryFindFreshBreakoutEvent(" in e
    assert "PrecisionBreakoutBufferAtr" in e
    assert "breakoutAnchor" in e
    assert "breakoutZone.Zone.CurrentUpper" in e
    assert "breakoutZone.Zone.CurrentLower" in e
    assert "MinimumTriggerBodyAtr" in e
    assert "MaximumTriggerRangeAtr" in e
    assert "MinimumCloseLocation" in e
    assert "CFIPClean80EntryMode.BreakoutMarket" in e
    assert "CFIPClean80EntryTriggerState.WaitingBreakout" in e
    assert "triggerReached" in e
    assert "BREAKOUT_TRIGGER_REACHED" in e


def test_phase7_has_continuation_and_reversal_pending_modes_without_broker_mutation():
    e = entry_engine(read(V80))
    assert "CFIPClean80EntryMode.ContinuationStop" in e
    assert "CFIPClean80EntryMode.ReversalLimit" in e
    assert "PendingOrderMode" in e
    assert "PlaceStopOrder" not in e
    assert "PlaceLimitOrder" not in e


def test_phase7_has_spread_aware_entry_gate():
    s = read(V80)
    engine = s[s.index("public sealed class CFIPClean80EntryTriggerEngine"):
                s.index("public sealed class CFIPClean80ExecutionIdentity")]
    assert "UseSpreadFilter" in engine
    assert "MaximumSpreadAtr" in engine
    assert "SpreadBlocked" in engine
def test_phase7_has_m1_m5_confirmation_controls():
    e = entry_engine(read(V80))
    assert 'configuration.Get("UseM5Confirmation", true)' in e
    assert 'configuration.Get("UseM1Trigger", false)' in e
    assert 'CFIPClean80BlockReason.MtfDisagreement' in e


def test_phase7_has_fresh_trigger_stale_setup_and_expiry_controls():
    e = entry_engine(read(V80))
    assert "RequireFreshM5Trigger" in e
    assert "MinimumFreshTriggerEvidence" in e
    assert "RetestLookbackBars" in e
    assert "RetestMaxBarsAfterDisplacement" in e
    assert "EntryTriggerState.Expired" in e
    assert "BREAKOUT_SETUP_EXPIRED" in e


def test_phase7_has_setup_invalidation():
    e = entry_engine(read(V80))
    assert 'configuration.Get("EnableSetupInvalidation", true)' in e
    assert "InvalidationZoneCloseAtr" in e
    assert "InvalidationStructureAtr" in e
    assert "CFIPClean80EntryTriggerState.Invalidated" in e


def test_phase7_has_late_entry_and_distance_limits():
    e = entry_engine(read(V80))
    assert "MaximumEntryDistanceAtr" in e
    assert "MaximumEntryExtensionAtr" in e
    assert "AvoidLateEntry" in e
    assert "LATE_BREAKOUT_ENTRY" in e


def test_phase7_state_wiring_persists_entry_snapshot():
    s = read(V80)
    assert "CFIPClean80EntrySnapshot Entry" in s
    assert "public void SetEntry(CFIPClean80EntrySnapshot value)" in s
    assert "Entry = null;" in s
    assert "_state.SetEntry(" in s


def test_phase7_preserves_manual_trade_entry_safety_contract():
    s = read(V80)
    assert "ManualTradeEntryControlsSupported { get { return false; } }" in s
    assert "ExecuteMarketOrder" not in entry_engine(s)
    assert "PlaceStopOrder" not in entry_engine(s)
    assert "PlaceLimitOrder" not in entry_engine(s)


def test_phase7_trigger_evidence_does_not_borrow_generic_market_evidence():
    e = entry_engine(read(V80))
    start = e.index("private bool TryFindFreshBreakoutEvent")
    end = e.index("private bool IsTriggerEvent", start)
    fresh = e[start:end]
    assert "IndependentEvidence" not in fresh
    assert "structuralBreakPresent" in fresh
    assert "displacementPresent" in fresh
    assert "liquiditySweepPresent" in fresh
    assert "triggerEvidence" in fresh


def test_phase7_retest_can_execute_after_confirmed_touch_with_live_distance_gate():
    e = entry_engine(read(V80))
    start = e.index("private bool TryFindRetestZone")
    end = e.index("private ZoneCandidate FindBestExecutionZone", start)
    retest = e[start:end]
    assert "priceInsideExpandedZone" in retest
    assert "bool touched =" in retest
    assert "MaximumEntryDistanceAtr" not in retest
    assert "RequireRetestCloseConfirmation" in retest


def test_phase7_retest_rejects_zone_created_after_reference_bar():
    e = entry_engine(read(V80))
    start = e.index("private bool TryFindRetestZone")
    end = e.index("private ZoneCandidate FindBestExecutionZone", start)
    retest = e[start:end]
    assert "m5.ClosedBarTimeUtc < zone.CreatedUtc" in retest


def test_phase7_breakout_ignores_future_structural_events():
    e = entry_engine(read(V80))
    start = e.index("private bool TryFindFreshBreakoutEvent")
    end = e.index("private bool IsTriggerEvent", start)
    fresh = e[start:end]
    assert "item.TimeUtc > referenceUtc" in fresh


def test_phase7_order_block_retest_uses_canonical_parameter_name():
    e = entry_engine(read(V80))
    assert '"RequireObRetest"' in e
    assert '"RequireOrderBlockRetest"' not in e

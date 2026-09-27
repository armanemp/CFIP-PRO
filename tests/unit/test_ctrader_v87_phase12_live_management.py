import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V86 = ROOT / "integrations/ctrader/calude-edit-v86.cs"
V87 = ROOT / "integrations/ctrader/calude-edit-v87.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def parameters(source: str):
    return re.findall(
        r'\[Parameter\("([^"]+)".*?\)\]\s*\r?\n\s*public\s+[^\s]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}',
        source,
    )


def section(source: str, start: str, end: str) -> str:
    a = source.index(start)
    b = source.index(end, a)
    return source[a:b]


def balanced(source: str) -> bool:
    depth = 0
    in_string = in_char = in_line = in_block = False
    escape = False
    i = 0
    while i < len(source):
        ch = source[i]
        nxt = source[i + 1] if i + 1 < len(source) else ""
        if in_line:
            if ch == "\n":
                in_line = False
            i += 1
            continue
        if in_block:
            if ch == "*" and nxt == "/":
                in_block = False
                i += 2
            else:
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
            in_line = True
            i += 2
            continue
        if ch == "/" and nxt == "*":
            in_block = True
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
    return depth == 0 and not in_string and not in_char and not in_block


def test_v87_preserves_v86_parameter_surface():
    s86 = read(V86)
    s87 = read(V87)
    assert len(parameters(s86)) == 513
    assert len(parameters(s87)) == 513
    assert parameters(s86) == parameters(s87)


def test_v87_is_version_isolated():
    s = read(V87)
    for token in (
        "CFIPClean86", "CFIPClean85", "CFIPClean84", "CFIPClean83",
        "CFIPClean82", "CFIPClean81", "CFIP86|", "CFIP85|",
        "CFIP84|", "CFIP83|", "CFIP82|", "CFIP81|",
        "CFIP-SMART-CLEAN86",
    ):
        assert token not in s
    assert "CFIPClean87" in s
    assert "CFIP87|" in s
    assert "CFIP-SMART-CLEAN87" in s
    assert "CFIP_MTF_LiveEntryEngine_Clean_v87" in s


def test_v87_source_is_balanced_and_uses_platform_time_only():
    s = read(V87)
    assert balanced(s)
    assert "DateTime.UtcNow" not in s
    assert "DateTime.Now" not in s


def test_phase12_has_single_live_position_manager_and_action_contract():
    s = read(V87)
    assert s.count("class CFIPClean87LivePositionManager") == 1
    assert s.count("class CFIPClean87LivePositionAction") == 1
    assert "ProtectionUpdate = 1" in s
    assert "PartialClose = 2" in s
    assert "Close = 3" in s
    assert "Evaluate(" in s


def test_phase12_live_manager_uses_position_plan_identity_not_current_signal_only():
    m = section(read(V87), "public sealed class CFIPClean87LivePositionManager", "// Presentation boundary")
    assert "PlanMatches(" in m
    assert "plan.Identity.PlanId" in m
    assert "position.Comment.IndexOf(" in m
    assert "snapshot.Comment.IndexOf(" in m


def test_phase12_sl_management_is_monotonic_and_broker_distance_aware():
    m = section(read(V87), "private CFIPClean87LivePositionAction EvaluateProtection(", "private bool HasCurrentDirectionalStructure(")
    assert "BetterStop(" in m
    assert "IsBrokerStopPriceValid(" in m
    assert '"MoveSlToBreakEven"' in m
    assert '"EnableStructuralSlRepricing"' in m
    assert '"SmartTrailMinimumRR"' in m
    assert '"MoveToBreakEvenAfterPartial"' in m


def test_phase12_target_management_is_confirmation_safe_and_cadenced():
    s = read(V87)
    m = section(s, "private CFIPClean87LivePositionAction EvaluateProtection(", "private bool HasCurrentDirectionalStructure(")
    assert '"EnableDynamicTpAdvance"' in m
    assert '"UpdateUnhitTargets"' in m
    assert '"StructuralTargetUpdatesOnly"' in m
    assert '"TargetUpdateStepAtr"' in m
    assert "LastTargetRepriceReferenceUtc" in s
    assert "ProtectionPending" in s
    assert "ProtectionObserved(" in s
    proposal = m[m.index('"EnableDynamicTpAdvance"'):]
    assert "context.ActiveTargetStage = next.Stage" not in proposal


def test_phase12_partial_tp_is_broker_gateway_owned_and_volume_safe():
    s = read(V87)
    g = section(s, "public sealed class CFIPClean87CTraderBrokerGateway", "public sealed class CFIPClean87CTraderBrokerStateReader")
    m = section(s, "private CFIPClean87LivePositionAction EvaluatePartialTakeProfit(", "private CFIPClean87LivePositionAction EvaluateProtection(")
    assert "PartialClosePosition(" in g
    assert "_host.ClosePosition(" in g
    assert '"EnablePartialTakeProfit"' in m
    assert '"PartialCloseTp1Percent"' in m
    assert '"PartialCloseTp2Percent"' in m
    assert "VolumeStepInUnits" in m
    assert "MinVolumeInUnits" in m


def test_phase12_forced_exits_cover_invalidation_reversal_exhaustion_and_eod():
    m = section(read(V87), "private CFIPClean87LivePositionAction EvaluateForcedExit(", "private CFIPClean87LivePositionAction EvaluatePartialTakeProfit(")
    for token in (
        '"EnableEndOfDayAutoClose"', '"EnableSetupInvalidation"',
        '"EnableReversalProtectionClose"', '"EnableLiveStructuralReversal"',
        '"EnableProfitExhaustionProtection"', '"ExhaustionRetracementPercent"',
        '"ExhaustionPressureThreshold"',
    ):
        assert token in m


def test_phase12_partial_result_is_confirmation_based_and_retry_safe():
    m = section(read(V87), "public void HandlePartialResult(", "public IReadOnlyList<CFIPClean87LivePositionAction> Evaluate(")
    assert "PartialPending = true" in m
    assert "PartialAcceptedUtc = utc" in m
    assert "PartialRejectAttempts++" in m
    assert "RetryDelay(" in m
    assert "PendingPartialExpectedRemainingVolume" in m
    assert "ConfirmPartialProgress(" in m


def test_phase12_protection_requests_are_deduplicated_until_broker_confirmation():
    s = read(V87)
    lifecycle = section(s, "public sealed class CFIPClean87PositionLifecycleManager", "// Presentation boundary")
    assert "RequestProtectionMutation(" in lifecycle
    assert "RemoveQueuedProtectionAction(" in lifecycle
    live = section(s, "public sealed class CFIPClean87LivePositionManager", "// Presentation boundary")
    assert "HandleProtectionRequested(" in live
    assert "ProtectionPending" in live
    assert "ProtectionObserved(" in live


def test_phase12_close_requests_never_mark_closed_without_broker_confirmation():
    s = read(V87)
    lifecycle = section(s, "public sealed class CFIPClean87PositionLifecycleManager", "// Presentation boundary")
    assert "RequestClose(" in lifecycle
    assert "HandleActionResult(" in lifecycle
    assert "MarkCloseActionAccepted(utc)" in lifecycle
    assert "record.MarkClosed(utc)" in lifecycle
    record = section(s, "public sealed class CFIPClean87PositionRecord", "public sealed class CFIPClean87PositionAction")
    assert "State = CFIPClean87PositionLifecycleState.ExitRequested;" in record


def test_phase12_broker_mutations_remain_gateway_owned():
    s = read(V87)
    gateway = section(s, "public sealed class CFIPClean87CTraderBrokerGateway", "public sealed class CFIPClean87CTraderBrokerStateReader")
    before = s[:s.index("public sealed class CFIPClean87CTraderBrokerGateway")]
    after = s[s.index("public sealed class CFIPClean87CTraderBrokerStateReader"):]
    for token in (
        "_host.ExecuteMarketOrder(", "_host.PlaceStopOrder(", "_host.PlaceLimitOrder(",
        "_host.ModifyPosition(", "_host.ClosePosition(", "_host.CancelPendingOrder(",
        "_host.ModifyPendingOrder(",
    ):
        assert token in gateway
        assert token not in before
        assert token not in after


def test_phase12_disconnect_guard_prevents_live_management_during_broker_outage():
    s = read(V87)
    proc = section(s, "private void ProcessLivePositionManagement()", "private void ProcessPositionLifecycle()")
    assert "!Server.IsConnected" in proc
    assert "_state.Broker.Positions" in proc
    assert "_brokerGateway." in proc


def test_phase12_parameter_key_wiring_has_no_silent_unknown_keys():
    s = read(V87)
    props = set(re.findall(r'\[Parameter\([\s\S]*?\)\]\s*\r?\n\s*public\s+[^\s]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}', s))
    keys = set(re.findall(r'(?:_configuration|configuration)\.Get(?:<[^>]+>)?\(\s*"([^"]+)"', s))
    assert keys <= props


def test_phase12_manual_trade_entry_controls_remain_disabled():
    s = read(V87)
    assert "ManualTradeEntryControlsSupported { get { return false; } }" in s
    assert "BuyButton" not in s
    assert "SellButton" not in s
    assert "BuyMarketButton" not in s
    assert "SellMarketButton" not in s

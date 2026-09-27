import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V82 = ROOT / "integrations" / "ctrader" / "calude-edit-v82.cs"
V83 = ROOT / "integrations" / "ctrader" / "calude-edit-v83.cs"


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


def pending_manager(source: str) -> str:
    start = source.index("public sealed class CFIPClean83PendingOrderLifecycleManager")
    end = source.index("// Presentation boundary", start)
    return source[start:end]


def gateway(source: str) -> str:
    start = source.index("public sealed class CFIPClean83CTraderBrokerGateway")
    end = source.index("public sealed class CFIPClean83CTraderBrokerStateReader", start)
    return source[start:end]


def host(source: str) -> str:
    start = source.index("public class CFIP_MTF_LiveEntryEngine_Clean_v83")
    return source[start:]


def test_v83_preserves_v82_parameter_surface():
    s82 = read(V82)
    s83 = read(V83)
    assert len(parameters(s82)) == 513
    assert len(parameters(s83)) == 513
    assert parameters(s82) == parameters(s83)


def test_v83_is_version_isolated():
    s = read(V83)
    assert "CFIPClean82" not in s
    assert "CFIPClean81" not in s
    assert "calude-edit-v82" not in s
    assert "CFIP-PRO-v83" in s
    assert "CFIP_MTF_LiveEntryEngine_Clean_v83" in s


def test_v83_braces_are_balanced():
    assert csharp_braces_balanced(read(V83))


def test_phase10_has_first_class_pending_order_records_and_states():
    m = pending_manager(read(V83))
    assert "CFIPClean83PendingOrderRecord" in m
    assert "PendingOrderLifecycleState" in m
    assert "Pending" in m
    assert "ProtectionRecoveryRequired" in m
    assert "Filled" in m
    assert "Cancelled" in m
    assert "Reconciled" in m


def test_phase10_subscribes_to_all_pending_order_events():
    h = host(read(V83))
    assert "PendingOrders.Created += PendingOrders_Created" in h
    assert "PendingOrders.Modified += PendingOrders_Modified" in h
    assert "PendingOrders.Filled += PendingOrders_Filled" in h
    assert "PendingOrders.Cancelled += PendingOrders_Cancelled" in h


def test_phase10_filled_event_owns_position_transition():
    h = host(read(V83))
    assert "PendingOrderFilledEventArgs" in h
    assert "args.PendingOrder" in h
    assert "args.Position" in h
    assert "PENDING_FILLED_TO_POSITION" in h
    assert "LifecycleState.LivePosition" in h


def test_phase10_cancelled_event_reconciles_instead_of_declaring_closed_blindly():
    h = host(read(V83))
    start = h.index("private void PendingOrders_Cancelled")
    end = h.index("private void ProcessPendingOrderLifecycle", start)
    section = h[start:end]
    assert "args.Reason.ToString()" not in section
    assert "ReconcileBrokerState();" in section


def test_phase10_pending_expiry_and_daily_loss_actions():
    m = pending_manager(read(V83))
    assert "EXPECTED_EXPIRY" in m
    assert "DAILY_LOSS_LIMIT" in m
    assert "PendingOrderActionKind.Cancel" in m
    assert "DrainActions()" in m


def test_phase10_missing_pending_protection_is_explicit():
    m = pending_manager(read(V83))
    assert "PENDING_OR_FILLED_PROTECTION_MISSING" in m
    assert "RestoreProtection" in m
    assert "MarkProtectionRecoveryRequired" in m


def test_phase10_filled_position_missing_protection_is_queued_for_recovery():
    m = pending_manager(read(V83))
    start = m.index("public void HandleFilled")
    end = m.index("public void HandleCancelled", start)
    section = m[start:end]
    assert "!position.StopLoss.HasValue" in section
    assert "!position.TakeProfit.HasValue" in section
    assert "QueueProtectionRecoveryForRecord(record)" in section


def test_phase10_pending_protection_mutation_stays_inside_gateway():
    g = gateway(read(V83))
    assert "ModifyPendingProtection(" in g
    assert "_host.ModifyPendingOrder(" in g
    assert "PENDING_PROTECTION_MODIFIED" in g


def test_phase10_event_handlers_do_not_directly_mutate_broker():
    h = host(read(V83))
    callbacks = h[h.index("private void PendingOrders_Created"):
                 h.index("private void ProcessPendingOrderLifecycle")]
    forbidden = (
        "ExecuteMarketOrder(",
        "PlaceStopOrder(",
        "PlaceLimitOrder(",
        "ModifyPosition(",
        "ClosePosition(",
        "CancelPendingOrder(",
        "ModifyPendingProtection(",
    )
    assert not any(token in callbacks for token in forbidden)


def test_phase10_actions_use_gateway_for_broker_mutation():
    h = host(read(V83))
    start = h.index("private void ProcessPendingOrderLifecycle")
    end = h.index("private bool IsDailyLossLimitBreached", start)
    section = h[start:end]
    assert "_brokerGateway.CancelPendingOrder(" in section
    assert "_brokerGateway.ModifyPendingProtection(" in section


def test_phase10_existing_pending_orders_are_adopted_at_startup():
    h = host(read(V83))
    assert "foreach (var order in PendingOrders)" in h
    assert "_pendingOrderLifecycle.RegisterExisting(" in h
    assert "DateTime.MinValue" in h


def test_phase10_submitted_pending_orders_receive_expected_identity_and_protection():
    h = host(read(V83))
    assert "_pendingOrderLifecycle.RegisterSubmitted(" in h
    assert "intent.Identity" in h
    assert "intent.StopLoss" in h
    assert "intent.EffectiveTarget" in h


def test_phase10_pending_manager_is_label_scoped():
    m = pending_manager(read(V83))
    assert "_managedLabel" in m
    assert "order.Label" in m
    assert "StringComparison.Ordinal" in m


def test_phase10_identity_is_recoverable_from_pending_comment():
    m = pending_manager(read(V83))
    assert "ParseIdentity(" in m
    assert "CFIP83|SIGNAL|" in m
    assert "CFIP83|PLAN|" in m


def test_phase10_gateway_contract_exposes_pending_protection_mutation():
    s = read(V83)
    start = s.index("public interface ICFIPClean83BrokerGateway")
    end = s.index("// Phase-4 engine shell", start)
    interface = s[start:end]
    assert "ModifyPendingProtection(" in interface


def test_phase10_no_manual_entry_controls_are_introduced():
    s = read(V83)
    assert "ManualTradeEntryControlsSupported { get { return false; } }" in s
    assert "BuyButton" not in s
    assert "SellButton" not in s


def test_phase10_pending_record_carries_filled_position_identity():
    s = read(V83)
    m = pending_manager(s)
    assert "BrokerPositionId" in m
    assert "MarkFilled(" in m
    assert "BrokerPositionId =" in m
    host = s[s.index("public class CFIP_MTF_LiveEntryEngine_Clean_v83"):]
    assert "args.Position" in host
    assert "HandleFilled(" in host


def test_phase10_restart_reconciliation_can_match_filled_position_by_plan_identity():
    m = pending_manager(read(V83))
    assert "ReconcileBrokerState(" in m
    assert "FindMatchingPosition(" in m
    assert "position.Comment" in m
    assert "record.PlanId" in m
    assert "record.MarkFilled(" in m


def test_phase10_reconciliation_marks_unseen_pending_as_reconciled_when_no_position_matches():
    m = pending_manager(read(V83))
    start = m.index("public void ReconcileBrokerState")
    end = m.index("public void Evaluate", start)
    section = m[start:end]
    assert "record.MarkReconciled()" in section
    assert "activeOrderIds.Contains(record.BrokerOrderId)" in section


def test_phase10_broker_position_snapshot_retains_comment_for_restart_identity():
    s = read(V83)
    assert "public string Comment" in s
    assert "position.Comment" in s


def test_phase10_execution_configuration_keys_exist_on_canonical_parameter_surface():
    s = read(V83)
    properties = {
        name
        for name in re.findall(
            r'\[Parameter\("[^"]+".*?\)\]\s*\r?\n\s*public\s+[^\s]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}',
            s,
        )
    }
    keys = set(re.findall(r'configuration\.Get\(\s*"([^"]+)"\s*,', s))
    assert len(properties) == 513
    assert keys <= properties
    assert len(keys) >= 100


def test_phase10_gateway_persists_parseable_cfip83_identity_comment():
    s = read(V83)
    assert '"CFIP83|SIGNAL|" +' in s
    assert '"|PLAN|" +' in s
    assert '"CFIP-SMART-CLEAN83"' in s
    assert '"CFIP-SMART-CLEAN82"' not in s


def test_phase10_identity_parser_returns_values_not_marker_prefixes():
    m = pending_manager(read(V83))
    start = m.index("private void ParseIdentity")
    end = m.index("private bool IsManaged", start)
    section = m[start:end]
    assert "signalStart + signalMarker.Length" in section
    assert "planStart + planMarker.Length" in section
    assert ".Trim('|')" in section


def test_phase10_plan_identity_is_v83_end_to_end():
    s = read(V83)
    assert "return\n            \"CFIP83|PLAN|\"" in s
    assert "return\n            \"CFIP83|SIGNAL|\"" in s
    assert "CFIP81|PLAN|" not in s
    assert "CFIP81|SIGNAL|" not in s
    assert "CFIP82|EXEC|" not in s


def test_phase10_stale_pending_orders_are_cancelled_on_plan_supersession():
    m = pending_manager(read(V83))
    assert "InvalidateSupersededPlan(" in m
    assert "PLAN_SUPERSEDED" in m
    assert "string.Equals(" in m
    h = host(read(V83))
    assert "_pendingOrderLifecycle.InvalidateSupersededPlan(" in h
    assert "IsExecutionDuplicate(_state.Plan)" in h


def test_phase10_stale_order_without_plan_identity_is_not_automatically_cancelled():
    m = pending_manager(read(V83))
    start = m.index("public void InvalidateSupersededPlan")
    end = m.index("public void Evaluate", start)
    section = m[start:end]
    assert "string.IsNullOrWhiteSpace(record.PlanId)" in section
    assert "continue;" in section


def test_phase10_pending_protection_uses_single_absolute_gateway_mutation():
    g = gateway(read(V83))
    start = g.index("public CFIPClean83ExecutionResult ModifyPendingProtection")
    end = g.index("public CFIPClean83ExecutionResult CancelPendingOrder", start)
    section = g[start:end]
    assert "_host.ModifyPendingOrder(" in section
    assert "ProtectionType.Absolute" in section
    assert "order.ExpirationTime" in section
    assert "ModifyStopLossPrice" not in section
    assert "ModifyTakeProfitPrice" not in section

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V86 = ROOT / "integrations/ctrader/calude-edit-v86.cs"
V87 = ROOT / "integrations/ctrader/calude-edit-v87.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def section(source: str, start: str, end: str | None = None) -> str:
    a = source.index(start)
    if end is None:
        return source[a:]
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


def test_v87_is_version_isolated_from_v86():
    s = read(V87)
    for token in ("CFIPClean86", "CFIP86|", "CFIP-SMART-CLEAN86", "v86"):
        assert token not in s
    assert "CFIPClean87" in s
    assert "CFIP87|" in s
    assert "CFIP-SMART-CLEAN87" in s
    assert "CFIP_MTF_LiveEntryEngine_Clean_v87" in s


def test_v87_source_is_balanced_and_has_no_malformed_if_continuation():
    s = read(V87)
    assert balanced(s)
    assert "DateTime.UtcNow" not in s
    assert "DateTime.Now" not in s
    assert not re.search(r"&&\s*\r?\n\s*if\s*\(", s)


def test_v87_pending_position_identity_storage_is_complete():
    s = read(V87)
    pending = section(
        s,
        "public sealed class CFIPClean87PendingOrderRecord",
        "public sealed class CFIPClean87PendingOrderAction",
    )
    assert "private readonly List<string> _brokerPositionIds" in pending
    assert "public IReadOnlyList<string> BrokerPositionIds" in pending


def test_v87_execution_idempotency_state_is_declared_and_used():
    s = read(V87)
    main = section(
        s,
        "public class CFIP_MTF_LiveEntryEngine_Clean_v87",
    )
    assert "private readonly HashSet<string> _submittedExecutionKeys" in main
    assert "_submittedExecutionKeys.Add(" in main
    assert "IdempotencyKey" in main


def test_v87_final_gateway_protection_validation_covers_all_mutation_paths():
    s = read(V87)
    gateway = section(
        s,
        "public sealed class CFIPClean87CTraderBrokerGateway",
        "public sealed class CFIPClean87CTraderBrokerStateReader",
    )
    assert gateway.count("ValidateProtectionLevels(") >= 3
    assert "ModifyPosition(" in gateway
    assert "ModifyPendingProtection(" in gateway
    assert "MinimumDistancePips(" in gateway


def test_v87_protection_validation_is_price_direction_and_distance_aware():
    s = read(V87)
    gateway = section(
        s,
        "public sealed class CFIPClean87CTraderBrokerGateway",
        "public sealed class CFIPClean87CTraderBrokerStateReader",
    )
    assert "PROTECTION_DIRECTION_INVALID" in gateway
    assert "TARGET_DIRECTION_INVALID" in gateway
    assert "BROKER_STOP_DISTANCE_INVALID" in gateway
    assert "BROKER_TARGET_DISTANCE_INVALID" in gateway
    assert "SymbolMinDistanceType.Pips" in gateway


def test_v87_exposure_gate_counts_positions_and_pending_orders_together():
    s = read(V87)
    policy = section(
        s,
        "public sealed class CFIPClean87ExecutionPolicy",
        "public sealed class CFIPClean87ExecutionPlanner",
    )
    assert "int managedExposure =" in policy
    assert "runtime.ManagedPositionCount +" in policy
    assert "runtime.ManagedPendingOrderCount" in policy
    assert "ExistingExposureBlocked" in policy


def test_v87_execution_policy_requires_complete_trade_identity():
    s = read(V87)
    policy = section(
        s,
        "public sealed class CFIPClean87ExecutionPolicy",
        "public sealed class CFIPClean87ExecutionPlanner",
    )
    assert "plan.Identity == null" in policy
    assert 'plan.Identity.SignalId' in policy
    assert 'plan.Identity.PlanId' in policy
    assert "DataIncomplete" in policy


def test_v87_broker_confirmation_grace_prevents_false_close_race():
    s = read(V87)
    main = section(
        s,
        "public class CFIP_MTF_LiveEntryEngine_Clean_v87",
    )
    assert "_awaitingBrokerConfirmations" in main
    assert "BrokerConfirmationGrace" in main
    assert "RegisterBrokerConfirmation(" in main
    assert "HasPendingBrokerConfirmation(" in main
    assert "BROKER_CONFIRMATION_TIMEOUT" in main
    assert "!awaitingConfirmation" in main


def test_v87_event_handlers_are_runtime_null_safe():
    s = read(V87)
    main = section(
        s,
        "public class CFIP_MTF_LiveEntryEngine_Clean_v87",
    )
    assert "_state != null" in main
    assert "_state.Runtime != null" in main
    assert "if (args == null ||" in main


def test_v87_pending_actions_feed_results_back_into_lifecycle():
    s = read(V87)
    pending = section(
        s,
        "public sealed class CFIPClean87PendingOrderLifecycleManager",
        "public sealed class CFIPClean87LivePositionAction",
    )
    main = section(
        s,
        "public class CFIP_MTF_LiveEntryEngine_Clean_v87",
    )
    assert "HandleActionResult(" in pending
    assert "IsCancelRetryDue(" in pending
    assert "IsProtectionRetryDue(" in pending
    assert "RetryDelay(" in pending
    assert "_pendingOrderLifecycle.HandleActionResult(" in main


def test_v87_position_protection_detects_drift_not_only_missing_values():
    s = read(V87)
    lifecycle = section(
        s,
        "public sealed class CFIPClean87PositionLifecycleManager",
        "public sealed class CFIPClean87PositionAction",
    )
    assert "stopDrift" in lifecycle
    assert "targetDrift" in lifecycle
    assert "PricesMatch(" in lifecycle
    assert "POSITION_PROTECTION_DRIFT" in lifecycle


def test_v87_lifecycle_can_adopt_existing_broker_position_or_pending_order():
    s = read(V87)
    lifecycle = section(
        s,
        "public sealed class CFIPClean87LifecycleManager",
        "public interface ICFIPClean87OutcomeRecorder",
    )
    assert "to == CFIPClean87LifecycleState.PendingOrder" in lifecycle
    assert "to == CFIPClean87LifecycleState.LivePosition" in lifecycle
    assert "to == CFIPClean87LifecycleState.RecoveryRequired" in lifecycle


def test_v87_protection_queue_cleanup_uses_the_exact_dedup_key_prefix():
    s = read(V87)
    lifecycle = section(
        s,
        "public sealed class CFIPClean87PositionLifecycleManager",
        "public sealed class CFIPClean87PositionAction",
    )
    assert "string prefix =" in lifecycle
    assert "RestoreProtection.ToString()" in lifecycle
    assert "key.StartsWith(" in lifecycle

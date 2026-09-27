import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V85 = ROOT / "integrations" / "ctrader" / "calude-edit-v85.cs"
V86 = ROOT / "integrations" / "ctrader" / "calude-edit-v86.cs"


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


def test_v86_preserves_v85_parameter_surface():
    assert len(parameters(read(V85))) == 513
    assert len(parameters(read(V86))) == 513
    assert parameters(read(V85)) == parameters(read(V86))


def test_v86_isolation():
    s = read(V86)
    for token in (
        "CFIPClean85",
        "CFIPClean84",
        "CFIPClean83",
        "CFIPClean82",
        "CFIPClean81",
        "CFIP85|",
        "CFIP84|",
        "CFIP83|",
        "CFIP82|",
        "CFIP81|",
        "CFIP-SMART-CLEAN85",
        "CFIP-SMART-CLEAN84",
    ):
        assert token not in s
    assert "CFIPClean86" in s
    assert "CFIP86|" in s
    assert "CFIP-SMART-CLEAN86" in s
    assert "CFIP_MTF_LiveEntryEngine_Clean_v86" in s


def test_v86_source_is_balanced_and_uses_platform_time_authority():
    s = read(V86)
    assert balanced(s)
    assert "DateTime.UtcNow" not in s
    assert "DateTime.Now" not in s


def test_v86_pending_fill_does_not_mutate_consumed_pending_order():
    s = read(V86)
    filled = section(
        s,
        "public void HandleFilled(",
        "public void HandleCancelled(",
    )
    assert "record.MarkFilled(utc, position.Id.ToString())" in filled
    assert "QueueProtectionRecoveryForRecord(record)" not in filled

    reconcile = section(
        s,
        "public void ReconcileBrokerState(",
        "public IReadOnlyList<CFIPClean86PendingOrderRecord> Records",
    )
    assert "FindMatchingPositions(" in reconcile
    assert "QueueProtectionRecoveryForRecord(record)" not in reconcile


def test_v86_pending_order_supports_multiple_resulting_positions():
    s = read(V86)
    record = section(
        s,
        "public sealed class CFIPClean86PendingOrderRecord",
        "public sealed class CFIPClean86PendingOrderAction",
    )
    assert "IReadOnlyList<string> BrokerPositionIds" in record
    assert "_brokerPositionIds.Contains(id)" in record


def test_v86_position_close_is_result_aware_and_confirmation_based():
    s = read(V86)
    manager = section(
        s,
        "public sealed class CFIPClean86PositionLifecycleManager",
        "// Presentation boundary",
    )
    assert "public bool RequestClose(" in manager
    assert "public void HandleActionResult(" in manager
    assert "record.MarkCloseActionAccepted(utc)" in manager
    assert "record.MarkCloseActionFailed(utc)" in manager
    assert "record.MarkClosed(utc)" in manager

    record = section(
        s,
        "public sealed class CFIPClean86PositionRecord",
        "public sealed class CFIPClean86PositionAction",
    )
    assert "State = CFIPClean86PositionLifecycleState.ExitRequested;" in record
    assert "_closeActionPending = false;" in record


def test_v86_protection_retry_uses_exponential_backoff_state():
    s = read(V86)
    record = section(
        s,
        "public sealed class CFIPClean86PositionRecord",
        "public sealed class CFIPClean86PositionAction",
    )
    assert "_protectionAttempts" in record
    assert "_nextProtectionRetryUtc" in record
    assert "RetryDelay(_protectionAttempts)" in record

    manager = section(
        s,
        "public sealed class CFIPClean86PositionLifecycleManager",
        "// Presentation boundary",
    )
    assert "QueueProtectionIfDue(" in manager
    assert "EvaluateRetryActions(" in manager
    assert "MarkProtectionActionAccepted" in manager
    assert "MarkProtectionActionFailed" in manager


def test_v86_disconnect_never_reconciles_or_mutates_from_stale_broker_snapshot():
    s = read(V86)
    assert "if (_brokerStateReader == null ||" in s
    assert "!Server.IsConnected" in s

    pos_processor = section(
        s,
        "private void ProcessPositionLifecycle()",
        "private void ProcessPendingOrderLifecycle()",
    )
    assert "!Server.IsConnected" in pos_processor

    pending_processor = section(
        s,
        "private void ProcessPendingOrderLifecycle()",
        "private void ProcessPositionLifecycle()",
    )
    assert "if (_pendingOrderLifecycle == null ||" in pending_processor
    assert "_state.Runtime == null" in pending_processor


def test_v86_pending_to_position_protection_handoff_uses_expected_plan_values():
    s = read(V86)
    process = section(
        s,
        "private void ProcessPositionLifecycle()",
        "private void ProcessPendingOrderLifecycle()",
    )
    assert "pending.ExpectedStopLoss" in process
    assert "pending.ExpectedTakeProfit" in process
    assert "pending.BrokerPositionIds" in process
    assert "_positionLifecycle.RegisterExpectedProtection(" in process


def test_v86_position_broker_mutations_remain_gateway_owned():
    s = read(V86)
    gateway = section(
        s,
        "public sealed class CFIPClean86CTraderBrokerGateway",
        "public sealed class CFIPClean86CTraderBrokerStateReader",
    )
    outside = s[:s.index("public sealed class CFIPClean86CTraderBrokerGateway")]
    outside += s[s.index("public sealed class CFIPClean86CTraderBrokerStateReader"):]
    for token in (
        "_host.ModifyPosition(",
        "_host.ClosePosition(",
        "_host.CancelPendingOrder(",
        "_host.ModifyPendingOrder(",
    ):
        assert token not in outside
    assert "_host.ModifyPosition(" in gateway
    assert "_host.ClosePosition(" in gateway


def test_v86_gateway_interface_calls_align():
    s = read(V86)
    gateway = section(
        s,
        "public sealed class CFIPClean86CTraderBrokerGateway",
        "public sealed class CFIPClean86CTraderBrokerStateReader",
    )
    interface = section(
        s,
        "public interface ICFIPClean86BrokerGateway",
        "public interface ICFIPClean86BrokerStateReader",
    )
    calls = set(re.findall(r"_brokerGateway\.(\w+)\s*\(", s))
    methods = set(
        re.findall(
            r"public\s+CFIPClean86ExecutionResult\s+(\w+)\s*\(",
            gateway,
        )
    )
    iface = set(
        re.findall(
            r"CFIPClean86ExecutionResult\s+(\w+)\s*\(",
            interface,
        )
    )
    assert calls <= methods
    assert methods == iface
    assert "ModifyPositionProtection" not in s

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V84 = ROOT / "integrations" / "ctrader" / "calude-edit-v84.cs"
V85 = ROOT / "integrations" / "ctrader" / "calude-edit-v85.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def parameters(source: str):
    return re.findall(
        r'\[Parameter\("([^"]+)".*?\)\]\s*\r?\n\s*public\s+[^\s]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}',
        source,
    )


def balanced(source: str) -> bool:
    depth = 0
    in_string = False
    in_char = False
    in_line = False
    in_block = False
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


def section(source: str, start_marker: str, end_marker: str) -> str:
    start = source.index(start_marker)
    end = source.index(end_marker, start)
    return source[start:end]


def test_v85_preserves_v84_parameter_surface():
    assert len(parameters(read(V84))) == 513
    assert len(parameters(read(V85))) == 513
    assert parameters(read(V84)) == parameters(read(V85))


def test_v85_is_fully_version_isolated():
    s = read(V85)
    for old in ("CFIPClean84", "CFIPClean83", "CFIPClean82", "CFIPClean81"):
        assert old not in s
    for old in ("CFIP84|", "CFIP83|", "CFIP82|", "CFIP81|", "CFIP79|"):
        assert old not in s
    assert "CFIPClean85" in s
    assert "CFIP85|" in s
    assert "CFIP-SMART-CLEAN85" in s
    assert "CFIP-SMART-CLEAN66" not in s


def test_v85_source_is_balanced():
    assert balanced(read(V85))


def test_v85_gateway_interface_and_host_calls_align():
    s = read(V85)
    gateway = section(
        s,
        "public sealed class CFIPClean85CTraderBrokerGateway",
        "public sealed class CFIPClean85CTraderBrokerStateReader",
    )
    interface = section(
        s,
        "public interface ICFIPClean85BrokerGateway",
        "public interface ICFIPClean85BrokerStateReader",
    )
    calls = set(re.findall(r"_brokerGateway\.(\w+)\s*\(", s))
    methods = set(
        re.findall(
            r"public\s+CFIPClean85ExecutionResult\s+(\w+)\s*\(",
            gateway,
        )
    )
    interface_methods = set(
        re.findall(
            r"CFIPClean85ExecutionResult\s+(\w+)\s*\(",
            interface,
        )
    )
    assert calls <= methods
    assert methods == interface_methods
    assert "ModifyPositionProtection" not in s
    assert "ModifyProtection" in calls


def test_v85_pending_fill_rebases_with_broker_position_identity():
    s = read(V85)
    pending = section(
        s,
        "public void HandleFilled(",
        "public void HandleCancelled(",
    )
    assert "record.MarkFilled(utc, position.Id.ToString())" in pending


def test_v85_position_expected_protection_has_valid_mutation_boundary():
    s = read(V85)
    assert "public void SetExpectedProtection(" in s
    manager = section(
        s,
        "public sealed class CFIPClean85PositionLifecycleManager",
        "// Presentation boundary",
    )
    assert "record.ExpectedStopLoss =" not in manager
    assert "record.ExpectedTakeProfit =" not in manager
    assert "record.SetExpectedProtection(" in manager


def test_v85_broker_state_reader_is_current_identity_scoped():
    s = read(V85)
    reader = section(
        s,
        "public sealed class CFIPClean85CTraderBrokerStateReader",
        "// Phase 10 — first-class Pending Order lifecycle",
    )
    assert "HasCurrentIdentity(position.Comment)" in reader
    assert "HasCurrentIdentity(order.Comment)" in reader
    assert '"CFIP85|"' in reader


def test_v85_runtime_counts_and_daily_loss_use_current_identity_scope():
    s = read(V85)
    host = section(
        s,
        "public class CFIP_MTF_LiveEntryEngine_Clean_v85",
        "    }\n}",
    )
    assert "HasCurrentIdentity(position.Comment)" in host
    assert "HasCurrentIdentity(order.Comment)" in host
    assert "HasCurrentIdentity(trade.Comment)" in host


def test_v85_target_ladder_validates_contiguous_stages_and_entry_side():
    s = read(V85)
    start = s.index("public sealed class CFIPClean85TargetLadder")
    end = s.index("// ------------------------------------------------------------------------\n    // Runtime / time snapshots", start)
    ladder = s[start:end]
    assert "ValidateForDirection(" in ladder
    assert "double referencePrice" in ladder
    assert "if ((int)item.Stage != i + 1)" in ladder
    assert "current <= referencePrice" in ladder
    assert "current >= referencePrice" in ladder


def test_v85_pending_execution_rejects_expired_or_wrong_side_orders():
    s = read(V85)
    policy = section(
        s,
        "public sealed class CFIPClean85ExecutionPolicy",
        "public sealed class CFIPClean85ExecutionPlanner",
    )
    assert "entry.ExpiresUtc.Value <= runtime.ServerUtc" in policy
    assert "validPendingSide" in policy


def test_v85_broker_gateway_checks_protection_and_target_direction():
    s = read(V85)
    gateway = section(
        s,
        "public sealed class CFIPClean85CTraderBrokerGateway",
        "public sealed class CFIPClean85CTraderBrokerStateReader",
    )
    assert "PROTECTION_DIRECTION_INVALID" in gateway
    assert "TARGET_DIRECTION_INVALID" in gateway


def test_v85_no_direct_broker_mutation_outside_gateway():
    s = read(V85)
    gateway_start = s.index(
        "public sealed class CFIPClean85CTraderBrokerGateway"
    )
    gateway_end = s.index(
        "public sealed class CFIPClean85CTraderBrokerStateReader",
        gateway_start,
    )
    outside = s[:gateway_start] + s[gateway_end:]
    for token in (
        "_host.ExecuteMarketOrder(",
        "_host.PlaceStopOrder(",
        "_host.PlaceLimitOrder(",
        "_host.ModifyPosition(",
        "_host.ClosePosition(",
        "_host.CancelPendingOrder(",
        "_host.ModifyPendingOrder(",
    ):
        assert token not in outside


def test_v85_manual_trade_entry_controls_remain_unsupported():
    s = read(V85)
    assert "ManualTradeEntryControlsSupported { get { return false; } }" in s
    assert "BuyButton" not in s
    assert "SellButton" not in s
    assert "StopButton" not in s
    assert "LimitButton" not in s

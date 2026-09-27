import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V83 = ROOT / "integrations" / "ctrader" / "calude-edit-v83.cs"
V84 = ROOT / "integrations" / "ctrader" / "calude-edit-v84.cs"


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


def manager(source: str) -> str:
    start = source.index("public sealed class CFIPClean84PositionLifecycleManager")
    end = source.index("// Presentation boundary", start)
    return source[start:end]


def host(source: str) -> str:
    start = source.index("public class CFIP_MTF_LiveEntryEngine_Clean_v84")
    return source[start:]


def gateway(source: str) -> str:
    start = source.index("public sealed class CFIPClean84CTraderBrokerGateway")
    end = source.index("public sealed class CFIPClean84CTraderBrokerStateReader", start)
    return source[start:end]


def test_v84_preserves_v83_parameter_surface():
    assert len(parameters(read(V83))) == 513
    assert len(parameters(read(V84))) == 513
    assert parameters(read(V83)) == parameters(read(V84))


def test_v84_isolated_from_older_identity_versions():
    s = read(V84)
    assert "CFIPClean83" not in s
    assert "CFIPClean82" not in s
    assert "CFIPClean81" not in s
    assert "CFIP83|" not in s
    assert "CFIP82|" not in s
    assert "CFIP81|" not in s
    assert "CFIP84|" in s
    assert "CFIP-SMART-CLEAN84" in s


def test_v84_source_is_balanced():
    assert balanced(read(V84))


def test_phase11_position_manager_has_explicit_states():
    s = read(V84)
    record = s[
        s.index("public sealed class CFIPClean84PositionRecord"):
        s.index("public sealed class CFIPClean84PositionAction")
    ]
    manager_source = manager(s)
    for state in (
        "Adopted",
        "Protected",
        "ProtectionRecoveryRequired",
        "ExitRequested",
        "Closed",
        "Reconciled",
        "Orphan",
        "RecoveryRequired",
    ):
        assert state in record or state in manager_source


def test_phase11_subscribes_to_position_events():
    h = host(read(V84))
    assert "Positions.Opened += Positions_Opened" in h
    assert "Positions.Modified += Positions_Modified" in h
    assert "Positions.Closed += Positions_Closed" in h
    assert "PositionOpenedEventArgs" in h
    assert "PositionModifiedEventArgs" in h
    assert "PositionClosedEventArgs" in h


def test_phase11_startup_adopts_all_managed_positions():
    h = host(read(V84))
    assert "foreach (var position in Positions)" in h
    assert "_positionLifecycle.RegisterOpened(" in h


def test_phase11_restart_reconciliation_does_not_clear_live_broker_owner():
    m = manager(read(V84))
    start = m.index("public void ReconcileBrokerState")
    end = m.index("public void HandleModified", start)
    section = m[start:end]
    assert "activeIds.Contains(record.BrokerPositionId)" in section
    assert "record.MarkReconciled()" in section


def test_phase11_orphan_position_is_explicit_and_not_auto_closed():
    m = manager(read(V84))
    assert "record.MarkOrphan()" in m
    assert "CFIPClean84PositionActionKind.Close" not in m[
        m.index("if (string.IsNullOrWhiteSpace(record.PlanId))"):
        m.index("VerifyProtectionSnapshot", m.index("if (string.IsNullOrWhiteSpace(record.PlanId))"))
    ]


def test_phase11_protection_drift_is_recovery_state():
    m = manager(read(V84))
    assert "BROKER_PROTECTION_DRIFT" in m
    assert "POSITION_PROTECTION_MISSING" in m
    assert "RestoreProtection" in m


def test_phase11_position_mutation_remains_gateway_owned():
    g = gateway(read(V84))
    assert "ModifyPositionProtection(" in g
    assert "_host.ModifyPosition(" in g
    h = host(read(V84))
    callbacks = h[h.index("private void Positions_Opened"):
                 h.index("private void ProcessPositionLifecycle")]
    assert "_host.ModifyPosition(" not in callbacks
    assert "_host.ClosePosition(" not in callbacks


def test_phase11_position_processing_uses_gateway_result():
    h = host(read(V84))
    start = h.index("private void ProcessPositionLifecycle")
    section = h[start:h.index("private void ProcessPendingOrderLifecycle", start)]
    assert "_brokerGateway.ModifyPositionProtection(" in section
    assert "POSITION_PROTECTION_RECOVERY_FAILED_" in section


def test_phase11_filled_pending_rebases_position_identity():
    h = host(read(V84))
    start = h.index("private void PendingOrders_Filled")
    end = h.index("private void Positions_Opened", start)
    section = h[start:end]
    assert "_positionLifecycle.RegisterOpened(" in section
    assert "args.PendingOrder.StopLoss" in section
    assert "args.PendingOrder.TakeProfit" in section

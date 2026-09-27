from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[2]
V88 = ROOT / "integrations" / "ctrader" / "calude-edit-v88.cs"
V89 = ROOT / "integrations" / "ctrader" / "calude-edit-v89.cs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def section(source: str, start: str, end: str) -> str:
    a = source.index(start)
    b = source.index(end, a)
    return source[a:b]


def balanced(source: str) -> bool:
    depth = 0
    for char in source:
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth < 0:
                return False
    return depth == 0


def parameter_names(source: str) -> set[str]:
    return set(
        re.findall(
            r'\[Parameter\([\s\S]*?\)\]\s*'
            r'public\s+[^\s]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}',
            source,
        )
    )


def test_v89_source_is_balanced_and_isolated_from_v88():
    source = read(V89)
    assert balanced(source)
    assert "CFIPClean88" not in source
    assert "CFIP88|" not in source
    assert "CFIP_MTF_LiveEntryEngine_Clean_v88" not in source
    assert re.search(
        r'class\s+CFIP_MTF_LiveEntryEngine_Clean_v89\b',
        source,
    )


def test_v89_preserves_public_parameter_surface():
    assert parameter_names(read(V89)) == parameter_names(read(V88))


def test_v89_main_dependencies_are_declared():
    source = read(V89)
    start = source.index(
        "public class CFIP_MTF_LiveEntryEngine_Clean_v89",
    )
    main = source[start:]
    names = set(re.findall(r"\b_[A-Za-z][A-Za-z0-9_]*\b", main))
    declared = set(
        re.findall(
            r"\b(_[A-Za-z][A-Za-z0-9_]*)\s*(?:=|;|,|\))",
            main,
        )
    )
    assert sorted(names - declared) == []


def test_v89_protection_request_replacement_clears_queued_state():
    record = section(
        read(V89),
        "public sealed class CFIPClean89PositionRecord",
        "public sealed class CFIPClean89PositionLifecycleManager",
    )
    lifecycle = section(
        read(V89),
        "public sealed class CFIPClean89PositionLifecycleManager",
        "// Phase 12 — first-class Live Position Management",
    )
    assert "ResetProtectionActionQueue()" in record
    assert "private bool RemoveQueuedProtectionAction(" in lifecycle
    assert "return removed;" in lifecycle
    assert "if (RemoveQueuedProtectionAction(" in lifecycle
    assert "record.ResetProtectionActionQueue();" in lifecycle


def test_v89_protection_queue_reports_actual_enqueue_result():
    lifecycle = section(
        read(V89),
        "public sealed class CFIPClean89PositionLifecycleManager",
        "// Phase 12 — first-class Live Position Management",
    )
    assert "private bool QueueProtectionIfDue(" in lifecycle
    assert "return false;" in lifecycle
    assert "return true;" in lifecycle
    assert "return QueueProtectionIfDue(" in lifecycle


def test_v89_restart_can_restore_plan_owned_missing_protection():
    live = section(
        read(V89),
        "public sealed class CFIPClean89LivePositionManager",
        "public class CFIP_MTF_LiveEntryEngine_Clean_v89",
    )
    assert "A matching live Plan is the authoritative source" in live
    assert "plan.StructuralStop.Price" in live
    assert "GetManagedTargetPrice(" in live


def test_v89_partial_close_confirmation_timeout_is_retryable():
    live = section(
        read(V89),
        "public sealed class CFIPClean89LivePositionManager",
        "public class CFIP_MTF_LiveEntryEngine_Clean_v89",
    )
    assert "context.PartialPending = false;" in live
    assert "context.PartialRejectAttempts++;" in live
    assert "RetryDelay(" in live
    assert "context.NextPartialRetryUtc =" in live
    assert "context.ManagementRecoveryRequired = true;" in live


def test_v89_close_confirmation_remains_confirmation_based():
    lifecycle = section(
        read(V89),
        "public sealed class CFIPClean89PositionLifecycleManager",
        "// Phase 12 — first-class Live Position Management",
    )
    assert "State = CFIPClean89PositionLifecycleState.ExitRequested;" in lifecycle
    assert "record.MarkCloseActionAccepted(utc)" in lifecycle
    assert "record.MarkClosed(utc)" in lifecycle
    assert "_nextCloseRetryUtc = utc + TimeSpan.FromSeconds(5);" in lifecycle


def test_v89_gateway_retains_final_protection_validation_on_all_mutations():
    gateway = section(
        read(V89),
        "public sealed class CFIPClean89CTraderBrokerGateway",
        "public sealed class CFIPClean89CTraderBrokerStateReader",
    )
    assert gateway.count("ValidateProtectionLevels(") >= 3
    assert "_host.ModifyPosition(" in gateway
    assert "_host.ModifyPendingOrder(" in gateway


def test_v89_no_manual_entry_surface_and_no_direct_broker_mutation_outside_gateway():
    source = read(V89)
    assert "BuyButton" not in source
    assert "SellButton" not in source
    assert "ShowTradeActionButtons" not in source

    start = source.index("public sealed class CFIPClean89CTraderBrokerGateway")
    end = source.index("public sealed class CFIPClean89CTraderBrokerStateReader")
    outside_gateway = source[:start] + source[end:]

    for token in (
        "_host.ExecuteMarketOrder(",
        "_host.PlaceStopOrder(",
        "_host.PlaceLimitOrder(",
        "_host.ModifyPosition(",
        "_host.ModifyPendingOrder(",
        "_host.ClosePosition(",
        "_host.CancelPendingOrder(",
    ):
        assert token not in outside_gateway


def test_v89_pending_order_still_supports_multi_position_fill_ownership():
    pending = section(
        read(V89),
        "public sealed class CFIPClean89PendingOrderRecord",
        "public sealed class CFIPClean89PendingOrderAction",
    )
    assert "private readonly List<string> _brokerPositionIds" in pending
    assert "public IReadOnlyList<string> BrokerPositionIds" in pending
    assert "_brokerPositionIds.Contains(id)" in pending


def test_v89_live_position_protection_uses_current_market_side():
    gateway = section(
        read(V89),
        "public sealed class CFIPClean89CTraderBrokerGateway",
        "public sealed class CFIPClean89CTraderBrokerStateReader",
    )
    assert "ValidateLivePositionProtection(" in gateway
    assert "double stopAnchor =" in gateway
    assert "double targetAnchor =" in gateway
    assert "position.EntryPrice" not in section(
        gateway,
        "public CFIPClean89ExecutionResult ModifyProtection(",
        "public CFIPClean89ExecutionResult PartialClosePosition(",
    )


def test_v89_unprotected_managed_positions_are_not_dropped_from_live_context():
    live = section(
        read(V89),
        "public sealed class CFIPClean89LivePositionManager",
        "public class CFIP_MTF_LiveEntryEngine_Clean_v89",
    )
    assert "InitialRiskPrice = risk" in live
    assert "ManagementRecoveryRequired =" in live
    assert "if (risk <= 0)\n                return;" not in live


def test_v89_gateway_enforces_managed_object_ownership_before_mutation():
    gateway = section(
        read(V89),
        "public sealed class CFIPClean89CTraderBrokerGateway",
        "public sealed class CFIPClean89CTraderBrokerStateReader",
    )
    assert "private bool IsManagedPosition(" in gateway
    assert "private bool IsManagedPendingOrder(" in gateway
    assert gateway.count("POSITION_NOT_MANAGED") >= 3
    assert gateway.count("PENDING_ORDER_NOT_MANAGED") >= 2


def test_v89_initializes_state_and_lifecycle_before_broker_events():
    source = read(V89)
    state_pos = source.index(
        "_state = new CFIPClean89EngineState();",
        source.index("protected override void Initialize()"),
    )
    lifecycle_pos = source.index(
        "_lifecycle = new CFIPClean89LifecycleManager();",
        source.index("protected override void Initialize()"),
    )
    event_pos = source.index(
        "PendingOrders.Created += PendingOrders_Created;",
        source.index("protected override void Initialize()"),
    )
    assert state_pos < event_pos
    assert lifecycle_pos < event_pos

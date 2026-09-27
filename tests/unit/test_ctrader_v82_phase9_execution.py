import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
V81 = ROOT / "integrations" / "ctrader" / "calude-edit-v81.cs"
V82 = ROOT / "integrations" / "ctrader" / "calude-edit-v82.cs"


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


def phase9(source: str) -> str:
    start = source.index("public sealed class CFIPClean82ExecutionReadiness")
    end = source.index("// Presentation boundary", start)
    return source[start:end]


def gateway(source: str) -> str:
    start = source.index("public sealed class CFIPClean82CTraderBrokerGateway")
    end = source.index("public sealed class CFIPClean82CTraderBrokerStateReader", start)
    return source[start:end]


def host(source: str) -> str:
    start = source.index("public class CFIP_MTF_LiveEntryEngine_Clean_v82")
    return source[start:]


def test_v82_preserves_v81_parameter_surface():
    s81 = read(V81)
    s82 = read(V82)
    assert len(parameters(s81)) == 513
    assert len(parameters(s82)) == 513
    assert parameters(s81) == parameters(s82)


def test_v82_is_version_isolated():
    s = read(V82)
    assert "CFIPClean81" not in s
    assert "CFIPClean80" not in s
    assert "calude-edit-v81" not in s
    assert "CFIP-PRO-v82" in s
    assert "CFIP_MTF_LiveEntryEngine_Clean_v82" in s


def test_v82_braces_are_balanced():
    assert csharp_braces_balanced(read(V82))


def test_phase9_has_single_execution_policy_and_gateway():
    s = read(V82)
    assert s.count("class CFIPClean82ExecutionPolicy") == 1
    assert s.count("class CFIPClean82CTraderBrokerGateway") == 1
    assert s.count("class CFIPClean82ExecutionPlanner") == 1


def test_phase9_execution_uses_exact_decision_entry_and_plan_chain():
    s = read(V82)
    e = phase9(s)
    assert "entry.Decision != decision" in e
    assert "entry.Mode != plan.EntryMode" in e
    assert "entry.Direction != plan.Direction" in e
    h = host(s)
    assert "_state.SetDecision(" in h
    assert "_state.SetEntry(" in h
    assert "_state.SetPlan(" in h
    assert h.count("_decisionEngine.Evaluate(") == 1
    assert h.count("_entryTriggerEngine.Evaluate(") == 1
    assert h.count("_tradePlanBuilder.Build(") == 1
    assert h.count("TryExecuteCurrentCycle();") == 1


def test_phase9_execution_anchor_is_used_for_sizing_and_risk():
    s = read(V82)
    h = host(s)
    assert "plan.ExecutionAnchor.Price" in h
    assert "VolumeForProportionalRisk" in h
    assert "AmountRisked" in h
    assert "Symbol.QuantityToVolumeInUnits" in h
    assert "Symbol.NormalizeVolumeInUnits" in h


def test_phase9_estimated_margin_is_a_hard_guard():
    s = read(V82)
    assert "EstimatedMargin" in s
    assert "FreeMargin" in s
    assert "MaxAutoMarginUsagePercent" in s
    assert "estimatedMargin" in s
def test_phase9_risk_budget_is_explicit():
    e = phase9(read(V82))
    assert "riskBudget" in e
    assert "riskAmount > riskBudget * 1.01" in e
    assert "RiskPercentEquity" in e


def test_phase9_market_and_pending_auto_gates_are_distinct():
    e = phase9(read(V82))
    assert '"EnableAutoTrading"' in e
    assert '"EnableAutomaticOrders"' in e
    assert "ExecutionKind.Market" in e
    assert "ExecutionKind.Stop" in e
    assert "ExecutionKind.Limit" in e
    assert "PolicyBlocked" in e


def test_phase9_pending_orders_carry_plan_identity_comment():
    g = gateway(read(V82))
    assert "intent.TradeIdentity.SignalId" in g
    assert "intent.TradeIdentity.PlanId" in g
    assert "PlaceStopOrder(" in g
    assert "PlaceLimitOrder(" in g
    assert "ProtectionType.Relative" in g
    assert "intent.ExpiryUtc" in g
    assert "comment" in g


def test_phase9_broker_mutation_isolated_to_gateway():
    s = read(V82)
    g = gateway(s)
    forbidden_outside = (
        "ExecuteMarketOrder",
        "PlaceStopOrder",
        "PlaceLimitOrder",
        "ModifyPosition",
        "ClosePosition",
        "CancelPendingOrder",
    )
    before = s[:s.index("public sealed class CFIPClean82CTraderBrokerGateway")]
    after = s[s.index("public sealed class CFIPClean82CTraderBrokerStateReader"):]
    assert all(token not in before for token in forbidden_outside)
    # State-reader/host code may contain method names in contracts, but no
    # direct broker mutation should occur outside the gateway implementation.
    assert not any(
        token in after
        for token in (
            "_host.ExecuteMarketOrder(",
            "_host.PlaceStopOrder(",
            "_host.PlaceLimitOrder(",
            "_host.ModifyPosition(",
            "_host.ClosePosition(",
            "_host.CancelPendingOrder(",
        )
    )
    assert "ExecuteMarketOrder(" in g
    assert "PlaceStopOrder(" in g
    assert "PlaceLimitOrder(" in g
    assert "ModifyPosition(" in g
    assert "ClosePosition(" in g
    assert "CancelPendingOrder(" in g


def test_phase9_actual_fill_and_protection_state_are_broker_derived():
    s = read(V82)
    g = gateway(s)
    assert "ActualFill" in s
    assert "TradeResult" in g
    assert "EntryPrice" in g
    assert "StopLoss" in g
    assert "TakeProfit" in g
    assert "ProtectionState" in g
def test_phase9_idempotency_checks_local_and_broker_state():
    s = read(V82)
    assert "History.FindAll" in s
    assert "Positions" in s
    assert "PendingOrders" in s
    assert "PlanId" in s
    assert "duplicate" in s.lower()
def test_phase9_lifecycle_tracks_execution_result():
    h = host(read(V82))
    assert "SignalDetected" in h
    assert "PlanReady" in h
    assert "ExecutionReady" in h
    assert "LivePosition" in h
    assert "PendingOrder" in h
    assert "Rejected" in h
    assert "BROKER_MARKET_ACCEPTED" in h
    assert "BROKER_PENDING_ACCEPTED" in h


def test_phase9_broker_reconciliation_can_confirm_closure():
    h = host(read(V82))
    assert "ReadManagedState(" in h
    assert "BROKER_OBJECTS_ABSENT" in h
    assert "LifecycleState.LivePosition" in h
    assert "LifecycleState.PendingOrder" in h
    assert "LifecycleState.Closed" in h


def test_phase9_execution_result_has_explicit_status_detail():
    s = read(V82)
    assert "string StatusDetail" in s
    assert "string statusDetail = \"\"" in s
    assert "MARKET_EXECUTED" in s
    assert "PENDING_ORDER_ACCEPTED" in s
    assert "EXECUTION_FAILED" in s


def test_phase9_indicator_has_no_manual_entry_controls():
    s = read(V82)
    assert "ManualTradeEntryControlsSupported { get { return false; } }" in s
    assert "BuyButton" not in s
    assert "SellButton" not in s


def test_phase9_plan_carries_authoritative_execution_anchor():
    s = read(V82)
    plan = s[s.index("public sealed class CFIPClean82TradePlan"):
               s.index("// Phase 8", s.index("public sealed class CFIPClean82TradePlan"))]
    assert "ExecutionAnchor" in plan
    assert "PLAN_EXECUTION_ANCHOR" in s


def test_phase9_planner_interface_matches_implementation():
    s = read(V82)
    planner_interface = s[
        s.index("public interface ICFIPClean82ExecutionPlanner"):
        s.index("public sealed class CFIPClean82ExecutionPlanner")
    ]
    planner = s[s.index("public sealed class CFIPClean82ExecutionPlanner"):
                s.index("public sealed class CFIPClean82CTraderBrokerGateway")]
    assert "CFIPClean82EntrySnapshot" in planner_interface
    assert "ExecutionIntent" in planner_interface
    assert "CreateIntent(" in planner
def test_phase9_runtime_snapshot_carries_pip_size_and_account_margin_inputs():
    s = read(V82)
    assert "double PipSize" in s
    assert "double Balance" in s
    assert "double Margin" in s
    assert "double MarginLevel" in s
    assert "TradingDayStartUtc" in s
    assert "Math.Max(0, Symbol.PipSize)" in host(s)


def test_phase9_broker_constraints_cover_sl_tp_and_volume():
    e = phase9(read(V82))
    assert "MinStopDistancePips" in e
    assert "MinTakeProfitDistancePips" in e
    assert "MinVolumeInUnits" in e
    assert "VolumeStepInUnits" in e
    assert "BrokerConstraintsBlocked" in e


def test_phase9_session_and_friday_guards_are_policy_owned():
    e = phase9(read(V82))
    assert '"UseSessionFilter"' in e
    assert '"SessionStartUtc"' in e
    assert '"SessionEndUtc"' in e
    assert '"AvoidFridayLateEntry"' in e
    assert '"FridayCutoffUtc"' in e
    assert "IsWithinConfiguredSession" in e
    assert "SessionBlocked" in e


def test_phase9_daily_history_uses_label_symbol_scoped_find_all():
    h = host(read(V82))
    assert "History.FindAll(" in h
    assert '"OneOrderPerSignal"' in h
    assert '"AutoTradeLabel"' in h


def test_phase9_pending_stop_and_limit_both_persist_identity_comment():
    g = gateway(read(V82))
    assert g.count("ProtectionType.Relative") >= 2
    assert g.count("intent.ExpiryUtc") >= 2
    assert g.count("comment") >= 3


def test_phase9_requested_tp_stage_is_not_silently_replaced():
    s = read(V82)
    planner = s[s.index("public sealed class CFIPClean82ExecutionPlanner"):
                 s.index("public sealed class CFIPClean82CTraderBrokerGateway")]
    assert 'configuration.Get(' in planner
    assert "AutoTpStage" in planner
    assert "if (target == null)" in planner
    assert "Requested execution target stage is unavailable." in planner


def test_phase9_protection_recovery_failure_enters_recovery_state():
    h = host(read(V82))
    assert "result.ReconciliationRequired" in h
    assert "CFIPClean82ExecutionResult protectionResult" in h
    assert "BROKER_PROTECTION_RECOVERY_FAILED" in h
    assert "CFIPClean82LifecycleState.RecoveryRequired" in h
    assert "_state.Execution = protectionResult" in h

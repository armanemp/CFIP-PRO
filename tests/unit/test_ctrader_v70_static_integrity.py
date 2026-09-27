from pathlib import Path
import re


REPO_ROOT = Path(__file__).resolve().parents[2]
V69 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v69.cs"
V70 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v70.cs"


def read_source(path: Path) -> str:
    assert path.is_file(), f"Missing cTrader source: {path}"
    return path.read_text(encoding="utf-8")


def extract_method(source: str, method_name: str) -> str:
    match = re.search(
        rf"(?m)^\s*(?:private|public|protected|internal)\s+[^\n]+\b"
        rf"{re.escape(method_name)}\s*\(",
        source,
    )
    assert match, f"Method not found: {method_name}"

    opening = source.find("{", match.end())
    assert opening >= 0, f"Method body not found: {method_name}"

    depth = 0
    state = "code"

    for index in range(opening, len(source)):
        char = source[index]
        nxt = source[index + 1] if index + 1 < len(source) else ""

        if state == "line":
            if char == "\n":
                state = "code"
            continue

        if state == "block":
            if char == "*" and nxt == "/":
                state = "code"
            continue

        if state == "string":
            if char == "\\":
                continue
            if char == '"':
                state = "code"
            continue

        if char == "/" and nxt == "/":
            state = "line"
            continue

        if char == "/" and nxt == "*":
            state = "block"
            continue

        if char == '"':
            state = "string"
            continue

        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[match.start() : index + 1]

    raise AssertionError(f"Unbalanced method body: {method_name}")


def direct_call(method: str, name: str) -> bool:
    return re.search(
        rf"(?<!Try)\b{re.escape(name)}\s*\(",
        method,
    ) is not None


def test_v69_baseline_and_v70_are_both_preserved() -> None:
    v69 = read_source(V69)
    v70 = read_source(V70)

    assert "CFIP_MTF_LiveEntryEngine_Clean_v69" in v69
    assert "CFIP_MTF_LiveEntryEngine_Clean_v70" in v70


def test_v70_runtime_toggles_do_not_mutate_parameters() -> None:
    v70 = read_source(V70)

    runtime_setters = "\n".join(
        [
            extract_method(v70, "SetAutoTradingRuntimeState"),
            extract_method(v70, "SetAutomaticOrdersRuntimeState"),
        ]
    )

    assert "EnableAutoTrading = enabled" not in runtime_setters
    assert "EnableAutomaticOrders = enabled" not in runtime_setters
    assert "_autoTradingEnabledRuntime = enabled" in runtime_setters
    assert "_automaticOrdersEnabledRuntime = enabled" in runtime_setters


def test_v70_external_configuration_changes_are_detected_explicitly() -> None:
    v70 = read_source(V70)
    ensure = extract_method(v70, "EnsureExecutionRuntimeState")

    assert "_lastConfiguredAutoTrading" in ensure
    assert "_lastConfiguredAutomaticOrders" in ensure
    assert "EnableAutoTrading != _lastConfiguredAutoTrading" in ensure
    assert "EnableAutomaticOrders !=" in ensure


def test_v70_monitor_timeout_cannot_terminate_live_plan() -> None:
    v70 = read_source(V70)
    monitor = extract_method(v70, "MonitorOutcome")

    assert "_outcomeTelemetryTimedOut = true" in monitor
    assert "_plan = null" not in monitor
    assert "remains under live management" in monitor


def test_v70_partial_tp_requires_success_before_hit_state() -> None:
    v70 = read_source(V70)

    execute_partial = extract_method(v70, "ExecutePartialClose")
    assert "closeResult.IsSuccessful" in execute_partial
    assert "return false" in execute_partial

    evaluate = extract_method(v70, "EvaluateActivePlan")
    assert "tp1Processed" in evaluate
    assert "tp2Processed" in evaluate
    assert "_tp1Hit = 1" in evaluate
    assert "_tp2Hit = 1" in evaluate


def test_v70_live_exit_is_broker_authoritative() -> None:
    v70 = read_source(V70)

    gateway = extract_method(v70, "RequestLivePlanExit")
    assert "SetLifecycleState" in gateway
    assert "TryClosePosition" in gateway
    assert "ExitRequested" in gateway
    assert "RecoveryRequired" in gateway

    evaluate = extract_method(v70, "EvaluateActivePlan")
    sl_start = evaluate.index("if (hitSl &&")
    tp1_start = evaluate.index("if (hitTp1 &&", sl_start)
    tp4_start = evaluate.index("if (hitTp4 &&")
    reversal_start = evaluate.index(
        "if (CheckLiveReversalAgainstPlan(",
        tp4_start,
    )

    assert "_plan = null" not in evaluate[sl_start:tp1_start]
    assert "_plan = null" not in evaluate[tp4_start:reversal_start]

    exhaustion = extract_method(v70, "CheckProfitExhaustionExit")
    reversal = extract_method(v70, "CheckLiveReversalAgainstPlan")
    assert not direct_call(exhaustion, "ClosePosition")
    assert not direct_call(reversal, "ClosePosition")


def test_v70_cleanup_and_reversal_are_result_aware() -> None:
    v70 = read_source(V70)

    reversal = extract_method(v70, "CheckReversalProtection")
    cleanup = extract_method(v70, "CleanupPendingOrdersIfNeeded")

    assert "TryClosePosition" in reversal
    assert not direct_call(reversal, "ClosePosition")

    assert "TryCancelPendingOrder" in cleanup
    assert not direct_call(cleanup, "CancelPendingOrder")


def test_v70_broker_mutations_have_single_authority_gateway() -> None:
    v70 = read_source(V70)

    assert v70.count(".ModifyStopLossPrice(") == 1
    assert v70.count(".ModifyTakeProfitPrice(") == 1

    assert "private bool TryModifyStopLoss(" in v70
    assert "private bool TryModifyTakeProfit(" in v70
    assert "TradeResult result" in extract_method(v70, "TryModifyStopLoss")
    assert "TradeResult result" in extract_method(v70, "TryModifyTakeProfit")


def test_v70_authoritative_broker_events_are_wired() -> None:
    v70 = read_source(V70)

    required_events = [
        "Positions.Opened += OnPositionOpened;",
        "Positions.Modified += OnPositionModified;",
        "Positions.Closed += OnPositionClosed;",
        "PendingOrders.Created += OnPendingOrderCreated;",
        "PendingOrders.Modified += OnPendingOrderModified;",
        "PendingOrders.Filled += OnPendingOrderFilled;",
        "PendingOrders.Cancelled += OnPendingOrderCancelled;",
    ]

    for event in required_events:
        assert event in v70

    assert "private void OnPositionModified(" in v70
    assert "private void OnPendingOrderCreated(" in v70
    assert "private void OnPendingOrderModified(" in v70
    assert "private void OnPendingOrderCancelled(" in v70


def test_v70_broker_close_event_owns_final_outcome_counting() -> None:
    v70 = read_source(V70)
    closed = extract_method(v70, "OnPositionClosed")

    assert "if (!_outcomeRegistered)" in closed
    assert "if (EnableOutcomeTelemetry)" in closed
    assert "_wins++" in closed
    assert "_losses++" in closed


def test_v70_has_balanced_braces() -> None:
    v70 = read_source(V70)

    stripped = re.sub(r"//.*$", "", v70, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)

    assert stripped.count("{") == stripped.count("}")


def test_v70_restores_c_trader_indicator_initialize_lifecycle() -> None:
    v70 = read_source(V70)

    assert re.search(
        r"protected\s+override\s+void\s+Initialize\s*\(\)",
        v70,
    )
    assert v70.count("protected override void Initialize()") == 1
    assert v70.count("public override void Calculate(int index)") == 1



def test_v71_mtf_context_uses_one_closed_reference() -> None:
    v71 = read_source(
        REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
    )

    assert "class MtfClosedContext" in v71
    assert "BuildMtfClosedContext(" in v71
    assert "MapM5ToClosedChart(" in v71
    assert "BuildDecision(" in v71
    assert "MapM5ToClosedChart(\n                        closedM5,\n                        index)" in v71


def test_v71_closed_frame_contract_rejects_forming_bar() -> None:
    v71 = read_source(
        REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
    )
    analyze = extract_method(v71, "AnalyzeFrame")

    assert "index >= bars.Count - 1" in analyze


def test_v71_closed_trigger_has_no_live_price_dependency() -> None:
    v71 = read_source(
        REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
    )

    closed_trigger = extract_method(v71, "ClosedBarTriggerReady")

    assert "Symbol.Ask" not in closed_trigger
    assert "Symbol.Bid" not in closed_trigger
    assert "index >= bars.Count - 1" in closed_trigger

    assert "EntryTriggerReady" not in v71
    assert "TriggerReadyWithoutPrecisionGate" not in v71


def test_v71_chart_confluence_cannot_use_forming_bar() -> None:
    v71 = read_source(
        REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
    )
    live_bias = extract_method(v71, "LiveBias")

    assert "chartIndex >= Bars.Count - 1" in live_bias


def test_v71_runtime_clock_uses_ctrader_server_time() -> None:
    v71 = read_source(
        REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
    )

    assert "DateTime.UtcNow" not in v71
    assert "DateTime.Now" not in v71
    assert "TimeInUtc" in v71


def test_v71_d1_w1_frame_gates_match_analyze_contract() -> None:
    v71 = read_source(
        REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
    )

    assert "d1Index >= 30" in v71
    assert "w1Index >= 30" in v71
    assert "d1Index >= 10" not in v71
    assert "w1Index >= 10" not in v71



def test_v71_decision_policy_is_explicit_and_named() -> None:
    v71 = read_source(
        REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
    )

    assert "enum CFIPClean71DecisionPolicyMode" in v71
    for mode in ["Confirmed", "Soft", "Aggressive", "Pending"]:
        assert f"CFIPClean71DecisionPolicyMode.{mode}" in v71

    assert "ResolveDecisionPolicy(" in v71
    assert "allowUnconfirmedAutoPlan" not in v71
    assert "private bool ShouldCreatePlan(" in v71
    assert "CFIPClean71DecisionPolicyMode policy" in v71

from pathlib import Path
import re


REPO_ROOT = Path(__file__).resolve().parents[2]
V74 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v74.cs"


def read_source() -> str:
    assert V74.is_file(), f"Missing cTrader v74 foundation: {V74}"
    return V74.read_text(encoding="utf-8")


def test_v74_has_canonical_direction_contract() -> None:
    source = read_source()

    assert "enum CFIPClean74Direction" in source
    assert "Sell = -1" in source
    assert "Wait = 0" in source
    assert "Buy = 1" in source


def test_v74_has_explicit_entry_model_terms() -> None:
    source = read_source()

    for name in [
        "IdealEntry",
        "EntryZone",
        "Trigger",
        "Invalidation",
        "RequestedEntry",
        "ActualFill",
    ]:
        assert name in source


def test_v74_separates_plan_intent_result_and_broker_state() -> None:
    source = read_source()

    for name in [
        "CFIPClean74TradePlan",
        "CFIPClean74ExecutionIntent",
        "CFIPClean74ExecutionResult",
        "CFIPClean74BrokerStateSnapshot",
    ]:
        assert name in source


def test_v74_has_single_lifecycle_authority() -> None:
    source = read_source()

    assert "class CFIPClean74LifecycleManager" in source
    assert "TryTransition(" in source
    assert "private CFIPClean74LifecycleState _state" in source
    assert source.count("private CFIPClean74LifecycleState _state") == 1


def test_v74_has_idempotency_identity() -> None:
    source = read_source()

    assert "class CFIPClean74ExecutionIdentity" in source
    assert "IdempotencyKey" in source
    assert "IdempotencyKey is mandatory" in source


def test_v74_target_ladder_is_directionally_validated() -> None:
    source = read_source()

    assert "class CFIPClean74TargetLadder" in source
    assert "ValidateForDirection(" in source
    assert "current <= previous" in source
    assert "current >= previous" in source


def test_v74_execution_envelope_splits_entry_constraints() -> None:
    source = read_source()

    for name in [
        "MaxEntryChaseAtr",
        "MaxBrokerSlippagePips",
        "MaxBreakoutFillDeviationPips",
        "MaxPlanRebaseDistancePips",
    ]:
        assert name in source


def test_v74_broker_mutation_is_behind_gateway_contract() -> None:
    source = read_source()

    assert "interface ICFIPClean74BrokerGateway" in source

    forbidden_direct_calls = [
        "ExecuteMarketOrder(",
        "PlaceStopOrder(",
        "PlaceLimitOrder(",
        "ClosePosition(",
        "CancelPendingOrder(",
        "ModifyStopLossPrice(",
        "ModifyTakeProfitPrice(",
    ]

    assert "ICFIPClean74BrokerGateway" in source

    for token in forbidden_direct_calls:
        if token in source:
            assert source.count(token) == 1
            assert "interface ICFIPClean74BrokerGateway" in source


def test_v74_has_no_chart_authority() -> None:
    source = read_source()

    assert "Chart." not in source
    assert "Draw" not in source
    assert "CreatePanel" not in source


def test_v74_has_canonical_service_boundaries() -> None:
    source = read_source()

    for name in [
        "ICFIPClean74DecisionEngine",
        "ICFIPClean74TradePlanBuilder",
        "ICFIPClean74ExecutionPlanner",
        "ICFIPClean74BrokerGateway",
        "ICFIPClean74BrokerStateReader",
        "ICFIPClean74OutcomeRecorder",
        "ICFIPClean74PresentationProjector",
    ]:
        assert name in source


def test_v74_indicator_host_is_orchestration_shell_only() -> None:
    source = read_source()

    assert "CFIP_MTF_LiveEntryEngine_Clean_v74" in source
    assert source.count("protected override void Initialize()") == 1
    assert source.count("public override void Calculate(int index)") == 1


def test_v74_braces_are_balanced() -> None:
    source = read_source()

    stripped = re.sub(r"//.*$", "", source, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)

    assert stripped.count("{") == stripped.count("}")


def test_v74_has_no_v73_manual_entry_ui_or_trading_side_effects() -> None:
    source = read_source()

    for token in [
        "TradeActionButtons",
        "ExecuteMarketOrder(",
        "PlaceStopOrder(",
        "PlaceLimitOrder(",
        "CloseAllPositions(",
        "CancelAllOrders(",
    ]:
        assert token not in source

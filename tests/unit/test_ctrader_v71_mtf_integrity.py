from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[2]
V70 = ROOT / "integrations" / "ctrader" / "calude-edit-v70.cs"
V71 = ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"


def read(path: Path) -> str:
    assert path.is_file(), path
    return path.read_text(encoding="utf-8")


def method(source: str, name: str) -> str:
    match = re.search(
        rf"(?m)^\s*(?:private|public|protected|internal)\s+[^\n]+\b"
        rf"{re.escape(name)}\s*\(",
        source,
    )
    assert match, name
    opening = source.find("{", match.end())
    assert opening >= 0

    depth = 0
    state = "code"
    for i in range(opening, len(source)):
        ch = source[i]
        nxt = source[i + 1] if i + 1 < len(source) else ""

        if state == "line":
            if ch == "\n":
                state = "code"
            continue
        if state == "block":
            if ch == "*" and nxt == "/":
                state = "code"
                continue
            continue
        if state == "string":
            if ch == "\\":  # static scanner; escaped chars are skipped
                continue
            if ch == '"':
                state = "code"
            continue
        if state == "char":
            if ch == "\\":  # static scanner
                continue
            if ch == "'":
                state = "code"
            continue

        if ch == "/" and nxt == "/":
            state = "line"
            continue
        if ch == "/" and nxt == "*":
            state = "block"
            continue
        if ch == '"':
            state = "string"
            continue
        if ch == "'":
            state = "char"
            continue
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return source[match.start() : i + 1]

    raise AssertionError(f"unbalanced method: {name}")


def balanced(source: str) -> bool:
    # Reuse a lightweight lexical scan so braces inside strings/comments
    # do not create false failures.
    depth = 0
    state = "code"
    for i, ch in enumerate(source):
        nxt = source[i + 1] if i + 1 < len(source) else ""
        if state == "line":
            if ch == "\n":
                state = "code"
            continue
        if state == "block":
            if ch == "*" and nxt == "/":
                state = "code"
            continue
        if state == "string":
            if ch == "\\":
                continue
            if ch == '"':
                state = "code"
            continue
        if state == "char":
            if ch == "\\":
                continue
            if ch == "'":
                state = "code"
            continue
        if ch == "/" and nxt == "/":
            state = "line"
            continue
        if ch == "/" and nxt == "*":
            state = "block"
            continue
        if ch == '"':
            state = "string"
            continue
        if ch == "'":
            state = "char"
            continue
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth < 0:
                return False
    return depth == 0 and state == "code"


def test_v71_isolated_from_v70() -> None:
    v70, v71 = read(V70), read(V71)
    assert "CFIP_MTF_LiveEntryEngine_Clean_v70" in v70
    assert "CFIP_MTF_LiveEntryEngine_Clean_v71" in v71
    assert not re.search(r"CFIPClean70|CLEAN70|CFIP_CLEAN70_|Clean_v70|\bv70\b", v71)


def test_v71_has_real_ctrader_initialize_override() -> None:
    v71 = read(V71)
    assert v71.count("protected override void Initialize()") == 1
    assert v71.count("public override void Calculate(int index)") == 1


def test_v71_closed_index_is_time_safe() -> None:
    v71 = read(V71)
    closed = method(v71, "ClosedIndex")
    assert "reference < bars.OpenTimes[0]" in closed
    assert "GetIndexByTime" in closed
    assert "span" in closed
    assert "span <=\n                    TimeSpan.Zero" in closed


def test_v71_mtf_agreement_respects_weight_and_closed_index() -> None:
    v71 = read(V71)
    agreement = method(v71, "TimeframeAgreement")

    for token in (
        "M5Weight",
        "M15Weight",
        "M30Weight",
        "H1Weight",
        "H4Weight",
        "D1Weight",
        "W1Weight",
        "SmartWeeklyContext",
        "ClosedIndex",
        "frames[i].Index != closedIndex",
        "alignedWeight",
        "totalWeight",
    ):
        assert token in agreement

    decision = method(v71, "BuildDecision")
    assert "TimeframeAgreement(\n                    d.Direction,\n                    reference)" in decision


def test_v71_broker_open_event_reconciles_actual_fill() -> None:
    v71 = read(V71)
    opened = method(v71, "OnPositionOpened")
    for token in (
        "position.EntryPrice",
        "_plan.PositionId",
        "_plan.IsLivePosition = true",
        "SetLifecycleState",
    ):
        assert token in opened


def test_v71_broker_events_are_balanced_and_wired() -> None:
    v71 = read(V71)

    for token in (
        "Positions.Opened += OnPositionOpened;",
        "Positions.Modified += OnPositionModified;",
        "Positions.Closed += OnPositionClosed;",
        "PendingOrders.Created += OnPendingOrderCreated;",
        "PendingOrders.Modified += OnPendingOrderModified;",
        "PendingOrders.Filled += OnPendingOrderFilled;",
        "PendingOrders.Cancelled += OnPendingOrderCancelled;",
        "Positions.Opened -= OnPositionOpened;",
        "Positions.Modified -= OnPositionModified;",
        "Positions.Closed -= OnPositionClosed;",
        "PendingOrders.Created -= OnPendingOrderCreated;",
        "PendingOrders.Modified -= OnPendingOrderModified;",
        "PendingOrders.Filled -= OnPendingOrderFilled;",
        "PendingOrders.Cancelled -= OnPendingOrderCancelled;",
    ):
        assert token in v71

    assert balanced(v71)


def test_v71_live_exit_and_protection_have_single_mutation_authority() -> None:
    v71 = read(V71)

    assert v71.count(".ModifyStopLossPrice(") == 1
    assert v71.count(".ModifyTakeProfitPrice(") == 1

    exit_gateway = method(v71, "RequestLivePlanExit")
    assert "TryClosePosition" in exit_gateway
    assert "ExitRequested" in exit_gateway
    assert "RecoveryRequired" in exit_gateway

    active = method(v71, "EvaluateActivePlan")
    sl = active[active.index("if (hitSl &&") : active.index("if (hitTp1 &&")]
    tp4 = active[active.index("if (hitTp4 &&") : active.index("if (CheckLiveReversalAgainstPlan(")]
    assert "_plan = null" not in sl
    assert "_plan = null" not in tp4

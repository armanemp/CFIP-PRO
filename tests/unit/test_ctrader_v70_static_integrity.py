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
        rf"(?m)^\s*(?:private|public|protected|internal)\s+[^\n]+\b{re.escape(method_name)}\s*\(",
        source,
    )
    assert match, f"Method not found: {method_name}"

    opening = source.find("{", match.end())
    assert opening >= 0, f"Method body not found: {method_name}"

    depth = 0
    in_string = False
    escaped = False
    in_line_comment = False
    in_block_comment = False

    for index in range(opening, len(source)):
        char = source[index]
        nxt = source[index + 1] if index + 1 < len(source) else ""

        if in_line_comment:
            if char == "\n":
                in_line_comment = False
            continue

        if in_block_comment:
            if char == "*" and nxt == "/":
                in_block_comment = False
            continue

        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue

        if char == "/" and nxt == "/":
            in_line_comment = True
            continue

        if char == "/" and nxt == "*":
            in_block_comment = True
            continue

        if char == '"':
            in_string = True
            continue

        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[match.start() : index + 1]

    raise AssertionError(f"Unbalanced method body: {method_name}")


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
    assert re.search(
        r"\bprivate\s+bool\s+ExecutePartialClose\s*\(",
        execute_partial,
    )

    assert "closeResult.IsSuccessful" in execute_partial
    assert "return false" in execute_partial

    evaluate = extract_method(v70, "EvaluateActivePlan")
    assert "tp1Processed" in evaluate
    assert "tp2Processed" in evaluate
    assert "_tp1Hit = 1" in evaluate
    assert "_tp2Hit = 1" in evaluate


def test_v70_has_balanced_braces() -> None:
    v70 = read_source(V70)

    stripped = re.sub(r'//.*$', '', v70, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)

    assert stripped.count("{") == stripped.count("}")

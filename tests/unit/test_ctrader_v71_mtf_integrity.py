from pathlib import Path
import re


REPO_ROOT = Path(__file__).resolve().parents[2]
V69 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v69.cs"
V70 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v70.cs"
V71 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"


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


def test_v71_preserves_previous_versions() -> None:
    v69 = read_source(V69)
    v70 = read_source(V70)
    v71 = read_source(V71)

    assert "CFIP_MTF_LiveEntryEngine_Clean_v69" in v69
    assert "CFIP_MTF_LiveEntryEngine_Clean_v70" in v70
    assert "CFIP_MTF_LiveEntryEngine_Clean_v71" in v71
    assert "CFIPClean70" not in v71
    assert "CLEAN70" not in v71


def test_v71_decision_chart_confluence_uses_closed_chart_bar() -> None:
    v71 = read_source(V71)
    decision = extract_method(v71, "BuildDecision")

    assert "MapM5ToClosedChart" in decision
    assert "LiveBias" in decision
    assert "LiveBias(\n                        chartIndex" not in decision


def test_v71_calculate_derives_closed_m5_from_single_reference() -> None:
    v71 = read_source(V71)
    calculate = extract_method(v71, "Calculate")

    assert calculate.count("DateTime reference =") == 1
    assert "int closedM5 =" in calculate
    assert "ClosedIndex(" in calculate
    assert "_m5Bars.Count - 2" not in calculate


def test_v71_closed_chart_mapping_never_returns_forming_chart_bar() -> None:
    v71 = read_source(V71)
    helper = extract_method(v71, "ClosedChartIndex")

    assert "Bars.Count - 2" in helper
    assert "closed < Bars.Count - 1" in helper
    assert "return closed;" in helper


def test_v71_runtime_clock_is_c_trader_server_utc() -> None:
    v71 = read_source(V71)

    assert "DateTime.UtcNow" not in v71
    assert "DateTime.Now" not in v71
    assert "TimeInUtc" in v71
    assert "Application.UserTimeOffset" in v71


def test_v71_closed_index_is_guarded_and_shared() -> None:
    v71 = read_source(V71)
    closed = extract_method(v71, "ClosedIndex")

    assert "reference == DateTime.MinValue" in closed
    assert "bars.Count < 2" in closed
    assert "bars.OpenTimes.GetIndexByTime" in closed


def test_v71_has_balanced_braces() -> None:
    v71 = read_source(V71)
    stripped = re.sub(r"//.*$", "", v71, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)

    assert stripped.count("{") == stripped.count("}")

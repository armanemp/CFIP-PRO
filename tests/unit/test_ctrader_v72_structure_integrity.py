from pathlib import Path
import re


REPO_ROOT = Path(__file__).resolve().parents[2]
V69 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v69.cs"
V70 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v70.cs"
V71 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v71.cs"
V72 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v72.cs"


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


def test_v72_preserves_previous_versions() -> None:
    assert "CFIP_MTF_LiveEntryEngine_Clean_v69" in read_source(V69)
    assert "CFIP_MTF_LiveEntryEngine_Clean_v70" in read_source(V70)
    assert "CFIP_MTF_LiveEntryEngine_Clean_v71" in read_source(V71)

    v72 = read_source(V72)
    assert "CFIP_MTF_LiveEntryEngine_Clean_v72" in v72
    assert "CFIPClean71" not in v72
    assert "CLEAN71" not in v72


def test_v72_fvg_discovery_and_live_retest_are_separate() -> None:
    v72 = read_source(V72)

    fvg = extract_method(v72, "FindNearestFvg")
    execution = extract_method(v72, "FindNearestFvgForExecution")
    model = extract_method(v72, "BuildExecutionModel")

    assert "selectionPrice = double.NaN" in fvg
    assert "requireCurrentRetest = true" in fvg
    assert "requireCurrentRetest &&" in fvg
    assert "nearestPrice" in fvg

    assert "FindNearestFvg(" in execution
    assert "false" in execution
    assert "market" in execution

    assert "FindNearestFvgForExecution" in model


def test_v72_ob_discovery_and_live_retest_are_separate() -> None:
    v72 = read_source(V72)

    ob = extract_method(v72, "FindNearestOrderBlock")
    execution = extract_method(v72, "FindNearestOrderBlockForExecution")
    model = extract_method(v72, "BuildExecutionModel")

    assert "selectionPrice = double.NaN" in ob
    assert "requireHistoricalRetest = true" in ob
    assert "requireHistoricalRetest &&" in ob

    assert "FindNearestOrderBlock(" in execution
    assert "false" in execution
    assert "market" in execution

    assert "FindNearestOrderBlockForExecution" in model


def test_v72_buy_sell_liquidity_sweep_uses_symmetric_penetration_depth() -> None:
    v72 = read_source(V72)
    bull = extract_method(v72, "BullLiquiditySweep")
    bear = extract_method(v72, "BearLiquiditySweep")
    ob = extract_method(v72, "HasOrderBlockLiquiditySweep")

    for method in (bull, bear, ob):
        assert "minimumDepth" in method
        assert "penetration" in method
        assert "atr" in method
        assert "Symbol.Ask" not in method
        assert "Symbol.Bid" not in method

    assert "prior -\n                bars.LowPrices[index]" in bull
    assert "bars.HighPrices[index] -\n                prior" in bear


def test_v72_ob_quality_uses_managed_fvg_confluence() -> None:
    v72 = read_source(V72)
    confluence = extract_method(v72, "HasOrderBlockFvgConfluence")

    assert "BuildManagedFvgZone" in confluence
    assert "managed == null" in confluence


def test_v72_fvg_full_breach_never_resurrects_a_zone() -> None:
    v72 = read_source(V72)
    fvg = extract_method(v72, "BuildManagedFvgZone")

    assert "managedHigh = managedLow" in fvg
    assert "managedLow = managedHigh" in fvg
    assert "managedLow >= managedHigh" in fvg
    assert "fully consumed FVG" in v72 or "fully-swept FVG" in v72


def test_v72_fvg_full_fill_parameter_is_safety_enforced() -> None:
    v72 = read_source(V72)

    assert (
        'Parameter("FVG Invalidate On Full Fill (Safety-Enforced)"'
        in v72
    )


def test_v72_has_balanced_braces() -> None:
    v72 = read_source(V72)
    stripped = re.sub(r"//.*$", "", v72, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)
    assert stripped.count("{") == stripped.count("}")

from pathlib import Path
import re

REPO_ROOT = Path(__file__).resolve().parents[2]
V75 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v75.cs"
V76 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v76.cs"


def read(path: Path) -> str:
    assert path.is_file(), f"Missing source: {path}"
    return path.read_text(encoding="utf-8")


def parameter_names(source: str) -> list[str]:
    return re.findall(
        r"\[Parameter\([\s\S]*?\)\]\s*public\s+[\w<>\?]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}",
        source,
    )


def extract_method(source: str, method_name: str) -> str:
    match = re.search(
        rf"(?m)^\s*(?:private|public|protected|internal)\s+[^\n]+\b"
        rf"{re.escape(method_name)}\s*\(",
        source,
    )
    assert match, f"Method not found: {method_name}"

    opening = source.find("{", match.end())
    assert opening >= 0

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


def test_v76_preserves_complete_parameter_surface_from_v75() -> None:
    v75 = read(V75)
    v76 = read(V76)

    names75 = parameter_names(v75)
    names76 = parameter_names(v76)

    assert len(names75) == 512
    assert len(names76) == 512
    assert names76 == names75
    assert len(set(names76)) == 512


def test_v76_has_closed_mtf_snapshot_contract() -> None:
    source = read(V76)

    for name in [
        "CFIPClean76MtfSnapshot",
        "CFIPClean76MtfBarSnapshot",
        "CFIPClean76MtfSnapshotBuilder",
        "ResolveM5Reference(",
        "ResolveClosedBar(",
        "IsCoherent(",
    ]:
        assert name in source


def test_v76_mtf_snapshot_contains_all_required_timeframes() -> None:
    source = read(V76)

    for name in [
        "M1",
        "M5",
        "M15",
        "M30",
        "H1",
        "H4",
        "D1",
        "W1",
        "Chart",
    ]:
        assert f"CFIPClean76MtfBarSnapshot {name}" in source


def test_v76_closed_index_never_returns_forming_final_series_item() -> None:
    source = read(V76)
    helper = extract_method(source, "ResolveClosedBar")

    assert "bars.Count < 2" in helper
    assert "if (probe == bars.Count - 1)" in helper
    assert "probe--" in helper
    assert "nextOpen > reference" in helper
    assert "i + 1 >= bars.Count" in helper
    assert "return" in helper


def test_v76_closed_bar_requires_next_open_at_or_before_reference() -> None:
    source = read(V76)
    helper = extract_method(source, "ResolveClosedBar")

    assert "reference < bars.OpenTimes[0]" in helper
    assert "nextOpen > reference" in helper
    assert "IsFullyClosedAtReference" in source


def test_v76_reference_is_derived_from_m5_series() -> None:
    source = read(V76)
    reference = extract_method(source, "ResolveM5Reference")

    assert "m5Bars.OpenTimes[last]" in reference
    assert "reference > serverUtc" in reference

    calculate = extract_method(source, "Calculate")
    assert "Build(" in calculate
    assert "_m5Bars" in calculate
    assert "TimeInUtc" in calculate
    assert "Application.UserTimeOffset" in calculate


def test_v76_uses_one_mtf_snapshot_per_calculation_cycle() -> None:
    source = read(V76)
    calculate = extract_method(source, "Calculate")

    assert calculate.count("CFIPClean76MtfSnapshot mtf") == 1
    assert calculate.count("CFIPClean76MtfSnapshotBuilder.Build(") == 1
    assert "_state.SetMtf(mtf);" in calculate


def test_v76_mtf_builder_reads_all_eight_series_once() -> None:
    source = read(V76)

    builder_start = source.index(
        "public static CFIPClean76MtfSnapshot Build("
    )
    builder_end = source.index(
        "public static bool HasPrimaryHistory(",
        builder_start,
    )
    builder = source[builder_start:builder_end]

    for token in [
        "m1Bars",
        "m5Bars",
        "m15Bars",
        "m30Bars",
        "h1Bars",
        "h4Bars",
        "d1Bars",
        "w1Bars",
    ]:
        assert token in builder

    assert builder.count("ResolveClosedBar(") == 8


def test_v76_mtf_resolution_uses_ctrader_time_series_mapping() -> None:
    source = read(V76)
    helper = extract_method(source, "ResolveClosedBar")

    assert "bars.OpenTimes.GetIndexByTime" in helper


def test_v76_server_and_user_time_are_separated() -> None:
    source = read(V76)

    calculate = extract_method(source, "Calculate")
    assert "TimeInUtc" in calculate
    assert "Application.UserTimeOffset" in calculate


def test_v76_no_local_process_clock() -> None:
    source = read(V76)
    assert "DateTime.UtcNow" not in source
    assert "DateTime.Now" not in source


def test_v76_mtf_coherence_is_explicit() -> None:
    source = read(V76)
    coherent = extract_method(source, "IsCoherent")

    assert "snapshot.IsPrimaryDecisionReady" in coherent
    assert "item.NextBarOpenUtc" in coherent
    assert "item.BarOpenUtc >= snapshot.ReferenceUtc" in coherent
    assert "item.IsFullyClosedAtReference" in coherent
    assert "snapshot.IsReferenceFresh" in coherent
    assert "optional data is not temporal leakage" in source


def test_v76_preserves_version_isolation() -> None:
    source = read(V76)

    assert "CFIPClean75" not in source
    assert "Clean_v75" not in source
    assert "CFIP_MTF_LiveEntryEngine_Clean_v76" in source


def test_v76_broker_mutations_and_ui_authority_remain_deferred() -> None:
    source = read(V76)

    for token in [
        ".ExecuteMarketOrder(",
        ".PlaceStopOrder(",
        ".PlaceLimitOrder(",
        ".ModifyStopLossPrice(",
        ".ModifyTakeProfitPrice(",
        ".ClosePosition(",
        ".CancelPendingOrder(",
        "Chart.",
        "CreatePanel(",
    ]:
        assert token not in source


def test_v76_braces_are_balanced() -> None:
    source = read(V76)
    stripped = re.sub(r"//.*$", "", source, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)
    assert stripped.count("{") == stripped.count("}")

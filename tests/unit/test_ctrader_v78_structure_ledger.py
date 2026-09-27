from pathlib import Path
import re

REPO_ROOT = Path(__file__).resolve().parents[2]
V77 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v77.cs"
V78 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v78.cs"


def read(path: Path) -> str:
    assert path.is_file(), f"Missing source: {path}"
    return path.read_text(encoding="utf-8")


def parameter_names(source: str) -> list[str]:
    return re.findall(
        r"\[Parameter\([\s\S]*?\)\]\s*public\s+[\w<>\?]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}",
        source,
    )


def test_v78_preserves_all_v77_parameters() -> None:
    a = parameter_names(read(V77))
    b = parameter_names(read(V78))
    assert len(a) == 513
    assert len(b) == 513
    assert a == b
    assert len(set(b)) == 513


def test_v78_has_one_canonical_structural_ledger() -> None:
    source = read(V78)
    for name in [
        "CFIPClean78StructureLedgerBuilder",
        "CFIPClean78StructureSnapshot",
        "CFIPClean78StructureEventRecord",
        "CFIPClean78ZoneRecord",
        "CFIPClean78LiquidityRecord",
        "CFIPClean78PremiumDiscountState",
    ]:
        assert source.count(name) >= 1

    for legacy_declaration in [
        "public sealed class CFIPClean78StructureEvent ",
        "public sealed class CFIPClean78ZoneSnapshot ",
        "public sealed class CFIPClean78LiquiditySnapshot ",
    ]:
        assert legacy_declaration not in source

    for decl in [
        "public sealed class CFIPClean78StructureLedgerBuilder",
        "public sealed class CFIPClean78StructureSnapshot",
        "public sealed class CFIPClean78ZoneRecord",
        "public sealed class CFIPClean78LiquidityRecord",
    ]:
        assert source.count(decl) == 1


def test_v78_has_typed_structure_and_zone_lifecycle() -> None:
    source = read(V78)
    for token in [
        "BreakOfStructure",
        "MarketStructureShift",
        "ChangeOfCharacter",
        "Displacement",
        "LiquiditySweep",
        "FairValueGap",
        "OrderBlock",
        "PartiallyMitigated",
        "Consumed",
        "Invalidated",
        "Expired",
        "CFIPClean78ZoneLifecycle.PartiallyMitigated",
        "CFIPClean78ZoneLifecycle.Consumed",
        "CFIPClean78ZoneLifecycle.Expired",
    ]:
        assert token in source


def test_v78_fvg_consumption_never_resurrects() -> None:
    source = read(V78)
    start = source.index("private CFIPClean78ZoneRecord BuildFvg(")
    end = source.index("private void BuildObZones(", start)
    block = source[start:end]
    assert "consumed = true" in block
    assert "currentLower = 0; currentUpper = 0;" in block or (
        "currentLower = 0" in block and "currentUpper = 0" in block
    )
    assert "CFIPClean78ZoneLifecycle.Consumed" in block
    assert "UseTwoBarImbalanceFvg" in source
    assert "EnableFvgPartialMitigation" in source


def test_v78_preserves_ob_mitigation_controls() -> None:
    source = read(V78)
    start = source.index("private void BuildObZones(")
    end = source.index("private void BuildEqualLiquidity(", start)
    block = source[start:end]
    for token in [
        "RequireObDisplacement",
        "ObDisplacementAtr",
        "ObUseBodyForZone",
        "MaximumZoneAgeBars",
    ]:
        assert token in block
    assert "CFIPClean78ZoneKind.OrderBlock" in block
    assert "CFIPClean78ZoneLifecycle.Consumed" in block


def test_v78_has_equal_liquidity_sweep_depth_and_forecast_state() -> None:
    source = read(V78)
    for token in [
        "EqualHigh",
        "EqualLow",
        "LiquiditySweepMinimumDepthAtr",
        "lowPen",
        "highPen",
        "Forecast",
        "ForecastCandidate",
        "MarkForecastLiquidity",
        "UseLiquidityForecast",
    ]:
        assert token in source


def test_v78_has_prior_day_week_session_and_pivots() -> None:
    source = read(V78)
    for token in [
        "PriorDayHigh",
        "PriorDayLow",
        "PriorWeekHigh",
        "PriorWeekLow",
        "SessionHigh",
        "SessionLow",
        "DailyPivot",
        "DailyR1",
        "DailyR2",
        "DailyS1",
        "DailyS2",
    ]:
        assert token in source


def test_v78_consumers_share_one_structure_snapshot() -> None:
    source = read(V78)
    assert "CFIPClean78StructureSnapshot Structure" in source
    assert "public void SetStructure(CFIPClean78StructureSnapshot value)" in source
    assert "_structureBuilder.Build(" in source


def test_v78_structure_build_is_closed_mtf_driven() -> None:
    source = read(V78)
    start = source.index("public CFIPClean78StructureSnapshot Build(")
    end = source.index("private void BuildTimeframe(", start)
    block = source[start:end]
    assert "mtf.IsPrimaryDecisionReady" in block
    assert "CFIPClean78MtfSnapshotBuilder.IsCoherent(mtf)" in block
    assert "mtf.M5" in block
    assert "mtf.M15" in block


def test_v78_no_broker_or_chart_authority() -> None:
    source = read(V78)
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


def test_v78_has_no_previous_version_references() -> None:
    source = read(V78)
    assert "CFIPClean77" not in source
    assert "Clean_v77" not in source


def test_v78_no_duplicate_public_type_declarations() -> None:
    source = read(V78)
    types = re.findall(
        r"public\s+(?:sealed\s+)?(?:class|enum|interface|static class)\s+(\w+)",
        source,
    )
    assert len(types) == len(set(types))


def test_v78_braces_are_balanced() -> None:
    source = read(V78)
    stripped = re.sub(r"//.*$", "", source, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)
    assert stripped.count("{") == stripped.count("}")


def test_v78_version_isolation() -> None:
    assert "CFIP_MTF_LiveEntryEngine_Clean_v77" in read(V77)
    assert "CFIP_MTF_LiveEntryEngine_Clean_v78" in read(V78)

from pathlib import Path
import re

REPO_ROOT = Path(__file__).resolve().parents[2]
V76 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v76.cs"
V77 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v77.cs"


def read(path: Path) -> str:
    assert path.is_file(), f"Missing source: {path}"
    return path.read_text(encoding="utf-8")


def parameter_names(source: str) -> list[str]:
    return re.findall(
        r"\[Parameter\([\s\S]*?\)\]\s*public\s+[\w<>\?]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}",
        source,
    )


def test_v77_preserves_complete_v76_parameter_surface() -> None:
    v76 = read(V76)
    v77 = read(V77)

    names76 = parameter_names(v76)
    names77 = parameter_names(v77)

    assert len(names76) == 513
    assert len(names77) == 513
    assert names77 == names76
    assert len(set(names77)) == 513


def test_v77_has_one_normalized_market_model() -> None:
    source = read(V77)

    for name in [
        "CFIPClean77MarketModelBuilder",
        "CFIPClean77MarketFrame",
        "CFIPClean77FeatureEvidence",
        "CFIPClean77Regime",
        "CFIPClean77MarketFeature",
        "BiasDirection",
        "IndependentEvidence",
        "RegimeQuality",
    ]:
        assert name in source


def test_v77_market_model_contains_unique_feature_evidence_contracts() -> None:
    source = read(V77)

    build_start = source.index(
        "private CFIPClean77MarketFrame BuildFrame("
    )
    add_start = source.index(
        "private static void AddFeature(",
        build_start,
    )
    build_frame = source[build_start:add_start]

    feature_names = [
        "Trend",
        "Momentum",
        "Rsi",
        "Dmi",
        "EmaSlope",
        "Rejection",
        "VolumeExpansion",
        "MacdBias",
        "VwapBias",
        "HealthyVolatility",
    ]

    for feature in feature_names:
        assert (
            "AddFeature(features, CFIPClean77MarketFeature."
            + feature
        ) in build_frame

    assert sum(
        build_frame.count(
            "AddFeature(features, CFIPClean77MarketFeature."
        )
        for feature in feature_names
    ) == 10


def test_v77_market_model_consumes_closed_mtf_snapshot_indices() -> None:
    source = read(V77)

    assert "snapshot.ClosedIndex" in source
    assert "snapshot.IsFullyClosedAtReference" in source
    assert "snapshot.IsAvailable" in source

    builder_start = source.index(
        "public CFIPClean77MarketModel Build("
    )
    builder_end = source.index(
        "private void AddFrame(",
        builder_start,
    )
    builder = source[builder_start:builder_end]

    assert "CFIPClean77MtfSnapshot mtf" in builder
    assert "mtf.IsReferenceValid" in builder
    assert "CFIPClean77MtfSnapshotBuilder.IsCoherent" in builder


def test_v77_frame_never_selects_forming_bar() -> None:
    source = read(V77)

    build_start = source.index(
        "private CFIPClean77MarketFrame BuildFrame("
    )
    build_end = source.index(
        "private void AddFeature(",
        build_start,
    )
    build_frame = source[build_start:build_end]

    assert "snapshot.ClosedIndex" in build_frame
    assert "index >= bars.Count - 1" in build_frame
    assert "bars.Count - 1" in build_frame
    assert "bars.Count - 2" not in build_frame


def test_v77_uses_c_trader_native_indicator_accessors() -> None:
    source = read(V77)

    for token in [
        "IIndicatorsAccessor",
        "ExponentialMovingAverage",
        "AverageTrueRange",
        "RelativeStrengthIndex",
        "DirectionalMovementSystem",
        "MovingAverageType.WilderSmoothing",
    ]:
        assert token in source


def test_v77_preserves_rsi_exhaustion_and_configurable_regime_threshold() -> None:
    source = read(V77)

    assert "AvoidRsiExhaustion" in source
    assert "bullScore - 5" in source
    assert "bearScore - 5" in source
    assert 'configuration.Get(\n                    "AdxMinimum"' in source
    assert "if (adx < adxMinimum)" in source


def test_v77_regime_is_typed_not_string_based() -> None:
    source = read(V77)

    assert "enum CFIPClean77Regime" in source
    assert "CFIPClean77Regime.Expansion" in source
    assert "CFIPClean77Regime.Compression" in source
    assert "CFIPClean77Regime.Range" in source
    assert "CFIPClean77Regime.Transition" in source
    assert "CFIPClean77Regime.Trend" in source


def test_v77_market_model_has_no_broker_or_chart_authority() -> None:
    source = read(V77)
    builder_start = source.index(
        "public sealed class CFIPClean77MarketModelBuilder"
    )
    structure_start = source.index(
        "public sealed class CFIPClean77StructureEvent",
        builder_start,
    )
    block = source[builder_start:structure_start]

    for token in [
        ".ExecuteMarketOrder(",
        ".PlaceStopOrder(",
        ".PlaceLimitOrder(",
        ".ModifyStopLossPrice(",
        ".ModifyTakeProfitPrice(",
        ".ClosePosition(",
        ".CancelPendingOrder(",
        "Chart.",
    ]:
        assert token not in block


def test_v77_market_model_wiring_is_single_and_reference_driven() -> None:
    source = read(V77)
    calculate = source[
        source.index("public override void Calculate(int index)")
        : source.rindex("\n    }\n}")
    ]

    assert calculate.count("CFIPClean77MarketModel market") == 1
    assert calculate.count("_marketModelBuilder.Build(") == 1
    assert calculate.count("_state.SetMarket(market);") == 1
    assert "mtf.ReferenceUtc != _lastMarketReferenceUtc" in calculate


def test_v77_has_no_duplicate_public_type_declarations() -> None:
    source = read(V77)

    public_types = re.findall(
        r"public\s+(?:sealed\s+)?(?:class|enum|interface|static class)\s+(\w+)",
        source,
    )
    assert len(public_types) == len(set(public_types))


def test_v77_version_isolation_and_balance() -> None:
    source = read(V77)

    assert "CFIPClean76" not in source
    assert "Clean_v76" not in source
    stripped = re.sub(r"//.*$", "", source, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)
    assert stripped.count("{") == stripped.count("}")


def test_v77_host_keeps_broker_and_ui_deferred() -> None:
    source = read(V77)

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

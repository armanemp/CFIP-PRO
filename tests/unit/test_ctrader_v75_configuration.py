from pathlib import Path
import re

REPO_ROOT = Path(__file__).resolve().parents[2]
V73 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v73.cs"
V75 = REPO_ROOT / "integrations" / "ctrader" / "calude-edit-v75.cs"


def read(path: Path) -> str:
    assert path.is_file(), f"Missing source: {path}"
    return path.read_text(encoding="utf-8")


def parameter_names(source: str) -> list[str]:
    return re.findall(
        r"\[Parameter\([\s\S]*?\)\]\s*public\s+[\w<>\?]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}",
        source,
    )


def test_v75_preserves_complete_v73_parameter_surface() -> None:
    v73 = read(V73)
    v75 = read(V75)

    names73 = parameter_names(v73)
    names75 = parameter_names(v75)

    assert len(names73) == 512
    assert len(names75) == 512
    assert names75 == names73
    assert len(set(names75)) == 512


def test_v75_has_one_configuration_snapshot_and_runtime_authority() -> None:
    source = read(V75)

    assert "class CFIPClean75ConfigSnapshot" in source
    assert "class CFIPClean75RuntimeAuthority" in source
    assert "CFIPClean75ConfigSnapshot.Build(" in source
    assert "_runtimeAuthority.InitializeFromConfiguration(" in source
    assert "public bool ManualTradeEntryControlsSupported" in source


def test_v75_has_canonical_configuration_sections() -> None:
    source = read(V75)

    for name in [
        "Decision",
        "Mtf",
        "Structure",
        "Zones",
        "Liquidity",
        "Indicators",
        "Entry",
        "RiskTargets",
        "LiveManagement",
        "Automation",
        "SmartExecution",
        "Display",
        "Accuracy",
        "SmartEngine",
    ]:
        assert f"CFIPClean75ConfigurationSection.{name}" in source or f"    {name}," in source


def test_v75_duplicate_protection_control_is_explicitly_mapped() -> None:
    source = read(V75)

    assert "AutoBrokerProtection" in source
    assert "AutoProtectBrokerPositions" in source


def test_v75_manual_entry_authority_is_not_supported() -> None:
    source = read(V75)

    assert "ManualTradeEntryControlsSupported { get { return false; } }" in source
    assert "manual BUY/SELL/order-placement authority" in source


def test_v75_has_no_direct_broker_mutation() -> None:
    source = read(V75)

    for token in [
        ".ExecuteMarketOrder(",
        ".PlaceStopOrder(",
        ".PlaceLimitOrder(",
        ".ModifyStopLossPrice(",
        ".ModifyTakeProfitPrice(",
        ".ClosePosition(",
        ".CancelPendingOrder(",
    ]:
        assert token not in source


def test_v75_braces_are_balanced() -> None:
    source = read(V75)
    stripped = re.sub(r"//.*$", "", source, flags=re.MULTILINE)
    stripped = re.sub(r'"(?:\\.|[^"\\])*"', '""', stripped)
    assert stripped.count("{") == stripped.count("}")


def test_v75_indicator_shell_has_single_lifecycle_entry_points() -> None:
    source = read(V75)

    assert "CFIP_MTF_LiveEntryEngine_Clean_v75" in source
    assert source.count("protected override void Initialize()") == 1
    assert source.count("public override void Calculate(int index)") == 1

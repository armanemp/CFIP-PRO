from pydantic import ValidationError
import pytest

from cfip.domain.alert_contracts import AlertCondition
from cfip.domain.workspace_contracts import ChartPanel, TerminalWorkspace, WorkspaceSyncGroup

def test_workspace_rejects_unknown_selected_panel() -> None:
    workspace = TerminalWorkspace(
        id="w1",
        name="Default",
        panels=(ChartPanel(id="p1", symbol="EUR/USD", timeframe="1m"),),
        selected_panel_id="missing",
    )
    with pytest.raises(ValueError, match="selected_panel_not_found"):
        workspace.validate_references()

def test_workspace_rejects_unknown_sync_panel() -> None:
    workspace = TerminalWorkspace(
        id="w1",
        name="Default",
        panels=(ChartPanel(id="p1", symbol="EUR/USD", timeframe="1m"),),
        sync_groups=(WorkspaceSyncGroup(id="g1", panel_ids=("missing",), mode="all"),),
    )
    with pytest.raises(ValueError, match="sync_group_panel_not_found"):
        workspace.validate_references()

def test_indicator_alert_requires_indicator_id_and_value() -> None:
    with pytest.raises(ValidationError):
        AlertCondition(kind="indicator-threshold", value=70, direction="above")

def test_analysis_alert_accepts_valid_bias() -> None:
    assert AlertCondition(kind="analysis-bias", analysis_value="bullish").analysis_value == "bullish"

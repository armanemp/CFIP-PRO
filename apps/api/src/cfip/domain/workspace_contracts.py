"""Provider-neutral terminal workspace contracts.

A workspace is persisted state, not presentation state. The web terminal consumes these
contracts so layouts, synchronization, chart preferences and object visibility remain
replaceable and auditable.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

ChartKind = Literal["candles", "bars", "line", "area", "baseline"]
PaneKind = Literal["chart", "indicator", "volume", "depth", "tape"]
SyncMode = Literal["none", "symbol", "timeframe", "crosshair", "all"]

class ChartPanel(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=80)
    symbol: str = Field(min_length=1, max_length=64)
    venue: str = Field(default="reference", min_length=1, max_length=64)
    timeframe: str = Field(min_length=1, max_length=16)
    chart_type: ChartKind = "candles"
    panes: tuple[PaneKind, ...] = ("chart", "volume")
    visible: bool = True

class WorkspaceSyncGroup(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=80)
    panel_ids: tuple[str, ...] = ()
    mode: SyncMode = "none"

class WorkspacePreferences(BaseModel):
    model_config = ConfigDict(extra="forbid")
    grid: bool = True
    volume: bool = True
    sessions: bool = False
    bid_ask: bool = True
    magnet: bool = False
    auto_fit: bool = True
    show_last_price: bool = True

class TerminalWorkspace(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    name: str = Field(min_length=1, max_length=160)
    schema_version: int = Field(default=1, ge=1)
    panels: tuple[ChartPanel, ...] = ()
    sync_groups: tuple[WorkspaceSyncGroup, ...] = ()
    preferences: WorkspacePreferences = Field(default_factory=WorkspacePreferences)
    selected_panel_id: str | None = None
    revision: int = Field(default=1, ge=1)

    def validate_references(self) -> "TerminalWorkspace":
        panel_ids = {panel.id for panel in self.panels}
        if self.selected_panel_id is not None and self.selected_panel_id not in panel_ids:
            raise ValueError("selected_panel_not_found")
        if any(panel_id not in panel_ids for group in self.sync_groups for panel_id in group.panel_ids):
            raise ValueError("sync_group_panel_not_found")
        return self

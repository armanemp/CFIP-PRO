"""Persistable chart-terminal workspace contracts."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

ChartType = Literal["candles","bars","line","area","baseline","heikin-ashi","hollow-candles"]
PaneKind = Literal["chart","indicator","volume","analysis"]

class ChartViewport(BaseModel):
    model_config = ConfigDict(extra="forbid")
    from_time: int | None = Field(default=None, gt=0)
    to_time: int | None = Field(default=None, gt=0)
    autoscale: bool = True

class DrawingObject(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    kind: str
    points: tuple[tuple[float, float], ...] = ()
    style: dict[str, str | float | int | bool] = Field(default_factory=dict)
    locked: bool = False
    visible: bool = True

class Pane(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    kind: PaneKind
    height_ratio: float = Field(default=1.0, gt=0)
    indicators: tuple[str, ...] = ()

class ChartWorkspace(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    name: str
    symbol: str
    timeframe: str
    chart_type: ChartType = "candles"
    panes: tuple[Pane, ...] = ()
    drawings: tuple[DrawingObject, ...] = ()
    compare_symbols: tuple[str, ...] = ()
    viewport: ChartViewport = ChartViewport()
    locale: str = "en-US"
    rtl: bool = False
    version: int = Field(default=1, ge=1)

class WorkspaceTemplate(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    name: str
    workspace: ChartWorkspace
    built_in: bool = False

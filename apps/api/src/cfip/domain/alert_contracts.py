"""Provider-neutral alert and notification contracts."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

AlertKind = Literal["price","indicator","analysis","risk","system"]
AlertOperator = Literal["above","below","crosses-above","crosses-below","equals"]

class AlertRule(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    name: str
    symbol: str
    timeframe: str
    kind: AlertKind
    metric: str
    operator: AlertOperator
    threshold: float | str
    enabled: bool = True
    cooldown_seconds: int = Field(default=60, ge=0)
    channels: tuple[Literal["in-app","browser","email","webhook"], ...] = ("in-app",)

class AlertEvaluation(BaseModel):
    model_config = ConfigDict(extra="forbid")
    rule_id: str
    triggered: bool
    value: float | str | None = None
    message: str = ""
    evaluated_at: int = Field(gt=0)
    dedupe_key: str

class Notification(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str
    severity: Literal["info","success","warning","critical"]
    title: str
    body: str
    channels: tuple[str, ...]
    created_at: int = Field(gt=0)

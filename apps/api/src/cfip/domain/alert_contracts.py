"""Deterministic alert and notification contracts.

Alert evaluation is separated from delivery. Providers, browser notifications, email and
webhooks consume normalized notification intents rather than owning alert semantics.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field, model_validator

AlertConditionKind = Literal[
    "price-cross", "price-change", "indicator-threshold", "analysis-bias",
    "analysis-recommendation", "data-quality", "spread-threshold"
]
AlertDirection = Literal["above", "below", "cross-up", "cross-down"]
Severity = Literal["info", "success", "warning", "critical"]
NotificationChannel = Literal["in-app", "browser", "email", "webhook"]

class AlertCondition(BaseModel):
    model_config = ConfigDict(extra="forbid")
    kind: AlertConditionKind
    value: float | None = Field(default=None)
    direction: AlertDirection | None = None
    indicator_id: str | None = Field(default=None, max_length=80)
    analysis_value: str | None = Field(default=None, max_length=64)

    @model_validator(mode="after")
    def validate_shape(self) -> "AlertCondition":
        if self.kind in {"price-cross", "price-change", "indicator-threshold", "spread-threshold"} and self.value is None:
            raise ValueError("numeric_alert_condition_requires_value")
        if self.kind == "indicator-threshold" and not self.indicator_id:
            raise ValueError("indicator_threshold_requires_indicator_id")
        if self.kind in {"price-cross", "price-change", "indicator-threshold", "spread-threshold"} and self.direction is None:
            raise ValueError("numeric_alert_condition_requires_direction")
        if self.kind == "analysis-bias" and self.analysis_value not in {"bullish", "bearish", "neutral"}:
            raise ValueError("analysis_bias_requires_valid_value")
        if self.kind == "analysis-recommendation" and self.analysis_value not in {"long", "short", "wait"}:
            raise ValueError("analysis_recommendation_requires_valid_value")
        return self

class AlertRule(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    name: str = Field(min_length=1, max_length=160)
    symbol: str = Field(min_length=1, max_length=64)
    timeframe: str | None = Field(default=None, max_length=16)
    condition: AlertCondition
    enabled: bool = True
    once: bool = False
    cooldown_seconds: int = Field(default=60, ge=0, le=604800)
    severity: Severity = "info"
    channels: tuple[NotificationChannel, ...] = ("in-app",)

class NotificationIntent(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    rule_id: str | None = None
    symbol: str | None = None
    title: str = Field(min_length=1, max_length=200)
    body: str = Field(min_length=1, max_length=4000)
    severity: Severity = "info"
    channels: tuple[NotificationChannel, ...] = ("in-app",)
    created_at: int = Field(gt=0)
    dedupe_key: str = Field(min_length=1, max_length=256)

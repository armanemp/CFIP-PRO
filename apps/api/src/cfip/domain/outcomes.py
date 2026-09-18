"""Canonical signal lifecycle, outcome attribution, calibration and drift contracts."""

from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator

SignalState = Literal[
    "candidate", "accepted", "emitted", "expired", "triggered", "closed", "cancelled"
]
OutcomeLabel = Literal["win", "loss", "breakeven", "unknown"]
OutcomeEvent = Literal["entry", "stop", "tp1", "tp2", "tp3", "expiry", "cancel"]
Direction = Literal["long", "short"]


class SignalLifecycle(BaseModel):
    model_config = ConfigDict(extra="forbid")

    signal_id: str = Field(min_length=1, max_length=160)
    symbol: str = Field(min_length=1, max_length=64)
    timeframe: str = Field(min_length=1, max_length=16)
    direction: Direction
    decision_time: int = Field(gt=0)
    state: SignalState
    emitted_at: int | None = Field(default=None, gt=0)
    triggered_at: int | None = Field(default=None, gt=0)
    closed_at: int | None = Field(default=None, gt=0)
    expires_at: int | None = Field(default=None, gt=0)

    @model_validator(mode="after")
    def validate_times_and_state(self) -> "SignalLifecycle":
        timestamps = (
            self.emitted_at,
            self.triggered_at,
            self.closed_at,
            self.expires_at,
        )
        if any(value is not None and value < self.decision_time for value in timestamps):
            raise ValueError("signal_event_before_decision")
        if self.triggered_at is not None and self.emitted_at is not None:
            if self.triggered_at < self.emitted_at:
                raise ValueError("triggered_before_emitted")
        if self.closed_at is not None and self.triggered_at is not None:
            if self.closed_at < self.triggered_at:
                raise ValueError("closed_before_triggered")
        if self.state in {"emitted", "triggered", "closed"} and self.emitted_at is None:
            raise ValueError("state_requires_emitted_at")
        if self.state in {"triggered", "closed"} and self.triggered_at is None:
            raise ValueError("state_requires_triggered_at")
        if self.state == "closed" and self.closed_at is None:
            raise ValueError("closed_state_requires_closed_at")
        if self.state in {"expired", "cancelled"} and self.closed_at is not None:
            raise ValueError("terminal_cancelled_signal_cannot_be_closed")
        if self.expires_at is not None and self.expires_at < self.decision_time:
            raise ValueError("expires_before_decision")
        return self


class SignalGatePolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")

    startup_suppression_bars: int = Field(default=2, ge=0, le=100)
    cooldown_bars: int = Field(default=3, ge=0, le=1000)
    minimum_bars_between_signals: int = Field(default=3, ge=0, le=1000)
    expiry_bars: int = Field(default=24, ge=1, le=10000)


class SignalEmissionDecision(BaseModel):
    model_config = ConfigDict(extra="forbid")

    eligible: bool
    reason: str
    signal_id: str


class OutcomeObservation(BaseModel):
    model_config = ConfigDict(extra="forbid")

    time: int = Field(gt=0)
    high: float = Field(gt=0)
    low: float = Field(gt=0)
    close: float = Field(gt=0)

    @model_validator(mode="after")
    def validate_ohlc(self) -> "OutcomeObservation":
        if self.low > self.high:
            raise ValueError("low_above_high")
        if not self.low <= self.close <= self.high:
            raise ValueError("close_outside_range")
        return self


class OutcomeLabelResult(BaseModel):
    model_config = ConfigDict(extra="forbid")

    signal_id: str
    label: OutcomeLabel
    event: OutcomeEvent
    decision_time: int = Field(gt=0)
    evaluation_time: int = Field(gt=0)
    entry: float = Field(gt=0)
    stop: float | None = Field(default=None, gt=0)
    tp1: float | None = Field(default=None, gt=0)
    tp2: float | None = Field(default=None, gt=0)
    tp3: float | None = Field(default=None, gt=0)
    mfe_r: float | None = None
    mae_r: float | None = None
    realized_r: float | None = None
    attribution: dict[str, float] = Field(default_factory=dict)
    evidence_ids: list[str] = Field(default_factory=list, max_length=100)

    @model_validator(mode="after")
    def validate_evaluation_time(self) -> "OutcomeLabelResult":
        if self.evaluation_time < self.decision_time:
            raise ValueError("outcome_before_decision")
        return self


class CalibrationBin(BaseModel):
    model_config = ConfigDict(extra="forbid")

    lower: float = Field(ge=0, le=1)
    upper: float = Field(ge=0, le=1)
    sample_count: int = Field(ge=0)
    predicted_mean: float = Field(ge=0, le=1)
    observed_rate: float = Field(ge=0, le=1)

    @model_validator(mode="after")
    def validate_interval(self) -> "CalibrationBin":
        if self.upper <= self.lower:
            raise ValueError("invalid_calibration_interval")
        return self


class CalibrationReport(BaseModel):
    model_config = ConfigDict(extra="forbid")

    sample_count: int = Field(ge=0)
    brier_score: float | None = Field(default=None, ge=0, le=1)
    log_loss: float | None = Field(default=None, ge=0)
    expected_calibration_error: float | None = Field(default=None, ge=0, le=1)
    bins: list[CalibrationBin] = Field(default_factory=list, max_length=50)


class DriftReport(BaseModel):
    model_config = ConfigDict(extra="forbid")

    baseline_count: int = Field(ge=0)
    current_count: int = Field(ge=0)
    minimum_sample_count: int = Field(default=50, ge=1)
    score: float | None = Field(default=None, ge=0)
    detected: bool = False
    reason: str

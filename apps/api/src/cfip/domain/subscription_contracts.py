"""Two-tier subscription contracts; entitlement is separate from payment state."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

PlanId = Literal["free","pro"]
SubscriptionState = Literal["trial","active","past_due","expired","cancelled"]

class PlanDefinition(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: PlanId
    name: str
    price_minor: int = Field(ge=0)
    currency: str = Field(min_length=3, max_length=3)
    billing_period: Literal["month","year"]
    entitlements: tuple[str, ...]
    limits: dict[str, int | float | bool] = Field(default_factory=dict)

class Subscription(BaseModel):
    model_config = ConfigDict(extra="forbid")
    user_id: str
    plan_id: PlanId
    state: SubscriptionState
    current_period_start: int = Field(gt=0)
    current_period_end: int = Field(gt=0)
    payment_reference: str | None = None

PLANS: tuple[PlanDefinition, ...] = (
    PlanDefinition(id="free", name="Free", price_minor=0, currency="USD", billing_period="month",
                   entitlements=("terminal.basic","analysis.basic","watchlist.basic"),
                   limits={"symbols":10,"indicators":3,"saved_workspaces":1,"replay_bars":500}),
    PlanDefinition(id="pro", name="Pro", price_minor=4900, currency="USD", billing_period="month",
                   entitlements=("terminal.full","analysis.full","intelligence.elyrava","replay.full","paper-trading","research.full"),
                   limits={"symbols":1000,"indicators":100,"saved_workspaces":50,"replay_bars":100000}),
)

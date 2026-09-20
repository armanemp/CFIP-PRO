"""API boundary for deterministic broker-aware risk planning."""

from fastapi import APIRouter

from cfip.application.risk import RiskService
from cfip.domain.risk import (
    AccountRiskContext,
    InstrumentRiskContext,
    RiskTargetPlan,
    RiskTargetRequest,
)
from pydantic import BaseModel, ConfigDict, Field


router = APIRouter(prefix="/risk")


class RiskPlanRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    target: RiskTargetRequest
    account: AccountRiskContext | None = None
    instrument: InstrumentRiskContext | None = None
    quote_to_account_rate: float | None = Field(default=None, gt=0)


@router.post("/plan", response_model=RiskTargetPlan)
def plan_risk(request: RiskPlanRequest) -> RiskTargetPlan:
    return RiskService.plan(
        request.target,
        account=request.account,
        instrument=request.instrument,
        quote_to_account_rate=request.quote_to_account_rate,
    )

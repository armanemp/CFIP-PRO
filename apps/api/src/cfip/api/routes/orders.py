"""Paper execution boundary."""
from fastapi import APIRouter, Request
from cfip.application.paper_oms import PaperOMS
from cfip.domain.execution_contracts import ExecutionResult, OrderRequest

router = APIRouter(prefix="/orders", tags=["orders"])

def _oms(request: Request) -> PaperOMS:
    value = getattr(request.app.state, "paper_oms", None)
    if value is None:
        raise RuntimeError("paper OMS is not initialized")
    return value

@router.post("/paper", response_model=ExecutionResult)
async def submit_paper_order(request: Request, order: OrderRequest) -> ExecutionResult:
    return _oms(request).submit(order)

@router.get("/paper", response_model=tuple[ExecutionResult, ...])
async def paper_orders(request: Request) -> tuple[ExecutionResult, ...]:
    return _oms(request).list_orders()

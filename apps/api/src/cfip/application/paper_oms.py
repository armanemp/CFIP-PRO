"""Deterministic paper order-management service.

Live execution is deliberately outside this service and must be supplied by an
authorized provider adapter.
"""
from collections import OrderedDict
from cfip.domain.execution_contracts import ExecutionResult, OrderRequest

class PaperOMS:
    def __init__(self, max_orders: int = 1000) -> None:
        self._max_orders = max_orders
        self._orders: OrderedDict[str, ExecutionResult] = OrderedDict()

    def submit(self, request: OrderRequest) -> ExecutionResult:
        if request.mode != "paper":
            return ExecutionResult(client_order_id=request.client_order_id, accepted=False, status="rejected", reason="paper_oms_only")
        if request.client_order_id in self._orders:
            return self._orders[request.client_order_id]
        result = ExecutionResult(
            client_order_id=request.client_order_id,
            accepted=True,
            status="accepted",
            provider_order_id=f"paper:{request.client_order_id}",
        )
        self._orders[request.client_order_id] = result
        self._orders.move_to_end(request.client_order_id)
        while len(self._orders) > self._max_orders:
            self._orders.popitem(last=False)
        return result

    def list_orders(self) -> tuple[ExecutionResult, ...]:
        return tuple(self._orders.values())

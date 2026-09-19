"""Pure risk/position-sizing calculation service."""
from cfip.domain.risk_contracts import PositionSizingRequest, PositionSizingResult

class RiskService:
    def size(self, request: PositionSizingRequest) -> PositionSizingResult:
        distance = abs(request.entry_price - request.stop_price)
        if distance <= 0:
            raise ValueError("stop_must_differ_from_entry")
        risk_amount = request.equity * request.risk_fraction
        units = risk_amount / (distance * request.contract_multiplier)
        notional = units * request.entry_price * request.contract_multiplier
        warnings: list[str] = []
        if request.leverage is not None and notional > request.equity * request.leverage:
            warnings.append("required_notional_exceeds_leverage_capacity")
        return PositionSizingResult(
            risk_amount=risk_amount, stop_distance=distance, units=units,
            notional=notional, leverage_used=request.leverage, warnings=tuple(warnings),
        )

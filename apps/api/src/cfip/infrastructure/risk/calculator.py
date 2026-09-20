"""Deterministic risk calculator; no broker SDK or network access."""

from cfip.domain.risk_contracts import RiskCalculation, RiskRequest


def calculate_risk(request: RiskRequest) -> RiskCalculation:
    """Size a position from account risk and leverage constraints.

    ``contract_size`` expresses quote currency notional per unit. The conversion
    rate converts quote P/L into account currency, keeping the calculation
    independent of a broker implementation.
    """
    stop_distance = abs(request.entry_price - request.stop_price)
    stop_fraction = stop_distance / request.entry_price
    risk_amount = request.equity * request.risk_fraction
    risk_per_unit = stop_distance * request.contract_size * request.quote_to_account_rate
    units_by_risk = risk_amount / risk_per_unit
    units_by_leverage = (
        request.equity * request.leverage
    ) / (request.entry_price * request.contract_size)
    recommended_units = max(0.0, min(units_by_risk, units_by_leverage))
    notional = recommended_units * request.entry_price * request.contract_size
    margin_required = notional / request.leverage
    warnings: list[str] = []
    if units_by_leverage < units_by_risk:
        warnings.append("leverage_limit_reduced_position")
    if stop_fraction > 0.1:
        warnings.append("wide_stop_distance")

    return RiskCalculation(
        symbol=request.symbol,
        side=request.side,
        risk_amount=risk_amount,
        stop_distance=stop_distance,
        stop_distance_fraction=stop_fraction,
        units_by_risk=units_by_risk,
        units_by_leverage=units_by_leverage,
        recommended_units=recommended_units,
        notional=notional,
        margin_required=margin_required,
        risk_fraction=request.risk_fraction,
        warnings=warnings,
    )

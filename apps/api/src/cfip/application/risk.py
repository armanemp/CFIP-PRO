"""Deterministic broker-aware risk and target calculation service."""

from math import floor, isfinite

from cfip.domain.risk import (
    AccountRiskContext,
    InstrumentRiskContext,
    RiskTargetPlan,
    RiskTargetRequest,
)


class RiskService:
    @staticmethod
    def plan(
        request: RiskTargetRequest,
        *,
        account: AccountRiskContext | None,
        instrument: InstrumentRiskContext | None,
        quote_to_account_rate: float | None,
    ) -> RiskTargetPlan:
        if account is None or instrument is None:
            return RiskTargetPlan(
                available=False,
                reason="account_or_instrument_context_required",
                direction=request.direction,
                entry=request.entry,
            )
        if (
            quote_to_account_rate is None
            or not isfinite(quote_to_account_rate)
            or quote_to_account_rate <= 0
        ):
            return RiskTargetPlan(
                available=False,
                reason="quote_to_account_conversion_required",
                direction=request.direction,
                entry=request.entry,
            )

        distance = max(request.atr * request.stop_atr_multiplier, instrument.min_stop_distance)
        if request.direction == "long":
            stop = request.entry - distance
            targets = tuple(request.entry + distance * rr for rr in request.target_rr)
        else:
            stop = request.entry + distance
            targets = tuple(request.entry - distance * rr for rr in request.target_rr)

        if stop <= 0:
            return RiskTargetPlan(
                available=False,
                reason="stop_price_non_positive",
                direction=request.direction,
                entry=request.entry,
                stop=stop,
                risk_distance=distance,
            )
        risk_amount = account.equity * account.risk_fraction
        value_per_price_unit = instrument.tick_value_per_unit / instrument.tick_size
        quantity = risk_amount / (distance * value_per_price_unit * quote_to_account_rate)
        stepped = floor(quantity / instrument.quantity_step) * instrument.quantity_step
        if stepped < instrument.min_quantity:
            return RiskTargetPlan(
                available=False,
                reason="risk_budget_below_minimum_quantity",
                direction=request.direction,
                entry=request.entry,
                stop=stop,
                tp1=targets[0],
                tp2=targets[1],
                tp3=targets[2],
                risk_distance=distance,
                risk_amount=risk_amount,
            )
        stepped = min(stepped, instrument.max_quantity)
        margin_required = (stepped * request.entry * quote_to_account_rate) / account.leverage

        return RiskTargetPlan(
            available=True,
            reason="calculated",
            direction=request.direction,
            entry=request.entry,
            stop=stop,
            tp1=targets[0],
            tp2=targets[1],
            tp3=targets[2],
            risk_distance=distance,
            risk_amount=risk_amount,
            quantity=stepped,
            margin_required=margin_required,
        )

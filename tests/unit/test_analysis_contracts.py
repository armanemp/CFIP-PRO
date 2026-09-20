from cfip.domain.analysis import RiskTargetPlan as AnalysisRiskTargetPlan
from cfip.domain.risk import RiskTargetPlan


def test_analysis_uses_canonical_risk_target_contract() -> None:
    assert AnalysisRiskTargetPlan is RiskTargetPlan


def test_canonical_risk_target_serializes_broker_sizing_fields() -> None:
    plan = RiskTargetPlan(
        available=True,
        reason="calculated",
        direction="long",
        entry=1.1,
        stop=1.0985,
        tp1=1.1015,
        tp2=1.103,
        tp3=1.1045,
        risk_distance=0.0015,
        risk_amount=100,
        quantity=6666,
        margin_required=73.326,
    )
    payload = plan.model_dump()
    assert payload["direction"] == "long"
    assert payload["quantity"] == 6666
    assert payload["margin_required"] == 73.326

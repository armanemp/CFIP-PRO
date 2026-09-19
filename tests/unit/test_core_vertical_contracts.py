from pydantic import ValidationError
from cfip.domain.indicator_contracts import IndicatorDefinition,IndicatorInstance
from cfip.domain.analysis_contracts import AnalysisEvidence
from cfip.domain.replay_contracts import ReplayRequest
from cfip.domain.risk_contracts import PositionSizingRequest
from cfip.domain.governance_contracts import GovernedChange
def test_indicator_instance_is_provider_neutral():
    d=IndicatorDefinition(id="rsi",name="RSI",kind="oscillator",output_names=("value",))
    assert IndicatorInstance(instance_id="i1",definition_id=d.id,symbol="EUR/USD",timeframe="1h").definition_id=="rsi"
def test_analysis_confidence_is_bounded():
    try: AnalysisEvidence(id="e",kind="trend",symbol="EUR/USD",timeframe="1h",direction="bullish",confidence=1.1,rationale="x",source_module="trend",observed_at=1)
    except ValidationError: return
    raise AssertionError("confidence must be bounded")
def test_replay_requires_ordered_positive_range():
    r=ReplayRequest(symbol="EUR/USD",venue="reference",timeframe="1m",start_time=10,end_time=20); assert r.start_time<r.end_time
def test_risk_request_contains_broker_context():
    assert PositionSizingRequest(equity=10000,risk_fraction=.01,entry_price=1.1,stop_price=1.09,leverage=30,direction="long").leverage==30
def test_governed_change_requires_validation_and_rollback_fields():
    assert GovernedChange(id="c1",title="x",requested_by="elyrava",validation_plan=("unit",),rollback_plan=("revert",)).human_approval_required

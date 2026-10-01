from cfip.application.autonomy import AutonomyService

def test_low_and_medium_risk_do_not_require_human_approval() -> None:
    service = AutonomyService()
    assert not service.evaluate(action="commit", risk="low").requires_human_approval
    assert not service.evaluate(action="commit", risk="medium").requires_human_approval

def test_protected_path_escalates_to_human_approval() -> None:
    decision = AutonomyService().evaluate(action="commit", paths=(".github/workflows/ci.yml",), risk="low")
    assert decision.risk == "high"
    assert decision.requires_human_approval

def test_merge_is_high_risk_even_for_low_risk_change() -> None:
    decision = AutonomyService().evaluate(action="merge", risk="low")
    assert decision.requires_human_approval


def test_protected_paths_are_escalated_not_ignored() -> None:
    decision = AutonomyService().evaluate(action="commit", paths=(".github/workflows/ci.yml",), risk="medium")
    assert decision.requires_human_approval
    assert "protected_path" in decision.reasons

import pytest

from cfip.application.improvement_governance import can_transition, require_transition


def test_governed_transition_sequence():
    assert can_transition("observed", "diagnosed")
    assert can_transition("diagnosed", "proposed")
    assert can_transition("proposed", "validated")
    assert can_transition("validated", "approved")
    assert can_transition("approved", "applied")
    assert can_transition("applied", "rolled_back")


def test_governance_blocks_stage_skipping():
    assert not can_transition("proposed", "applied")
    with pytest.raises(ValueError, match="invalid_improvement_transition"):
        require_transition("proposed", "applied")

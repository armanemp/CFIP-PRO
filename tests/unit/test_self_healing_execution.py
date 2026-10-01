"""Tests for governed self-healing execution gates."""

from cfip.application.self_healing_execution import SelfHealingExecutionService
from cfip.domain.self_healing_execution import (
    RepairExecutionRequest,
    SelfDevelopmentChange,
    SelfHealingExecutionPolicy,
)


def _request() -> RepairExecutionRequest:
    return RepairExecutionRequest(
        proposal_id="repair-1",
        artifact_kind="cache",
        risk="low",
        artifact_digest="a" * 16,
        rollback_digest="b" * 16,
        test_evidence_ids=["test-1"],
        security_evidence_ids=["security-1"],
        safety_invariant_ids=["safe-1"],
    )


def test_execution_is_blocked_without_signed_artifact() -> None:
    result = SelfHealingExecutionService.gate(
        _request(),
        SelfHealingExecutionPolicy(),
        approval_present=False,
        signed_artifact=False,
        consecutive_repairs=0,
        cooldown_elapsed=True,
    )
    assert not result.allowed
    assert "signed_artifact_required" in result.reasons


def test_low_risk_code_change_can_self_apply_after_safety_evidence() -> None:
    request = _request().model_copy(update={"artifact_kind": "code", "risk": "low"})
    result = SelfHealingExecutionService.gate(
        request,
        SelfHealingExecutionPolicy(),
        approval_present=False,
        signed_artifact=True,
        consecutive_repairs=0,
        cooldown_elapsed=True,
    )
    assert result.allowed


def test_high_risk_code_change_requires_approval() -> None:
    request = _request().model_copy(update={"artifact_kind": "code", "risk": "high"})
    result = SelfHealingExecutionService.gate(
        request, SelfHealingExecutionPolicy(), approval_present=False,
        signed_artifact=True, consecutive_repairs=0, cooldown_elapsed=True,
    )
    assert not result.allowed
    assert "approval_required" in result.reasons


def test_high_risk_self_development_requires_human_approval() -> None:
    change = SelfDevelopmentChange(
        change_id="c1",
        risk="high",
        branch_ref="refs/heads/agent/c1",
        artifact_digest="a" * 16,
        test_evidence_ids=["t1"],
    )
    result = SelfHealingExecutionService.gate_self_development(
        change,
        approval_present=True,
        signed_artifact=True,
    )
    assert not result.allowed
    assert "human_approval_required" in result.reasons

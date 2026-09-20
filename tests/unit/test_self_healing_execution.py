"""Tests for governed self-healing execution gates."""

from cfip.application.health_invariants import HealthInvariantService
from cfip.application.self_healing_execution import SelfHealingExecutionService
from cfip.domain.health_invariants import HealthInvariantObservation
from cfip.domain.self_healing_execution import (
    RepairExecutionRequest,
    SelfDevelopmentChange,
    SelfHealingExecutionPolicy,
)


_IDS = (
    "tests_green", "security_clean", "data_quality_healthy", "risk_gate_intact",
    "audit_chain_healthy", "authorization_intact", "market_data_fresh",
)


def _request() -> RepairExecutionRequest:
    return RepairExecutionRequest(
        proposal_id="repair-1",
        artifact_kind="cache",
        artifact_digest="a" * 16,
        rollback_digest="b" * 16,
        test_evidence_ids=["test-1"],
        security_evidence_ids=["security-1"],
        safety_invariant_ids=["safe-1"],
    )


def _health(*, failed: str | None = None):
    return HealthInvariantService.evaluate([
        HealthInvariantObservation(
            invariant_id=item, passed=item != failed, observed_at=1,
            evidence_ids=[f"e-{item}"], detail="healthy" if item != failed else "failed",
        )
        for item in _IDS
    ])


def test_execution_is_blocked_without_signed_artifact() -> None:
    result = SelfHealingExecutionService.gate(
        _request(), SelfHealingExecutionPolicy(), approval_present=False,
        signed_artifact=False, consecutive_repairs=0, cooldown_elapsed=True,
    )
    assert not result.allowed
    assert "signed_artifact_required" in result.reasons


def test_execution_requires_approval_for_code() -> None:
    request = _request().model_copy(update={"artifact_kind": "code"})
    result = SelfHealingExecutionService.gate(
        request, SelfHealingExecutionPolicy(), approval_present=False,
        signed_artifact=True, consecutive_repairs=0, cooldown_elapsed=True,
    )
    assert not result.allowed
    assert "approval_required" in result.reasons


def test_failed_risk_invariant_blocks_execution_even_with_other_gates_ready() -> None:
    result = SelfHealingExecutionService.gate(
        _request(), SelfHealingExecutionPolicy(), approval_present=True,
        signed_artifact=True, consecutive_repairs=0, cooldown_elapsed=True,
        health_report=_health(failed="risk_gate_intact"),
    )
    assert not result.allowed
    assert "health_invariants_blocking" in result.reasons
    assert "health_invariant:risk_gate_intact" in result.reasons


def test_healthy_snapshot_does_not_add_invariant_block() -> None:
    result = SelfHealingExecutionService.gate(
        _request(), SelfHealingExecutionPolicy(), approval_present=True,
        signed_artifact=True, consecutive_repairs=0, cooldown_elapsed=True,
        health_report=_health(),
    )
    assert result.allowed


def test_self_development_cannot_reach_production_by_default() -> None:
    change = SelfDevelopmentChange(
        change_id="c1", branch_ref="refs/heads/agent/c1",
        artifact_digest="a" * 16, test_evidence_ids=["t1"],
    )
    result = SelfHealingExecutionService.gate_self_development(
        change, approval_present=True, signed_artifact=True,
    )
    assert not result.allowed
    assert "production_apply_not_granted" in result.reasons

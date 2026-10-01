"""Tests for self-healing safety boundaries."""

from cfip.application.self_healing import SelfHealingService
from cfip.domain.self_healing import HealthSignal, SelfHealingPolicy


def test_self_healing_defaults_to_approval() -> None:
    signal = HealthSignal(
        id="health-1", component="analysis", metric="latency_ms",
        value=900, threshold=500, severity="warning", observed_at=1,
    )
    diagnosis = SelfHealingService().diagnose(
        signal, evidence_ids=["health-1"], root_cause="upstream latency",
        confidence=0.9, blast_radius="local", reversible=True,
    )
    proposal = SelfHealingService().propose(
        diagnosis, action="restart", rationale="restore responsiveness",
        expected_effect="reduce latency", rollback_plan="restore previous process",
        policy=SelfHealingPolicy(),
    )
    assert proposal.requires_approval
    assert not proposal.can_auto_apply


def test_self_healing_can_auto_apply_only_with_explicit_policy() -> None:
    signal = HealthSignal(
        id="health-2", component="cache", metric="hit_rate",
        value=0.2, threshold=0.5, severity="warning", observed_at=1,
    )
    service = SelfHealingService()
    diagnosis = service.diagnose(
        signal, evidence_ids=["health-2"], root_cause="stale cache",
        confidence=0.95, blast_radius="local", reversible=True,
    )
    proposal = service.propose(
        diagnosis, action="invalidate_cache", rationale="remove stale entries",
        expected_effect="restore cache correctness", rollback_plan="repopulate from source",
        policy=SelfHealingPolicy(allow_auto_apply_reversible=True),
    )
    assert proposal.can_auto_apply
    assert not proposal.requires_approval

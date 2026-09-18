"""Deterministic guardrails for autonomous diagnosis and repair proposals."""

from cfip.domain.self_healing import HealthSignal, Diagnosis, RepairProposal, SelfHealingPolicy


class SelfHealingService:
    def diagnose(
        self,
        signal: HealthSignal,
        *,
        evidence_ids: list[str],
        root_cause: str,
        confidence: float,
        blast_radius: str,
        reversible: bool,
    ) -> Diagnosis:
        return Diagnosis(
            id=f"diagnosis:{signal.id}",
            component=signal.component,
            root_cause=root_cause,
            confidence=confidence,
            evidence_ids=evidence_ids,
            blast_radius=blast_radius,
            reversible=reversible,
        )

    def propose(
        self,
        diagnosis: Diagnosis,
        *,
        action: str,
        rationale: str,
        expected_effect: str,
        rollback_plan: str,
        policy: SelfHealingPolicy,
    ) -> RepairProposal:
        can_auto = (
            policy.allow_auto_apply_reversible
            and diagnosis.reversible
            and diagnosis.blast_radius in {"local", "component"}
            and diagnosis.blast_radius == policy.max_blast_radius
            and bool(rollback_plan.strip())
        )
        return RepairProposal(
            id=f"repair:{diagnosis.id}:{action}",
            diagnosis_id=diagnosis.id,
            component=diagnosis.component,
            action=action,
            rationale=rationale,
            expected_effect=expected_effect,
            rollback_plan=rollback_plan,
            status="diagnosed",
            requires_approval=not can_auto,
            can_auto_apply=can_auto,
        )

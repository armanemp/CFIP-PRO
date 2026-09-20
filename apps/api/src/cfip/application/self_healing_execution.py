"""Pure policy gate for self-healing execution; executors remain outside the domain."""

from cfip.domain.health_invariants import HealthInvariantReport
from cfip.domain.self_healing_execution import (
    ExecutionGateResult,
    RepairExecutionRequest,
    SelfDevelopmentChange,
    SelfHealingExecutionPolicy,
)


class SelfHealingExecutionService:
    @staticmethod
    def gate(
        request: RepairExecutionRequest,
        policy: SelfHealingExecutionPolicy,
        *,
        approval_present: bool,
        signed_artifact: bool,
        consecutive_repairs: int,
        consecutive_failures: int = 0,
        cooldown_elapsed: bool,
        health_report: HealthInvariantReport | None = None,
    ) -> ExecutionGateResult:
        reasons: list[str] = []
        mutation_requires_approval = request.artifact_kind in {
            "code",
            "dependency",
            "service",
            "config",
            "index",
        }
        if (
            request.artifact_kind == "code"
            and policy.require_approval_for_code
            and not approval_present
        ):
            reasons.append("approval_required")
        if (
            request.artifact_kind == "dependency"
            and policy.require_approval_for_dependency
            and not approval_present
        ):
            reasons.append("approval_required")
        if policy.require_signed_artifact and not signed_artifact:
            reasons.append("signed_artifact_required")
        if policy.require_rollback_digest and not request.rollback_digest:
            reasons.append("rollback_artifact_required")
        if policy.require_canary and not request.canary_required:
            reasons.append("canary_required")
        if (
            policy.require_security_evidence_for_mutation
            and mutation_requires_approval
            and not request.security_evidence_ids
        ):
            reasons.append("security_evidence_required")
        if len(request.test_evidence_ids) < policy.min_test_evidence:
            reasons.append("insufficient_test_evidence")
        if consecutive_repairs >= policy.max_consecutive_repairs:
            reasons.append("repair_budget_exhausted")
        if consecutive_failures >= policy.circuit_breaker_after_failures:
            reasons.append("circuit_breaker_open")
        if not cooldown_elapsed:
            reasons.append("repair_cooldown")
        if health_report is not None and not health_report.healthy:
            reasons.append("health_invariants_blocking")
            reasons.extend(f"health_invariant:{item}" for item in health_report.blocking_invariants)
        return ExecutionGateResult(allowed=not reasons, reasons=reasons)

    @staticmethod
    def gate_self_development(
        change: SelfDevelopmentChange,
        *,
        approval_present: bool,
        signed_artifact: bool,
        health_report: HealthInvariantReport | None = None,
    ) -> ExecutionGateResult:
        reasons: list[str] = []
        if change.review_required and not approval_present:
            reasons.append("review_required")
        if not approval_present:
            reasons.append("production_approval_required")
        if not signed_artifact:
            reasons.append("signed_artifact_required")
        if not change.test_evidence_ids:
            reasons.append("test_evidence_required")
        if not change.security_evidence_ids:
            reasons.append("security_evidence_required")
        if not change.production_apply_allowed:
            reasons.append("production_apply_not_granted")
        if health_report is not None and not health_report.healthy:
            reasons.append("health_invariants_blocking")
            reasons.extend(f"health_invariant:{item}" for item in health_report.blocking_invariants)
        return ExecutionGateResult(allowed=not reasons, reasons=reasons)

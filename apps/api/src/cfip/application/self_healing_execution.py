"""Pure policy gate for self-healing execution; executors remain outside the domain."""
from cfip.domain.self_healing_execution import ExecutionGateResult, RepairExecutionRequest, SelfDevelopmentChange, SelfHealingExecutionPolicy

class SelfHealingExecutionService:
    @staticmethod
    def gate(request: RepairExecutionRequest, policy: SelfHealingExecutionPolicy, *, approval_present: bool, signed_artifact: bool, consecutive_repairs: int, cooldown_elapsed: bool) -> ExecutionGateResult:
        reasons=[]
        if policy.require_approval_for_code and request.artifact_kind=="code" and not approval_present:
            reasons.append("approval_required")
        if policy.require_signed_artifact and not signed_artifact:
            reasons.append("signed_artifact_required")
        if policy.require_rollback_digest and not request.rollback_digest:
            reasons.append("rollback_artifact_required")
        if policy.require_canary and not request.canary_required:
            reasons.append("canary_required")
        if len(request.test_evidence_ids)<policy.min_test_evidence:
            reasons.append("insufficient_test_evidence")
        if consecutive_repairs>=policy.max_consecutive_repairs:
            reasons.append("repair_budget_exhausted")
        if not cooldown_elapsed:
            reasons.append("repair_cooldown")
        return ExecutionGateResult(allowed=not reasons,reasons=reasons)

    @staticmethod
    def gate_self_development(change: SelfDevelopmentChange, *, approval_present: bool, signed_artifact: bool) -> ExecutionGateResult:
        reasons=[]
        if not change.review_required: reasons.append("review_required")
        if not approval_present: reasons.append("production_approval_required")
        if not signed_artifact: reasons.append("signed_artifact_required")
        if not change.test_evidence_ids: reasons.append("test_evidence_required")
        if not change.production_apply_allowed: reasons.append("production_apply_not_granted")
        return ExecutionGateResult(allowed=not reasons,reasons=reasons)

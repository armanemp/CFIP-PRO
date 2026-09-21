"""Governed remote Git/GitHub control-plane facade.

This service deliberately does not implement provider-specific HTTP calls. A GitHub
adapter can be added without leaking GitHub semantics into MIOS domain contracts.
"""
from cfip.application.autonomy import AutonomyService
from cfip.domain.git_platform_contracts import RemoteGitAdapter, RemoteGitRequest, RemoteGitResult

class GovernedRemoteGitService:
    def __init__(self, adapter: RemoteGitAdapter, autonomy: AutonomyService | None = None) -> None:
        self.adapter = adapter
        self.autonomy = autonomy or AutonomyService()

    def execute(self, request: RemoteGitRequest) -> RemoteGitResult:
        changed_files = len(request.paths)
        action = "merge" if request.operation == "merge" else (
            "deploy" if request.operation == "rollback" else "pull_request"
        )
        decision = self.autonomy.evaluate(
            action=action,
            paths=request.paths,
            risk=request.risk,
            changed_files=changed_files,
        )
        if decision.requires_human_approval:
            return RemoteGitResult(
                accepted=False,
                operation=request.operation,
                requires_human_approval=True,
                message="human_approval_required",
            )
        return self.adapter.execute(request)

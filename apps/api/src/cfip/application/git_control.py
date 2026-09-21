"""Governed application facade for local Git operations."""
from cfip.application.autonomy import AutonomyService
from cfip.domain.git_control import GitChangeProposal, GitScope, direct_main_commit_allowed, proposal_paths_allowed
from cfip.infrastructure.git.local import LocalGit

class GovernedGitService:
    def __init__(self, git: LocalGit, autonomy: AutonomyService | None = None) -> None:
        self.git = git
        self.autonomy = autonomy or AutonomyService()

    def status(self) -> str:
        return self.git.status()

    def diff(self, staged: bool = False) -> str:
        return self.git.diff(staged=staged)

    def commit(self, proposal: GitChangeProposal) -> dict[str, object]:
        scope = GitScope(repository=proposal.repository, allowed_paths=proposal.paths)
        if not proposal_paths_allowed(proposal, scope):
            raise PermissionError("protected_git_path")
        if not direct_main_commit_allowed(proposal):
            raise PermissionError("direct_main_commit_forbidden")
        decision = self.autonomy.evaluate(
            action="commit", paths=proposal.paths, risk=proposal.risk, changed_files=len(proposal.paths)
        )
        if decision.requires_human_approval:
            raise PermissionError("human_approval_required")
        commit = self.git.commit(message=proposal.title, paths=proposal.paths)
        return {"commit": commit, "risk": decision.risk, "requires_human_approval": False}

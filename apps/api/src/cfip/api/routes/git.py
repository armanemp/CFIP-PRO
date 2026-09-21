"""Read-only Git control-plane status; mutation requires a separately authorized executor."""
from fastapi import APIRouter
from cfip.domain.git_control import GitScope
from cfip.domain.autonomy_policy import AutonomyPolicy

router = APIRouter(prefix="/git", tags=["git"])

@router.get("/policy")
async def policy() -> dict[str, object]:
    scope = GitScope(repository="configured")
    return {
        "repository": scope.repository,
        "operations": ["read","diff","branch","commit","pull_request"],
        "mutation_requires_approval": True,
        "shell_execution": False,
        "protected_path_prefixes": list(scope.protected_paths),
    }

@router.get("/autonomy-policy")
async def autonomy_policy() -> dict[str, object]:
    policy = AutonomyPolicy()
    return {
        "autonomous_risks": list(policy.autonomous_risks),
        "human_approval_risks": list(policy.approval_risks),
        "protected_paths": list(policy.protected_paths),
        "protected_actions": list(policy.protected_actions),
        "direct_main_commit": False,
    }

"""Read-only Git control-plane status; mutation requires a separately authorized executor."""
from fastapi import APIRouter
from cfip.domain.git_control import GitScope

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

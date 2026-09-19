"""Governed Git control-plane contracts.

Repository intelligence may inspect state and prepare a change, but mutation is an
explicit, auditable operation. This domain contract never executes a shell command.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

GitOperation = Literal["read","diff","branch","commit","pull_request"]
GitRisk = Literal["low","medium","high","critical"]

class GitScope(BaseModel):
    model_config = ConfigDict(extra="forbid")
    repository: str = Field(min_length=1, max_length=200)
    allowed_paths: tuple[str, ...] = ()
    protected_paths: tuple[str, ...] = (
        ".github/workflows/",
        ".github/",
        ".env",
        ".env.",
        "secrets/",
        "apps/api/src/cfip/core/config.py",
    )

class GitChangeProposal(BaseModel):
    model_config = ConfigDict(extra="forbid")
    proposal_id: str = Field(min_length=1, max_length=128)
    operation: GitOperation
    repository: str = Field(min_length=1, max_length=200)
    base_ref: str = Field(min_length=1, max_length=200)
    title: str = Field(min_length=1, max_length=200)
    rationale: str = Field(min_length=1, max_length=4000)
    paths: tuple[str, ...] = ()
    evidence: tuple[str, ...] = ()
    validation_plan: tuple[str, ...] = ()
    rollback_plan: tuple[str, ...] = ()
    risk: GitRisk = "medium"
    requires_approval: bool = True

class GitAuthorization(BaseModel):
    model_config = ConfigDict(extra="forbid")
    proposal_id: str
    actor: str = Field(min_length=1, max_length=200)
    approved: bool
    approval_reason: str = Field(default="", max_length=2000)
    expires_at: int | None = Field(default=None, gt=0)

def path_allowed(path: str, scope: GitScope) -> bool:
    normalized = path.replace("\\", "/").lstrip("/")
    if any(normalized.startswith(prefix) for prefix in scope.protected_paths):
        return False
    return not scope.allowed_paths or any(
        normalized == allowed or normalized.startswith(allowed.rstrip("/") + "/")
        for allowed in scope.allowed_paths
    )

def proposal_paths_allowed(proposal: GitChangeProposal, scope: GitScope) -> bool:
    return all(path_allowed(path, scope) for path in proposal.paths)

def direct_main_commit_allowed(proposal: GitChangeProposal) -> bool:
    return proposal.operation != "commit" or proposal.base_ref not in {"main", "master"}

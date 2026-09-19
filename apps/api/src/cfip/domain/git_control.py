"""Governed Git control-plane contracts.

The intelligence layer can inspect repository state and propose changes, but production
mutation is represented as an explicit, auditable command. No shell execution lives here.
"""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

GitOperation = Literal["read","diff","branch","commit","pull_request"]

class GitScope(BaseModel):
    model_config = ConfigDict(extra="forbid")
    repository: str = Field(min_length=1, max_length=200)
    allowed_paths: tuple[str, ...] = ()
    protected_paths: tuple[str, ...] = (
        ".github/workflows/",
        ".env",
        ".env.",
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
    requires_approval: bool = True

class GitAuthorization(BaseModel):
    model_config = ConfigDict(extra="forbid")
    proposal_id: str
    actor: str
    approved: bool
    approval_reason: str = Field(default="", max_length=2000)

def path_allowed(path: str, scope: GitScope) -> bool:
    normalized = path.replace("\\", "/").lstrip("/")
    if any(normalized.startswith(prefix) for prefix in scope.protected_paths):
        return False
    return not scope.allowed_paths or any(
        normalized == allowed or normalized.startswith(allowed.rstrip("/") + "/")
        for allowed in scope.allowed_paths
    )

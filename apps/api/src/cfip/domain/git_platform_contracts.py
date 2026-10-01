"""Git/GitHub-level control-plane contracts; execution stays behind adapters."""
from typing import Literal, Protocol
from pydantic import BaseModel, ConfigDict, Field

RemoteGitOperation = Literal["branch", "commit", "pull_request", "review", "check", "merge", "rollback"]

class RemoteGitRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    repository: str = Field(min_length=3, max_length=200)
    operation: RemoteGitOperation
    base_ref: str = Field(min_length=1, max_length=200)
    head_ref: str | None = Field(default=None, max_length=200)
    paths: tuple[str, ...] = ()
    title: str = Field(default="", max_length=200)
    body: str = Field(default="", max_length=10000)
    risk: Literal["low", "medium", "high", "critical"] = "medium"

class RemoteGitResult(BaseModel):
    model_config = ConfigDict(extra="forbid")
    accepted: bool
    operation: RemoteGitOperation
    external_id: str | None = None
    url: str | None = None
    requires_human_approval: bool = False
    message: str = ""

class RemoteGitAdapter(Protocol):
    id: str
    version: str
    def execute(self, request: RemoteGitRequest) -> RemoteGitResult: ...

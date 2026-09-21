"""MIOS autonomy policy: low/medium risk may self-apply; high/critical requires approval."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

Risk = Literal["low", "medium", "high", "critical"]
Action = Literal["inspect", "test", "branch", "commit", "pull_request", "merge", "deploy"]

class AutonomyDecision(BaseModel):
    model_config = ConfigDict(extra="forbid")
    allowed: bool
    risk: Risk
    requires_human_approval: bool
    reasons: tuple[str, ...] = ()

class AutonomyPolicy(BaseModel):
    model_config = ConfigDict(extra="forbid")
    autonomous_risks: tuple[Risk, ...] = ("low", "medium")
    approval_risks: tuple[Risk, ...] = ("high", "critical")
    protected_paths: tuple[str, ...] = (
        ".github/", ".env", ".env.", "secrets/",
        "infra/production/", "apps/api/src/cfip/core/config.py",
    )
    protected_actions: tuple[Action, ...] = ("merge", "deploy")
    max_changed_files_without_approval: int = Field(default=30, ge=1, le=1000)

    def evaluate(self, *, action: Action, paths: tuple[str, ...], risk: Risk, changed_files: int = 1) -> AutonomyDecision:
        reasons: list[str] = []
        normalized = tuple(p.replace("\\", "/").lstrip("/") for p in paths)
        protected = any(any(p.startswith(prefix) for prefix in self.protected_paths) for p in normalized)
        if protected:
            risk = "high"
            reasons.append("protected_path")
        if action in self.protected_actions:
            risk = "high"
            reasons.append("protected_action")
        if changed_files > self.max_changed_files_without_approval:
            risk = "high"
            reasons.append("change_budget_exceeded")
        approval = risk in self.approval_risks
        if approval:
            reasons.append("human_approval_required")
        return AutonomyDecision(
            allowed=True,
            risk=risk,
            requires_human_approval=approval,
            reasons=tuple(reasons),
        )

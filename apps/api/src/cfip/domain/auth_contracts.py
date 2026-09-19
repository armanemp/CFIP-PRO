"""Authentication and authorization contracts."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

Role = Literal["user","pro","admin","service"]
Permission = Literal["terminal:read","terminal:trade","research:read","intelligence:read","intelligence:propose","git:read","git:propose","git:apply","admin:settings"]

class Principal(BaseModel):
    model_config = ConfigDict(extra="forbid")
    subject: str = Field(min_length=1, max_length=160)
    roles: tuple[Role, ...] = ("user",)

class AuthorizationDecision(BaseModel):
    model_config = ConfigDict(extra="forbid")
    allowed: bool
    permission: Permission
    reason: str = ""

ROLE_PERMISSIONS: dict[Role, frozenset[Permission]] = {
    "user": frozenset({"terminal:read","research:read","intelligence:read","git:read"}),
    "pro": frozenset({"terminal:read","terminal:trade","research:read","intelligence:read","git:read"}),
    "admin": frozenset({"terminal:read","terminal:trade","research:read","intelligence:read","intelligence:propose","git:read","git:propose","git:apply","admin:settings"}),
    "service": frozenset({"terminal:read","research:read","intelligence:read","intelligence:propose","git:read"}),
}

def authorize(principal: Principal, permission: Permission) -> AuthorizationDecision:
    allowed = any(permission in ROLE_PERMISSIONS[role] for role in principal.roles)
    return AuthorizationDecision(allowed=allowed, permission=permission, reason="role_grant" if allowed else "permission_denied")

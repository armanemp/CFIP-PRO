from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

AuditAction = Literal["read","create","update","delete","approve","reject","promote","rollback","execute"]

class AuditEvent(BaseModel):
    model_config = ConfigDict(extra="forbid")
    event_id: str = Field(min_length=1, max_length=128)
    occurred_at: int = Field(gt=0)
    actor: str = Field(min_length=1, max_length=200)
    action: AuditAction
    resource: str = Field(min_length=1, max_length=300)
    resource_id: str | None = None
    correlation_id: str = Field(min_length=1, max_length=128)
    outcome: Literal["accepted","rejected","failed","dry-run"] = "accepted"
    metadata: dict[str, str] = {}

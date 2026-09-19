"""Deterministic bootstrap seeds. Runtime provisioning must remain idempotent."""
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field

class SeedIdentity(BaseModel):
    model_config = ConfigDict(extra="forbid")
    key: str
    role: Literal["user","admin","service"]
    display_name: str
    secret_ref: str = Field(min_length=1)

class SeedBundle(BaseModel):
    model_config = ConfigDict(extra="forbid")
    schema_version: int = 1
    identities: tuple[SeedIdentity, ...]
    plans: tuple[str, ...] = ("free","pro")

DEFAULT_SEEDS = SeedBundle(
    identities=(
        SeedIdentity(key="bootstrap-user", role="user", display_name="CFIP User", secret_ref="secrets/bootstrap/user"),
        SeedIdentity(key="bootstrap-admin", role="admin", display_name="CFIP Administrator", secret_ref="secrets/bootstrap/admin"),
        SeedIdentity(key="cfip-service", role="service", display_name="CFIP Service", secret_ref="secrets/bootstrap/service"),
    ),
)

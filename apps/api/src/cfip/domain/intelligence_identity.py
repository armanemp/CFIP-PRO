"""Canonical MIOS identity for the entire platform."""
from pydantic import BaseModel, ConfigDict, Field

DEFAULT_INTELLIGENCE_NAME = "MIOS"

class IntelligenceIdentity(BaseModel):
    model_config = ConfigDict(extra="forbid")
    name: str = Field(default=DEFAULT_INTELLIGENCE_NAME, min_length=2, max_length=48)
    short_name: str = Field(default="MIOS", min_length=2, max_length=12)
    domain: str = Field(default="finance.market-intelligence", min_length=3, max_length=64)
    description: str = Field(
        default="Advanced evidence-grounded financial market intelligence with learning, self-healing and governed self-development.",
        min_length=8,
        max_length=240,
    )

DEFAULT_INTELLIGENCE_IDENTITY = IntelligenceIdentity()

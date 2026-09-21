"""Configurable identity for the platform intelligence layer.

The identity is presentation/configuration data, not executable policy. Keeping it in a
small contract prevents the intelligence name from becoming a hard-coded UI/backend concern.
"""
from pydantic import BaseModel, ConfigDict, Field


DEFAULT_INTELLIGENCE_NAME = "MIOS"


class IntelligenceIdentity(BaseModel):
    """Human-facing identity metadata for the platform intelligence."""

    model_config = ConfigDict(extra="forbid")

    name: str = Field(default=DEFAULT_INTELLIGENCE_NAME, min_length=2, max_length=48)
    short_name: str = Field(default="MIOS", min_length=2, max_length=12)
    domain: str = Field(default="finance.forex", min_length=3, max_length=64)
    description: str = Field(
        default="Evidence-grounded financial and foreign-exchange intelligence.",
        min_length=8,
        max_length=240,
    )


DEFAULT_INTELLIGENCE_IDENTITY = IntelligenceIdentity()

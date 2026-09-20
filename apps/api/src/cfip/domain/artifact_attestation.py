"""Content-addressed artifact attestation primitives.

Digest verification is real cryptographic hashing using the Python standard
library. Signature verification remains delegated to a configured verifier;
the platform never treats a caller-supplied signature as verified by itself.
"""
import hashlib
import hmac
from typing import Literal
from pydantic import BaseModel, ConfigDict, Field


class ArtifactAttestation(BaseModel):
    model_config = ConfigDict(extra="forbid")
    digest: str = Field(min_length=64, max_length=128)
    algorithm: Literal["sha256"] = "sha256"
    signature: str | None = None
    key_id: str | None = Field(default=None, min_length=1, max_length=256)
    verified_at: int | None = Field(default=None, gt=0)


def compute_sha256(artifact: bytes) -> str:
    return hashlib.sha256(artifact).hexdigest()


def verify_sha256(artifact: bytes, expected_digest: str) -> bool:
    if len(expected_digest) != 64:
        return False
    return hmac.compare_digest(compute_sha256(artifact), expected_digest.lower())

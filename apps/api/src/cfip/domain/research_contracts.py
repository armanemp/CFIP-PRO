"""Vendor-neutral research acquisition and evidence contracts owned by MIOS."""
from typing import Protocol
from pydantic import BaseModel, ConfigDict, Field

class ResearchSource(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    name: str = Field(min_length=1, max_length=200)
    uri: str
    enabled: bool = True
    freshness_seconds: int = Field(default=86400, ge=0)
    allowed_content: tuple[str, ...] = ()
    rights_note: str = ""

class EvidenceRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")
    id: str = Field(min_length=1, max_length=128)
    source_id: str
    observed_at: int = Field(gt=0)
    content_hash: str = Field(min_length=8, max_length=256)
    claim: str = Field(min_length=1, max_length=4000)
    confidence: float = Field(ge=0, le=1)
    supersedes: str | None = None

class ResearchRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    url: str = Field(min_length=8, max_length=4096)
    timeout_seconds: int = Field(default=15, ge=1, le=60)

class ResearchDocument(BaseModel):
    model_config = ConfigDict(extra="forbid")
    url: str
    title: str = ""
    text: str = Field(min_length=1)
    content_digest: str = Field(min_length=16, max_length=128)
    extractor: str
    extractor_version: str
    fetched_at_epoch: int = Field(gt=0)

class ResearchAdapter(Protocol):
    id: str
    version: str
    def available(self) -> bool: ...
    def extract(self, request: ResearchRequest) -> ResearchDocument: ...

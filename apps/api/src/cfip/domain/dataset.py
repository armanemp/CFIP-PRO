"""Immutable dataset lineage contracts for the intelligence training fabric."""

from pydantic import BaseModel, ConfigDict, Field, model_validator


class DatasetManifest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    dataset_id: str = Field(min_length=1, max_length=128)
    schema_version: str = Field(min_length=1, max_length=32)
    created_at: int = Field(gt=0)
    example_ids: list[str] = Field(min_length=1, max_length=100000)
    source_hashes: list[str] = Field(min_length=1, max_length=100000)
    feature_schema: dict[str, str] = Field(min_length=1, max_length=128)
    label_policy: str = Field(min_length=1, max_length=2000)
    provenance_hash: str = Field(min_length=16, max_length=128)
    immutable: bool = True

    @model_validator(mode="after")
    def validate_manifest(self) -> "DatasetManifest":
        if len(set(self.example_ids)) != len(self.example_ids):
            raise ValueError("dataset_example_ids_must_be_unique")
        if len(set(self.source_hashes)) != len(self.source_hashes):
            raise ValueError("dataset_source_hashes_must_be_unique")
        if not self.immutable:
            raise ValueError("dataset_manifest_must_be_immutable")
        return self

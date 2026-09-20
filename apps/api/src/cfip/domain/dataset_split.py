"""Leakage-safe temporal dataset split contracts for the training fabric.

The training fabric must never infer future labels or allow the same example to
cross train/validation/test boundaries. Splits are deterministic and evaluated
against an explicit as-of boundary.
"""
from pydantic import BaseModel, ConfigDict, Field, model_validator


class DatasetExample(BaseModel):
    model_config = ConfigDict(extra="forbid")
    example_id: str = Field(min_length=1, max_length=128)
    observed_at: int = Field(gt=0)
    source_hash: str = Field(min_length=16, max_length=128)


class TemporalDatasetSplit(BaseModel):
    model_config = ConfigDict(extra="forbid")
    train_ids: tuple[str, ...] = ()
    validation_ids: tuple[str, ...] = ()
    test_ids: tuple[str, ...] = ()
    as_of: int = Field(gt=0)

    @model_validator(mode="after")
    def validate_boundaries(self) -> "TemporalDatasetSplit":
        groups = {
            "train": set(self.train_ids),
            "validation": set(self.validation_ids),
            "test": set(self.test_ids),
        }
        if any(not ids for ids in groups.values()):
            raise ValueError("dataset_split_requires_all_partitions")
        if (
            (groups["train"] & groups["validation"])
            or (groups["train"] & groups["test"])
            or (groups["validation"] & groups["test"])
        ):
            raise ValueError("dataset_split_overlap")
        return self


def build_temporal_split(
    examples: list[DatasetExample],
    *,
    train_end: int,
    validation_end: int,
    as_of: int,
) -> TemporalDatasetSplit:
    if not examples:
        raise ValueError("dataset_examples_required")
    if not (train_end < validation_end <= as_of):
        raise ValueError("dataset_split_boundaries_invalid")
    seen: set[str] = set()
    for example in examples:
        if example.example_id in seen:
            raise ValueError("dataset_example_ids_must_be_unique")
        seen.add(example.example_id)
        if example.observed_at > as_of:
            raise ValueError("future_example_after_as_of")
    train = tuple(e.example_id for e in examples if e.observed_at <= train_end)
    validation = tuple(
        e.example_id for e in examples if train_end < e.observed_at <= validation_end
    )
    test = tuple(
        e.example_id for e in examples if validation_end < e.observed_at <= as_of
    )
    return TemporalDatasetSplit(
        train_ids=train,
        validation_ids=validation,
        test_ids=test,
        as_of=as_of,
    )

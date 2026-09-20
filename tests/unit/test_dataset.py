"""Tests for immutable intelligence dataset manifests."""

import pytest
from pydantic import ValidationError

from cfip.domain.dataset import DatasetManifest


def _manifest(**updates):
    data = {
        "dataset_id": "dataset-1",
        "schema_version": "1",
        "created_at": 1,
        "example_ids": ["example-1", "example-2"],
        "source_hashes": ["a" * 16, "b" * 16],
        "feature_schema": {"score": "float"},
        "label_policy": "closed-bar outcome only",
        "provenance_hash": "c" * 16,
    }
    data.update(updates)
    return DatasetManifest(**data)


def test_manifest_is_immutable_by_contract() -> None:
    assert _manifest().immutable


def test_duplicate_examples_are_rejected() -> None:
    with pytest.raises(ValidationError, match="dataset_example_ids_must_be_unique"):
        _manifest(example_ids=["example-1", "example-1"])


def test_duplicate_source_hashes_are_rejected() -> None:
    with pytest.raises(ValidationError, match="dataset_source_hashes_must_be_unique"):
        _manifest(source_hashes=["a" * 16, "a" * 16])


def test_mutable_manifest_is_rejected() -> None:
    with pytest.raises(ValidationError, match="dataset_manifest_must_be_immutable"):
        _manifest(immutable=False)

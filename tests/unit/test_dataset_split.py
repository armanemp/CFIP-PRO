import pytest
from cfip.domain.dataset_split import DatasetExample, TemporalDatasetSplit, build_temporal_split


def _examples():
    return [
        DatasetExample(example_id="a", observed_at=10, source_hash="a"*16),
        DatasetExample(example_id="b", observed_at=20, source_hash="b"*16),
        DatasetExample(example_id="c", observed_at=30, source_hash="c"*16),
    ]


def test_temporal_split_has_disjoint_partitions() -> None:
    split = build_temporal_split(_examples(), train_end=10, validation_end=20, as_of=30)
    assert split.train_ids == ("a",)
    assert split.validation_ids == ("b",)
    assert split.test_ids == ("c",)


def test_future_example_is_rejected() -> None:
    examples = _examples() + [DatasetExample(example_id="future", observed_at=31, source_hash="f"*16)]
    with pytest.raises(ValueError, match="future_example_after_as_of"):
        build_temporal_split(examples, train_end=10, validation_end=20, as_of=30)


def test_overlap_is_rejected() -> None:
    with pytest.raises(ValueError, match="dataset_split_overlap"):
        TemporalDatasetSplit(train_ids=("a",), validation_ids=("a",), test_ids=("b",), as_of=30)

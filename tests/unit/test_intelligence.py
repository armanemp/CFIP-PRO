"""Tests for governed intelligence contracts."""

import pytest
from pydantic import ValidationError

from cfip.domain.intelligence import IntelligenceEvidence, LearningRecord


def test_learning_requires_auditable_evidence() -> None:
    with pytest.raises(ValidationError, match="learning_requires_evidence"):
        LearningRecord(
            id="lesson-1",
            type="trade_outcome",
            created_at=1,
            subject="EUR/USD",
            lesson="Outcome confirms the original hypothesis.",
            confidence=0.8,
        )


def test_learning_can_be_recorded_as_candidate() -> None:
    record = LearningRecord(
        id="lesson-2",
        type="trade_outcome",
        created_at=1,
        subject="EUR/USD",
        evidence_ids=["outcome-1"],
        lesson="The setup failed after a liquidity sweep.",
        confidence=0.7,
    )
    assert record.status == "candidate"
    assert record.validation_count == 0


def test_evidence_contract_bounds_confidence_and_freshness() -> None:
    evidence = IntelligenceEvidence(
        id="market-1",
        kind="market",
        source="reference",
        observed_at=1,
        content_hash="abcdef12",
        freshness_seconds=30,
        confidence=0.95,
    )
    assert evidence.confidence == 0.95

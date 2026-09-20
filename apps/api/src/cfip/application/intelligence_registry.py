"""Application boundary for Elyrava improvement records.

The registry is deliberately storage-agnostic. A database adapter can implement the
same repository protocol without changing the intelligence lifecycle contracts.
"""

from collections.abc import Sequence
from typing import Protocol

from cfip.domain.intelligence_governance import ImprovementProposal, LearningOutcome, ValidationResult


class IntelligenceRecordRepository(Protocol):
    def save_proposal(self, proposal: ImprovementProposal) -> ImprovementProposal: ...
    def save_validation(self, result: ValidationResult) -> ValidationResult: ...
    def save_learning(self, outcome: LearningOutcome) -> LearningOutcome: ...
    def proposals(self) -> Sequence[ImprovementProposal]: ...
    def validations(self) -> Sequence[ValidationResult]: ...
    def learnings(self) -> Sequence[LearningOutcome]: ...


class InMemoryIntelligenceRecordRepository:
    """Reference implementation for tests/local operation; no persistence is implied."""

    def __init__(self) -> None:
        self._proposals: dict[str, ImprovementProposal] = {}
        self._validations: dict[str, ValidationResult] = {}
        self._learnings: dict[str, LearningOutcome] = {}

    def save_proposal(self, proposal: ImprovementProposal) -> ImprovementProposal:
        self._proposals[proposal.id] = proposal
        return proposal

    def save_validation(self, result: ValidationResult) -> ValidationResult:
        self._validations[result.proposal_id] = result
        return result

    def save_learning(self, outcome: LearningOutcome) -> LearningOutcome:
        self._learnings[outcome.proposal_id] = outcome
        return outcome

    def proposals(self) -> Sequence[ImprovementProposal]:
        return tuple(self._proposals.values())

    def validations(self) -> Sequence[ValidationResult]:
        return tuple(self._validations.values())

    def learnings(self) -> Sequence[LearningOutcome]:
        return tuple(self._learnings.values())

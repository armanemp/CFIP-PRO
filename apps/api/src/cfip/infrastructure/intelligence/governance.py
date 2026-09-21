"""Deterministic governance decisions for Elyrava improvement proposals."""

from cfip.domain.intelligence_governance import ImprovementProposal, ProposalRisk, ValidationResult

_AUTO_PROMOTABLE: frozenset[ProposalRisk] = frozenset({"low"})


def requires_human_approval(proposal: ImprovementProposal) -> bool:
    """Return whether a proposal must enter the admin approval queue."""
    return proposal.requires_human_approval or proposal.risk not in _AUTO_PROMOTABLE


def can_promote(proposal: ImprovementProposal, validation: ValidationResult) -> bool:
    """Promotion requires complete validation and no rollback trigger."""
    return validation.passed and not validation.rollback_required and all(validation.checks.values())

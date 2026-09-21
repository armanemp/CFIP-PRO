"""Pure transition policy for governed self-development proposals."""

from cfip.domain.self_development import ImprovementStage

_ALLOWED: dict[ImprovementStage, frozenset[ImprovementStage]] = {
    "observed": frozenset({"diagnosed", "rejected"}),
    "diagnosed": frozenset({"proposed", "rejected"}),
    "proposed": frozenset({"validated", "rejected"}),
    "validated": frozenset({"approved", "rejected"}),
    "approved": frozenset({"applied", "rejected"}),
    "applied": frozenset({"rolled_back"}),
    "rolled_back": frozenset(),
    "rejected": frozenset(),
}


def can_transition(current: ImprovementStage, target: ImprovementStage) -> bool:
    """Return whether a proposal can move one governed step."""
    return target in _ALLOWED[current]


def require_transition(current: ImprovementStage, target: ImprovementStage) -> None:
    if not can_transition(current, target):
        raise ValueError(f"invalid_improvement_transition:{current}->{target}")

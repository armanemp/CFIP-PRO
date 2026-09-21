"""Application facade for MIOS autonomous mutation decisions."""
from cfip.domain.autonomy_policy import Action, AutonomyDecision, AutonomyPolicy, Risk

class AutonomyService:
    def __init__(self, policy: AutonomyPolicy | None = None) -> None:
        self.policy = policy or AutonomyPolicy()

    def evaluate(self, *, action: Action, paths: tuple[str, ...] = (), risk: Risk = "low", changed_files: int = 1) -> AutonomyDecision:
        return self.policy.evaluate(action=action, paths=paths, risk=risk, changed_files=changed_files)

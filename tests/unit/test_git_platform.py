from cfip.application.git_platform import GovernedRemoteGitService
from cfip.domain.git_platform_contracts import RemoteGitRequest, RemoteGitResult

class FakeRemote:
    id = "fake"
    version = "1"
    def execute(self, request: RemoteGitRequest) -> RemoteGitResult:
        return RemoteGitResult(accepted=True, operation=request.operation, external_id="1")

def test_remote_git_high_risk_stops_before_adapter() -> None:
    service = GovernedRemoteGitService(FakeRemote())
    result = service.execute(
        RemoteGitRequest(
            repository="armanemp/CFIP-PRO",
            operation="merge",
            base_ref="main",
            risk="low",
        )
    )
    assert result.accepted is False
    assert result.requires_human_approval is True

def test_remote_git_low_risk_can_reach_adapter() -> None:
    service = GovernedRemoteGitService(FakeRemote())
    result = service.execute(
        RemoteGitRequest(
            repository="armanemp/CFIP-PRO",
            operation="branch",
            base_ref="main",
            risk="low",
        )
    )
    assert result.accepted is True

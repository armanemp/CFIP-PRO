"""ChartWorkspace use cases kept independent from persistence implementation."""
from cfip.domain.workspace_contracts import ChartWorkspace
from cfip.domain.workspace_service_contracts import WorkspaceRepository

class WorkspaceService:
    def __init__(self, repository: WorkspaceRepository) -> None:
        self.repository = repository

    async def get(self, user_id: str, workspace_id: str) -> ChartWorkspace | None:
        return await self.repository.get(user_id, workspace_id)

    async def list(self, user_id: str) -> tuple[ChartWorkspace, ...]:
        return await self.repository.list(user_id)

    async def save(self, user_id: str, workspace: ChartWorkspace) -> ChartWorkspace:
        return await self.repository.save(workspace)

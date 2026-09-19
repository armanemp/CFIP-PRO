from cfip.domain.git_control import GitChangeProposal, GitScope, path_allowed, proposal_paths_allowed, direct_main_commit_allowed

def test_git_scope_blocks_workflow_and_env_files() -> None:
    scope = GitScope(repository="repo", allowed_paths=("apps/", "docs/"))
    assert path_allowed("apps/web/page.tsx", scope)
    assert not path_allowed(".github/workflows/ci.yml", scope)
    assert not path_allowed(".env", scope)

def test_git_proposal_rejects_protected_paths() -> None:
    scope = GitScope(repository="repo", allowed_paths=("apps/", "docs/"))
    proposal = GitChangeProposal(
        proposal_id="p1",
        operation="commit",
        repository="repo",
        base_ref="feature/x",
        title="UI improvement",
        rationale="evidence",
        paths=("apps/web/page.tsx", ".github/workflows/ci.yml"),
        validation_plan=("typecheck",),
        rollback_plan=("revert",),
    )
    assert not proposal_paths_allowed(proposal, scope)

def test_direct_main_commit_is_forbidden() -> None:
    proposal = GitChangeProposal(
        proposal_id="p2",
        operation="commit",
        repository="repo",
        base_ref="main",
        title="change",
        rationale="evidence",
        paths=("apps/web/page.tsx",),
    )
    assert not direct_main_commit_allowed(proposal)

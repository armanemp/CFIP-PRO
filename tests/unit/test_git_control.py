from cfip.domain.git_control import GitScope, path_allowed

def test_git_scope_blocks_workflow_and_env_files() -> None:
    scope = GitScope(repository="repo", allowed_paths=("apps/", "docs/"))
    assert path_allowed("apps/web/page.tsx", scope)
    assert not path_allowed(".github/workflows/ci.yml", scope)
    assert not path_allowed(".env", scope)

def test_git_scope_blocks_paths_outside_allowlist() -> None:
    scope = GitScope(repository="repo", allowed_paths=("apps/",))
    assert not path_allowed("infra/prod.tf", scope)

from pathlib import Path
import pytest
from cfip.infrastructure.git.local import LocalGit

def test_git_path_validation_rejects_traversal(tmp_path: Path) -> None:
    git = LocalGit(tmp_path)
    with pytest.raises(ValueError, match="invalid_git_path"):
        git._validate_path("../outside.txt")

def test_git_ref_validation_rejects_flags() -> None:
    with pytest.raises(ValueError, match="invalid_git_ref"):
        LocalGit._validate_ref("-force")

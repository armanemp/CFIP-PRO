"""Minimal local Git adapter with no shell interpolation.

The adapter is intentionally infrastructure-only. Web/API callers must pass through
the governance service before invoking mutation methods.
"""
from __future__ import annotations

import subprocess
from pathlib import Path

class GitCommandError(RuntimeError):
    pass

class LocalGit:
    def __init__(self, repository: Path, timeout_seconds: int = 20) -> None:
        self.repository = repository.resolve()
        self.timeout_seconds = timeout_seconds

    def _run(self, *args: str) -> str:
        try:
            result = subprocess.run(
                ["git", *args],
                cwd=self.repository,
                check=True,
                capture_output=True,
                text=True,
                timeout=self.timeout_seconds,
                shell=False,
            )
        except (OSError, subprocess.CalledProcessError, subprocess.TimeoutExpired) as exc:
            raise GitCommandError(str(exc)) from exc
        return result.stdout.strip()

    def status(self) -> str:
        return self._run("status", "--short", "--branch")

    def current_branch(self) -> str:
        return self._run("branch", "--show-current")

    def branches(self) -> str:
        return self._run("branch", "--format=%(refname:short)")

    def log(self, limit: int = 20) -> str:
        if not 1 <= limit <= 200:
            raise ValueError("invalid_log_limit")
        return self._run("log", f"-{limit}", "--oneline", "--decorate")

    def diff(self, staged: bool = False) -> str:
        return self._run("diff", "--cached" if staged else "--")

    def create_branch(self, name: str) -> str:
        self._validate_ref(name)
        return self._run("switch", "-c", name)

    def commit(self, *, message: str, paths: tuple[str, ...]) -> str:
        if not message.strip():
            raise ValueError("empty_commit_message")
        safe_paths = tuple(self._validate_path(p) for p in paths)
        if not safe_paths:
            raise ValueError("commit_paths_required")
        self._run("add", "--", *safe_paths)
        return self._run("commit", "-m", message)

    def _validate_path(self, value: str) -> str:
        normalized = value.replace("\\", "/").lstrip("/")
        if not normalized or normalized == "." or normalized.startswith("../") or "/../" in normalized:
            raise ValueError("invalid_git_path")
        candidate = (self.repository / normalized).resolve()
        if self.repository != candidate and self.repository not in candidate.parents:
            raise ValueError("path_outside_repository")
        return normalized

    @staticmethod
    def _validate_ref(value: str) -> None:
        if not value or value.startswith("-") or ".." in value or " " in value:
            raise ValueError("invalid_git_ref")

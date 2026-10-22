"""Detect and bump project semver sources."""

from __future__ import annotations

import json
import re
from dataclasses import dataclass
from pathlib import Path

SEMVER_RE = re.compile(r"^(\d+)\.(\d+)\.(\d+)(?:[-+][0-9A-Za-z.-]+)?$")
PYPROJECT_VERSION_RE = re.compile(
    r'(?m)^(\s*version\s*=\s*)(["\'])([^"\']+)\2'
)
PACKAGE_JSON_VERSION_RE = re.compile(
    r'(?m)^(\s*"version"\s*:\s*)(")([^"]+)(")'
)


@dataclass(frozen=True)
class VersionSource:
    kind: str  # version_file | pyproject | package_json
    path: Path
    version: str


def bump_semver(version: str, level: str) -> str:
    match = SEMVER_RE.match(version.strip())
    if not match:
        raise ValueError(f"not a semver X.Y.Z version: {version!r}")
    major, minor, patch = (int(match.group(i)) for i in range(1, 4))
    level = level.lower().strip()
    if level == "major":
        return f"{major + 1}.0.0"
    if level == "minor":
        return f"{major}.{minor + 1}.0"
    if level == "patch":
        return f"{major}.{minor}.{patch + 1}"
    raise ValueError(f"level must be major|minor|patch, got {level!r}")


def detect_version_source(repo: Path) -> VersionSource | None:
    version_file = repo / "VERSION"
    if version_file.is_file():
        text = version_file.read_text(encoding="utf-8").strip()
        if text:
            return VersionSource("version_file", version_file, text.splitlines()[0].strip())

    pyproject = repo / "pyproject.toml"
    if pyproject.is_file():
        text = pyproject.read_text(encoding="utf-8")
        match = PYPROJECT_VERSION_RE.search(text)
        if match:
            return VersionSource("pyproject", pyproject, match.group(3).strip())

    package_json = repo / "package.json"
    if package_json.is_file():
        data = json.loads(package_json.read_text(encoding="utf-8"))
        ver = data.get("version")
        if isinstance(ver, str) and ver.strip():
            return VersionSource("package_json", package_json, ver.strip())

    return None


def read_version(source: VersionSource) -> str:
    return source.version


def write_version(source: VersionSource, new_version: str) -> None:
    path = source.path
    if source.kind == "version_file":
        path.write_text(new_version + "\n", encoding="utf-8")
        return
    text = path.read_text(encoding="utf-8")
    if source.kind == "pyproject":
        updated, n = PYPROJECT_VERSION_RE.subn(
            lambda m: f'{m.group(1)}{m.group(2)}{new_version}{m.group(2)}',
            text,
            count=1,
        )
        if n != 1:
            raise ValueError(f"could not update version in {path}")
        path.write_text(updated, encoding="utf-8")
        return
    if source.kind == "package_json":
        updated, n = PACKAGE_JSON_VERSION_RE.subn(
            lambda m: f'{m.group(1)}"{new_version}"',
            text,
            count=1,
        )
        if n != 1:
            raise ValueError(f"could not update version in {path}")
        path.write_text(updated, encoding="utf-8")
        return
    raise ValueError(f"unknown version source kind: {source.kind}")

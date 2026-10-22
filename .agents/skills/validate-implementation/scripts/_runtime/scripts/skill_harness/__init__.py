"""Shared helpers for compiled skill harnesses.

Deterministic parsing and rewriting used by thin skill bootloaders.
See ``.agent/SKILL_COMPILATION.md``.
"""

from __future__ import annotations

from scripts.skill_harness.acceptance import AcceptanceCriterion, parse_acceptance_criteria
from scripts.skill_harness.changelog import promote_unreleased
from scripts.skill_harness.io_util import emit, find_repo_root
from scripts.skill_harness.version import VersionSource, bump_semver, detect_version_source, read_version, write_version

__all__ = [
    "AcceptanceCriterion",
    "VersionSource",
    "bump_semver",
    "detect_version_source",
    "emit",
    "find_repo_root",
    "parse_acceptance_criteria",
    "promote_unreleased",
    "read_version",
    "write_version",
]

"""Parse acceptance criteria (AC-n) from specs."""

from __future__ import annotations

import re
from dataclasses import asdict, dataclass

# Matches "AC-1", "**AC-1**", "`AC-1`", "- AC-1:", "AC-1 | ..." etc.
AC_LINE_RE = re.compile(
    r"(?im)^(?:\s*[-*]\s+|\s*\d+\.\s+)?\*?\*?`?(AC-\d+)`?\*?\*?\s*[:.\-|–—)]?\s*(.+?)\s*$"
)
# Also catch table rows: | AC-1 | criterion |
AC_TABLE_RE = re.compile(
    r"(?im)^\|\s*`?(AC-\d+)`?\s*\|\s*([^|]+?)\s*\|"
)


@dataclass(frozen=True)
class AcceptanceCriterion:
    id: str
    text: str

    def to_dict(self) -> dict[str, str]:
        return asdict(self)


def parse_acceptance_criteria(spec_text: str) -> list[AcceptanceCriterion]:
    """Extract unique AC-n criteria in first-seen order."""
    found: dict[str, AcceptanceCriterion] = {}

    for match in AC_TABLE_RE.finditer(spec_text):
        ac_id = _norm_id(match.group(1))
        text = match.group(2).strip()
        if text.lower() in {"criterion", "criteria", "description", "---", ":---"}:
            continue
        if ac_id not in found:
            found[ac_id] = AcceptanceCriterion(ac_id, text)

    for match in AC_LINE_RE.finditer(spec_text):
        ac_id = _norm_id(match.group(1))
        text = match.group(2).strip().strip("*").strip()
        if ac_id not in found:
            found[ac_id] = AcceptanceCriterion(ac_id, text)

    # Sort by numeric id for stable output
    items = list(found.values())
    items.sort(key=lambda c: int(c.id.split("-", 1)[1]))
    return items


def _norm_id(raw: str) -> str:
    m = re.match(r"(?i)AC-(\d+)", raw.strip())
    if not m:
        raise ValueError(f"invalid AC id: {raw!r}")
    return f"AC-{int(m.group(1))}"


VERDICTS = frozenset({"Verified", "Failed", "Unknown"})


def render_validation_report(
    title: str,
    rows: list[dict[str, str]],
) -> str:
    """Render the standard validation report markdown."""
    lines = [
        f"## Validation report — {title}",
        "",
        "| AC | Criterion (short) | Verdict | Evidence |",
        "|----|-------------------|---------|----------|",
    ]
    verified = failed = unknown = 0
    for row in rows:
        verdict = row.get("verdict", "Unknown")
        if verdict not in VERDICTS:
            raise ValueError(f"invalid verdict {verdict!r}; expected one of {sorted(VERDICTS)}")
        if verdict == "Verified":
            verified += 1
            if not (row.get("evidence") or "").strip():
                raise ValueError(f"{row.get('id')}: Verified requires non-empty evidence")
        elif verdict == "Failed":
            failed += 1
        else:
            unknown += 1
        crit = (row.get("criterion") or "").replace("|", "/").strip()
        evidence = (row.get("evidence") or "").replace("|", "/").strip()
        lines.append(
            f"| {row.get('id', '')} | {crit} | {verdict} | {evidence} |"
        )
    lines.append("")
    lines.append(
        f"Overall: {verified} verified / {failed} failed / {unknown} unknown."
    )
    if failed or unknown:
        lines.append("Blockers or next steps: resolve Failed/Unknown before claiming the spec is satisfied.")
    else:
        lines.append("All acceptance criteria Verified.")
    lines.append("")
    return "\n".join(lines)

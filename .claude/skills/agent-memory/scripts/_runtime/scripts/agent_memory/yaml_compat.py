"""Small PyYAML compatibility wrapper with a JSON fallback for tests/minimal envs."""

from __future__ import annotations

import json
from typing import Any

try:  # pragma: no cover - exercised when PyYAML is installed
    import yaml as _pyyaml
except ModuleNotFoundError:  # pragma: no cover - fallback is covered in minimal envs
    _pyyaml = None


if _pyyaml is not None:  # pragma: no cover - depends on optional dependency
    YAMLError = _pyyaml.YAMLError
else:

    class YAMLError(ValueError):
        """Raised when fallback YAML parsing fails."""


def safe_load(text: Any) -> Any:
    if hasattr(text, "read"):
        text = text.read()
    if _pyyaml is not None:
        return _pyyaml.safe_load(text)
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return _simple_yaml_load(str(text))


def _simple_yaml_load(text: str) -> Any:
    """Parse the limited flat/list YAML emitted in this repo when PyYAML is absent."""
    result: dict[str, Any] = {}
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        line = lines[i]
        if not line.strip():
            i += 1
            continue
        if line.startswith("  ") or ":" not in line:
            i += 1
            continue
        key, raw = line.split(":", 1)
        key = key.strip().strip('"')
        raw = raw.strip()
        if raw == "[]":
            result[key] = []
        elif raw == "":
            values: list[Any] = []
            i += 1
            while i < len(lines) and lines[i].startswith("  - "):
                item_text = lines[i][4:].strip()
                try:
                    values.append(json.loads(item_text))
                except json.JSONDecodeError:
                    values.append(item_text.strip('"'))
                i += 1
            result[key] = values
            continue
        else:
            result[key] = raw.strip('"')
        i += 1
    if result:
        return result
    raise YAMLError("unsupported YAML without PyYAML installed")


def safe_dump(data: Any, **kwargs: Any) -> str:
    if _pyyaml is not None:
        return _pyyaml.safe_dump(data, **kwargs)
    indent = None if kwargs.get("default_flow_style") else 2
    return json.dumps(data, indent=indent, ensure_ascii=False) + "\n"

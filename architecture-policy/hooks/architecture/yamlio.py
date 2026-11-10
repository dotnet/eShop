"""YAML loading that keeps dates as strings so documents round-trip through JSON Schema."""
from __future__ import annotations

from pathlib import Path

import yaml


class _PolicyLoader(yaml.SafeLoader):
    pass


# Drop the implicit timestamp resolver: '2026-11-09' stays a string.
_PolicyLoader.yaml_implicit_resolvers = {
    key: [(tag, regexp) for tag, regexp in resolvers if tag != "tag:yaml.org,2002:timestamp"]
    for key, resolvers in yaml.SafeLoader.yaml_implicit_resolvers.items()
}


def load_yaml(path: Path):
    with open(path, "r", encoding="utf-8") as fh:
        return yaml.load(fh, Loader=_PolicyLoader)  # noqa: S506 - SafeLoader subclass


def loads_yaml(text: str):
    return yaml.load(text, Loader=_PolicyLoader)  # noqa: S506

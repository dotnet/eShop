"""Loads and indexes the Architecture Policy Pack. The only reader of policy files."""
from __future__ import annotations

import hashlib
import importlib
import json
import sys
from pathlib import Path

from .yamlio import load_yaml

PACK_ROOT = Path(__file__).resolve().parents[2]
CHECKPOINTS = ("pre_commit", "code_review", "testing", "ci")


class PolicyError(Exception):
    pass


class PolicyPack:
    def __init__(self, root: Path | None = None, repo_root: Path | None = None, profile: str | None = None):
        self.root = Path(root or PACK_ROOT).resolve()
        self.repo_root = Path(repo_root or self.root.parent).resolve()
        self.load_errors: list[str] = []
        self.schema_errors: list[str] = []
        self.duplicate_ids: list[str] = []
        self.documents: dict[str, dict] = {}  # relative path -> document

        self.schema = json.loads((self.root / "architecture-rule.schema.json").read_text(encoding="utf-8"))
        self.manifest = self._load("policy.yaml", "PolicyManifest") or {}
        self.profile_id = profile or self.manifest.get("default_profile", "dotnet")
        self.profile = self._load(f"profiles/{self.profile_id}/profile.yaml", "Profile") or {}

        self.rules: dict[str, dict] = {}
        self.rule_files: dict[str, str] = {}
        for cat in self.manifest.get("categories", []):
            doc = self._load(cat["file"], "RuleSet")
            if not doc:
                continue
            for rule in doc.get("rules", []):
                self._index(self.rules, rule, cat["file"])
                self.rule_files[rule.get("id")] = cat["file"]

        self.patterns: dict[str, dict] = {}
        for path in sorted((self.root / "patterns").glob("*.yaml")):
            rel = path.relative_to(self.root).as_posix()
            doc = self._load(rel, "Pattern")
            if doc:
                self._index(self.patterns, doc, rel)

        registry = self._load(self.manifest.get("validators", {}).get("registry", "validators/registry.yaml"), "ValidatorRegistry") or {}
        self.validators: dict[str, dict] = {}
        for v in registry.get("validators", []):
            self._index(self.validators, v, "validators/registry.yaml")

        exc_doc = self._load(self.manifest.get("exceptions", {}).get("file", "exceptions/exceptions.yaml"), "ExceptionRegister") or {}
        self.exceptions: list[dict] = exc_doc.get("exceptions", []) or []
        seen = set()
        for e in self.exceptions:
            if e.get("id") in seen:
                self.duplicate_ids.append(f"exception {e.get('id')}")
            seen.add(e.get("id"))

        ev_doc = self._load(self.manifest.get("evidence", {}).get("types_file", "evidence/evidence-types.yaml"), "EvidenceTypes") or {}
        self.evidence_types: dict[str, dict] = {}
        for t in ev_doc.get("evidence_types", []):
            self._index(self.evidence_types, t, "evidence/evidence-types.yaml")

        self._impl_cache: dict[str, object] = {}

    # -- loading -------------------------------------------------------------------
    def _load(self, rel: str, kind: str) -> dict | None:
        path = self.root / rel
        try:
            doc = load_yaml(path)
        except FileNotFoundError:
            self.load_errors.append(f"{rel}: file not found")
            return None
        except Exception as exc:  # yaml errors
            self.load_errors.append(f"{rel}: YAML parse error: {exc}")
            return None
        if not isinstance(doc, dict):
            self.load_errors.append(f"{rel}: expected a mapping document")
            return None
        if doc.get("kind") != kind:
            self.schema_errors.append(f"{rel}: kind must be {kind!r}, found {doc.get('kind')!r}")
        self.schema_errors.extend(f"{rel}: {e}" for e in self.validate_document(doc, kind))
        self.documents[rel] = doc
        return doc

    def validate_document(self, doc: dict, kind: str) -> list[str]:
        try:
            from jsonschema import Draft202012Validator
        except ImportError:  # selfcheck reports this; runtime validation continues
            return []
        sub = dict(self.schema)
        sub["$ref"] = f"#/$defs/{kind}"
        validator = Draft202012Validator(sub)
        errors = []
        for err in sorted(validator.iter_errors(doc), key=lambda e: list(e.absolute_path)):
            loc = "/".join(str(p) for p in err.absolute_path) or "<root>"
            errors.append(f"{loc}: {err.message}")
        return errors

    def _index(self, table: dict, item: dict, source: str):
        item_id = item.get("id")
        if item_id in table:
            self.duplicate_ids.append(f"{item_id} (in {source})")
        table[item_id] = item

    # -- accessors -------------------------------------------------------------------
    def active_rules(self) -> list[dict]:
        return [r for _, r in sorted(self.rules.items()) if r.get("status") == "active"]

    def checkpoint_config(self, checkpoint: str) -> dict:
        return self.manifest.get("checkpoints", {}).get(checkpoint, {})

    def implementation(self, ref: str):
        """Resolve 'module:function' from the validators directory."""
        if ref in self._impl_cache:
            return self._impl_cache[ref]
        module_name, func_name = ref.split(":", 1)
        impl_dir = str(self.root / self.manifest.get("validators", {}).get("implementation_dir", "validators"))
        if impl_dir not in sys.path:
            sys.path.insert(0, impl_dir)
        module = importlib.import_module(module_name)
        fn = getattr(module, func_name)
        if not callable(fn):
            raise PolicyError(f"{ref} is not callable")
        self._impl_cache[ref] = fn
        return fn

    def content_hash(self) -> str:
        h = hashlib.sha256()
        for path in sorted(self.root.rglob("*")):
            if not path.is_file():
                continue
            rel = path.relative_to(self.root).as_posix()
            if rel.startswith("evidence/runs/") or "__pycache__" in rel or rel.startswith("tests/"):
                continue
            if path.suffix in (".yaml", ".yml", ".json", ".py", ".md", ".sh", ".ps1"):
                h.update(rel.encode())
                h.update(path.read_bytes().replace(b"\r\n", b"\n"))
        return h.hexdigest()


_PACK_CACHE: dict[tuple, PolicyPack] = {}


def get_pack(root: Path | None = None, repo_root: Path | None = None, profile: str | None = None, reload: bool = False) -> PolicyPack:
    key = (str(root), str(repo_root), profile)
    if reload or key not in _PACK_CACHE:
        _PACK_CACHE[key] = PolicyPack(root, repo_root, profile)
    return _PACK_CACHE[key]

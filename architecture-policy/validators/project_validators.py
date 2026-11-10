"""Deterministic project-graph validators over .csproj files."""
from __future__ import annotations

import posixpath
import re

from _vcommon import fail, normalize

PROJECT_REF = re.compile(r'<ProjectReference\s+[^>]*?Include\s*=\s*"([^"]+)"', re.S)
PACKAGE_REF = re.compile(r'<PackageReference\s+[^>]*?Include\s*=\s*"([^"]+)"[^>]*?>', re.S)
INLINE_VERSION = re.compile(r'<PackageReference\s+[^>]*?\b(Version|VersionOverride)\s*=\s*"[^"]*"', re.S)


def _csproj_files(ctx):
    return [f for f in ctx.files if f.endswith(".csproj")]


def _references(ctx, path, text):
    base = posixpath.dirname(path)
    for m in PROJECT_REF.finditer(text):
        target_path = normalize(posixpath.normpath(posixpath.join(base, normalize(m.group(1)))))
        target = ctx.index.project_by_dir(posixpath.dirname(target_path)) or ctx.index.project_for(target_path)
        yield m, target_path, target


def layer_dependencies(ctx) -> list[dict]:
    """Project references must follow profile.layer_dependencies; every project must be classified."""
    rules = ctx.profile.get("layer_dependencies", {})
    results = []
    for path in _csproj_files(ctx):
        text = ctx.read(path) or ""
        source = ctx.index.project_for(path)
        if source is None or not source.get("layer"):
            results.append(fail(path, None, None, "Project is not classified in profiles/dotnet/profile.yaml (type/layer/service required)."))
            continue
        allowed = rules.get(source["layer"], [])
        for m, target_path, target in _references(ctx, path, text):
            if "*" in allowed:
                continue
            if target is None:
                results.append(fail(path, text, m.start(), f"Reference to unclassified project '{target_path}'."))
                continue
            ok = any(a == "shared" and target.get("layer") == "shared"
                     or a.startswith("same_service:") and target.get("layer") == a.split(":", 1)[1]
                     for a in allowed)
            if not ok:
                results.append(fail(path, text, m.start(),
                                    f"{source['layer']} project '{source['name']}' must not reference {target.get('layer')} project '{target['name']}'."))
    return results


def forbidden_packages(ctx) -> list[dict]:
    """Packages forbidden for a layer (profile.forbidden_packages_by_layer)."""
    by_layer = ctx.profile.get("forbidden_packages_by_layer", {})
    results = []
    for path in _csproj_files(ctx):
        source = ctx.index.project_for(path)
        forbidden = by_layer.get(source.get("layer") if source else "", [])
        if not forbidden:
            continue
        text = ctx.read(path) or ""
        for m in PACKAGE_REF.finditer(text):
            pkg = m.group(1)
            if any(pkg == f or pkg.startswith(f if f.endswith(".") else f + ".") or pkg == f.rstrip(".") for f in forbidden):
                results.append(fail(path, text, m.start(), f"Package '{pkg}' is forbidden in the {source['layer']} layer."))
    return results


def service_isolation(ctx) -> list[dict]:
    """A service project may not reference a project owned by a different service."""
    results = []
    for path in _csproj_files(ctx):
        source = ctx.index.project_for(path)
        if not source or not source.get("service"):
            continue
        text = ctx.read(path) or ""
        for m, _, target in _references(ctx, path, text):
            if target and target.get("service") and target["service"] != source["service"]:
                results.append(fail(path, text, m.start(),
                                    f"Service '{source['service']}' references '{target['name']}' owned by service '{target['service']}'. "
                                    "Integrate through APIs or integration events instead."))
    return results


def central_package_management(ctx) -> list[dict]:
    """Package versions are managed centrally in Directory.Packages.props."""
    results = []
    for path in _csproj_files(ctx):
        text = ctx.read(path) or ""
        for m in INLINE_VERSION.finditer(text):
            results.append(fail(path, text, m.start(),
                                f"PackageReference declares '{m.group(1)}'; set the version in Directory.Packages.props."))
    return results

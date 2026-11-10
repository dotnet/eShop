"""Profile index: resolves files to projects, services, layers, traits and datastore owners."""
from __future__ import annotations

from .globs import normalize


class ProjectIndex:
    def __init__(self, profile: dict):
        self.profile = profile
        self.projects = sorted(profile.get("projects", []), key=lambda p: len(p["path"]), reverse=True)
        self.services = {s["id"]: s for s in profile.get("services", [])}
        self._datastore_owner = {}
        for s in profile.get("services", []):
            for d in s.get("owns_datastores", []):
                self._datastore_owner[d.lower()] = s["id"]

    def project_for(self, path: str) -> dict | None:
        path = normalize(path)
        for proj in self.projects:
            root = proj["path"].rstrip("/") + "/"
            if path.startswith(root) or path == proj["path"]:
                return proj
        return None

    def project_by_dir(self, directory: str) -> dict | None:
        directory = normalize(directory).rstrip("/")
        for proj in self.projects:
            if proj["path"].rstrip("/") == directory:
                return proj
        return None

    def service_for(self, path: str) -> dict | None:
        proj = self.project_for(path)
        if proj and proj.get("service"):
            return self.services.get(proj["service"])
        return None

    def traits_for(self, path: str) -> set:
        svc = self.service_for(path)
        return set(svc.get("traits", [])) if svc else set()

    def datastore_owner(self, name: str) -> str | None:
        return self._datastore_owner.get(name.lower())

    def project_files(self, files: list[str], project: dict) -> list[str]:
        root = project["path"].rstrip("/") + "/"
        return [f for f in files if f.startswith(root)]

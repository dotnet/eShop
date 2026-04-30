# Copilot Instructions for eShop

Start with [AGENTS.md](../AGENTS.md), then follow the nearest scoped `AGENTS.md` for the files being changed.

## Copilot-Specific Guidance

- Prefer the scoped AGENTS files over duplicating context in this file; they are the project index for future sessions.
- For tests, use `dotnet test --project <test-project.csproj>`; bare test directory paths fail in this repo.
- When changing integration events, check every service-local copy of the event contract, not just the publisher.


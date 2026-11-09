# Agent instructions

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- graphify-out/ is built locally (gitignored except the semantic cache). If graphify-out/graph.json is missing, run `scripts/graphify-build.sh` first (no API cost, a few seconds; replays the shared semantic cache), then follow the rules below.
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
- graphify-out/cache/semantic/ is committed and shared, so everyone gets the LLM-extracted doc/image nodes without an API key. After adding or changing docs or images (.md, .pdf, diagrams), run `scripts/graphify-build.sh --extract` and commit only the new files under graphify-out/cache/semantic/. Never commit anything else in graphify-out/.

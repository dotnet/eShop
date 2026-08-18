---
name: dotnet-inspect
description: "Query .NET APIs across NuGet packages, platform libraries, and local files. Search for types, list API surfaces, compare and diff versions, find extension methods and implementors. Use whenever you need to answer questions about .NET library contents."
---

# dotnet-inspect

Query .NET library APIs — the same commands work across NuGet packages, platform libraries (System.*, Microsoft.AspNetCore.*), and local .dll/.nupkg files.

## Quick Decision Tree

- **Code broken?** → `diff --package Foo@old..new` first, then `member --oneline`
- **Need API surface?** → `member Type --package Foo --oneline` (token-efficient)
- **Need signatures?** → `member Type --package Foo -m Method` (default shows full signatures + docs)
- **Need source/IL?** → `member Type --package Foo -m Method -v:d` (adds Source, Lowered C#, IL)
- **Need constructors?** → `member 'Type<T>' --package Foo -m .ctor` (use `<T>` not `<>`)
- **Need all overloads?** → `member Type --package Foo --select` (shows `Name:N` indices)

## When to Use This Skill

- **"What types are in this package?"** — `type` discovers types (terse), `find` searches by pattern
- **"What's the API surface?"** — `type` for discovery, `member` for detailed inspection (docs on)
- **"What changed between versions?"** — `diff` classifies breaking/additive changes
- **"This code uses an old API — fix it"** — `diff` the old..new version, then `member --oneline` to see the new API
- **"What extends this type?"** — `extensions` finds extension methods/properties
- **"What implements this interface?"** — `implements` finds concrete types
- **"What does this type depend on?"** — `depends` walks the type hierarchy upward
- **"What version/metadata does this have?"** — `package` and `library` inspect metadata
- **"Show me something cool"** — `demo` runs curated showcase queries

## Key Patterns

Use `--oneline` as the default for scanning — it works on `type`, `member`, `find`, `diff`, and `implements`:

```bash
dnx dotnet-inspect -y -- member JsonSerializer --package System.Text.Json --oneline  # scan members
dnx dotnet-inspect -y -- type --package System.Text.Json --oneline                   # scan types
dnx dotnet-inspect -y -- diff --package System.CommandLine@2.0.0-beta4.22272.1..2.0.3 --oneline  # triage changes
```

Use `--shape` to understand a type's hierarchy and surface at a glance:

```bash
dnx dotnet-inspect -y -- type 'HashSet<T>' --platform System.Collections --shape
```

Use `diff` first when fixing broken code — `--oneline` for triage, then full detail on specific types:

```bash
dnx dotnet-inspect -y -- diff --package System.CommandLine@2.0.0-beta4.22272.1..2.0.3 --oneline  # what changed?
dnx dotnet-inspect -y -- diff -t Command --package System.CommandLine@2.0.0-beta4.22272.1..2.0.3  # detail on Command
dnx dotnet-inspect -y -- member Command --package System.CommandLine@2.0.3 --oneline              # new API surface
```

## Search Scope

Search commands (`find`, `extensions`, `implements`, `depends`) use scope flags:

- **(no flags)** — platform frameworks + Microsoft.Extensions.AI
- **`--platform`** — all platform frameworks
- **`--extensions`** — curated Microsoft.Extensions.* packages
- **`--aspnetcore`** — curated Microsoft.AspNetCore.* packages
- **`--package Foo`** — specific NuGet package (combinable with scope flags)

`type`, `member`, `library`, `diff` accept `--platform <name>` as a string for a specific platform library.

## Command Reference

| Command | Purpose |
| ------- | ------- |
| `type` | **Discover types** — terse output, no docs, use `--shape` for hierarchy |
| `member` | **Inspect members** — docs on by default, supports dotted syntax (`-m Type.Member`) |
| `find` | Search for types by glob pattern across any scope |
| `diff` | Compare API surfaces between versions — breaking/additive classification |
| `extensions` | Find extension methods/properties for a type |
| `implements` | Find types implementing an interface or extending a base class |
| `depends` | Walk the type dependency hierarchy upward (interfaces, base classes) |
| `package` | Package metadata, files, versions, dependencies, `search` for NuGet discovery |
| `library` | Library metadata, symbols, references, dependencies |
| `demo` | Run curated showcase queries — list, invoke, or feeling-lucky |

## Output Limiting

**Do not pipe output through `head`, `tail`, or `Select-Object`.** The tool has built-in line limiting that preserves headers and formatting:

```bash
dnx dotnet-inspect -y -- member JsonSerializer --package System.Text.Json --oneline -10  # first 10 lines
dnx dotnet-inspect -y -- find "*Logger*" -n 5                                            # first 5 lines
dnx dotnet-inspect -y -- member JsonSerializer --package System.Text.Json -v:q -s Methods  # select specific section
```

- **`-n N` or `-N`** — line limit, like `head`. Keeps headers, truncates cleanly.
- **`-s Section`** — show only a specific section (glob-capable). Use `-s` alone to list available sections.
- **`-v:q`** — quiet verbosity for compact summary output.

## Key Syntax

- **Generic types** need quotes: `'Option<T>'`, `'IEnumerable<T>'`
- **Use `<T>` not `<>`** for generic types — `"Option<>"` resolves to the abstract base, `'Option<T>'` resolves to the concrete generic with constructors
- **`type` uses `-t`** for type filtering, **`member` uses `-m`** for member filtering (not `--filter`)
- **Dotted syntax** for `member`: `-m JsonSerializer.Deserialize`
- **Diff ranges** use `..`: `--package System.Text.Json@9.0.0..10.0.0`
- **Signatures** include `params` and default values from metadata
- **Derived types** only show their own members — query the base type too (e.g., `RootCommand` inherits `Add()` and `SetAction()` from `Command`)

## Installation

Use `dnx` (like `npx`). Always use `-y` and `--` to prevent interactive prompts:

```bash
dnx dotnet-inspect -y -- <command>
```

## Full Documentation

For comprehensive syntax, edge cases, and the flag compatibility matrix:

```bash
dnx dotnet-inspect -y -- llmstxt
```
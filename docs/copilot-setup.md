# GitHub Copilot setup with Graphify and T3

This guide gets a new developer from zero to using GitHub Copilot in this repository, both from the terminal and inside [T3 Code](https://t3.codes). It covers Windows and macOS.

1. [Install the GitHub Copilot CLI](#1-install-the-github-copilot-cli)
2. [Add Copilot to T3 Code as a provider](#2-add-copilot-to-t3-code-as-a-provider)
3. [Enable graphify for Copilot](#3-enable-graphify-for-copilot)
4. [Verify the setup](#4-verify-the-setup)

## Prerequisites

- An active GitHub Copilot subscription (individual, or through the organization). If you get Copilot through an organization, the Copilot CLI policy must be enabled for it.
- Git, and this repository cloned locally.
- **Windows:** PowerShell 6 or later (`pwsh`). Check with `$PSVersionTable.PSVersion`.
- **Optional:** Node.js 22 or later, only if you prefer to install the CLI through npm.

## 1. Install the GitHub Copilot CLI

### Windows

Install with WinGet from PowerShell:

```powershell
winget install GitHub.Copilot
```

Alternatively, with Node.js 22+:

```powershell
npm install -g @github/copilot
```

Open a **new** terminal so the updated `PATH` is picked up, then check the install:

```powershell
copilot --version
```

### macOS

Install with Homebrew:

```bash
brew install --cask copilot-cli
```

Alternatively, use the install script or npm (Node.js 22+):

```bash
curl -fsSL https://gh.io/copilot-install | bash
# or
npm install -g @github/copilot
```

The install script places the binary in `~/.local/bin`. Make sure that directory is on your `PATH` (add `export PATH="$HOME/.local/bin:$PATH"` to `~/.zshrc` if needed), then check the install:

```bash
copilot --version
```

### Sign in (both platforms)

From the repository root, start the CLI and sign in:

```text
copilot
/login
```

Follow the device-code prompts in your browser. When asked, trust the repository folder so Copilot can read the project instructions.

Copilot automatically loads this repository's custom instructions ([`.github/copilot-instructions.md`](../.github/copilot-instructions.md) and [`AGENTS.md`](../AGENTS.md)) and the project skills in [`.github/skills`](../.github/skills) (Aspire, Playwright, dotnet-inspect, feature demo video).

To update later, run `copilot update` (or `winget upgrade GitHub.Copilot` / `brew upgrade --cask copilot-cli`).

> [!TIP]
> In CI or other headless environments, you can authenticate with a fine-grained personal access token that has the **Copilot Requests** account permission, exported as `COPILOT_GITHUB_TOKEN`.

## 2. Add Copilot to T3 Code as a provider

T3 Code runs Copilot through the [Agent Client Protocol (ACP)](https://docs.github.com/copilot/reference/acp-server). The steps are the same on Windows and macOS.

1. Open T3 Code and go to **Settings → Providers**.
2. Select **Add provider**, then **Show more**.
3. Search the ACP Registry for **GitHub Copilot**.

   > [!IMPORTANT]
   > The registry has two entries named *GitHub Copilot*. Choose the one with ID **`github-copilot-cli`** (package `@github/copilot`), which is the Copilot CLI. The other entry (`github-copilot`) is the Copilot language server.

4. Select **Add**, confirm the name and instance ID, then select **Sign in** in the provider's account section and complete the GitHub sign-in.
5. Add this repository as a project in T3 Code (if it isn't already), start a new thread, and pick a Copilot model from the model picker.

Notes:

- The registry agent runs on the machine hosting your T3 Code server and keeps its own copy of the CLI under T3's home folder. It reads the same `~/.copilot` configuration (sign-in, skills) as the CLI you installed in step 1.
- **Using the CLI you installed instead:** choose **Add provider → Show more → Local ACP command**, set the executable to `copilot` (or its full path from `where.exe copilot` on Windows / `which copilot` on macOS), and add the single argument `--acp`. Alternatively, keep the registry provider and set its **Executable override**.
- If a model you expect is missing from the picker, add its ID under **Custom models** in the provider instance settings.

## 3. Enable graphify for Copilot

This repository uses a [graphify](https://github.com/safishamsi/graphify) knowledge graph in `graphify-out/`. The graph is built locally with `scripts/graphify-build.sh`, which needs no API key. It combines a fresh code (AST) pass with the shared LLM extraction of docs and images committed in `graphify-out/cache/semantic/`. Everything else in `graphify-out/` is gitignored. After changing docs or images, run `scripts/graphify-build.sh --extract` (uses the `claude` CLI) and commit the new files in `graphify-out/cache/semantic/`; they are content-addressed, so they never conflict. Without the `claude` CLI, run `scripts/graphify-build.sh`: your local graph covers the changed docs from code only, and someone with the CLI runs `--extract` later to share them. The Copilot instructions tell the agent to run `graphify query` before reading source files. For that to work, the `graphify` command must be on your `PATH` and the graphify Copilot skill must be installed.

### Windows

```powershell
pip install --user "graphifyy==0.9.58"

# Add the Python user Scripts folder to your user PATH (adjust Python314 to your Python version)
$scripts = "$env:APPDATA\Python\Python314\Scripts"
$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
if (($userPath -split ';') -notcontains $scripts) {
  [Environment]::SetEnvironmentVariable('Path', "$($userPath.TrimEnd(';'));$scripts", 'User')
}
```

Open a new terminal, then:

```powershell
graphify install --platform copilot
git config core.hooksPath .githooks
```

If you're unsure of the Scripts folder, run `pip show -f graphifyy`: the `graphify.exe` entry is listed relative to the `Location` line.

### macOS

[`scripts/setup.sh`](../scripts/setup.sh) installs graphify, builds the graph and enables the git hooks. To do it manually:

```bash
pipx install "graphifyy==0.9.58"   # or: python3 -m pip install --user "graphifyy==0.9.58"
git config core.hooksPath .githooks
```

Either way, then install the Copilot skill (`setup.sh` doesn't do this step):

```bash
graphify install --platform copilot
```

If `graphify` isn't found after a `pip install --user`, add `$(python3 -m site --user-base)/bin` to your `PATH` in `~/.zshrc`.

### What this does

- `graphify install --platform copilot` copies the graphify skill to `~/.copilot/skills/graphify` (Windows: `%USERPROFILE%\.copilot\skills\graphify`), so `/graphify` works in Copilot.
- `core.hooksPath .githooks` enables post-commit, post-merge, post-checkout and post-rewrite hooks that run `scripts/graphify-build.sh` in the background to keep your local graph fresh. Nothing is staged or committed, so the graph never causes merge conflicts. The hooks skip quietly if `graphify` isn't on `PATH`.
- Build the graph once after cloning with `scripts/graphify-build.sh` (Windows: `bash scripts/graphify-build.sh` from Git Bash).

**Restart T3 Code** (and any open terminals) after changing `PATH` so the Copilot agent inherits it.

## 4. Verify the setup

From the repository root:

```text
copilot --version
graphify query "basket checkout flow"
```

The second command should print matching graph nodes. Then, in a T3 Code thread using the Copilot provider, ask a codebase question such as *"How does the WebApp call the Basket API?"*. Copilot should run `graphify query` before searching source files.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| `copilot` not recognized | Open a new terminal. On macOS, confirm `~/.local/bin` (script install) or the npm global bin is on `PATH`. |
| Copilot says the CLI is disabled | Your organization's Copilot policy blocks the CLI. Ask an org admin to enable it. |
| Sign-in in T3 Code expires or fails | Retry from **Settings → Providers**, or run `copilot` then `/login` in a terminal on the T3 server machine. |
| `graphify` not recognized inside T3 Code | The Python Scripts folder isn't on `PATH`, or T3 Code was started before you changed it. Fix `PATH` and restart T3 Code. |
| Commit prints `[graphify] not installed` | Same cause: the pre-commit hook can't find `graphify` on `PATH`. |

## References

- [Installing GitHub Copilot CLI](https://docs.github.com/copilot/how-tos/copilot-cli/set-up-copilot-cli/install-copilot-cli)
- [Copilot CLI ACP server](https://docs.github.com/copilot/reference/acp-server)
- [ACP Registry](https://agentclientprotocol.com/get-started/registry)

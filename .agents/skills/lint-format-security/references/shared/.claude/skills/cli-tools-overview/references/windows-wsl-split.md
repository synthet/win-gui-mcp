# Windows / WSL2 split

```text
Windows host:
  Cursor / VS Code / Claude Desktop / Docker Desktop / local LLM apps
  Simple rg/fd, GitHub CLI, uv/pnpm on native paths

WSL2 Ubuntu:
  Primary git checkout for many teams
  rg/fd/jq/yq, ast-grep, build/test, Docker Compose, MCP stdio servers
```

## When native Windows is enough

- Simple text search with `rg` / `fd`
- Editing and small scripts in PowerShell
- `gh` issue/PR operations
- Python with `uv`, Node with `pnpm` when tools are Windows-native

## When WSL2 is better

- Bash-heavy repos and shell scripts
- Docker Compose workflows expecting Linux paths
- Java/Node/Python monorepos with Unix-only tooling
- MCP servers that assume `/` paths or Linux binaries
- Reproducing CI-like test commands from GitHub Actions

## Path discipline

- Do not mix Windows and WSL paths in one command (`D:\...` vs `/mnt/d/...`).
- Pick one environment per task; open the repo from that environment in the IDE when possible.

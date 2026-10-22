# Bounded output patterns

Use these to keep agent context small and responses readable.

## Search

```bash
rg "SomeSymbol" . --glob '!node_modules' --glob '!dist' --glob '!build' --max-count 50
fd -e ts -e tsx src/
tree -L 3 -I 'node_modules|dist|build|.git'
```

## Read slices

```bash
bat --line-range 1:160 path/to/file.py
sed -n '1,160p' path/to/file.py
```

PowerShell:

```powershell
Get-Content .\path\to\file.py -TotalCount 160
```

## Git inspection

```bash
git status --short
git diff --stat
git diff -- path/to/file | delta
git log --oneline -n 20
```

## Structured config

```bash
jq '.scripts' package.json
yq '.services' docker-compose.yml
```

## When fff MCP is connected

Prefer `ffgrep` / `fffind` / `fff-multi-grep` for repeated repo search instead of unbounded shell loops; still cap result review in the agent response.

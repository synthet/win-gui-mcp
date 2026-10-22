# Install blocks (human provisioning)

Run these on a **workstation** when setting up an agent environment. Agents should not bulk-install without user approval.

For tier order and Block B extensions, see [install-tiers.md](install-tiers.md).

## Windows PowerShell (winget)

```powershell
winget install Git.Git
winget install GitHub.cli
winget install BurntSushi.ripgrep.MSVC
winget install sharkdp.fd
winget install jqlang.jq
winget install dandavison.delta
winget install sharkdp.bat
winget install ajeetdsouza.zoxide
winget install OpenJS.NodeJS.LTS
corepack enable
corepack prepare pnpm@latest --activate
npm i -g @ast-grep/cli
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
uv tool install ruff
uv tool install pyright
```

Optional file-search MCP: install [fff-mcp](https://github.com/dmtrKovalenko/fff) to `%LOCALAPPDATA%\fff-mcp\bin` and add to PATH.

## WSL2 Ubuntu

```bash
sudo apt update
sudo apt install -y git curl jq ripgrep fd-find shellcheck sqlite3 direnv tree
sudo ln -sf $(which fdfind) /usr/local/bin/fd 2>/dev/null || true
curl -LsSf https://astral.sh/uv/install.sh | sh
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt install -y nodejs
corepack enable && corepack prepare pnpm@latest --activate
npm i -g @ast-grep/cli
cargo install just 2>/dev/null || sudo apt install -y just
```

## macOS (Homebrew)

```bash
brew install git gh ripgrep fd jq bat delta zoxide node
brew install ast-grep
brew install uv
uv tool install ruff pyright
```

# Integrating sibling MCP servers

WinGuiMcp is intentionally **generic**. Domain adapters (separate executables, separate tool namespaces) should register as additional MCP servers in the client config.

## Principles

- One server = one contract owner ([`tools.md`](../tools.md) vs domain `dxo_*` tools).
- Agents must name which server they call in plans and logs.
- Do not merge domain tools into `src/WinGuiMcp/Tools/` without a constitution amendment.

## Example: PhotoLab

| Server key | Role |
|------------|------|
| `win-gui-mcp` | UIA: `snapshot`, `click`, `set_value`, `wait_for`, … |
| `dxo-photolab` | Files/process: `dxo_open_images`, `dxo_tweak_sliders`, `dxo_list_exports`, … |

Machine-readable link registry: [`cross-project/LINKS.json`](../cross-project/LINKS.json).

PhotoLab UI recipes using WinGuiMcp: [`D:\Projects\dxo-photolab-mcp\docs\gui-automation-win-gui-mcp.md`](file:///D:/Projects/dxo-photolab-mcp/docs/gui-automation-win-gui-mcp.md).

## Client configuration sketch

See `D:\Projects\dxo-photolab-mcp\config\mcp.dual-server.example.json` for a two-server template.

## Orchestration

Future **application profiles** (win-gui-mcp) may reference sibling server keys and scenario IDs; until then, operators chain tools explicitly: domain prep → UIA export → `file-stable` on output path.

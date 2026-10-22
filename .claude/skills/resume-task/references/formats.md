# Where each tool keeps its sessions

What `scripts/harvest.py` reads, and the known gaps. These are internal, undocumented formats that change between releases. If a source returns 0 sessions but `--probe` shows the store exists, the format has probably drifted: open one file, compare it with the notes below, and patch the matching `parse_*` function.

`homes()` checks the current user's `~`. To include a Windows profile from WSL,
pass its explicitly authorized path with `--profile-root /mnt/c/Users/<name>`.
Other users' profiles are never discovered automatically.

## Claude Code
- **Path:** `~/.claude/projects/<encoded-cwd>/<session-uuid>.jsonl`. `$CLAUDE_CONFIG_DIR` and `~/.config/claude` are also checked. Subagent logs live under `…/subagents/` and are skipped unless `--include-subagents` is passed.
- **Line format:** one JSON object per line.
  - `type` is `user`, `assistant`, `summary` or `custom-title`.
  - Other fields: `cwd`, `sessionId`, `timestamp`, `isMeta`, `isSidechain`, `isCompactSummary`.
  - `message.content` is a string or a list of blocks: `text`, `tool_use` or `tool_result`.
- **Plans:**
  - The last `TodoWrite` `input.todos[]`, with fields `content` and `status`.
  - `TaskCreate` / `TaskUpdate` calls. The task id is read from the "Task #N created" tool result.
  - `ExitPlanMode` `input.plan`.
- **Exports:** `/export` writes a text transcript. User lines start with `> ` and assistant lines with `⏺`. Pass it with `--md`.

## OpenAI Codex CLI
- **Path:** `$CODEX_HOME` or `~/.codex`, under `sessions/YYYY/MM/DD/rollout-<ts>-<uuid>.jsonl`. `archived_sessions/` is read too.
- **Line format:** `{timestamp, type, payload}`.
  - `session_meta.payload.cwd` gives the project.
  - `response_item` with `payload.type=message` and `role` `user` or `assistant`. The `content[]` entries are `input_text` or `output_text`.
  - `event_msg` with `user_message` or `agent_message` is the fallback when no response items exist.
  - `turn_aborted` means the turn was interrupted.
- **Injected context:** user messages that start with `<environment_context>`, `<user_instructions>` or `# AGENTS.md` are dropped.
- **Plans:** `response_item` with `function_call` and `name=update_plan`. The `arguments` JSON contains `plan[]` with fields `step` and `status`.
- **Legacy files:** older flat lines (no `payload`) and single-JSON `rollout-*.json` files are handled.

## Cursor IDE
- **Path:** the user dir is `~/Library/Application Support/Cursor/User` (macOS), `~/.config/Cursor/User` (Linux) or `%APPDATA%\Cursor\User` (Windows).
- **Global database:** `globalStorage/state.vscdb`, a SQLite database with a `cursorDiskKV` table.
  - `composerData:<composerId>` holds JSON with `name`, `createdAt`/`lastUpdatedAt` (in ms), `fullConversationHeadersOnly[]` (bubble order) and sometimes `todos`. Older builds put messages inline in `conversation[]`.
  - `bubbleId:<composerId>:<bubbleId>` holds one message. `type` is 1 for the user and 2 for the assistant, and the content is in `text`.
- **Project mapping:**
  - `workspaceStorage/<hash>/workspace.json` gives the folder URI.
  - That folder's `state.vscdb` `ItemTable['composer.composerData'].allComposers[]` maps each composer id to its workspace.
- **Locking:** opened with `mode=ro`. If Cursor holds a lock, the DB is copied to a temp dir first.

## cursor-agent (Cursor CLI)
- **Path:** `~/.cursor/chats/<hash>/<id>/store.db`, a SQLite database with `meta` and content-addressed `blobs` tables.
- **Parsing:** best effort only. JSON blobs with a `role` are read; protobuf blobs are skipped. Expect partial coverage.

## Google Antigravity
- **IDE:** `~/.gemini/antigravity/`.
  - Conversations are `conversations/<uuid>.pb`. They are encrypted or compressed and **not readable**.
  - Readable working files are in `brain/<conversation-id>/`: `task.md` (checklist; `[x]` done, `[/]` in progress, `[ ]` open), `implementation_plan.md`, `walkthrough.md`, `*.md.resolved` and `*.metadata.json`.
  - IDE sessions therefore show plan artifacts but no dialogue.
- **CLI (`agy`):** `~/.gemini/antigravity-cli/`.
  - `conversations/<uuid>.db` (SQLite, schema undocumented) is not read.
  - The display log `brain/<id>/.system_generated/logs/transcript_full.jsonl` is parsed generically: the role comes from `role`, `type`, `author` or `source` fields, and the text from `content`, `text` or `message`.
- **Project:** guessed from `file://` URIs and `cwd`/`workspace` keys, then collapsed to the git root.
- **Exports:** Antigravity can export a conversation to Markdown, and SpecStory (`specstory sync antigravity`) can too. Pass those with `--md` for full dialogue.

## Markdown / text exports
- **Input:** any `.md`, `.markdown` or `.txt` passed with `--md`, plus `<project>/.specstory/history/*.md` for every project found.
- **Speaker markers recognized:**
  - SpecStory: `_**User**_` and `_**Assistant**_`
  - headings: `## User`, `### Assistant`
  - bold labels: `**User:**`
  - plain prefixes: `Human:` and `Assistant:`
  - Claude Code export: `>` and `⏺`
  - A file with no markers is treated as one document.
- **Date:** taken from the filename (`2026-10-10_14-30Z-…`), otherwise from mtime.

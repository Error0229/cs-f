# Code Formatter DevToys Plugin — Design Document

**Date**: 2025-11-27
**Status**: Approved

## Overview

A DevToys plugin that formats code in multiple languages using configurable formatters. Supports both clipboard-based (paste & format) and file-based workflows.

## Core Decisions

| Aspect | Decision |
|--------|----------|
| Modes | Paste & format + file-based |
| Language selection | Manual dropdown, persists last choice |
| Config location | `%APPDATA%/DevToys/CodeFormatter/config.toml` |
| Config format | TOML |
| First run | Auto-generate default config |
| UI layout | Side-by-side (input left, output right) |
| Error display | Inline in output panel |

## Supported Languages & Formatters

| Language | Formatter | Bundled | Requires Node |
|----------|-----------|---------|---------------|
| Python | Ruff | Yes (ruff.exe) | No |
| JavaScript | dprint | Yes (dprint.exe) | No |
| TypeScript | dprint | Yes (dprint.exe) | No |
| Java | prettier-java | No | Yes |
| SQL | sql-formatter | No | Yes |

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    DevToys Plugin                       │
├─────────────────────────────────────────────────────────┤
│  UI Layer (IGuiTool)                                    │
│  - Side-by-side input/output panels                     │
│  - Language dropdown (persisted)                        │
│  - Action buttons: Format, Copy, Swap, Clear, Load, Save│
├─────────────────────────────────────────────────────────┤
│  Formatter Service                                      │
│  - Routes to correct formatter based on language        │
│  - Handles stdin/stdout piping                          │
│  - Returns formatted code or error message              │
├─────────────────────────────────────────────────────────┤
│  Config Manager                                         │
│  - Reads/writes TOML config                             │
│  - Auto-generates defaults on first run                 │
│  - Location: %APPDATA%/DevToys/CodeFormatter/config.toml│
├─────────────────────────────────────────────────────────┤
│  Bundled Binaries                                       │
│  - ruff.exe (Python)                                    │
│  - dprint.exe (JS/TS)                                   │
│  Node-dependent (user's Node):                          │
│  - prettier + prettier-java (Java)                      │
│  - sql-formatter (SQL)                                  │
└─────────────────────────────────────────────────────────┘
```

## UI Layout

```
┌──────────────────────────────────────────────────────────────────┐
│  Code Formatter                                                  │
├──────────────────────────────────────────────────────────────────┤
│  [Language ▼] [Format] [Swap ⇄] [Clear ✕] [Load 📁] [Save 💾]   │
├───────────────────────────────┬──────────────────────────────────┤
│          Input                │           Output         [Copy]  │
│  ┌─────────────────────────┐  │  ┌─────────────────────────────┐ │
│  │                         │  │  │                             │ │
│  │   (paste or load code)  │  │  │   (formatted result or      │ │
│  │                         │  │  │    error message)           │ │
│  │                         │  │  │                             │ │
│  └─────────────────────────┘  │  └─────────────────────────────┘ │
└───────────────────────────────┴──────────────────────────────────┘
```

### Button Behaviors

| Button | Action |
|--------|--------|
| Format | Run formatter, display result in Output |
| Swap | Move Output content → Input, clear Output |
| Clear | Clear both Input and Output panels |
| Load | Open file dialog, load file content into Input |
| Save | Save Output content to file (save dialog) |
| Copy | Copy Output to clipboard |

## Configuration

### Default Config (auto-generated)

```toml
# Code Formatter Configuration

[defaults]
lastLanguage = "python"

[formatters.python]
command = "ruff"
args = ["format", "-"]

[formatters.javascript]
command = "dprint"
args = ["fmt", "--stdin", "file.js"]

[formatters.typescript]
command = "dprint"
args = ["fmt", "--stdin", "file.ts"]

[formatters.java]
command = "npx"
args = ["prettier", "--parser", "java"]
requiresNode = true

[formatters.sql]
command = "npx"
args = ["sql-formatter", "--language", "postgresql"]
requiresNode = true

[paths]
# Override bundled binary paths (optional)
# ruff = "C:/custom/path/ruff.exe"
# dprint = "C:/custom/path/dprint.exe"
```

## Error Handling

All errors display inline in the Output panel:

```
┌─────────────────────────────────────────┐
│ ❌ Formatting Error                     │
│                                         │
│ Python syntax error at line 12:         │
│   unexpected indent                     │
│                                         │
│ Original input preserved in left panel. │
└─────────────────────────────────────────┘
```

### Error Categories

| Error Type | Message |
|------------|---------|
| Syntax error | Show formatter's stderr output |
| Formatter not found | "Formatter [name] not found. Check config paths." |
| Node.js missing | "Node.js required. Download: nodejs.org" |
| Timeout (>10s) | "Formatting timed out. Code may be too large." |
| Unknown | "Unexpected error: [details]" |

### Node.js Dependency Flow

When Java or SQL is selected:
1. Check if Node.js is installed (`node --version`)
2. If missing, show inline message with download link
3. If present, run `npx` command

## Project Structure

```
cs-f/
├── cs-f.csproj                         # Project file
├── Resources/
│   └── CodeFormatter.resx              # Localized strings
├── CodeFormatterResourceIdentifier.cs  # MEF resource discovery
├── CodeFormatterTool.cs                # Main IGuiTool implementation
├── Services/
│   ├── FormatterService.cs             # Routes to correct formatter
│   ├── ConfigManager.cs                # TOML config read/write
│   └── ProcessRunner.cs                # CLI execution helper
├── Models/
│   ├── FormatterConfig.cs              # TOML deserialization model
│   └── Language.cs                     # Language enum
└── Binaries/
    ├── ruff.exe                        # Bundled
    └── dprint.exe                      # Bundled
```

## Dependencies

### NuGet Packages
- `DevToys.Api` — DevToys extension API
- `Tomlyn` — TOML parser for .NET

### Bundled Binaries
- `ruff.exe` — Download from GitHub releases
- `dprint.exe` — Download from GitHub releases

## Data Flow

1. User pastes/loads code → Input panel
2. User selects language → Dropdown (persisted to config)
3. User clicks Format → FormatterService invoked
4. FormatterService:
   - Reads config for selected language
   - Checks Node.js if `requiresNode = true`
   - Spawns process with stdin piped
   - Captures stdout/stderr
5. Output (or error) displayed in Output panel

# Code Formatter for DevToys

A code formatting extension for [DevToys 2.0](https://devtoys.app/) that supports 22 programming languages with automatic live formatting.

## Features

- **Live Auto-Format**: Code is automatically formatted as you type (500ms debounce)
- **22 Languages**: Python, JavaScript, TypeScript, JSON, Markdown, TOML, CSS, SCSS, Less, HTML, Vue, Svelte, Astro, YAML, GraphQL, Dockerfile, Java, SQL, C, C++, Go, Shell/Bash
- **Per-Language Settings**: Configure formatting options for each language
- **Swap & Clear**: Quickly swap input/output or clear both editors
- **File Loading**: Load code directly from files

## Supported Languages & Formatters

| Language | Formatter | Notes |
|----------|-----------|-------|
| Python | [Ruff](https://github.com/astral-sh/ruff) | Bundled binary |
| JavaScript | [dprint](https://dprint.dev/) | Bundled binary |
| TypeScript | [dprint](https://dprint.dev/) | Bundled binary |
| JSON | [dprint](https://dprint.dev/) | Bundled binary |
| Markdown | [dprint](https://dprint.dev/) | Bundled binary |
| TOML | [dprint](https://dprint.dev/) | Bundled binary |
| CSS | [dprint](https://dprint.dev/) | Bundled binary |
| SCSS | [dprint](https://dprint.dev/) | Bundled binary |
| Less | [dprint](https://dprint.dev/) | Bundled binary |
| HTML | [dprint](https://dprint.dev/) | Bundled binary |
| Vue | [dprint](https://dprint.dev/) | Bundled binary |
| Svelte | [dprint](https://dprint.dev/) | Bundled binary |
| Astro | [dprint](https://dprint.dev/) | Bundled binary |
| YAML | [dprint](https://dprint.dev/) | Bundled binary |
| GraphQL | [dprint](https://dprint.dev/) | Bundled binary |
| Dockerfile | [dprint](https://dprint.dev/) | Bundled binary |
| Java | [Prettier](https://prettier.io/) + [prettier-plugin-java](https://github.com/jhipster/prettier-java) | Requires Node.js |
| SQL | [sql-formatter](https://github.com/sql-formatter-org/sql-formatter) | Requires Node.js |
| C | [clang-format](https://clang.llvm.org/docs/ClangFormat.html) | Bundled binary |
| C++ | [clang-format](https://clang.llvm.org/docs/ClangFormat.html) | Bundled binary |
| Go | [gofumpt](https://github.com/mvdan/gofumpt) | Bundled binary |
| Shell/Bash | [shfmt](https://github.com/mvdan/sh) | Bundled binary |

## Requirements

- [DevToys 2.0](https://devtoys.app/) (Preview or later)
- **For Java/SQL formatting**: Node.js with npm packages:
  ```bash
  npm install -g prettier prettier-plugin-java sql-formatter
  ```

## Installation

### From NuGet Package

1. Download the `.nupkg` file from [Releases](../../releases)
2. In DevToys, go to **Manage Extensions**
3. Click **Install from file** and select the `.nupkg`
4. Restart DevToys

### Manual Installation

1. Extract the `.nupkg` (it's a ZIP file) to:
   - **Windows**: `%LocalAppData%\DevToys\Plugins\CodeFormatter.DevToys.1.0.0\`
   - **macOS**: `~/Library/com.devtoys/Plugins/CodeFormatter.DevToys.1.0.0/`
   - **Linux**: `~/.local/share/devtoys/Plugins/CodeFormatter.DevToys.1.0.0/`
2. Restart DevToys

## Usage

1. Open DevToys and find **Code Formatter** under the **Formatters** category
2. Select your language from the dropdown
3. Paste or type code in the left editor
4. Formatted output appears automatically in the right editor

### Buttons

- **Swap**: Move output to input (useful for re-formatting)
- **Clear**: Clear both editors
- **Settings (gear icon)**: Configure formatter settings for each language
- **Load**: Load code from a file

## Configuration

Click the gear icon to open settings for any language. Available options vary by formatter:

### Python (Ruff)
- Line Length (40-400)
- Indent Style (space/tab)
- Quote Style (double/single/preserve)
- Line Ending (auto/lf/cr-lf/native)

### JavaScript/TypeScript (dprint)
- Line Width
- Indent Width
- Use Tabs
- Semicolons
- Quote Style

### JSON (dprint)
- Line Width
- Indent Width
- Use Tabs
- Trailing Commas

### SQL (sql-formatter)
- Language dialect (PostgreSQL, MySQL, SQLite, etc.)
- Tab Width
- Keyword Case
- Data Type Case
- Function Case

### C/C++ (clang-format)
- Style (LLVM, Google, Chromium, Mozilla, WebKit, Microsoft, GNU)

### Go (gofumpt)
- Extra Rules (stricter formatting)

### Shell/Bash (shfmt)
- Indent Width (0 for tabs)
- Binary Next Line
- Case Indent
- Space Redirects
- Keep Padding
- Function Next Line

Settings are saved to `~/.config/code-formatter/config.toml` (or equivalent on your OS).

## Building from Source

### Prerequisites

- .NET 8.0 SDK
- Node.js (for Java/SQL formatting tests)

### Build

```bash
# Clone the repository
git clone https://github.com/user/cs-f.git
cd cs-f

# Build
dotnet build -c Release

# Run tests
dotnet test

# Create NuGet package
dotnet pack -c Release -o ./nupkg
```

### Development

For development with hot reload:

1. Set environment variables:
   ```powershell
   [Environment]::SetEnvironmentVariable("DevToysGuiDebugEntryPoint", "C:\path\to\DevToys.exe", "User")
   ```

2. Press F5 in Visual Studio/VS Code/Rider to debug with DevToys

The `Properties/launchSettings.json` is configured for debugging with the `EXTRAPLUGIN` environment variable.

## Project Structure

```
cs-f/
├── CodeFormatterTool.cs      # Main UI and tool implementation
├── Models/
│   ├── Language.cs           # Language enum and extensions
│   ├── FormatterConfig.cs    # Configuration model
│   └── FormatterSettings.cs  # Per-language setting definitions
├── Services/
│   ├── FormatterService.cs   # Formatting orchestration
│   ├── ConfigManager.cs      # TOML config read/write
│   └── ProcessRunner.cs      # External process execution
├── Resources/
│   └── CodeFormatterStrings.resx  # Localized strings
├── Binaries/                 # Bundled formatter executables
│   ├── ruff.exe
│   ├── dprint.exe
│   ├── clang-format.exe
│   ├── gofumpt.exe
│   └── shfmt.exe
└── CodeFormatter.Tests/      # Integration tests
```

## License

MIT

## Acknowledgments

- [DevToys](https://devtoys.app/) - The extensible developer toolbox
- [Ruff](https://github.com/astral-sh/ruff) - Fast Python formatter
- [dprint](https://dprint.dev/) - Pluggable code formatter
- [Prettier](https://prettier.io/) - Opinionated code formatter
- [sql-formatter](https://github.com/sql-formatter-org/sql-formatter) - SQL formatter
- [clang-format](https://clang.llvm.org/docs/ClangFormat.html) - LLVM C/C++ formatter
- [gofumpt](https://github.com/mvdan/gofumpt) - Stricter gofmt for Go
- [shfmt](https://github.com/mvdan/sh) - Shell script formatter

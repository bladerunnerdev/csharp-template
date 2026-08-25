# Visual Studio vs. VS Code vs. CLI

A side-by-side of where to do common .NET dev tasks in each tool. The short version: Visual Studio wraps most of this in dialogs and wizards; VS Code (with C# Dev Kit) exposes the same functionality through the Command Palette and its UI panels; and the `dotnet` CLI (plus `git`) is what both of them call under the hood for almost everything project/build/test related. If you're comfortable in a terminal, you can do essentially all of this without either editor.

| Task | Visual Studio | VS Code (+ C# Dev Kit) | CLI |
|---|---|---|---|
| Create a new project | File → New → Project (template picker with search/filter UI) | Command Palette → **.NET: New Project** | `dotnet new <template> -n <name>` (e.g. `webapi`, `console`, `xunit`) |
| Create a new solution | File → New → Project → "Blank Solution" | Command Palette → **.NET: New Project** → solution templates, or terminal | `dotnet new sln --name <name>` |
| Add a project to a solution | Right-click solution → Add → Existing/New Project | Solution Explorer view → right-click solution → Add Project (or CLI) | `dotnet sln add <path/to.csproj>` |
| Add a project reference | Right-click project → Add → Project Reference | Solution Explorer view → right-click → Add Reference | `dotnet add <proj> reference <other-proj>` |
| Add a NuGet package | Right-click project → Manage NuGet Packages (browse/search UI) | NuGet Gallery extension, or C# Dev Kit's dependency UI, or terminal | `dotnet add package <PackageName>` |
| Restore packages | Automatic on build, or right-click → Restore | Automatic on build, or Command Palette → **.NET: Restore Project** | `dotnet restore` |
| Build | Build → Build Solution (Ctrl+Shift+B) | Command Palette → **.NET: Build**, or integrated terminal | `dotnet build` |
| Run without debugging | Ctrl+F5 | Run → Run Without Debugging, or terminal | `dotnet run` |
| Debug / breakpoints | F5, Locals/Watch/Call Stack windows | F5 (C# Dev Kit auto-detects launch target), same debug UI | n/a (debugging is inherently an editor/IDE feature; `dotnet run` alone has no debugger attached) |
| Attach to a running process | Debug → Attach to Process | Command Palette → **Debug: Attach to a .NET 5+ or .NET Core process** | n/a — same caveat as above |
| Hot reload | Automatic during F5 debugging, or Ctrl+Alt+F10 | Automatic during F5 debugging | `dotnet watch run` (its own file-watching hot reload, works outside any editor) |
| Run unit tests | Test Explorer window, run/debug individual tests | Testing view (flask icon), inline CodeLens "Run Test"/"Debug Test" | `dotnet test` (whole project); `dotnet test --filter <expr>` for a subset |
| Code formatting | Edit → Advanced → Format Document, format-on-save setting | Format Document (Shift+Alt+F), format-on-save setting | `dotnet format` |
| Static analysis / code cleanup | Built-in Roslyn analyzers, Error List, "Run Code Cleanup" profiles | Same Roslyn analyzers via the C# extension, Problems panel | `dotnet format analyzers`, or `dotnet build` (analyzers configured as build errors/warnings) |
| Refactoring (rename, extract method, etc.) | Right-click → Quick Actions and Refactorings, Ctrl+. | Ctrl+. (lightbulb) — same Roslyn refactoring engine | n/a — editor-only |
| Go to definition / Find references | F12 / Shift+F12, Solution Explorer, Class View | F12 / Shift+F12, Outline view | n/a — editor-only (`grep`/`rg` as a poor substitute) |
| Git operations | Git Changes window, built-in diff/merge UI | Source Control view, built-in diff, GitLens extension for blame/history | `git` directly (`status`, `add`, `commit`, `push`, etc.) |
| Manage `launchSettings.json` / launch profiles | UI dropdown next to the Run button | Edit `Properties/launchSettings.json` directly, or `.vscode/launch.json` for editor-side config | n/a — it's a JSON file either way; `dotnet run --launch-profile <name>` to pick one |
| Scaffolding (controllers, Razor pages, EF migrations) | Right-click → Add → Controller/View, EF Core Power Tools UI | `dotnet-aspnet-codegenerator` and `dotnet-ef` tools invoked from terminal (no built-in GUI) | `dotnet aspnet-codegenerator controller ...`, `dotnet ef migrations add <Name>` |
| Publish / deploy | Right-click project → Publish (wizard: folder, IIS, Azure, container, etc.) | Command Palette → **.NET: Publish** (thinner UI, same targets) | `dotnet publish -c Release -o <dir>` |
| Package/library dependency vulnerability check | NuGet Package Manager UI flags vulnerable packages | `dependi` extension (already in this template's recommendations) surfaces it inline | `dotnet list package --vulnerable` |
| Container support | Right-click → Add → Container Orchestrator Support, Docker publish target | Dev Containers extension (this template's `.devcontainer/`), Docker extension | `docker build`/`docker run`, or `dotnet publish /t:PublishContainer` |
| Solution-wide search | Ctrl+; (Go To All), Find in Files | Ctrl+P (Go To File), Ctrl+Shift+F (Find in Files) | `grep`/`rg` |
| Extensions/plugins | Extensions and Updates dialog, Visual Studio Marketplace | Extensions view, VS Code Marketplace (`.vscode/extensions.json` in this template pins recommendations) | n/a |

## Where they genuinely differ

- **Visual Studio** bundles specialized designers and wizards that don't have a real CLI or VS Code equivalent — the WPF/WinForms XAML designer, the EF Core Power Tools reverse-engineering UI, and the Publish wizard's guided Azure resource creation. If you're doing heavy WPF/WinForms UI work or clicking through Azure resource setup, VS is genuinely more convenient.
- **VS Code** is lighter weight, cross-platform (this template's dev container assumes Linux), and everything it does is one Command Palette entry or terminal command away from being scriptable — which is why this repo leans on it.
- **The CLI** (`dotnet`, `git`, `docker`) is the common foundation both editors sit on for anything build/test/package/publish-related. The things the CLI *can't* replace are the genuinely interactive parts: setting breakpoints, stepping through code, inline refactoring previews, and visual diffing — those need an editor (or a debugger UI) by nature, not because of a tooling gap.

So: you're not making life harder by staying in VS Code for this template. You'd only feel friction if you needed one of the VS-exclusive designers above — nothing in this repo's workflow (console app, Web API, xUnit tests, dev container) requires them.

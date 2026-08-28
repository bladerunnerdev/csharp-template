# .NET tooling notes: Visual Studio vs. VS Code vs. CLI

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
| Generate an XML doc comment skeleton (`///`) | Built-in — `///` above a member always auto-expands to the full `<summary>`/`<param>`/`<returns>` skeleton | Off by default — needs `"editor.formatOnType": true` in `.vscode/settings.json` (set in this template) | n/a — editor-only; `dotnet build` just consumes whatever comments already exist, see below |
| Go to definition / Find references | F12 / Shift+F12, Solution Explorer, Class View | F12 / Shift+F12, Outline view | n/a — editor-only (`grep`/`rg` as a poor substitute) |
| Git operations | Git Changes window, built-in diff/merge UI | Source Control view, built-in diff, GitLens extension for blame/history | `git` directly (`status`, `add`, `commit`, `push`, etc.) |
| Manage `launchSettings.json` / launch profiles | UI dropdown next to the Run button | Edit `Properties/launchSettings.json` directly, or `.vscode/launch.json` for editor-side config | n/a — it's a JSON file either way; `dotnet run --launch-profile <name>` to pick one |
| Scaffolding (controllers, Razor pages, EF migrations) | Right-click → Add → Controller/View, EF Core Power Tools UI | `dotnet-aspnet-codegenerator` and `dotnet-ef` tools invoked from terminal (no built-in GUI) | `dotnet aspnet-codegenerator controller ...`, `dotnet ef migrations add <Name>` |
| Publish / deploy | Right-click project → Publish (wizard: folder, IIS, Azure, container, etc.) | Command Palette → **.NET: Publish** (thinner UI, same targets) | `dotnet publish -c Release -o <dir>` |
| Package/library dependency vulnerability check | NuGet Package Manager UI flags vulnerable packages | `dependi` extension (already in this template's recommendations) surfaces it inline | `dotnet list package --vulnerable` |
| Container support | Right-click → Add → Container Orchestrator Support, Docker publish target | Dev Containers extension (this template's `.devcontainer/`), Docker extension | `docker build`/`docker run`, or `dotnet publish /t:PublishContainer` |
| Solution-wide search | Ctrl+; (Go To All), Find in Files | Ctrl+P (Go To File), Ctrl+Shift+F (Find in Files) | `grep`/`rg` |
| Extensions/plugins | Extensions and Updates dialog, Visual Studio Marketplace | Extensions view, VS Code Marketplace (`.vscode/extensions.json` in this template pins recommendations) | n/a |

## Adding different project types

`dotnet new` has a template for pretty much every project shape; Visual Studio's "New Project" dialog and VS Code's **.NET: New Project** command are both just UI pickers over the same template set (C# Dev Kit's list mirrors the CLI names 1:1). The main place they diverge is designer support for the older desktop UI frameworks.

| Project type | CLI template | Visual Studio | VS Code (C# Dev Kit) | Typical use |
|---|---|---|---|---|
| Console app | `console` | "Console App" | .NET: New Project → Console App | entry point / CLI tools (this template's `CSharpTemplate` project) |
| Class library | `classlib` | "Class Library" | .NET: New Project → Class Library | code with no entry point of its own — **this is what you want for Domain, Application, Infrastructure, Data, or any other internal layer** |
| ASP.NET Core Web API, minimal | `webapi` | "ASP.NET Core Web API" (untick "Use controllers") | same, template prompts the same option | HTTP API entry point, minimal API style |
| ASP.NET Core Web API, controllers | `webapi -controllers` | "ASP.NET Core Web API" (tick "Use controllers") | same | HTTP API entry point, MVC controller style |
| ASP.NET Core MVC web app | `mvc` | "ASP.NET Core Web App (Model-View-Controller)" | same | server-rendered web app with controllers/views |
| Razor Pages web app | `webapp` | "ASP.NET Core Web App" | same | server-rendered, page-based web app |
| Blazor Web App | `blazor` | "Blazor Web App" (choose Server/WebAssembly/Auto render mode) | same | interactive web UI in C# instead of JS |
| Worker Service | `worker` | "Worker Service" | same | long-running background service, no HTTP endpoint (queue processors, scheduled jobs) |
| xUnit / NUnit / MSTest test project | `xunit` / `nunit` / `mstest` | "xUnit/NUnit/MSTest Test Project" | same | unit tests (this template uses `xunit` for `CSharpTemplate.Tests`) |
| gRPC service | `grpc` | "gRPC Service" | same | RPC-style API entry point |
| WPF app | `wpf` | "WPF Application", full XAML designer | template works, but no visual XAML designer — code/XAML only | Windows desktop UI |
| Windows Forms app | `winforms` | "Windows Forms App", full drag-and-drop designer | template works, no visual designer | Windows desktop UI |
| Azure Functions | needs Azure Functions Core Tools (`func`) or the `Azure.Functions.Cli` templates | "Azure Functions" (Azure workload installed) | Azure Functions extension provides its own project creation | serverless, event-triggered functions |

### Which type for Domain / Infrastructure / Data / Application layers?

All of those are **class libraries** (`dotnet new classlib`) — there's no separate "domain project" or "infrastructure project" template. The type name in `dotnet new` just describes what hosts/runs the code (console entry point, web host, test runner, or nothing); layering is a naming and dependency convention you apply on top of plain class libraries. A typical split, following the dependencies-point-inward rule of Clean/Onion architecture:

```sh
dotnet new classlib -n MyProject.Domain          # entities, value objects, domain logic — no dependencies on anything else
dotnet new classlib -n MyProject.Application     # use cases/services, depends on Domain only
dotnet new classlib -n MyProject.Infrastructure  # EF Core, external APIs, file/email/etc. — implements interfaces defined in Domain/Application
dotnet new webapi -n MyProject.Api               # composition root: wires everything up, depends on all of the above

dotnet sln add MyProject.Domain/MyProject.Domain.csproj MyProject.Application/MyProject.Application.csproj MyProject.Infrastructure/MyProject.Infrastructure.csproj MyProject.Api/MyProject.Api.csproj

dotnet add MyProject.Application reference MyProject.Domain
dotnet add MyProject.Infrastructure reference MyProject.Domain MyProject.Application
dotnet add MyProject.Api reference MyProject.Application MyProject.Infrastructure
```

If you want a dedicated persistence project instead of folding EF Core/DbContext/migrations into `Infrastructure`, that's also just a `classlib` — commonly named `MyProject.Persistence` or `MyProject.Data`, referenced by `Infrastructure` or directly by `Api`, depending on how strictly you're separating concerns. There's no wrong template choice here since it's always `classlib` — the only real decision is how many of these libraries you want and how you name/reference them.

## XML documentation comments (the JSDoc equivalent)

C#'s equivalent of JSDoc is triple-slash (`///`) XML documentation comments — `<summary>`, `<param>`, `<returns>`, `<exception>`, etc. above a type or member. [Calculator.cs](CSharpTemplate/Calculator.cs) is fully documented this way, as a reference.

This is a native compiler feature, not a package — [Directory.Build.props](Directory.Build.props) at the repo root sets `<GenerateDocumentationFile>true</GenerateDocumentationFile>`, so every project picks it up automatically (a `.sln`/`.slnx` can't do this itself — it's just a list of project references, not part of the MSBuild import chain; `Directory.Build.props` is what actually flows a shared property down into every project's build without repeating it per `.csproj`). The compiler then emits an XML file alongside each DLL that VS Code, Visual Studio, and Rider all read for hover/IntelliSense docs.

You don't need the repo-wide file for this — the same `<GenerateDocumentationFile>true</GenerateDocumentationFile>` works dropped directly into a single project's own `<PropertyGroup>` instead, scoped to just that project. `Directory.Build.props` is only worth it once you want the setting applied consistently everywhere without repeating it per `.csproj` (and risking a new project missing it).

It also means the compiler warns (`CS1591`) about any public member that's missing a comment — intentional, as a nudge to document your public API. [CalculatorTests.cs](CSharpTemplate.Tests/CalculatorTests.cs) deliberately leaves these warnings in place as a live example of what they look like on undocumented test code; add `<NoWarn>$(NoWarn);CS1591</NoWarn>` to a project's `.csproj` if you'd rather silence them there (e.g. for test projects, where the public members aren't really a documented API surface).

## Scaffolding with `dotnet-aspnet-codegenerator`

`aspnet-codegenerator` is the CLI-only equivalent of Visual Studio's right-click → Add → Controller/View wizard. It's a .NET tool, not part of the base SDK, so it has to be installed separately, and it needs a couple of design-time packages on the *target* project before it can generate anything into it.

### Install

Pick one:

```sh
# global tool — available in every project on this machine
dotnet tool install -g dotnet-aspnet-codegenerator
dotnet tool update -g dotnet-aspnet-codegenerator   # later, to upgrade

# local tool — pinned per-repo via a tool manifest (checked into source control, like this template's global.json pins the SDK)
dotnet new tool-manifest                             # only if the repo doesn't already have .config/dotnet-tools.json
dotnet tool install dotnet-aspnet-codegenerator
dotnet tool restore                                  # what teammates/CI run to get the same version
```

With a local tool, either prefix commands with `dotnet tool run` or just call `dotnet aspnet-codegenerator ...` directly — the local manifest is picked up automatically once restored.

Then, in the **target project** (the one you're scaffolding into, e.g. a `webapi`/`mvc`/`webapp` project — not this template's console app), add the design-time package it needs to reflect over your code:

```sh
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
```

If you're scaffolding anything EF Core-backed (a controller/page bound to a `DbContext`), also add:

```sh
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Run generation commands from inside the target project's directory (where its `.csproj` lives), or pass `-p <path-to-project>`. The project must build successfully first — the generator does a design-time build to discover your models/`DbContext`.

### Generators and common commands

| Generator | Produces | Example |
|---|---|---|
| `controller` | MVC or Web API controller, optionally with CRUD actions wired to a `DbContext` | `dotnet aspnet-codegenerator controller -name ProductsController -async -api -m Product -dc AppDbContext -outDir Controllers` |
| `controller` (MVC + views) | Controller with Create/Edit/Delete/Details/Index views | `dotnet aspnet-codegenerator controller -name ProductsController -m Product -dc AppDbContext -udl -outDir Controllers` |
| `view` | A single Razor view for an existing action | `dotnet aspnet-codegenerator view Edit Edit -m Product -dc AppDbContext -outDir Views/Products` |
| `razorpage` | Razor Pages CRUD page set | `dotnet aspnet-codegenerator razorpage Product CRUD -m Product -dc AppDbContext -udl -outDir Pages/Products` |
| `identity` | Scaffolds ASP.NET Core Identity's default UI (login/register/etc.) into your project so you can customize it | `dotnet aspnet-codegenerator identity -dc AppDbContext` |
| `area` | Empty MVC area folder structure (`Areas/<Name>/{Controllers,Views,Models}`) | `dotnet aspnet-codegenerator area Admin` |

Useful flags across the controller/view/razorpage generators:

- `-m <Model>` — the model class to scaffold CRUD against
- `-dc <DbContext>` — the `DbContext` class to query/save through (generator will offer to create one if it can't find it)
- `-api` — Web API controller (JSON actions, no views) instead of MVC
- `-actions` — include CRUD action methods (for an MVC controller *without* the `-api` flag)
- `-async` / `-a` — generate `async`/`await` action methods
- `-udl` — use the app's default `_Layout.cshtml` for generated views
- `-outDir <path>` — output folder, relative to the project
- `-f` — force overwrite of existing files

Run `dotnet aspnet-codegenerator <generator> -h` for the full flag list per generator — they differ slightly (e.g. `razorpage` takes a template name like `CRUD`/`Create`/`Delete`/`Details`/`Edit`/`List`/`Empty` as its second positional argument, the same set `view` accepts as its first).

This is scaffolding, not migrations — for EF Core schema changes (`dotnet ef migrations add`), see the "Scaffolding" row in the table at the top of this doc and install `dotnet-ef` separately the same way.

## Where they genuinely differ

- **Visual Studio** bundles specialized designers and wizards that don't have a real CLI or VS Code equivalent — the WPF/WinForms XAML designer, the EF Core Power Tools reverse-engineering UI, and the Publish wizard's guided Azure resource creation. If you're doing heavy WPF/WinForms UI work or clicking through Azure resource setup, VS is genuinely more convenient.
- **VS Code** is lighter weight, cross-platform (this template's dev container assumes Linux), and everything it does is one Command Palette entry or terminal command away from being scriptable — which is why this repo leans on it.
- **The CLI** (`dotnet`, `git`, `docker`) is the common foundation both editors sit on for anything build/test/package/publish-related. The things the CLI *can't* replace are the genuinely interactive parts: setting breakpoints, stepping through code, inline refactoring previews, and visual diffing — those need an editor (or a debugger UI) by nature, not because of a tooling gap.

So: you're not making life harder by staying in VS Code for this template. You'd only feel friction if you needed one of the VS-exclusive designers above — nothing in this repo's workflow (console app, Web API, xUnit tests, dev container) requires them.

# .NET tooling notes: Visual Studio vs. VS Code vs. CLI

A side-by-side of where to do common .NET dev tasks in each tool. The short version: Visual Studio wraps most of this in dialogs and wizards; VS Code (with C# Dev Kit) exposes the same functionality through the Command Palette and its UI panels; and the `dotnet` CLI (plus `git`) is what both of them call under the hood for almost everything project/build/test related. If you're comfortable in a terminal, you can do essentially all of this without either editor.

**CLI working directory:** unlike the editors (which always know which project you mean, regardless of what's focused), `dotnet` commands with no explicit project/solution argument resolve against whatever single `.csproj`/`.sln`/`.slnx` they find in the *current* directory only — never a parent or child directory. The CLI column below calls out where that matters. This template's solution root is the repo root (where `CSharpTemplate.slnx` lives).

| Task | Visual Studio | VS Code (+ C# Dev Kit) | CLI |
|---|---|---|---|
| Create a new project | File → New → Project (template picker with search/filter UI) | Command Palette → **.NET: New Project** | `dotnet new <template> -n <name>` (e.g. `webapi`, `console`, `xunit`) — run from wherever you want the new project folder created (repo root, typically) |
| Create a new solution | File → New → Project → "Blank Solution" | Command Palette → **.NET: New Project** → solution templates, or terminal | `dotnet new sln --name <name>` — run from wherever you want the `.sln`/`.slnx` created (repo root) |
| Add a project to a solution | Right-click solution → Add → Existing/New Project | Solution Explorer view → right-click solution → Add Project (or CLI) | `dotnet sln add <path/to.csproj>` — run from the directory containing the `.sln`/`.slnx` (repo root here), or pass it explicitly: `dotnet sln <path> add <csproj>` |
| Add a project reference | Right-click project → Add → Project Reference | Solution Explorer view → right-click → Add Reference | `dotnet add <proj> reference <other-proj>` — project paths are relative to cwd, so simplest run from the repo root |
| Add a NuGet package | Right-click project → Manage NuGet Packages (browse/search UI) | NuGet Gallery extension, or C# Dev Kit's dependency UI, or terminal | `dotnet add package <PackageName>` — run from inside the target project's own folder, or add `--project <path>` |
| Restore packages | Automatic on build, or right-click → Restore | Automatic on build, or Command Palette → **.NET: Restore Project** | `dotnet restore` — run from the solution root to restore every project, or a single project's folder to restore just that one |
| Build | Build → Build Solution (Ctrl+Shift+B) | Command Palette → **.NET: Build**, or integrated terminal | `dotnet build` — same cwd rule as Restore above |
| Run without debugging | Ctrl+F5 | Run → Run Without Debugging, or terminal | `dotnet run` — run from inside the target project's own folder; a solution isn't a valid target, so pass `--project <path>` if running from elsewhere |
| Debug / breakpoints | F5, Locals/Watch/Call Stack windows | F5 (C# Dev Kit auto-detects launch target), same debug UI | n/a (debugging is inherently an editor/IDE feature; `dotnet run` alone has no debugger attached) |
| Attach to a running process | Debug → Attach to Process | Command Palette → **Debug: Attach to a .NET 5+ or .NET Core process** | n/a — same caveat as above |
| Hot reload | Automatic during F5 debugging, or Ctrl+Alt+F10 | Automatic during F5 debugging | `dotnet watch run` (its own file-watching hot reload, works outside any editor) — same directory rule as `dotnet run` above |
| Run unit tests | Test Explorer window, run/debug individual tests | Testing view (flask icon), inline CodeLens "Run Test"/"Debug Test" | `dotnet test` (whole project); `dotnet test --filter <expr>` for a subset — run from the solution root (every test project) or a single test project's folder |
| Code formatting | Edit → Advanced → Format Document, format-on-save setting | Format Document (Shift+Alt+F), format-on-save setting | `dotnet format` — run from the solution root or a project's folder |
| Static analysis / code cleanup | Built-in Roslyn analyzers, Error List, "Run Code Cleanup" profiles | Same Roslyn analyzers via the C# extension, Problems panel | `dotnet format analyzers`, or `dotnet build` (analyzers configured as build errors/warnings) — same cwd rule as `dotnet format`/`dotnet build` above |
| Refactoring (rename, extract method, etc.) | Right-click → Quick Actions and Refactorings, Ctrl+. | Ctrl+. (lightbulb) — same Roslyn refactoring engine | n/a — editor-only |
| Generate an XML doc comment skeleton (`///`) | Built-in — `///` above a member always auto-expands to the full `<summary>`/`<param>`/`<returns>` skeleton | Off by default — needs `"editor.formatOnType": true` in `.vscode/settings.json` (set in this template) | n/a — editor-only; `dotnet build` just consumes whatever comments already exist, see below |
| Go to definition / Find references | F12 / Shift+F12, Solution Explorer, Class View | F12 / Shift+F12, Outline view | n/a — editor-only (`grep`/`rg` as a poor substitute) |
| Git operations | Git Changes window, built-in diff/merge UI | Source Control view, built-in diff, GitLens extension for blame/history | `git` directly (`status`, `add`, `commit`, `push`, etc.) — works from any directory inside the repo; `git` walks up to find `.git` itself |
| Manage `launchSettings.json` / launch profiles | UI dropdown next to the Run button | Edit `Properties/launchSettings.json` directly, or `.vscode/launch.json` for editor-side config | n/a — it's a JSON file either way; `dotnet run --launch-profile <name>` to pick one, same directory rule as `dotnet run` above |
| Scaffolding (controllers, Razor pages, EF migrations) | Right-click → Add → Controller/View, EF Core Power Tools UI | `dotnet-aspnet-codegenerator` and `dotnet-ef` tools invoked from terminal (no built-in GUI) | `dotnet aspnet-codegenerator controller ...`, `dotnet ef migrations add <Name>` — both run from inside the target project's own folder; see the dedicated section below |
| Publish / deploy | Right-click project → Publish (wizard: folder, IIS, Azure, container, etc.) | Command Palette → **.NET: Publish** (thinner UI, same targets) | `dotnet publish -c Release -o <dir>` — run from inside the target project's own folder, or add `--project <path>` |
| Package/library dependency vulnerability check | NuGet Package Manager UI flags vulnerable packages | `dependi` extension (already in this template's recommendations) surfaces it inline | `dotnet list package --vulnerable` — run from the solution root or a project's folder |
| Container support | Right-click → Add → Container Orchestrator Support, Docker publish target | Dev Containers extension (this template's `.devcontainer/`), Docker extension | `docker build`/`docker run` — run from wherever the `Dockerfile` is (repo root, typically); or `dotnet publish /t:PublishContainer` — run from inside the target project's own folder |
| Solution-wide search | Ctrl+; (Go To All), Find in Files | Ctrl+P (Go To File), Ctrl+Shift+F (Find in Files) | `grep`/`rg` — works from any directory, searches the tree below it |
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

All of those are **class libraries** (`dotnet new classlib`) — there's no separate "domain project" or "infrastructure project" template. The type name in `dotnet new` just describes what hosts/runs the code (console entry point, web host, test runner, or nothing); layering is a naming and dependency convention you apply on top of plain class libraries. A typical split, following the dependencies-point-inward rule of Clean/Onion architecture — run the whole block below from the repo root (each `dotnet new` creates its project folder there, and the `dotnet sln add`/`dotnet add reference` lines use paths relative to it):

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

## Adding new files: classes, interfaces, and other "Add New Item" equivalents

Visual Studio's right-click a folder → Add → New Item (Ctrl+Shift+A) opens a template picker — Class, Interface, Enum, Struct/Record on recent VS versions, plus config files like `.gitignore` or `NuGet.Config` — and drops the result straight into that folder. Here's what maps to it outside VS.

### Plain C# types (class, interface, enum, struct, record) — no CLI equivalent exists

Checked against this repo's pinned SDK (`dotnet new list`): the base .NET SDK ships **no** item template for a bare `class`/`interface`/`enum`/`struct`/`record`. Only project-level templates (`console`, `classlib`, ...) and a handful of specific web/test item templates (below) are built in — nothing generic for "just a type in a file." This isn't a gap so much as a non-problem: this template's projects are SDK-style, which glob every `.cs` file under the project folder automatically (see [SDK-style vs. old-style project files](README.md#sdk-style-vs-old-style-project-files) in the README) — a class file needs no registration anywhere, so there's nothing for a template to generate beyond the text itself. The honest CLI "equivalent" is creating the file yourself and typing the code:

```sh
# run from wherever the file should live, e.g. inside the project folder
New-Item Services/OrderService.cs   # PowerShell
touch Services/OrderService.cs      # bash, or this template's dev container
```

...then write `public class OrderService { }` by hand, or use a snippet to expand it (see [Overriding built-in code snippets](#overriding-built-in-code-snippets-class-ctor-etc) below — this is exactly the gap that section's `class`/`interface`/`enum` snippets exist to fill, and exactly why their missing access modifier matters).

VS Code does have a real equivalent, via C# Dev Kit: Command Palette → **.NET: New File...** draws on the same underlying item-template catalog Visual Studio's Add New Item uses (Class, Interface, Enum, Record, Struct, plus the web/Razor items below). Two differences from Visual Studio worth knowing before you rely on it:

- It's Command Palette (or VS Code's own **File: New File...**) driven, not a folder right-click — in this template's pinned C# Dev Kit version, the command isn't wired into the Explorer's right-click menu, so right-clicking a folder won't offer it.
- The generated file always lands at the target **project's root**, regardless of which folder had focus — move it afterwards if you wanted it in a subfolder (already called out in the README's [Doing this automatically in VS Code](README.md#doing-this-automatically-in-vs-code) section, for the same underlying command).

### Item types `dotnet new` does support

These are real templates — confirmed via `dotnet new list` and each one's `-h` — and they're the same templates `aspnet-codegenerator` itself calls under the hood for the simplest cases. For example, `dotnet aspnet-codegenerator controller -name PingController -api` (from the [Scaffolding](#scaffolding-with-dotnet-aspnet-codegenerator) section below) literally shells out to `dotnet new apicontroller --name PingController`. For that bare case — no model, no `DbContext` — you can skip installing the codegenerator tool entirely and call the template directly; unlike `aspnet-codegenerator`, these don't need the project to build first, don't need any extra NuGet packages, and don't even need to run inside a project folder — `-o <dir>` alone controls where the file lands, relative to wherever you run the command.

| Item | Template | Example | Notes |
|---|---|---|---|
| MVC controller | `mvccontroller` | `dotnet new mvccontroller -n ProductsController -o Controllers` | `-ac`/`--actions` adds empty CRUD action stubs (no model wiring) |
| Web API controller | `apicontroller` | `dotnet new apicontroller -n ProductsController -o Controllers` | Same `-ac`/`--actions` flag; this is what the minimal `aspnet-codegenerator controller -api` example above produces under the hood |
| Razor view | `view` | `dotnet new view -n Edit -o Views/Products` | Empty view, no model scaffolding — for CRUD views wired to a model, use `aspnet-codegenerator view` instead |
| Razor Page | `page` | `dotnet new page -n Edit -o Pages/Products` | `-np`/`--no-pagemodel` skips the code-behind `.cshtml.cs` |
| Razor component (Blazor) | `razorcomponent` | `dotnet new razorcomponent -n ProductCard -o Components` | |
| MSTest test class | `mstest-class` | `dotnet new mstest-class -n OrderServiceTests` | `--fixture <Kind>` adds a fixture method stub (`ClassInitialize`, `TestCleanup`, etc.) |
| NUnit test class | `nunit-test` | `dotnet new nunit-test -n OrderServiceTests` | This template's own `CSharpTemplate.Tests` uses xUnit instead, which has no bare "new test class" item template — `[Fact]`/`[Theory]` methods just go in any `.cs` file |
| Protocol Buffer file | `proto` | `dotnet new proto -n order -o Protos` | gRPC service/message contract |
| MVC `_ViewImports.cshtml` | `viewimports` | `dotnet new viewimports -o Views` | |
| MVC `_ViewStart.cshtml` | `viewstart` | `dotnet new viewstart -o Views` | |

All of these accept `-p:n`/`--namespace` to override the generated namespace (defaults to the placeholder `MyApp.Namespace` — it does **not** infer the real project/folder namespace the way Visual Studio's Add New Item does, so expect to fix it up afterwards, or pass `-p:n` explicitly). `-n`/`--name` sets both the file name and the type name inside it.

### Config and misc files

Visual Studio's Add New Item also covers non-code files — `.gitignore`, `.editorconfig`, `NuGet.Config`, and similar. `dotnet new` ships templates for these too:

| File | Template |
|---|---|
| `.editorconfig` | `dotnet new editorconfig` |
| `.gitignore` | `dotnet new gitignore` |
| `.gitattributes` | `dotnet new gitattributes` |
| `global.json` | `dotnet new globaljson` |
| `NuGet.Config` | `dotnet new nugetconfig` |
| `Web.config` | `dotnet new webconfig` |
| `Directory.Build.props` | `dotnet new buildprops` |
| `Directory.Build.targets` | `dotnet new buildtargets` |
| `Directory.Packages.props` (central package management) | `dotnet new packagesprops` |

Run `dotnet new list` yourself for the current, full catalog — the SDK adds and retires templates across versions, so both this table and the [project-type table](#adding-different-project-types) above reflect what ships with this repo's pinned SDK ([global.json](global.json)), not necessarily every SDK version.

## Overriding built-in code snippets (`class`, `ctor`, etc.)

Typing `class` + Tab in a `.cs` file and getting `class Foo` back with no `public` isn't a bug or a missing setting — it's the literal, verified content of the built-in snippet. It ships inside the `ms-dotnettools.csharp` extension itself (`snippets/csharp.json`), not VS Code core, so it isn't something this template's `.vscode/settings.json` could have configured away even if it tried. Checked directly against the installed extension's snippet file, here's what the type-declaration snippets actually contain:

- `class`, `interface`, `struct`, `enum`, `namespace` → insert the bare keyword only, e.g. `class ${1:$TM_FILENAME_BASE}` — genuinely zero access modifier, not even a placeholder for one.
- `ctor` → **does** include `public` by default — as the first editable tab stop (`${1:public} ${2:$TM_FILENAME_BASE}(...)`), not fixed text. It's easy to type straight through it without noticing, since it's pre-selected, but it's already there; deleting or retyping it is what leaves it blank, the snippet itself doesn't omit it.
- `prop` → already hardcodes `public` (`public ${1:int} ${2:MyProperty} { get; set; }`), no placeholder involved.

So the actual fix only needs to target `class`/`interface`/`struct`/`enum`/`namespace`. Two ways to do it, in order of how durable they are:

### Option A: add your own snippet with the same prefix

This template already ships an empty shell for this at [.vscode/template.code-snippets](.vscode/template.code-snippets) (mentioned in the README's "What's included"), and now includes exactly this: a `Public class` entry with `prefix: publicclass`:

```json
"Public class": {
    "scope": "csharp",
    "prefix": "publicclass",
    "body": ["public class ${1:$TM_FILENAME_BASE}", "{", "\t$0", "}"],
    "description": "Class declaration with the public modifier"
}
```

Type `publicclass` + Tab in any `.cs` file and it expands the same way `class` does, just with the modifier already in place — no lightbulb, no extra step.

It's deliberately named `publicclass` rather than reusing the built-in `class` prefix. Important caveat behind that choice: VS Code does **not** let a workspace/user snippet replace an extension's snippet with the same prefix — when two snippets share a prefix, both show up as separate entries in the IntelliSense suggestion list (distinguished by their name/description), and you pick between them with the arrow keys before hitting Tab/Enter. It's additive, not an override. Reusing `class` here would mean typing `class` + Tab still sometimes expands the bare built-in version by mistake (whichever entry is highlighted first); a distinct prefix sidesteps that ambiguity entirely at the cost of a few extra keystrokes to type. If you'd rather have it, the same file format works at the user level too — Command Palette → **Snippets: Configure User Snippets** → `csharp.json` (or **New Global Snippets File...**) — for a version that follows you across repos instead of living in this one.

### Option B (more durable): turn on the accessibility-modifier analyzer

[.editorconfig](.editorconfig) already states the *preference* for this, at line 59:

```ini
dotnet_style_require_accessibility_modifiers = for_non_interface_members:silent
```

— but `silent` severity means the Roslyn analyzer (IDE0040) evaluates it without surfacing anything: no squiggle, no lightbulb shown automatically. Raise the severity (`suggestion` or `warning`) and, after expanding `class`/`ctor`/etc., Roslyn offers an **"Add accessibility modifiers"** quick fix (Ctrl+.) right at the cursor — and `dotnet format` (or format-on-save) will fix every existing instance across the project in bulk, not just newly-typed ones. Since this is a `.editorconfig`-driven analyzer rather than an editor feature, it applies identically in Visual Studio and Rider too — worth doing instead of (or alongside) Option A if the goal is "never see this again" rather than "fix these two specific snippets."

Visual Studio has its own, separate snippet system — Tools → Code Snippets Manager (Ctrl+K, Ctrl+B), backed by `.snippet` XML files rather than VS Code's JSON format — if you're customizing snippets there instead, Option B above still applies unchanged, since it isn't editor-specific.

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
# global tool — available in every project on this machine; run from anywhere, -g ignores cwd entirely
dotnet tool install -g dotnet-aspnet-codegenerator
dotnet tool update -g dotnet-aspnet-codegenerator   # later, to upgrade

# local tool — pinned per-repo via a tool manifest (checked into source control, like this template's global.json pins the SDK)
# run these from the repo root
dotnet new tool-manifest -o .config    # only if the repo doesn't already have .config/dotnet-tools.json
dotnet tool install dotnet-aspnet-codegenerator
dotnet tool restore                    # what teammates/CI run to get the same version
```

`dotnet new tool-manifest` on its own writes a loose `dotnet-tools.json` into whatever directory you run it from, *not* `.config/dotnet-tools.json` — despite `.config/dotnet-tools.json` being the conventional location nearly every doc, CI template, and other repo expects (and the one `.gitignore` templates special-case). Passing `-o .config` puts it straight there in one step, as above. If you already ran it without `-o` and have a loose `dotnet-tools.json` sitting in the repo root, just move it: `mkdir .config && mv dotnet-tools.json .config/dotnet-tools.json` — `dotnet tool install`/`restore` pick it up from `.config/` the same way.

With a local tool, either prefix commands with `dotnet tool run` or just call `dotnet aspnet-codegenerator ...` directly — the local manifest is picked up automatically once restored. Unlike `dotnet new tool-manifest` itself (which only ever acts on the current directory, since it's creating the file rather than finding one), `dotnet tool install`, `dotnet tool restore`, and invoking an installed local tool all walk up from the current directory looking for `.config/dotnet-tools.json` — the same way `git` walks up looking for `.git` — so once it exists at the repo root, those commands work from any subdirectory too, including a project's own folder.

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
| `controller` (minimal) | Empty `[ApiController]` with routing already set up (`[Route("api/[controller]")]`) — no model, no `DbContext`, no output folder, `-name`/`-n` is the only flag it actually needs | `dotnet aspnet-codegenerator controller -name PingController -api` |
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

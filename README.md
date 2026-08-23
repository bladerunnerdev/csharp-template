# csharp-template

Template with boilerplate for a C# project targeting .NET 10, with a dev container, VS Code settings and extension suggestions, and editor-agnostic formatting via `.editorconfig`.

## What's included

- `CSharpTemplate/CSharpTemplate.csproj` / `CSharpTemplate.slnx` — a console app project and solution
- `global.json` — pins the .NET SDK version so local, dev container, and CI builds stay in sync
- `.editorconfig` — code style and formatting rules, respected by VS Code, Visual Studio, Rider, and `dotnet format`
- `.devcontainer/devcontainer.json` — a ready-to-use dev container with the .NET SDK and recommended extensions preinstalled
- `.vscode/` — workspace settings, extension recommendations, a launch configuration shell, and a snippets file

## Getting started

### Using the dev container

Open the folder in VS Code and choose **"Reopen in Container"** when prompted (requires the Dev Containers extension and Docker). This gives you a consistent environment with the .NET SDK and all recommended extensions already installed.

### Locally

Requires the [.NET SDK](https://dotnet.microsoft.com/download) version pinned in `global.json`.

```sh
dotnet build   # build the solution
dotnet run     # run the console app
```

### If VS Code shows "No Solution"

VS Code is supposed to auto-detect the single `CSharpTemplate.slnx` in the repo root, but this has been reported as unreliable in some C# Dev Kit versions. If the status bar shows "No Solution" in red, add this to `.vscode/settings.json`:

```json
"dotnet.defaultSolution": "CSharpTemplate.slnx"
```

It's not included by default since auto-detection does work in most setups — only add it if you actually hit this.

## Debugging

Press F5 (or use the Run and Debug view) with a `.cs` file open — with C# Dev Kit installed, no `launch.json` is required. It figures out the right project to build and launch dynamically. `.vscode/launch.json` in this template is deliberately left empty (`"configurations": []`); it's there as a shell to fill in only if you need a customized configuration (env vars, CLI args, auto-launching a browser, etc.), not because one is required to debug.

If VS Code (or a tutorial) prompts you to run **".NET: Generate Assets for Build and Debug"** — the dialog shown when doing so with C# Dev Kit installed warns that it's *not recommended*. That command is the older, pre-Dev-Kit way of debugging: it writes a `coreclr`-based `launch.json` plus a matching `tasks.json` build task. C# Dev Kit's dynamic configurations replace both, so choosing **Yes** ("use a dynamic configuration instead") is the right call — see [No launch.json required](https://code.visualstudio.com/docs/csharp/debugging#_no-launchjson) for details. Only choose **No** if you specifically want the legacy, file-based setup for some reason.

### Attaching to a process started outside VS Code

If you ran `dotnet run` in a plain terminal (or anywhere else outside VS Code's own Run and Debug flow) and want to attach a debugger to it afterwards, this isn't in the Run and Debug dropdown — that only lists launch configurations, not attach. Instead:

1. Open the Command Palette (Ctrl+Shift+P) and run **"Debug: Attach to a .NET 5+ or .NET Core process"**.
2. A process picker appears listing several processes — pick the actual app, e.g. `MyProject.exe`, **not** the shell/terminal process you ran `dotnet run` from (e.g. `bash`, `pwsh`). Attaching to the shell will look like it worked, but breakpoints won't bind and you'll get "No symbols have been loaded for this document" — that's the sign you picked the wrong one.

This works for any already-running .NET process, not just ones started via `dotnet run` — useful for attaching to something running in Docker, a separate terminal, or launched by another tool entirely.

If your solution has more than one project, `dotnet run` on its own only works from inside that project's own directory — from anywhere else (e.g. the solution root), point it at the project explicitly:

```sh
dotnet run --project MyProject/MyProject.csproj
```

## Adding a Web API project

There are two ways to go about this, depending on whether you want to keep the console app or replace it.

### Option A: keep the template project, add the API alongside it

Adds a new project to the existing `CSharpTemplate.slnx` without touching what's already here.

```sh
# pick one of the two:
dotnet new webapi -n MyApi                # minimal API (no controllers)
dotnet new webapi -controllers -n MyApi   # controller-based Web API

dotnet sln add MyApi/MyApi.csproj
```

### Option B: start from scratch

Removes the template's own console app, project, and solution files first, then creates a fresh solution containing only the new API.

```sh
rm -r CSharpTemplate.slnx CSharpTemplate

dotnet new sln --name MyApi

# pick one of the two:
dotnet new webapi -n MyApi                # minimal API (no controllers)
dotnet new webapi -controllers -n MyApi   # controller-based Web API

dotnet sln add MyApi/MyApi.csproj
```

`dotnet sln add` looks for a single `.sln`/`.slnx` in the current directory when none is given — if you skip `dotnet new sln` after deleting `CSharpTemplate.slnx`, it'll fail with "no solution file found."

Either way, if you're using C# Dev Kit, the same templates are available from the Command Palette via **".NET: New Project"**, which walks through picking a template, name, and location instead of using the CLI directly.

## Using this as a starting point

This repo is meant to be renamed once you start a real project. At minimum, update:

- `CSharpTemplate/CSharpTemplate.csproj`, `CSharpTemplate.slnx`, and the `CSharpTemplate` folder itself — rename all three and update the `<Project Path>` reference inside the `.slnx`
- This README

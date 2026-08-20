# csharp-template

Template with boilerplate for a C# project targeting .NET 10, with a dev container, VS Code settings and extension suggestions, and editor-agnostic formatting via `.editorconfig`.

## What's included

- `csharp-template.csproj` / `csharp-template.slnx` — a console app project and solution
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

## Adding a Web API project

The console app in this template is a starting point — you can add other project types to the same solution. For an ASP.NET Core Web API:

```sh
dotnet new webapi -n MyApi                # minimal API (no controllers)
dotnet new webapi -controllers -n MyApi   # controller-based Web API
dotnet sln add MyApi/MyApi.csproj
```

If you're using C# Dev Kit, the same templates are available from the Command Palette via **".NET: New Project"**, which walks through picking a template, name, and location instead of using the CLI directly.

## Using this as a starting point

This repo is meant to be renamed once you start a real project. At minimum, update:

- `csharp-template.csproj` and `csharp-template.slnx` — rename both files and update the `<Project Path>` reference inside the `.slnx`
- This README

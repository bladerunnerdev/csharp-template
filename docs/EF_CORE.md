# Entity Framework Core: setup and CLI reference

- [Entity Framework Core: setup and CLI reference](#entity-framework-core-setup-and-cli-reference)
  - [Installing the packages](#installing-the-packages)
    - [Which package goes in which project](#which-package-goes-in-which-project)
    - [Provider packages](#provider-packages)
    - [Optional add-ons](#optional-add-ons)
  - [Installing the `dotnet-ef` CLI tool](#installing-the-dotnet-ef-cli-tool)
  - [Design-time configuration: the error you'll hit first](#design-time-configuration-the-error-youll-hit-first)
    - [Option A: point at a startup project that registers the DbContext](#option-a-point-at-a-startup-project-that-registers-the-dbcontext)
    - [Option B: `OnConfiguring` on the DbContext](#option-b-onconfiguring-on-the-dbcontext)
    - [Option C: `IDesignTimeDbContextFactory<TContext>`](#option-c-idesigntimedbcontextfactorytcontext)
  - [Command reference](#command-reference)
    - [`dotnet ef migrations`](#dotnet-ef-migrations)
    - [`dotnet ef database`](#dotnet-ef-database)
    - [`dotnet ef dbcontext`](#dotnet-ef-dbcontext)
    - [Flags shared by nearly every command](#flags-shared-by-nearly-every-command)
  - [Database-first: reverse-engineering an existing database](#database-first-reverse-engineering-an-existing-database)
  - [Common workflows](#common-workflows)
  - [Visual Studio Package Manager Console equivalents](#visual-studio-package-manager-console-equivalents)
  - [Applying migrations outside your dev machine](#applying-migrations-outside-your-dev-machine)
  - [Gotchas worth knowing](#gotchas-worth-knowing)
  - [Where EF Core tooling differs across editors](#where-ef-core-tooling-differs-across-editors)

A companion to [DOTNET_TOOLING.md](DOTNET_TOOLING.md), covering the one piece that doc deliberately punts on: EF Core. Where `aspnet-codegenerator` scaffolds *controllers and views*, `dotnet-ef` manages your *schema* — migrations, database updates, and reverse-engineering an existing database into C# classes. They're separate tools with separate installs, and it's worth not conflating them.

Everything below was checked against this repo's pinned SDK ([global.json](../global.json) → 10.0.100, `rollForward: latestMinor`), `dotnet-ef` 10.0.12, and EF Core 10.0.12 — the current stable line for `net10.0`, which is what this template's projects target.

**CLI working directory:** the same rule from the sibling doc applies, and EF is stricter about it than most `dotnet` commands. `dotnet ef` resolves the project from the *current* directory unless you pass `-p`/`--project`, and it needs the project to **build successfully first** — it runs a design-time build and reflects over the compiled assembly to find your `DbContext`. A project that doesn't compile produces EF errors that look like configuration problems but aren't.

## Installing the packages

There's no single "install EF Core" package. You always need at least two things — the provider for your database (which transitively brings in `Microsoft.EntityFrameworkCore` itself, so you rarely reference the base package directly) and the design-time package that the CLI tool talks to.

```sh
# run from inside the project that will own your DbContext
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.SqlServer   # or whichever provider, see below
```

That's the whole minimum. `dotnet add package` resolves the latest stable version compatible with the project's target framework, so on a `net10.0` project both land on 10.0.x without you pinning anything.

One detail worth knowing, since it looks like a mistake the first time you see it: `Microsoft.EntityFrameworkCore.Design` is flagged as a development dependency, so `dotnet add package` writes it into the `.csproj` differently from a normal reference — verified output on a fresh `classlib`:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.12">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.12" />
```

`<PrivateAssets>all</PrivateAssets>` means the package is used at build/design time but doesn't flow to anything that references this project, and doesn't ship in your published output. That's correct and deliberate — don't "fix" it by deleting those lines.

### Which package goes in which project

This trips people up in layered solutions (the [Domain / Application / Infrastructure split](DOTNET_TOOLING.md#which-type-for-domain--infrastructure--data--application-layers) from the sibling doc). The rule: **EF packages go on the project that contains the `DbContext`**, not on the web/API project, and not on Domain.

| Package                                                | Goes on                                                     | Why                                                                                                                     |
| ------------------------------------------------------ | ----------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `Microsoft.EntityFrameworkCore.<Provider>`             | the project with your `DbContext` (`Infrastructure`/`Data`) | the provider *is* EF Core plus the database-specific bits; it brings `Microsoft.EntityFrameworkCore` in transitively    |
| `Microsoft.EntityFrameworkCore.Design`                 | the project with your `DbContext`                           | what `dotnet ef` loads to build the model at design time; without it every command fails immediately                    |
| `Microsoft.EntityFrameworkCore.Abstractions`           | Domain, if anything                                         | attributes/interfaces only, no runtime engine — lets Domain use e.g. `[Owned]` without depending on real EF             |
| `Microsoft.EntityFrameworkCore` (base package, direct) | rarely needed explicitly                                    | only if a project uses EF types but references no provider (e.g. a shared library defining a base `DbContext`)          |
| `Microsoft.EntityFrameworkCore.Tools`                  | the project with your `DbContext`, *only* for Visual Studio | powers the `Add-Migration`/`Update-Database` PowerShell cmdlets in Package Manager Console; pure CLI users don't need it |

`Microsoft.EntityFrameworkCore.Tools` and `dotnet-ef` are two front ends over the same `Design` package. Installing both is harmless (and normal if your team is split across VS and VS Code), but if you're CLI-only, `Tools` is dead weight.

### Provider packages

Pick exactly one per `DbContext`. Latest stable versions below are as published at time of writing, resolved via `dotnet package search <id> --exact-match`:

| Database               | Package                                   | Latest stable | Notes                                                                                                                  |
| ---------------------- | ----------------------------------------- | ------------- | ---------------------------------------------------------------------------------------------------------------------- |
| SQL Server / Azure SQL | `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.12       | first-party; also covers LocalDB and SQL Server in Docker                                                              |
| SQLite                 | `Microsoft.EntityFrameworkCore.Sqlite`    | 10.0.12       | first-party; ideal for the dev container and integration tests — a file, or `Data Source=:memory:`                     |
| PostgreSQL             | `Npgsql.EntityFrameworkCore.PostgreSQL`   | 10.0.3        | community-maintained but the de facto standard; tracks EF major versions closely                                       |
| Azure Cosmos DB        | `Microsoft.EntityFrameworkCore.Cosmos`    | 10.0.12       | document database — migrations don't apply, see the gotchas section                                                    |
| MySQL / MariaDB        | `Pomelo.EntityFrameworkCore.MySql`        | 9.0.0         | **no EF 10 release yet** — on a `net10.0` project you'll be pinned to the EF 9 line until Pomelo ships 10              |
| Oracle                 | `Oracle.EntityFrameworkCore`              | 10.23.26301   | vendor-maintained; its version numbers track Oracle's own scheme, not EF's                                             |
| In-memory (tests only) | `Microsoft.EntityFrameworkCore.InMemory`  | 10.0.12       | **not a relational database** — no constraints, no transactions, no SQL; Microsoft recommends SQLite for tests instead |

The MySQL and Oracle rows are the reason to check rather than assume: third-party providers don't ship on EF's release day, and `dotnet add package` will happily give you an older major version that then fails against a `net10.0` project's other EF 10 packages with a version-conflict error.

### Optional add-ons

| Package                                                | What it gives you                                                                          |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------ |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore`    | `IdentityDbContext` — user/role/claim tables, if you're using ASP.NET Core Identity        |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | the developer "migrations pending" error page, plus `UseMigrationsEndPoint()`              |
| `Microsoft.EntityFrameworkCore.Proxies`                | lazy-loading proxies (requires `virtual` navigation properties)                            |
| `EFCore.NamingConventions`                             | `UseSnakeCaseNamingConvention()` and friends — near-mandatory quality-of-life on PostgreSQL |

## Installing the `dotnet-ef` CLI tool

`dotnet-ef` is **not** part of the base SDK — `dotnet ef` on a clean machine fails with "Could not execute because the specified command or file was not found." It installs exactly like `dotnet-aspnet-codegenerator` in the sibling doc:

```sh
# global tool — available everywhere on this machine; -g ignores cwd entirely
dotnet tool install -g dotnet-ef
dotnet tool update -g dotnet-ef      # later, to upgrade

# local tool — pinned per-repo via a manifest checked into source control
# run these from the repo root
dotnet new tool-manifest -o .config  # only if .config/dotnet-tools.json doesn't exist yet
dotnet tool install dotnet-ef
dotnet tool restore                  # what teammates/CI run to get the same version
```

The `-o .config` detail from the sibling doc applies identically here: without it, `dotnet new tool-manifest` writes a loose `dotnet-tools.json` into the current directory rather than the conventional `.config/dotnet-tools.json`.

Local is the better default for a team repo — it pins the tool version alongside the SDK pin in [global.json](../global.json), so nobody generates migrations with a different EF version than everyone else. Verified: once `.config/dotnet-tools.json` exists at the repo root, plain `dotnet ef` works from any subdirectory (including a project's own folder), because `dotnet tool` walks up looking for the manifest the same way `git` walks up looking for `.git`.

**Keep the tool and the packages on the same major version.** A 10.x tool against 9.x packages (or the reverse) is the cause of most "the EF tools version is older than the runtime" style warnings. `dotnet ef --version` tells you the tool's; the `.csproj` tells you the packages'.

## Design-time configuration: the error you'll hit first

Almost everyone's first `dotnet ef migrations add` fails, and the message is long enough to be intimidating. Verified output from a class library with a `DbContext` but no configuration:

```text
Unable to create a 'DbContext' of type 'AppDbContext'. The exception 'No database provider has been
configured for this DbContext. A provider can be configured by overriding the 'DbContext.OnConfiguring'
method or by using 'AddDbContext' on the application service provider. ...' was thrown while attempting
to create an instance.
```

What it means: EF built your project fine and *found* the `DbContext` class, but couldn't construct an instance, because at design time there's no running app handing it a connection string. There are three ways to fix it, in rough order of preference.

### Option A: point at a startup project that registers the DbContext

The usual production setup: the `DbContext` lives in `Infrastructure`, but the connection string and `AddDbContext` call live in the API project's DI setup. Tell EF about both:

```sh
# run from the repo root
dotnet ef migrations add InitialCreate --project MyProject.Infrastructure --startup-project MyProject.Api
```

- `--project` / `-p` — where the migration *files* get written, and where the `DbContext` lives.
- `--startup-project` / `-s` — the project EF actually builds and runs to obtain configuration (`appsettings.json`, user secrets, DI registrations).

Both default to the current directory, which is exactly why it works with no flags in a single-project app and needs both flags in a layered one. Typing this on every command gets old fast — see [Common workflows](#common-workflows) for the alternative.

### Option B: `OnConfiguring` on the DbContext

The simplest thing that works, and fine for a prototype, a sample, or a SQLite-backed test project:

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=app.db");
}
```

Verified: with this in place, `dotnet ef migrations add` works on a bare `classlib` with no startup project and no flags at all. The catch is that it hardcodes the connection string into the class, and it takes precedence in ways that can surprise you when the same context is also configured through DI. Don't ship it for a real database.

### Option C: `IDesignTimeDbContextFactory<TContext>`

The explicit escape hatch: a class EF looks for by convention, used *only* at design time and ignored entirely at runtime. This keeps the `DbContext` itself DI-shaped (options injected via constructor) while still giving the tooling something it can construct:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=design.db")
            .Options;

        return new AppDbContext(options);
    }
}
```

```csharp
// the DbContext stays DI-friendly — a primary constructor keeps this to one line
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}
```

Verified end to end: dropping that factory into the same class library that produced the error above makes `dotnet ef migrations add Initial` succeed, with no startup project involved. Put the factory in the same project as the `DbContext`; EF discovers it automatically, no registration needed. Since it's design-time only, using a throwaway local database here (or reading a connection string from an environment variable) is normal and safe — it never runs in production.

## Command reference

`dotnet ef` has exactly three command groups. `dotnet ef <group> --help` is authoritative; the tables below are the practical subset.

### `dotnet ef migrations`

| Command                                          | What it does                                                          | Notes / useful flags                                                                                                      |
| ------------------------------------------------ | --------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `dotnet ef migrations add <Name>`                | Diffs your model against the last snapshot and writes a new migration | `-o/--output-dir <PATH>` (default `Migrations`), `-n/--namespace`. Name it like a commit message: `AddProductPriceColumn` |
| `dotnet ef migrations remove`                    | Deletes the most recent migration and rewinds the model snapshot      | `-f/--force` also reverts it from the database if it was already applied. Only ever removes the *last* one                |
| `dotnet ef migrations list`                      | Lists migrations and marks which are `(Pending)`                      | `--no-connect` to list without touching the database; `--json` for scripting                                              |
| `dotnet ef migrations has-pending-model-changes` | Exit code says whether the model drifted since the last migration     | Purpose-built for CI — fails the build when someone edits an entity and forgets `migrations add`                          |
| `dotnet ef migrations script`                    | Emits SQL instead of touching a database                              | `<FROM> <TO>` positionals, `-o/--output <FILE>`, `-i/--idempotent`, `--no-transactions`                                   |
| `dotnet ef migrations bundle`                    | Builds a self-contained `efbundle` executable that applies migrations | `--self-contained`, `-r/--target-runtime <RID>`, `-o/--output <FILE>`, `-f/--force` to overwrite                          |

`migrations script` with no arguments scripts everything from an empty database to the latest migration. Ranges are positional: `dotnet ef migrations script AddProducts AddOrders`. `-i/--idempotent` wraps each migration in an existence check so the script is safe to run against a database at any migration — the usual choice for handing SQL to a DBA.

### `dotnet ef database`

| Command                            | What it does                                                     | Notes                                                                          |
| ---------------------------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| `dotnet ef database update`        | Applies all pending migrations (creating the database if needed) | Takes an optional `<MIGRATION>` target                                         |
| `dotnet ef database update <Name>` | Migrates **to** that migration — forward or backward             | Rolls back anything applied after it                                           |
| `dotnet ef database update 0`      | Reverts *every* migration, leaving an empty database             | The literal `0` is a special target meaning "before the first migration"       |
| `dotnet ef database drop`          | Drops the database entirely                                      | Prompts for confirmation; `-f/--force` skips it, `--dry-run` names the target  |

`--connection <CONNECTION>` overrides whatever connection string the app config supplies — handy for pointing the same migrations at a staging database without editing `appsettings.json`.

### `dotnet ef dbcontext`

| Command                        | What it does                                                               | Notes                                                                           |
| ------------------------------ | -------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| `dotnet ef dbcontext list`     | Lists every `DbContext` type EF can find in the project                    | First thing to run when a command can't find or can't disambiguate a context    |
| `dotnet ef dbcontext info`     | Prints provider, database name, data source, and options for a context     | Fastest way to confirm which connection string design time actually resolved    |
| `dotnet ef dbcontext scaffold` | Reverse-engineers entity classes + a `DbContext` from an existing database | Database-first; see the next section                                            |
| `dotnet ef dbcontext script`   | Emits the full schema SQL from the model, ignoring migrations entirely     | `-o/--output <FILE>`. Useful when you don't use migrations at all               |
| `dotnet ef dbcontext optimize` | Generates a compiled model to cut startup time on large models             | `--precompile-queries`, `--nativeaot` (experimental). Re-run when the model changes |

Verified `dbcontext info` output shape:

```text
Type: Data.AppDbContext
Provider name: Microsoft.EntityFrameworkCore.Sqlite
Database name: main
Data source: app.db
Options: None
```

### Flags shared by nearly every command

These appear on essentially every `dotnet ef` subcommand:

- `-c <DbContext>` / `--context` — required when the project has more than one `DbContext`. `*` runs the command for all of them.
- `-p <PATH>` / `--project` — the project owning the `DbContext` and migrations. Defaults to cwd.
- `-s <PATH>` / `--startup-project` — the project EF builds and runs for configuration. Defaults to cwd.
- `--configuration <CONFIGURATION>` — e.g. `Release`, when design-time behaviour differs by configuration.
- `--framework <FRAMEWORK>` — needed only on multi-targeted projects; defaults to the first TFM.
- `--no-build` — skip the design-time build. **Read the gotcha below before using this.**
- `-v` / `--verbose` — full stack traces. The first thing to add when an error is unhelpful.
- `--json` / `--prefix-output` — machine-readable output, on the commands that support it.

## Database-first: reverse-engineering an existing database

`dbcontext scaffold` is the "I already have a database" path — it inspects a live database and writes entity classes and a `DbContext` to match. You need the provider and `Design` packages installed first, same as anything else.

```sh
# run from inside the project the generated code should land in
dotnet ef dbcontext scaffold "Data Source=app.db" Microsoft.EntityFrameworkCore.Sqlite \
  --output-dir Models --context ShopContext --context-dir Context
```

Both positional arguments are mandatory: the connection string, then the provider's **package name**.

| Flag                     | Effect                                                                        |
| ------------------------ | ----------------------------------------------------------------------------- |
| `-o/--output-dir <PATH>` | Where entity classes go, relative to the project                              |
| `--context-dir <PATH>`   | Where the `DbContext` goes, if you want it separate from the entities         |
| `-c/--context <NAME>`    | Name of the generated `DbContext` (defaults to the database name)             |
| `-t/--table <TABLE>...`  | Scaffold only these tables/views; repeatable, accepts `schema.table`          |
| `--schema <SCHEMA>...`   | Scaffold everything in these schemas                                          |
| `-d/--data-annotations`  | Use attributes where possible instead of fluent API only                      |
| `--use-database-names`   | Keep database identifiers verbatim instead of C#-ifying them                  |
| `--no-pluralize`         | Disable the pluralizer                                                        |
| `--no-onconfiguring`     | Don't emit `OnConfiguring` — **use this**, see below                          |
| `-f/--force`             | Overwrite existing generated files                                            |
| `-n/--namespace`         | Namespace for entities; `--context-namespace` for the context                 |

Verified run against a SQLite database with one table, using the flags above, produced exactly `Models/Product.cs`, `Models/EfmigrationsLock.cs`, and `Context/ShopContext.cs` — note that EF's own internal tables get scaffolded too if they exist, and are yours to delete.

That run also emitted this warning, which is worth taking seriously:

```text
To protect potentially sensitive information in your connection string, you should move it out of
source code. You can avoid scaffolding the connection string by using the Name= syntax to read it
from configuration...
```

By default the connection string you pass is baked into the generated `OnConfiguring`. Pass `--no-onconfiguring` and configure the context through DI instead, or use `Name=ConnectionStrings:Default` so only the *key* is generated.

**Scaffolding is a one-way, regenerate-only workflow.** `-f` overwrites your edits wholesale. If you need to customize generated entities, do it in `partial` classes in separate files, or accept scaffolding once and hand-maintaining thereafter.

## Common workflows

**Starting fresh (code-first), single project:**

```sh
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
# ...write your entities and DbContext...
dotnet ef migrations add InitialCreate
dotnet ef database update        # creates the database, then applies the migration
```

**You don't create the database yourself.** `dotnet ef database update` connects to the *server* named in your connection string and issues the `CREATE DATABASE` for you if the database doesn't exist yet — no `CREATE DATABASE` in SSMS or sqlcmd first, no empty database to prepare. It then applies every pending migration and records them in `__EFMigrationsHistory`.

What must already exist is the **server**: a running SQL Server instance or container ([LOCAL_DATABASES.md](LOCAL_DATABASES.md#sql-server)) reachable at the connection string's host and port, with a login that has permission to create databases (`sa` and `dbcreator` both qualify). SQLite is the easy case — the "server" is the file system, so `database update` just writes the `.db` file. If the database already exists, it's left alone and only the pending migrations run.

**The everyday change loop:** edit an entity → `dotnet ef migrations add DescribeTheChange` → **read the generated migration** → `dotnet ef database update`. That middle step is not optional busywork: EF's diff is a guess about intent, and renames in particular usually come out as drop-column + add-column, which silently destroys data. Change it to `migrationBuilder.RenameColumn(...)` by hand when that's what you meant.

**Undoing a migration you haven't shared yet:**

```sh
dotnet ef database update PreviousMigrationName   # roll the database back first
dotnet ef migrations remove                       # then delete the migration files
```

Doing it in the other order leaves the database's `__EFMigrationsHistory` referencing a migration whose code no longer exists. If you've already pushed, don't rewrite — add a new migration that corrects the old one.

**Layered solution, without typing `--project`/`--startup-project` every time:** add an `IDesignTimeDbContextFactory` ([Option C](#option-c-idesigntimedbcontextfactorytcontext) above) to the `DbContext`'s project and run commands from that project's folder. You give up reading real config from the API project, which is usually a fair trade for migrations.

**Keeping CI honest:**

```sh
dotnet ef migrations has-pending-model-changes --project MyProject.Infrastructure --startup-project MyProject.Api
```

Non-zero exit when someone changed an entity without adding a migration. Cheap, and catches the single most common EF mistake in a team.

## Visual Studio Package Manager Console equivalents

If you're following a tutorial written for Visual Studio, it'll use PowerShell cmdlets from `Microsoft.EntityFrameworkCore.Tools`. Same operations, different spelling — and they take the project and startup project from PMC dropdowns rather than flags.

| Package Manager Console                  | CLI equivalent                                     |
| ---------------------------------------- | -------------------------------------------------- |
| `Add-Migration <Name>`                   | `dotnet ef migrations add <Name>`                  |
| `Remove-Migration`                       | `dotnet ef migrations remove`                      |
| `Get-Migration`                          | `dotnet ef migrations list`                        |
| `Script-Migration`                       | `dotnet ef migrations script`                      |
| `Bundle-Migration`                       | `dotnet ef migrations bundle`                      |
| `Update-Database`                        | `dotnet ef database update`                        |
| `Update-Database -Migration <Name>`      | `dotnet ef database update <Name>`                 |
| `Drop-Database`                          | `dotnet ef database drop`                          |
| `Get-DbContext`                          | `dotnet ef dbcontext info`                         |
| `Scaffold-DbContext "<conn>" <Provider>` | `dotnet ef dbcontext scaffold "<conn>" <Provider>` |
| `Optimize-DbContext`                     | `dotnet ef dbcontext optimize`                     |

The CLI works everywhere including inside this template's dev container; PMC only exists in Visual Studio on Windows. Nothing is CLI-exclusive and nothing is PMC-exclusive.

## Applying migrations outside your dev machine

`dotnet ef database update` is a development command — it needs the SDK, the tool, and your source. Three better options for anywhere else:

| Approach                     | How                                                         | When it fits                                                                                   |
| ---------------------------- | ----------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| SQL script                   | `dotnet ef migrations script -i -o migrate.sql`             | A DBA or change-control process has to review and run the SQL. `-i` makes it safe to re-run    |
| Migration bundle             | `dotnet ef migrations bundle --self-contained -r linux-x64` | CI/CD — produces a standalone `efbundle` executable needing neither the SDK nor your source    |
| `context.Database.Migrate()` | Called at app startup                                       | Single-instance apps and local dev only                                                        |

Verified: `dotnet ef migrations bundle` in a class library produces `efbundle.exe` next to the project. It takes `--connection` at run time, so the same artifact promotes across environments.

The startup-migrate approach deserves a warning. It looks tidy and it's what most quickstarts show, but with more than one instance it races — several processes attempting the same schema change at once — and it means every app start holds schema-modification rights on the database. Fine for the dev container; not something to reach for in production.

## Gotchas worth knowing

- **`--no-build` will lie to you.** Verified: immediately after `migrations add`, running `dotnet ef migrations list --no-build` reported *"No migrations were found"* and `has-pending-model-changes` reported *"Changes have been made to the model"* — both wrong, both because the flag reused a stale assembly built before the new migration existed. The same commands without `--no-build` correctly showed `20260912103633_InitialCreate (Pending)` and no pending changes. Use `--no-build` only when you *just* built and nothing has changed since.
- **The model snapshot is a real source file.** `Migrations/<Context>ModelSnapshot.cs` is what `migrations add` diffs against. Never hand-edit it, always commit it, and expect it to be the thing that conflicts when two people add migrations on separate branches. Resolving that conflict properly means taking one side, then regenerating the other person's migration — not merging the snapshot line by line.
- **Migrations are ordered by timestamp, not by merge order.** Two branches that each add a migration will apply in timestamp order once merged, which may not be the order they were reviewed in. If they touch the same table, regenerate the later one.
- **`-i/--idempotent` isn't universal.** Verified: on SQLite it fails outright with *"Generating idempotent scripts for migrations is not currently supported for SQLite."* It works on SQL Server and PostgreSQL. Don't build a deployment pipeline around it without testing it on your actual provider.
- **Cosmos and other non-relational providers don't do migrations.** There's no schema to migrate; use `EnsureCreatedAsync()` instead.
- **`.InMemory` is not a database.** It ignores relational constraints, unique indexes, and transactions, so tests pass against it that would fail in production. Microsoft's own guidance is to use SQLite (file or `:memory:`) for tests that need realistic behaviour.
- **A design-time build is still a build.** Analyzer errors, nullable warnings escalated to errors, or a broken unrelated file in the same project will stop `dotnet ef` with an error that looks nothing like a compiler error. If a command fails confusingly, run `dotnet build` first.
- **The connection string EF uses at design time isn't necessarily your app's.** `dotnet ef dbcontext info` settles it in one command — check there before assuming a migration went to the wrong database by magic.
- **Windows 11 Smart App Control can block `dotnet ef` outright.** SAC only allows apps it considers signed and reputable, and a .NET tool installed from NuGet doesn't qualify — so commands die before EF runs, with a generic "blocked" dialog or an unexplained non-zero exit rather than an EF error. Check under **Windows Security → App & browser control → Smart App Control**, or read the state directly:

  ```powershell
  (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy').VerifiedAndReputablePolicyState
  # 0 = off, 1 = on (enforced), 2 = evaluation mode
  ```

  **Turning SAC off is a one-way door** — Windows does not let you switch it back on afterwards; that requires reinstalling Windows. It's only ever enabled on clean installs, and in evaluation mode (`2`) it often disables itself once it decides it's getting in your way. Before disabling it, consider whether the dev container ([.devcontainer/](../.devcontainer/)) or WSL2 is a better home for this work, since neither is subject to SAC. The same block can hit other unsigned dev tools — a migration `efbundle.exe`, or a portable `sqlite3.exe` from a downloaded zip.

## Where EF Core tooling differs across editors

- **Visual Studio** adds the PMC cmdlets and, with the third-party EF Core Power Tools extension, a genuine GUI for reverse-engineering with checkboxes per table and persisted options — the one EF workflow that's meaningfully nicer outside the CLI, especially against a large legacy database.
- **VS Code** has no EF-specific UI at all; it's `dotnet ef` in the integrated terminal. Nothing is missing functionally — `dbcontext scaffold` does everything Power Tools does, just with flags instead of checkboxes and no saved configuration between runs.
- **The CLI** is the full surface area, and the only option that works in CI, in this template's dev container, or on a build agent.

For everything else — creating the projects that hold your `DbContext`, adding references between layers, and scaffolding controllers on top of your entities — see [DOTNET_TOOLING.md](DOTNET_TOOLING.md), in particular [Scaffolding with `dotnet-aspnet-codegenerator`](DOTNET_TOOLING.md#scaffolding-with-dotnet-aspnet-codegenerator), which is what generates CRUD controllers and Razor pages bound to the `DbContext` you set up here.

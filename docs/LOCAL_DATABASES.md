# Running SQLite and SQL Server locally

- [SQLite](#sqlite)
  - [Installing SQLite](#installing-sqlite)
  - [CLI essentials](#cli-essentials)
  - [Query examples](#query-examples)
  - [Using it from .NET](#using-it-from-net)
  - [SQLite gotchas](#sqlite-gotchas)
- [SQL Server](#sql-server)
  - [Variant A: Windows service](#variant-a-windows-service)
  - [Variant B: Docker container](#variant-b-docker-container)
  - [LocalDB, the lightweight third option](#localdb-the-lightweight-third-option)
  - [Querying with sqlcmd](#querying-with-sqlcmd)
  - [SQL Server gotchas](#sql-server-gotchas)

How to get a database running on your own machine and poke at it. For the EF Core side — migrations, providers, scaffolding — see [EF_CORE.md](EF_CORE.md).

**Which one?** SQLite for tests, prototypes, and anything that should work in the dev container with zero setup. SQL Server when production is SQL Server and you need matching behaviour (T-SQL, real concurrency, stored procedures).

SQLite commands below were run against 3.53.4; SQL Server commands are from Microsoft's docs and the image tags were checked against the MCR registry.

## SQLite

A single file and a library — no server, no service, no port.

### Installing SQLite

The .NET provider embeds its own engine, so **you only need this for the command-line tool**.

```powershell
winget install SQLite.SQLite      # portable zip, puts sqlite3.exe on PATH
```

| Also useful                 | Command                                                    |
| --------------------------- | ---------------------------------------------------------- |
| DB Browser for SQLite (GUI) | `winget install DBBrowserForSQLite.DBBrowserForSQLite`     |
| DBeaver (multi-DB GUI)      | `winget install DBeaver.DBeaver.Community`                 |
| Dev container / Linux       | `sudo apt-get install -y sqlite3`                          |

Verify with `sqlite3 --version`. A database is created the first time you name one — `sqlite3 app.db` — and no file hits disk until you actually write something.

### CLI essentials

Dot-commands are CLI directives, not SQL, so they take no semicolon.

| Command                       | Does                                                         |
| ----------------------------- | ------------------------------------------------------------ |
| `.tables`                     | List tables                                                  |
| `.schema [Table]`             | Show CREATE statements                                       |
| `.mode box` \| `table` \| `json` \| `csv` \| `line` | Output format                          |
| `.headers on`                 | Column headers (implied by `box`/`json`)                     |
| `.once <file>` / `.output <file>` | Send next result / all results to a file                 |
| `.import --csv <file> <Table>`| Load a CSV                                                   |
| `.dump`                       | Whole database as SQL text                                   |
| `.backup <file>`              | Safe copy, even while in use                                 |
| `.read <file.sql>`            | Execute a SQL script                                         |
| `.databases`                  | Attached databases and their paths                           |
| `.quit`                       | Exit                                                         |

Run one-off queries without entering the shell by passing them as arguments — dot-commands work there too:

```sh
sqlite3 app.db "SELECT * FROM Products;"
sqlite3 app.db ".mode box" "SELECT * FROM Products;"
sqlite3 app.db -header -column "SELECT Name FROM Products;"
```

### Query examples

Set up a throwaway database:

```sh
sqlite3 demo.db <<'SQL'
CREATE TABLE Categories (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
CREATE TABLE Products (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Price REAL, CategoryId INT
                       REFERENCES Categories(Id));
INSERT INTO Categories (Name) VALUES ('Tools'), ('Toys');
INSERT INTO Products (Name, Price, CategoryId) VALUES ('Hammer', 12.5, 1), ('Yo-yo', 4.0, 2), ('Saw', 22.0, 1);
SQL
```

`INTEGER PRIMARY KEY` is SQLite's autoincrement — it aliases the internal rowid. Spelling it `INT PRIMARY KEY` does *not* do the same thing.

```sh
# pretty output
sqlite3 demo.db ".mode box" "SELECT * FROM Products;"
```

```text
╭────┬────────┬───────┬────────────╮
│ Id │  Name  │ Price │ CategoryId │
╞════╪════════╪═══════╪════════════╡
│  1 │ Hammer │  12.5 │          1 │
│  2 │ Yo-yo  │   4.0 │          2 │
│  3 │ Saw    │  22.0 │          1 │
╰────┴────────┴───────┴────────────╯
```

```sh
# join + aggregate
sqlite3 demo.db -header -column \
  "SELECT c.Name, COUNT(*) n, ROUND(AVG(p.Price),2) avg
   FROM Products p JOIN Categories c ON c.Id = p.CategoryId
   GROUP BY c.Name;"

# JSON out (handy for piping to jq)
sqlite3 demo.db ".mode json" "SELECT * FROM Products LIMIT 2;"

# export to CSV
sqlite3 demo.db ".mode csv" ".headers on" ".once out.csv" "SELECT * FROM Products;"

# health + housekeeping
sqlite3 demo.db "PRAGMA integrity_check;"    # -> ok
sqlite3 demo.db "VACUUM;"                    # reclaim space after deletes
sqlite3 demo.db ".backup backup.db"
```

**Importing CSV.** `.import` matches columns *positionally*, so a CSV missing the `Id` column fails with `expected 4 columns but found 3`. Either include every column, or import into a staging table — importing into a table that doesn't exist creates it from the header row:

```sh
sqlite3 demo.db ".import --csv partial.csv Staging" \
  "INSERT INTO Products(Name, Price, CategoryId)
   SELECT Name, CAST(Price AS REAL), CAST(CategoryId AS INT) FROM Staging;"
```

Use `--skip 1` when the target table already exists and your file has a header row.

### Using it from .NET

```sh
dotnet add package Microsoft.Data.Sqlite     # ADO.NET only
# or, for EF Core:
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

| Connection string                      | Gives you                                                      |
| -------------------------------------- | -------------------------------------------------------------- |
| `Data Source=app.db`                   | File next to the running app                                   |
| `Data Source=C:\data\app.db`           | Explicit path                                                  |
| `Data Source=:memory:`                 | In-memory, dies with the connection                            |
| `Data Source=app.db;Cache=Shared`      | Shared cache, needed for in-memory across connections          |
| `Data Source=app.db;Mode=ReadOnly`     | Read-only                                                      |

For integration tests, SQLite in-memory is the realistic alternative to EF's `InMemory` provider — it enforces constraints and transactions, which `InMemory` does not. Keep one connection open for the lifetime of the test, or the database vanishes.

### SQLite gotchas

- **Foreign keys are off by default.** `PRAGMA foreign_keys;` returns `0` on a fresh connection; declared `REFERENCES` clauses are parsed but not enforced. Set `PRAGMA foreign_keys=ON` per connection — EF Core's provider does this for you.
- **Types are suggestions.** SQLite uses dynamic typing, so a `TEXT` column will happily store a number. Don't rely on the database to reject bad data.
- **Limited `ALTER TABLE`.** No dropping constraints, no changing column types. EF migrations work around this by rebuilding the table — which is why SQLite migrations look far more dramatic than the same change on SQL Server.
- **Concurrency is coarse.** One writer at a time, database-wide. `PRAGMA journal_mode=WAL;` (persistent, set once) lets readers continue during a write and is worth enabling for anything beyond single-user.
- **Idempotent migration scripts aren't supported** — see [EF_CORE.md](EF_CORE.md#gotchas-worth-knowing).

## SQL Server

Two ways to run it locally. Docker is faster to set up and trivially disposable; the Windows service is what you want if you need SSMS, Windows authentication, or SQL Server Agent.

### Variant A: Windows service

```powershell
winget install Microsoft.SQLServer.2025.Developer   # full-featured, free for dev/test
# or
winget install Microsoft.SQLServer.2025.Express     # lighter, 10 GB database cap
```

Both launch Microsoft's installer UI — pick **Basic** unless you know you need otherwise. Add a GUI client:

```powershell
winget install Microsoft.SQLServerManagementStudio.21    # SSMS
winget install Microsoft.Azure.DataStudio                # cross-platform alternative
```

Substitute `2022` for `2025` in any of the IDs above for the previous major version.

| Instance                | Service name        | Connect with         |
| ----------------------- | ------------------- | -------------------- |
| Default (Developer)     | `MSSQLSERVER`       | `localhost` or `.`   |
| Named (Express default) | `MSSQL$SQLEXPRESS`  | `.\SQLEXPRESS`       |

```powershell
Get-Service MSSQL*                    # check state
Start-Service 'MSSQL$SQLEXPRESS'      # quote it — $ is a PowerShell sigil
Set-Service 'MSSQL$SQLEXPRESS' -StartupType Manual   # stop it eating boot time
```

Connection string, using your Windows account — no password anywhere:

```text
Server=.\SQLEXPRESS;Database=MyApp;Trusted_Connection=True;TrustServerCertificate=True
```

### Variant B: Docker container

```powershell
winget install Docker.DockerDesktop
```

Simplest run, with data in a Docker-managed volume:

```sh
docker run -d --name mssql \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=Your_Strong_Pass123" \
  -e "MSSQL_PID=Developer" \
  -p 1433:1433 \
  -v mssql-data:/var/opt/mssql \
  mcr.microsoft.com/mssql/server:2025-latest
```

**Keeping the data in a folder you choose.** Replace the named volume with a bind mount — left side is your host path, right side is always `/var/opt/mssql`:

```powershell
# PowerShell; create the folder first or Docker creates it as root-owned
New-Item -ItemType Directory -Force C:\mssql\data

docker run -d --name mssql `
  -e "ACCEPT_EULA=Y" `
  -e "MSSQL_SA_PASSWORD=Your_Strong_Pass123" `
  -e "MSSQL_PID=Developer" `
  -p 1433:1433 `
  -v C:\mssql\data:/var/opt/mssql `
  mcr.microsoft.com/mssql/server:2025-latest
```

That folder then holds `data/`, `log/`, and `secrets/` — your `.mdf`/`.ldf` files are in `data/`, readable and backup-able from Windows.

Bind-mounting a Windows path is the fragile part: the container runs as non-root user `mssql` (uid 10001), and the permissions Docker Desktop synthesises for `C:\` paths sometimes aren't enough, giving `Failed to open file ... Operation not permitted` and an immediate exit. Two fixes, in order of reliability:

1. **Put the folder inside WSL2** (`\\wsl$\Ubuntu\home\you\mssql`, or run Docker from a WSL shell against `/home/you/mssql`) — native Linux permissions, no translation layer.
2. **Use a named volume** (the first example) and reach the files via `docker cp` or a backup to a mounted folder when you actually need them on Windows.

| Flag / variable        | Purpose                                                                      |
| ---------------------- | ---------------------------------------------------------------------------- |
| `ACCEPT_EULA=Y`        | Required; container won't start without it                                   |
| `MSSQL_SA_PASSWORD`    | `sa` password. (`SA_PASSWORD` is the deprecated spelling)                    |
| `MSSQL_PID`            | `Developer` (default), `Express`, `Standard`, `Enterprise`                   |
| `-p 1433:1433`         | Host:container port — use `-p 14330:1433` if a local instance owns 1433      |
| `-v <host>:/var/opt/mssql` | Persist data; without it everything dies with the container               |
| `:2025-latest`         | Also `2022-latest`, `2019-latest`. Pin a CU tag for reproducibility          |

Day-to-day:

```sh
docker stop mssql / docker start mssql      # keeps data
docker logs mssql                           # first stop when it won't start
docker rm -f mssql                          # container gone, volume/bind-mount data stays
docker exec -it mssql bash                  # shell inside
```

Connection string:

```text
Server=localhost,1433;Database=MyApp;User Id=sa;Password=Your_Strong_Pass123;TrustServerCertificate=True
```

### LocalDB, the lightweight third option

Installed with SQL Server Express and with Visual Studio's data workload. A per-user instance that starts on demand and needs no service:

```powershell
sqllocaldb info                       # list instances
sqllocaldb start MSSQLLocalDB
```

```text
Server=(localdb)\MSSQLLocalDB;Database=MyApp;Trusted_Connection=True
```

Windows-only, single-user, no Agent — fine for solo development, not for anything shared.

### Querying with sqlcmd

```powershell
winget install Microsoft.Sqlcmd
```

| Flag        | Meaning                                            |
| ----------- | -------------------------------------------------- |
| `-S`        | Server (`localhost,1433`, `.\SQLEXPRESS`)          |
| `-U` / `-P` | SQL login and password                             |
| `-E`        | Windows authentication (default when `-U` omitted) |
| `-d`        | Database                                           |
| `-Q`        | Run a query and exit; `-i` runs a script file      |
| `-C`        | Trust the self-signed certificate                  |
| `-s` / `-W` | Column separator / trim whitespace — for CSV-ish output |

```sh
# create a database
sqlcmd -S localhost,1433 -U sa -P 'Your_Strong_Pass123' -C -Q "CREATE DATABASE MyApp;"

# list databases
sqlcmd -S localhost,1433 -U sa -P 'Your_Strong_Pass123' -C -Q "SELECT name FROM sys.databases;"

# query a table
sqlcmd -S localhost,1433 -U sa -P 'Your_Strong_Pass123' -C -d MyApp -Q "SELECT TOP 10 * FROM Products;"

# run a script
sqlcmd -S .\SQLEXPRESS -E -d MyApp -i seed.sql

# CSV-ish output
sqlcmd -S localhost,1433 -U sa -P 'Your_Strong_Pass123' -C -d MyApp -Q "SELECT * FROM Products;" -s "," -W
```

Inside the container, sqlcmd ships at a fixed path:

```sh
docker exec -it mssql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Your_Strong_Pass123' -C -Q "SELECT @@VERSION;"
```

### SQL Server gotchas

- **`TrustServerCertificate=True` is almost always needed locally.** `Microsoft.Data.SqlClient` 4.0+ defaults to `Encrypt=True`, and a local instance has only a self-signed certificate — without it you get *"A connection was successfully established... but then an error occurred during the login process"* or a certificate chain error. Same thing as sqlcmd's `-C`.
- **Password complexity kills containers silently.** The `sa` password needs 8+ characters from three of: uppercase, lowercase, digits, symbols. Too weak and the container exits seconds after starting — visible only in `docker logs`.
- **Port 1433 collides.** A Windows service instance and a container both want it. Map the container elsewhere (`-p 14330:1433`) and connect to `localhost,14330`.
- **`localhost` vs `.\SQLEXPRESS`.** A named instance isn't reachable as plain `localhost`. Named instances also need the SQL Server Browser service running for name resolution.
- **TCP/IP is disabled by default on Express.** Enable it in SQL Server Configuration Manager, then restart the service, if a non-local client can't connect.
- **From inside the dev container**, the host's SQL Server is `host.docker.internal`, not `localhost` — `localhost` is the dev container itself.
- **Comma, not colon**, separates host and port in SQL Server connection strings: `localhost,1433`.

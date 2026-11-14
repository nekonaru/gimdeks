<div align="center">
  <img src="assets/gimdeks-mark.png" width="120" alt="GimDeks logo" />
  <h1>GimDeks: Game Collection Manager for Excel Power Users</h1>
  <p><b>Track a game collection across every drive with automatic capacity totals,<br/>storage limits, and one-click Excel import and export. Fully offline.</b></p>
  <a href="https://github.com/nekonaru/gimdeks/releases/latest"><img src="https://img.shields.io/github/v/release/nekonaru/gimdeks?style=flat-square" alt="Release" /></a>
  <a href="https://github.com/nekonaru/gimdeks/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/nekonaru/gimdeks/ci.yml?branch=main&style=flat-square" alt="CI" /></a>
  <a href="https://github.com/nekonaru/gimdeks/releases/latest"><img src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square" alt=".NET" /></a>
  <a href="https://github.com/nekonaru/gimdeks/releases/latest"><img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D6?style=flat-square" alt="Platform" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green?style=flat-square" alt="License" /></a>
</div>

<br />

**English** | [Bahasa Indonesia](README.id.md)

> [!NOTE]
> GimDeks is built for one person managing a personal game collection on a Windows PC. It replaces the Koleksi_Game.xlsx spreadsheet with an app that reads and writes the same file, and it never sends data anywhere.

---

## Table of Contents

- [About](#about)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
- [Data Sources](#data-sources)
- [Project Structure](#project-structure)
- [Development](#development)
- [Testing](#testing)
- [Continuous Integration](#continuous-integration)
- [FAQ](#faq)
- [Author](#author)
- [License](#license)

---

## About

GimDeks manages a game collection across multiple drives (external HDDs, laptop HDDs, and a laptop SSD) and recalculates every total automatically. Your old Excel file (Koleksi_Game.xlsx) imports in one click with a full preview, and exports back out with the same layout and live total formulas.

> _"Your game shelf, without the spreadsheet."_

---

## Features

### Importing and exporting Excel

| Feature | Description |
| --- | --- |
| **One-click import** | Reads the Koleksi_Game.xlsx layout (all sheets, both number decimal cultures) in under a second. |
| **Import preview** | Shows detected storages, games, skipped sheets, and warnings before anything is stored. |
| **Two import modes** | `Gabung` (merge, skips duplicates) or `Ganti Semua` (replace everything, with automatic backup first). |
| **Ringkasan verification** | After import, every per-storage count and GB total is compared against the Ringkasan sheet and differences are shown. |
| **Excel export** | Writes the same layout back out: one sheet per storage with a live formula row, plus Ringkasan, Dikecualikan and Game Dieliminasi. |
| **Roundtrip safe** | Export, then re-import, yields the same data. Covered by a test. |

### Managing the collection

| Feature | Description |
| --- | --- |
| **Dashboard** | One card per storage: game count, GB used, remaining space, progress bar, and a badge when the storage is over its limit. |
| **Game list** | Sortable, searchable, filterable table (status, storage, size range) with virtualization for thousands of games. |
| **CRUD with multi-select** | Add, edit, delete, move between storages, and change status in bulk with confirmation. |
| **Storage management** | Add and edit storages: capacity, per-drive limit (default 900 GB for 1 TB external HDDs), and type. |
| **Over-limit warnings** | Any storage whose counted total passes its limit shows a warning badge. Exactly at the limit is fine. |
| **Excluded and eliminated** | Dedicated screens for online/mobile/free games and eliminated games, with restore to a storage. |
| **Duplicate detection** | Finds duplicate games (case, punctuation and version numbers ignored) and resolves them per group. |
| **Version split** | One action splits a trailing version like `v1.7` out of the game name into the Versi column. |
| **HDD distribution suggestion** | Proposes a download order that packs Belum Download games into your HDDs without breaking the limit. |
| **History** | Every move between storages is recorded and shown per storage. |

### App experience

| Feature | Description |
| --- | --- |
| **Fully offline** | Single SQLite file in `%LOCALAPPDATA%\GimDeks`. No account, no telemetry, no network calls. |
| **Backup and restore** | One-click database backup (or automatic before import) and restore from a `.db` copy. |
| **Dark and light theme** | Toggle in the sidebar; your choice is remembered. |
| **Indonesian UI** | All labels and messages are in Bahasa Indonesia. Numbers use id-ID formatting on screen and invariant storage internally. |
| **Safe by default** | No change is overwritten without a backup and confirmation; deletes always ask first. |

---

## Requirements

### To run (prebuilt)

| Dependency | Minimum Version | Notes |
| --- | --- | --- |
| **Windows** | 10 version 1809 or later, or Windows 11 | x64 |
| **.NET runtime** | None | The release exe is self-contained |
| **Koleksi_Game.xlsx** | Any Koleksi_Game.xlsx in the original layout | Only needed for importing |

> [!TIP]
> Users who do not want to build from source can download `GimDeks-win-x64.zip` from the [latest release](https://github.com/nekonaru/gimdeks/releases/latest).

### Never used a terminal? Start here

<details><summary><b>Open a terminal on Windows</b></summary>

1. Press `Win + R`.
2. Type `powershell` and press Enter.
3. A blue window opens. You can now type commands.
4. Close it with `exit` or the X button.

</details>

### To build from source

| Dependency | Minimum Version | Notes |
| --- | --- | --- |
| **Windows** | 10/11 | Required for WPF |
| **.NET SDK** | 10.0 | Builds the `net8.0` projects |
| **Git** | 2.x | To clone the repo |

---

## Installation

### Option A: Download the executable (easiest)

1. Go to the [latest release](https://github.com/nekonaru/gimdeks/releases/latest).
2. Download `GimDeks-win-x64.zip` and extract it.
3. Run `GimDeks.exe`. No installation wizard needed.
4. Your data lives in `%LOCALAPPDATA%\GimDeks`. Copy the folder to back up everything manually.

> [!IMPORTANT]
> Windows SmartScreen may warn about an unsigned binary. GimDeks is open source; you can inspect the code on GitHub and build it yourself instead (Option B).

### Option B: Build from source (for development / to read or change the code)

#### 1. Get the code

<details><summary><b>I have Git installed</b></summary>

```bash
git clone https://github.com/nekonaru/gimdeks.git
cd gimdeks
```

</details>

<details><summary><b>I don't have Git</b></summary>

1. Open https://github.com/nekonaru/gimdeks in your browser.
2. Click `Code` then `Download ZIP`.
3. Extract the ZIP and open the folder.

</details>

#### 2. Build and verify

> [!IMPORTANT]
> The solution builds against .NET 8 while the build tool is the .NET 10 SDK. Restore must finish without errors.

```bash
dotnet restore GimDeks.slnx
dotnet test GimDeks.slnx
```

#### 3. Build and run the app

```bash
dotnet build src/GimDeks.App -c Release
dotnet run --project src/GimDeks.App
```

---

## Usage

### First launch

1. Open GimDeks. You land on the Dashboard (empty at first).
2. Click `Import / Export` in the sidebar.
3. Click `Pilih File...` and choose your `Koleksi_Game.xlsx`.
4. Click `Lihat Pratinjau` and check the summary (10 storages, 169 games, verification rows marked Cocok).
5. Click `Impor Sekarang`. A backup is made automatically first.
6. Go back to `Dashboard`. All storages and totals are now there.

> [!NOTE]
> Importing with `Ganti Semua` erases the current collection before importing. Importing with `Gabung` keeps existing games and skips duplicates. Backup happens automatically before any import.

### Everyday use

- **Dashboard**: click any storage card to open its game list.
- **Daftar Game**: search, filter by status/storage/size, add, edit, delete, move, or change status of games, including bulk selection.
- **Storage**: manage capacities and limits, see per-storage stats and move history, or move many games at once.
- **Dikecualikan / Dieliminasi**: keep the online and removed games out of capacity math; restore them into a storage when they come back.
- **Duplikat**: find near-identical games and delete or mark them eliminated per group.
- **Saran HDD**: get a size-descending packing plan that keeps every HDD under its limit.
- **Export**: write the collection back to `.xlsx` with the original layout and live formulas.

---

## Data Sources

| Purpose | Source |
| --- | --- |
| Game collection data | Your `Koleksi_Game.xlsx` (local file, imported by you) |
| App state | SQLite database at `%LOCALAPPDATA%\GimDeks\gimdeks.db` |
| Backups | `%LOCALAPPDATA%\GimDeks\backups\gimdeks-backup-*.db` |
| Settings | `%LOCALAPPDATA%\GimDeks\settings.json` (window size, theme) |

> [!NOTE]
> GimDeks never reads from the network. The only file it parses from outside its own folder is the `.xlsx` you pick in the import dialog.

---

## Project Structure

```
gimdeks/
  GimDeks.slnx                    # solution
  assets/                         # brand assets (mark, icon, lockups)
  src/
    GimDeks.App/                  # WPF app (Views, ViewModels, DI)
    GimDeks.Core/                 # models, rules, interfaces (no WPF, no SQLite)
    GimDeks.Data/                 # SQLite, repositories, migrations, backup
    GimDeks.Excel/                # Excel importer/exporter
  tests/
    GimDeks.Tests/                # xUnit tests (import, capacity rules, roundtrip)
  .github/workflows/              # CI and release pipelines
  README.md                       # English documentation (this file)
  README.id.md                    # Bahasa Indonesia translation
```

---

## Development

### Adding a user-facing string

1. Add or reuse the key in the view model.
2. Write the Indonesian label next to it (UI language is fixed to Bahasa Indonesia; the README stays in both languages).
3. Do not put logic in code-behind files; use the view model.

### Changing the schema

1. Append a new migration to `src/GimDeks.Data/DatabaseInitializer.cs`; never edit an existing one.
2. Bump the `PRAGMA user_version` target accordingly and add tests.

### Changing the import/export layout

1. Read `src/GimDeks.Excel/ExcelImporter.cs` and `ExcelExporter.cs` first.
2. Keep the "read by pattern, not by position" rule.
3. Update the roundtrip test if semantics change.

> [!WARNING]
> Generated files (`.g.cs`, `.baml`) under `obj/` are rebuilt automatically. Never edit them by hand.

---

## Testing

Short intro: tests are xUnit and live in `tests/GimDeks.Tests`. They cover capacity math (including the exact-900-GB edge), decimal parsing on invariant and id-ID cultures, the importer (with an in-memory workbook), export roundtrip, and the schema migrations.

```bash
dotnet test GimDeks.slnx
```

| Check | Requirement |
| --- | --- |
| Unit tests | All pass |
| Analyzer / linter | Zero errors |
| Build (Release) | Zero errors |
| Coverage gate in CI | At least 60% line coverage on Core/Data/Excel |

Lint configuration is the default C# recommended set; the solution builds warning-free.

---

## Continuous Integration

| Workflow | Purpose |
| --- | --- |
| `.github/workflows/ci.yml` | Restore, Release build, tests with coverage, dependency audit, secret scan |
| `.github/workflows/release.yml` | On `v*` tags: test, publish self-contained single-file `win-x64`, package zip, generate release notes from changelogs, attach to GitHub Release |

The default branch is protected: CI must pass and the owner merges with 0 required approvals (single-maintainer repo). Releases are cut from green tags only.

---

## FAQ

<details><summary><b>Where is my data stored?</b></summary>

```
%LOCALAPPDATA%\GimDeks\gimdeks.db
%LOCALAPPDATA%\GimDeks\settings.json
%LOCALAPPDATA%\GimDeks\backups\
```

Copy the whole folder to move everything to another PC.

</details>

<details><summary><b>Will it mess up my Excel file?</b></summary>

No. Import reads the file; export writes a new file wherever you choose to save it. Your original is never edited.

</details>

<details><summary><b>Can I import my existing Koleksi_Game.xlsx?</b></summary>

Yes. Use the original file layout. The importer detects sheets by content and reports anything it skipped on the preview screen.

</details>

<details><summary><b>Numbers show as 32,2 on screen but 32.2 in the file. Is that a bug?</b></summary>

No. On screen, numbers use Indonesian formatting. In the file and the database, invariant culture is used. Parsing handles both.

</details>

<details><summary><b>Why does it need a WebView/free fonts/network?</b></summary>

It doesn't. GimDeks makes no network connections. Everything runs on your machine.

</details>

<details><summary><b>Is GimDeks on the Windows Store?</b></summary>

Not yet. Download the release zip from GitHub instead.

</details>

---

## Author

<div align="center">

| [![Nicolas Dwi Dharma](https://github.com/nekonaru.png?size=100)](https://github.com/nekonaru) |
|:---:|
| **[Nicolas Dwi Dharma](https://github.com/nekonaru)** |

</div>

---

## License

Distributed under the **MIT License**. See [`LICENSE`](LICENSE) for more information.

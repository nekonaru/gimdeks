# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/).

## v1.6.1

Existing features run lighter. Cover thumbnails, filtering, import, and moves no longer repeat work that only needs to happen once.

## Improvements

- **Cover lookup.** The covers folder is indexed once in memory, so rendering or scrolling the game list no longer scans it for every row.
- **Filtering.** The tag map loads once per refresh instead of once per search keystroke.
- **Dashboard.** Games are grouped once and ready drives are checked with one enumeration instead of one per storage.
- **Import.** A preview is reused for the same file and timestamp. Duplicate detection loads existing names once instead of querying once per game. Formula recalculation runs once per preview instead of once per cell.
- **Moving games.** History and storage updates are written in two statements instead of three queries per game.

## Bug fixes

- **Ringkasan verification.** A sheet with no verifiable rows no longer reports a successful check, and formula recalculation failures appear as warnings.
- **Schema upgrade backups.** Pre-upgrade database copies are removed only after migrations commit, so they no longer accumulate invisibly.

## v1.6.0

Pick a game in one click when you cannot decide what to play next.

## New features

- **Random pick.** A dashboard card picks one of your downloaded, unfinished games at random, opens the game list with every filter cleared, shows the choice in the status line, and selects and scrolls to the row.

## Bug fixes

- **Game list selection on navigation** is now re-applied when a page is created or reloaded, so programmatic navigation (from the dashboard) leaves the intended row highlighted.

## v1.5.0

Context menu on game list rows, and a one-click bulk download for covers that are missing.

## New features

- **Right-click actions on rows.** Edit, move, change status, split version, mark played through, download cover, or delete, all from a context menu on any game row.
- **Download all missing covers.** One button finds every game without a local cover and fetches Steam art in a batch, with confirmation.
- **Layout tweaks.** The games list gets a wider Unduh Cover side of the toolbar separated from the selected-row actions.

## v1.4.0

Visual refresh across the app: darker gradient surfaces, glow-accented cards and a transformed sidebar, plus a few refinements under the hood.

## New features

- **Focused secondary accent.** The active navigation item carries a teal accent bar and a soft teal pill background; hover states glow subtly.
- **Dashboard first impression.** The first-run empty state now shows the GimDeks wordmark centered over the prompt to import your collection.
- **Selection emphasis.** Storage cards on the dashboard open their game list on click, with the whole card as the hit target.

## Improvements

- **Visual refresh.** Sidebar moved to a darker gradient palette; cards carry gradient fills with a single-pixel accent line; the type hierarchy is tighter; titles render in semi-bold at 30px.
- **Asset packaging.** All brand assets (lockups, marks, icons, screenshots) now ship inside the release artifact.

## v1.3.0

Selection survives every refresh now, the schema upgrade keeps a safety backup, and the catalog exports to CSV.

## New features

- **Selection persists across refreshes.** Delete, move, change status or mark played: the rows you still have selected stay selected after the reload, in the list and for every bulk action on them.
- **Pre-migration backup.** When a schema upgrade is pending, the app keeps a `gimdeks-pre-upgrade-<timestamp>.db` copy of your database in the backups folder before applying migrations.
- **CSV export.** The whole catalog exports to a semicolon-separated UTF-8 CSV with a BOM, so both locales of Excel open it directly. Same data as the Excel export, with the storage name and tag list per game.

## Bug fixes

- **Mark played through dropped the selection.** The grid rebuild after a reload replaced the row objects, so the selection was cleared right after the toggle; bulk actions silently targeted nothing. The selection now survives.
- **Concurrent filter rebuild race.** A filter change triggered twice per reload could restore only part of the selection (one row went missing). All rebuild paths are serialized.
- **Storage list items have accessible names.**

## v1.2.0

Interaction overhaul: sidebar tabs navigate again, the app remembers where you left off, and Koleksi_Game.xlsx imports by dropping it onto the window.

## New features

- **Drag and drop import.** Drop Koleksi_Game.xlsx anywhere on the window: the path is pre-filled and the preview opens automatically.
- **Double-click to edit.** Double-click a game row to open the editor.
- **Mark played through in bulk.** Select several games and toggle the played-through flag for all of them at once.
- **Remembers where you left off.** The app reopens on the last open page and restores the window position and size (with off-screen clamping).
- **Accessible names.** Navigation items and icon-only buttons are exposed to screen readers and Windows UI Automation.

## Improvements

- **Keyboard-friendly dashboard cards.** Storage cards are real buttons with keyboard activation and an accessible name.
- **Window title and version line.** The title bar shows the current page, and the sidebar version line comes from the assembly instead of a hardcoded string.
- **Silent failures are visible.** A failed page refresh logs to gimdeks.log and shows in the status bar instead of doing nothing.
- **Crash log redacts paths.** Drive-letter paths are removed from the crash log.
- **Cover art cached.** Covers are cached in memory, so scrolling the list is smoother.

## Bug fixes

- **Sidebar navigation.** Sidebar tabs now switch pages. In v1.0.0 and v1.1.0 clicking the tabs never navigated (a binding mode issue in the navigation view model); only the Ctrl+1..9 shortcuts worked.
- **Toolbar actions without a selection** are disabled until a selection exists, instead of silently swallowing clicks.
- **Settings on window close** no longer discard the language, the theme and the imported file path.
- **The Excel file watcher** is stopped when the window closes and is correctly disposed when the language switches.

## v1.1.0

GimDeks now speaks English as well as Bahasa Indonesia, asks before every online action, and is harder to corrupt: safer restores, safer portable mode, a clearer release gate.

## New features

- **Bilingual UI.** Bahasa Indonesia stays the default; switch the whole app to English from the sidebar button, and the choice is remembered. Numbers follow the active language on screen.
- **Statistics page.** Collection totals, per-storage share, 10 biggest games, average size, and completion progress.
- **Tags and completion.** Custom comma-separated tags and a played-through flag per game, both filterable; completion stats on the statistics page.
- **Steam cover art (opt-in).** One click searches Steam for the game and downloads its header art as the cover. The game title is the only data sent, and nothing happens without an explicit click.
- **Portable mode.** One toggle moves the data folder next to the app; the database and settings travel with an external drive.
- **Drive linking.** Link a storage to a drive letter; the dashboard shows a badge when the drive is not connected.
- **Import watcher.** A banner offers to re-import when the imported Excel file changes on disk.
- **Keyboard shortcuts.** Ctrl+1 to Ctrl+9 switch pages, Ctrl+F searches, Ctrl+N adds a game, Ctrl+E exports, Ctrl+T toggles the theme.

## Improvements

- **UI polish.** Fade-in page transitions, a darker sidebar with an accent bar on the active item, and a status bar with the shortcut hint.
- **Number and date formatting** now follows the active UI language everywhere, including the storage screen.
- **Restore is atomic.** The database is copied to a temp file and swapped in place, so a failed restore never leaves a half-written file behind.
- **Portable mode off is safer.** The existing profile copy is kept as a timestamped backup (restored automatically on failure) and the copied file count and byte size are verified before the source is removed.
- **Excel import guards.** Decompression budget (256 MB) before parsing; malformed archives are rejected before expansion.
- **CI and the release workflow** now fail on any vulnerable package, parsed from the machine-readable report instead of a text match, and the release workflow runs the same audit before packaging.

## Bug fixes

- **Settings on window close** no longer wipe the language, the theme and the imported-file path.
- **The dashboard refreshes on first launch** instead of showing an empty state until the first navigation.
- **Navigation labels render correctly** after a language switch.
- **The file watcher** no longer risks crashing the app on shutdown.

## v1.0.0

The first public release. GimDeks replaces the Koleksi_Game.xlsx spreadsheet with an offline Windows app that imports the same file, tracks capacities automatically, and exports it back out.

## New features

- **Excel import and export roundtrip.** Reads the full Koleksi_Game.xlsx layout by content pattern, and exports it back with the same layout and live total formulas.
- **Import preview with Ringkasan verification.** Shows detected storages, games, skipped sheets, and compares results against the Ringkasan sheet before saving.
- **Automatic capacity totals.** Per-storage game counts and GB, remaining space, and a progress bar toward the storage limit, with a warning badge over the limit (default 900 GB for 1 TB external HDDs).
- **Dashboard and game list.** Card-per-storage dashboard and a virtualized, searchable, filterable game table with add, edit, delete, move and status change, including bulk selection.
- **Storage management.** Add and edit storages (capacity, limit, type), see per-storage stats and move history.
- **Dikecualikan and Dieliminasi.** Dedicated screens with restore to a storage; excluded games never count toward totals.
- **Duplicate detection.** Finds duplicate games (case, punctuation and version numbers ignored) and deletes or marks them eliminated per group.
- **HDD distribution suggestion.** Packs Belum Download games into your HDDs under the per-drive limit, largest first.
- **Version split.** One action moves a trailing version like `v1.7` into the Versi column.
- **Automatic backups.** A database backup is made before every import, plus one-click backup and restore.
- **Dark and light theme.** Toggle in the sidebar; the choice is remembered.
- **Offline and private.** Single SQLite file in `%LOCALAPPDATA%\GimDeks`. No account, telemetry, or network calls.

## Improvements

- **Import performance.** The original Excel file imports in under a second.

## Bug fixes

- None (first release).

# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/).

## v2.2.0

Import CSV files back into GimDeks and see how capacity changes over time.

## New features

- **CSV import.** The semicolon CSV exported by GimDeks can be imported again. Storage names and tags are resolved, duplicates are skipped with the same name normalizer, and a backup is created first.
- **Capacity trend.** The dashboard records one snapshot per storage per day and shows how total size changed since the last snapshot.

## v2.1.0

Keep the collection organized with screenshot galleries, backup management, advanced search, and saved filters.

## New features

- **Screenshot gallery.** Add local images to a game from its detail window, browse thumbnails, and remove them. Files are stored under the local covers folder and never uploaded.
- **Backup Manager.** A dedicated page lists local backups with date, size, and a preview of storage and game counts. Create a backup, open the folder, or restore with confirmation and an automatic safety copy.
- **Advanced search.** Search by name with regular expressions, by dated notes, and by size range from one dialog.
- **Saved filters.** Save the current filter set under a name and reapply it from the game list.
- **Import history.** Every successful import records its source, mode, and result. The history is shown on the Import / Export page.
- **Storage shortcuts.** Refresh connected drives and open the linked drive in Explorer from the Storage page.

## Improvements

- **Note-aware filtering.** Advanced note search covers both the Excel compatible Notes field and dated notes added in the detail window.

## v2.0.0

Track how long you play, mark favorites, and find any game or action instantly. This is the first major update with a new schema version.

## New features

- **Play sessions.** Launching a game starts a session that ends when the process exits. Duration, start time, and source are stored. Open sessions are closed safely when the app exits. The game detail panel shows the session list and total time.
- **Favorites, rating, and priority.** Each game can be a favorite, have a 0 to 5 rating, and carry a download priority. The game list has FAV and RATE columns, plus sorting by favorites, rating, priority, and time played.
- **Game detail panel.** A dedicated window shows the cover, notes, play sessions, and direct actions for launch and opening the local game folder.
- **Dated notes.** Store several notes per game with timestamps, separate from the Excel compatible Notes field.
- **Quick search.** Ctrl+P opens a command palette that searches all games and common actions, with keyboard navigation.
- **Local game folder.** Set a folder per game and open it in Explorer from the detail panel. The path stays local and is never exported.

## Improvements

- **Schema version 4.** Adds the extended tables and columns with the existing automatic pre-upgrade backup.
- **Stable game editing.** Editing a game now preserves play count, total time, and timestamps instead of resetting them.
- **Drive link feedback.** Failed drive link writes are logged and reported instead of silently claiming success.
- **Storage selection.** Stale async loads are discarded with a selection token, so fast clicks cannot show the previous storage's data.
- **Import and export guards.** Long-running commands cannot overlap while the page is busy.
- **Excel eliminated games.** Asal HDD and Alasan now use two columns so a reason string never becomes a fake source storage.

## Bug fixes

- **Play time preserved.** Launch no longer overwrites extended game fields, and session tracking replaces the old count-only launch path.
- **Dikecualikan detection.** A sheet with an Alasan column and an Asal column is no longer misclassified as Dikecualikan.

## v1.9.0

Scan a local drive for installed games and link executables to your collection, with a wide set of data and interaction fixes from a fresh audit.

## New features

- **Installed game scanner.** A new Scan Games page reads `.exe` files from a chosen drive or folder, suggests links for games without a launch path, and lets you apply the ones you confirm. Exact name matches are marked Exact, likely matches Likely. Scanning only reads files; applying only stores a local path. It never launches a process.

## Improvements

- **Stable import mode.** Import modes are keyed by a stable enum value instead of a localized label, so switching language can no longer silently downgrade Replace All to Merge.
- **Reliable deep links.** Dashboard storage cards, the random pick, and Health Check findings now queue their target and apply it after the page reloads. A fresh app no longer shows all games when you click a storage card.
- **Serialized reloads.** Game list and storage reloads are serialized, so overlapping refreshes cannot restore stale filters or selections. Clearing filters triggers one rebuild instead of seven.
- **Honest cover results.** Failed cover downloads are counted, logged, and shown in the status instead of being silently ignored.
- **Unique backups.** Backup file names include a unique suffix, so two backups in the same second cannot overwrite each other.

## Bug fixes

- **Restore safety.** Pruning can no longer delete the backup currently being restored.
- **Ringkasan comparison.** Sheets that normalize to the same name are aggregated instead of throwing, and the longest matching label wins so HDD 10 does not inherit HDD 1 totals.
- **Excel import.** Duplicate detection now uses the app's game name normalizer in both import modes. Storage sheet names are matched case-insensitively and allow documented suffixes. A real game named TOTAL no longer truncates a sheet. Ringkasan counts are clamped instead of overflowing.
- **Excel roundtrip.** Drive capacity keeps two decimals, so a 930 GB drive no longer comes back as 900 GB.
- **Preview cache.** The cache includes a file hash and is cleared after a successful import, so a rewritten file cannot yield stale data.
- **Data consistency.** No-op storage moves no longer write history rows, and tag creation is atomic under concurrent calls.
- **Interaction.** Launch is guarded against double clicks, status list selection clears on reload, and drag-and-drop import records the watched path. Remaining hardcoded English labels now use the active language.

## v1.8.0

Check the health of your collection at a glance, and sort games by how often and how recently you played them.

## New features

- **Health Check page.** A new sidebar page scans for over-limit storages, disconnected drives, duplicate groups, zero-size games, games with an inconsistent status, missing or stale backups, and a missing imported Excel source. Each finding has an Open button that navigates straight to the relevant page and selects the item.
- **Play-based sorting.** The game list can now sort by biggest size, most played, recently played, or status, alongside the default alphabetical order.

## Improvements

- **Actionable findings.** Health issues link into Storage, Duplicates, Game List, and Import / Export, so a scan leads directly to a fix.
- **Offline only.** All health checks read the local SQLite file and local drives. No network call is added.

## v1.7.0

Launch installed games directly from the collection, and keep a simple play log.

## New features

- **Local game launcher.** Set a game's `.exe` path in its editor, then press Launch from the toolbar, row context menu, or right-click menu. Only a local `.exe` the user chose is started, with no arguments and no shell.
- **Play log.** Each successful launch increments a play count and records the time. The game list shows a PLAYED column, with "Never played" until the first launch.

## Improvements

- **Executable picker.** The game editor has a Browse button restricted to `.exe` files.
- **Schema migration.** Version 3 adds the launch path and play log columns with the existing automatic pre-upgrade backup.

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

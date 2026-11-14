# Changelog

All notable changes to this project are documented here. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/).

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

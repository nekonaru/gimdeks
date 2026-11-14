# Security Policy

## Reporting a vulnerability

GimDeks is a fully offline desktop app with no accounts, no network calls, and no server components. Even so, if you find a security problem, please report it privately:

- Use [GitHub Security Advisories](https://github.com/nekonaru/gimdeks/security/advisories/new) on this repository.

Do not open a public issue for security problems. Include steps to reproduce, the affected version, and the potential impact. You will get a response within a reasonable time.

## Scope

- The GimDeks Windows app and its installer/release artifacts.
- The import pipeline (handling of .xlsx files you choose to import).
- The local SQLite database and backups on your machine.

## What is out of scope

- Third-party services (GitHub itself, Windows, SmartScreen). Report those to their owners.
- Issues in .xlsx files you author yourself with macros or formulas. GimDeks never executes macros or formulas as code.

## What this project does about security

- No network access, telemetry, or analytics of any kind.
- A database backup is created automatically before every import and before every restore.
- Dependencies are audited in CI, and secrets are scanned before merge.
- The repository has protected branches; releases ship only from green tags.

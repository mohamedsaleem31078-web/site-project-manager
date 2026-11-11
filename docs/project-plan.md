# Site Project Manager

This repository contains the initial foundation for a project management application designed for site snag tracking, reports, attachments, and client letters.

## Goals

- Manage projects and site-related work
- Track snag/defect records
- Attach photos and PDF files
- Add mark-ups to images
- Generate project reports
- Write formal letters for clients
- Support local backup and restore
- Optional AI writing assistance when internet is available

## Tech stack

- ASP.NET Core 10
- SQLite database
- Swagger for API testing
- Future-ready structure for WPF desktop packaging

## Run locally

1. Install .NET 10 SDK.
2. Open a terminal in the `src/SiteProjectManager` folder.
3. Run:

```bash
dotnet restore
dotnet run
```

4. Open Swagger in the browser at:

```text
https://localhost:5001/swagger
```

or:

```text
http://localhost:5000/swagger
```

## Project structure

- `src/SiteProjectManager` — application source
- `docs/` — planning and requirements documents

## Current status

This is the initial working foundation. The next phases will add:

- richer project dashboard
- better UI screens
- image annotation tools
- report/export features
- client letter templates
- backup and restore workflows
- AI writing support

## Important note

The GitHub pull requests inbox is not the project itself. This repository is the implementation workspace for the application.


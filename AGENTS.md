# Repository Guidelines

## Project Structure & Module Organization

This repository contains a Windows Forms .NET Framework 4.7.2 application in `OSRCGG/` and a console test harness in `OSRCGG.Tests/`. The main form is split into partials such as `AcksToolkitForm.Map*.cs`, `AcksToolkitForm.Characters.cs`, and `AcksToolkitForm.Dungeons.cs`. Domain logic lives under `Services/`, data contracts under `Models/`, generation code under `Generator/`, persistence and workbook code under `Infrastructure/`, reusable UI under `UI/`, and map images/fonts under `MapAssets/`. Architecture notes are in `OSRCGG/Documentation/Architecture/`. Local agent memory in `.agents/` is intentional project context; do not delete it.

## Build, Test, and Development Commands

Use Visual Studio MSBuild, not `dotnet build`, because the app has a `WMPLib` COM reference.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' OSRCGG.sln /restore /p:Configuration=Debug /p:Platform="Any CPU"
OSRCGG.Tests\bin\Debug\OSRCGG.Tests.exe
OSRCGG.Tests\bin\Debug\OSRCGG.Tests.exe --list
OSRCGG.Tests\bin\Debug\OSRCGG.Tests.exe character map
```

`/restore` refreshes PackageReference assets such as ClosedXML. The test executable accepts keys or tags, for example `character`, `map`, `dungeon`, `excel`, or `all`.

## Coding Style & Naming Conventions

Follow the existing C# style: 4-space indentation, PascalCase for types and methods, camelCase for locals and private fields, and partial form files grouped by feature. Keep non-trivial logic comments in Russian, matching current project notes. New user-visible strings should support both Russian and English paths when the surrounding UI is localized.

## Testing Guidelines

Tests are plain C# assertions in `OSRCGG.Tests/Program.cs`. Add focused assertions near the affected subsystem and prefer deterministic seeds for generators. Run the full harness before handoff when changes touch shared models, map generation/rendering, workbook import/export, or character/dungeon services.

## Commit & Pull Request Guidelines

Git history is short and does not enforce a strict message format. Use concise imperative commit subjects, for example `Optimize map viewport rendering` or `Fix NPC level generation`. PRs should describe the behavior change, list tests run, mention generated files intentionally excluded, and include screenshots for visible WinForms UI changes.

## Security & Cleanup Notes

Do not commit `bin/`, `obj/`, `.vs/`, `.codex_tmp/`, rendered documentation caches, or private/reference material under `pdfs/` and root scratch `.txt` files. Treat `.agents/` as project memory, not disposable output.

# Workout Catalog

A .NET 8 **WPF** desktop app for cataloguing, browsing, rating, and playing a personal library of
exercise videos — with an embedded video player, playlists, watch history, and a companion console
tool for batch processing.

> A personal project built to turn a large local video collection into a searchable, ratable catalogue.

<!-- Add a screenshot here, e.g. ![Workout Catalog](docs/screenshot.png) -->

## Highlights

- **MVVM WPF** (CommunityToolkit.Mvvm) with a hand-rolled dark theme — no third-party UI kit
- **EF Core 8 + SQLite** catalogue: exercises, comments, ratings, playlists, and view history
- **Embedded video playback** via LibVLCSharp — play/preview, seek, volume, fullscreen
- **FFmpeg / FFprobe** integration for probing videos and auto-generating thumbnails
- **Folder scanning** that turns a directory tree of videos into a catalogue
- A **console batch tool** for scan / merge / cleanup, each supporting `--dry-run`

## Solution structure

| Project | Description |
| --- | --- |
| **WorkoutCatalog** | The main WPF catalogue app (MVVM, EF Core/SQLite, LibVLCSharp, dark theme) |
| **WorkoutCatalog.Crawler** | Console tool: batch scan, merge video/audio streams via FFmpeg, recycle-bin cleanup |
| **VideoAudioMerger** | An earlier standalone WPF tool for merging video + audio MP4 pairs |

See [`Spec.md`](Spec.md) for the full feature spec and data model.

## Features

- Search by keyword; filter by type (Stretching / Strength / Core), level, and minimum rating
- Auto-generated thumbnails, 1–5 star ratings, and per-exercise comments
- Playlists (many-to-many), a Top 10 view, and automatic watch history
- Embedded player with transport controls and fullscreen
- Folder scan classifies videos (full vs preview) and upserts to SQLite keyed on folder path

## Tech stack

.NET 8 · WPF · MVVM (CommunityToolkit.Mvvm) · EF Core 8 (SQLite) · LibVLCSharp · FFmpeg / FFprobe

## Building

```bash
dotnet build VideoAudioMerger.sln -c Release
```

Requires the .NET 8 SDK (Windows). At runtime the app needs FFmpeg/FFprobe available and a
video-root folder configured on first launch. Video files and the local SQLite database live
outside the repo (under `%APPDATA%`), so no personal data ships with the code.

## License

[MIT](LICENSE)

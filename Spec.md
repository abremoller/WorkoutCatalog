# Workout Video Catalog

## Overview

A personal workout video catalog application for browsing, filtering, rating, and playing exercise videos organized in folders. Includes an embedded video player, playlist management, and a console batch-processing tool.

## Solution Structure

```
VideoAudioMerger.sln
  ├── VideoAudioMerger/           Existing WPF tool for merging video+audio MP4 pairs
  ├── WorkoutCatalog/             WPF catalog application (dark mode)
  └── WorkoutCatalog.Crawler/     Console tool for batch scan, merge, and cleanup
```

## WorkoutCatalog (WPF App)

### Tech Stack
- .NET 8.0 WPF with MVVM (CommunityToolkit.Mvvm 8.4.0)
- SQLite via Entity Framework Core 8.0 (`%APPDATA%/WorkoutCatalog/catalog.db`)
- LibVLCSharp for embedded video playback
- FFmpeg/FFprobe for video probing and thumbnail generation
- Custom dark theme (no external UI library)

### Data Model

| Entity | Purpose |
|--------|---------|
| **Exercise** | Core entity: name, folder path, description, type, level, durations, file names, rating (1-5), thumbnails |
| **ExerciseComment** | User comments on exercises (text + timestamp) |
| **ViewHistory** | Tracks which videos were played and when |
| **Playlist** | Named collection with description |
| **PlaylistExercise** | Many-to-many join table (exercise can belong to multiple playlists) |

### Enums
- **ExerciseType**: Unknown, Stretching, Strength, Core
- **ExerciseLevel**: Unknown, Beginner, Intermediate, Expert

### Features

**Browse & Filter**
- Search by keyword (name + description)
- Filter by type, level, minimum rating
- Thumbnail column in list view (auto-generated from video via FFmpeg)
- Play (▶) and Preview (▶) button columns: navigate to detail and auto-play the respective video
- Double-click row to open exercise detail without auto-play

**Exercise Detail**
- All fields shown as read-only labels by default
- Edit button reveals Type/Level ComboBoxes; Save button commits and returns to read-only
- Rating (1–5 stars, toggle to clear) always interactive, saves immediately
- Comments always editable (add/delete), save immediately
- Read-only description (loaded from .txt file)
- Embedded LibVLCSharp video player with Play Preview / Play Full buttons
- Transport controls: pause/resume, stop, seek slider, time display, volume slider (0–100)
- Full Screen button (visible when media is loaded): opens maximised window, Escape or double-click to exit
- Playlist management: view current playlists, add to / remove from playlists
- Image toggle (hides white-background images by default, shows in accent-bordered container)
- Comments with add/delete

**Playlists**
- Split-pane view: playlist list on left, exercises on right
- Create and delete playlists
- Remove exercises from playlists
- Double-click exercise to navigate to detail

**Top 10**
- Shows highest-rated exercises with rank numbers

**History**
- Shows recently viewed exercises (recorded automatically on video playback)
- Double-click to navigate to detail

**Folder Scan**
- Crawls subfolders of configured video root
- Folder name = exercise name
- Classifies MP4s: 1 file = full only; 2 files = longer is full, shorter is preview; 4 files = needs merging
- Reads first .txt as description, first image as summary
- Generates thumbnails via FFmpeg (single frame at 5s, scaled to 240px)
- Upserts to SQLite keyed on relative folder path

**Settings**
- Video root folder path (required on first launch)
- FFmpeg path
- Stored in App.config via ConfigurationManager

### Dark Theme
- Background #1E1E1E, surface #2D2D2D, primary #BB86FC, accent #03DAC6, star #FFD700
- Implicit styles for all controls (Window, TextBlock, TextBox, Button, ComboBox, ListView, etc.)
- Named styles: PrimaryButton, NavButton, StarButton

### Project Layout
```
WorkoutCatalog/
  App.xaml / App.xaml.cs / App.config
  Converters/          DurationToString, RatingToStars, BoolToVisibility,
                       NullToVisibility, ExerciseToThumbnail
  Data/                CatalogDbContext
  Models/              Exercise, ExerciseComment, ViewHistory, Playlist,
                       PlaylistExercise, FolderScanResult, enums
  Services/            SettingsService, FFmpegProbeService, FolderScanService,
                       ExerciseService, PlaylistService, ThumbnailService,
                       VideoPlayerService
  ViewModels/          MainViewModel, ExerciseListViewModel, ExerciseDetailViewModel,
                       VideoPlayerViewModel, TopTenViewModel, HistoryViewModel,
                       SettingsViewModel, PlaylistViewModel
  Views/               MainWindow, ExerciseListView, ExerciseDetailView,
                       TopTenView, HistoryView, SettingsDialog, PlaylistView
  Themes/              DarkTheme.xaml
```

## WorkoutCatalog.Crawler (Console Tool)

Batch-processing tool that shares the same SQLite database and models as the WPF app.

### Commands

```
crawler scan    --root <path> --ffmpeg <path>   Scan folders and populate database
crawler merge   --root <path> --ffmpeg <path>   Merge 4-MP4 folders into Full.mp4 + Preview.mp4
crawler cleanup --root <path>                   Move source files to Recycle Bin after merge
```

All commands support `--dry-run` to preview without making changes.

**scan** - Reuses FolderScanService and ThumbnailService from WorkoutCatalog. Crawls all subfolders, probes videos, generates thumbnails, upserts exercise records.

**merge** - Finds folders with exactly 4 MP4 files. Probes each with FFprobe to classify as video-only or audio-only. Pairs by file size (largest pair = Full, second = Preview). Merges using `ffmpeg -y -i video -i audio -shortest -c:v copy -c:a copy output`. Skips folders that already have Full.mp4 and Preview.mp4.

**cleanup** - In folders where Full.mp4 and Preview.mp4 exist, moves all other MP4 files to the Windows Recycle Bin via `Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile` with `SendToRecycleBin`.

## Folder Structure Expectations

Each subfolder in the video root represents one exercise:
```
VideoRoot/
  Exercise Name/
    info.txt              Description text
    info.png              Summary image (white background)
    preview.mp4           Short preview video
    full.mp4              Full-length video
    thumb.jpg             Auto-generated thumbnail (240px wide)
```

Variations handled:
- 1 MP4: treated as full video only
- 2 MP4s: longer = full, shorter = preview
- 4 MP4s: needs merging (2 video-only + 2 audio-only streams)
- No MP4s: recorded with warning
- 3 or 5+ MP4s: best-effort classification using top 2 by duration

## Original Requirements

> I have a lot of workout videos, each with a preview and a full video, mp4.
> I'm looking to build an application that catalogs these videos. The finished project should be a polished windows app that can filter the exercises by length, type (stretching, strength, core), level (beginner, intermediate, expert) and description keywords.
> It should have a star rating and comments for each, a top 10, history, and all other things that make video apps cool.
> Each folder in the main folder has the name of the exercise. Each folder contains a txt, an image with the summary, a preview video and a full video.
> In certain cases 4 mp4s are present, so a console tool crawls the folders, and if 4 files are found ffmpeg them into 2 files, with video and audio.
> There should also be a cleanup script that deletes (soft delete, recycle bin) the processed video files.
> Dark mode, but images are white background so they're hidden by default with a toggle to show.

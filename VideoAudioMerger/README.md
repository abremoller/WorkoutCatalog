# Video Audio Merger

A WPF desktop application for merging separate video and audio MP4 files using FFmpeg.

## Features

- **Auto-population**: Point to a folder and automatically detect video and audio files
- **Drag & Drop**: Drag MP4 files directly onto input fields
- **Duplicate Detection**: Checks project names against a tracking file
- **Smart Output Naming**: Auto-suggests output folder based on project name
- **FFmpeg Integration**: Uses FFmpeg and FFprobe to process and analyze media files

## Requirements

- .NET 8.0 or later
- FFmpeg (see installation instructions below)
- Windows OS

## FFmpeg Installation

This application requires FFmpeg to be installed on your system.

### Download FFmpeg

1. Visit the official FFmpeg website: https://ffmpeg.org/download.html
2. For Windows, download from one of these sources:
   - **gyan.dev** (recommended): https://www.gyan.dev/ffmpeg/builds/
     - Download "ffmpeg-release-essentials.zip"
   - **BtbN builds**: https://github.com/BtbN/FFmpeg-Builds/releases
     - Download "ffmpeg-master-latest-win64-gpl.zip"

### Installation Steps

1. Extract the downloaded ZIP file to a location on your computer (e.g., `C:\ffmpeg\`)
2. The extracted folder will contain a `bin` directory with `ffmpeg.exe` and `ffprobe.exe`
3. On first launch, the application will prompt you to locate `ffmpeg.exe`
4. Navigate to the `bin` folder and select `ffmpeg.exe`

The application will automatically save this location for future use.

## Project Structure

```
VideoAudioMerger/
├── Models/              # Data models for media files and results
├── Views/               # XAML dialogs and UI components
├── ViewModels/          # MVVM ViewModels with business logic
├── Services/            # Core services (FFmpeg, file tracking, auto-population)
├── MainWindow.xaml      # Main application window
└── App.xaml            # Application startup and configuration
```

## How to Build

```bash
cd "c:\Development\Video App"
dotnet build VideoAudioMerger.sln
```

## How to Run

```bash
cd "c:\Development\Video App\VideoAudioMerger"
dotnet run
```

Or open `VideoAudioMerger.sln` in Visual Studio and press F5.

## Usage

1. **First Launch**: Configure FFmpeg path when prompted
2. **Enter Project Name**: Type a unique name (duplicate check runs automatically)
3. **Select Files**: Either:
   - Browse to a folder and click "Auto-Populate" to detect files automatically
   - Manually browse or drag & drop MP4 files into each field
4. **Verify Output Folder**: Auto-suggested from project name, or browse to change
5. **Click Process**: Creates `Full.mp4` and `Preview.mp4` in the output folder

## Auto-Population Logic

The app scans the selected folder for the 10 most recent MP4 files, then:
- Uses FFprobe to identify video-only and audio-only files
- Matches largest video + largest audio = Full
- Matches second-largest video + second-largest audio = Preview

## Configuration

FFmpeg path is stored in `App.config`:

```xml
<appSettings>
  <add key="FFmpegPath" value="C:\path\to\ffmpeg.exe" />
</appSettings>
```

Processed names are tracked in:
```
%AppData%\VideoAudioMerger\processed_names.txt
```

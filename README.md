# HelloDotnetTen

A .NET 10 console application demonstrating modern C# features and cross-platform deployment.

[![Build and Publish](https://github.com/collabskus/HelloDotnetTen/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/collabskus/HelloDotnetTen/actions/workflows/build-and-publish.yml)

## Overview

This project showcases .NET 10 capabilities with a simple "Hello, World!" console application that can be compiled into fully self-contained executables for all major platforms.

## Features

- .NET 10 Preview targeting
- Cross-platform support (Windows, Linux, macOS)
- Self-contained single-file executables (no .NET runtime required on target machine)
- Automated CI/CD with GitHub Actions
- Downloadable binaries for 10 platforms

## Requirements

### For Development
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Preview)
- Any IDE: Visual Studio 2022, VS Code, Rider, or command line

### For Running Pre-built Binaries
- Nothing! The executables are fully self-contained.

## Quick Start

### Clone and Run
```bash
git clone https://github.com/collabskus/HelloDotnetTen.git
cd HelloDotnetTen/source/HelloDotnetTen/HelloDotnetTen.Console
dotnet run
```

### Build Self-Contained Executables
```powershell
# From repository root
./publish.ps1

# Clean previous builds first
./publish.ps1 -Clean

# Output will be in the 'publish' directory
```

## Download Pre-built Binaries

Pre-built executables are available from the [Actions tab](https://github.com/collabskus/HelloDotnetTen/actions). Click on any successful workflow run and scroll down to the **Artifacts** section to download binaries for your platform.

### Available Platforms

| Platform | Description | File |
|----------|-------------|------|
| `win-x64` | Windows 64-bit | `HelloDotnetTen.Console.exe` |
| `win-x86` | Windows 32-bit | `HelloDotnetTen.Console.exe` |
| `win-arm64` | Windows ARM64 | `HelloDotnetTen.Console.exe` |
| `linux-x64` | Linux 64-bit | `HelloDotnetTen.Console` |
| `linux-arm64` | Linux ARM64 (Raspberry Pi 4, AWS Graviton) | `HelloDotnetTen.Console` |
| `linux-arm` | Linux ARM32 (older Raspberry Pi) | `HelloDotnetTen.Console` |
| `linux-musl-x64` | Alpine Linux x64 (Docker) | `HelloDotnetTen.Console` |
| `linux-musl-arm64` | Alpine Linux ARM64 | `HelloDotnetTen.Console` |
| `osx-x64` | macOS Intel | `HelloDotnetTen.Console` |
| `osx-arm64` | macOS Apple Silicon (M1/M2/M3/M4) | `HelloDotnetTen.Console` |

### Running on Linux/macOS

After downloading, you may need to make the file executable:
```bash
chmod +x HelloDotnetTen.Console
./HelloDotnetTen.Console
```

## Project Structure

```
HelloDotnetTen/
├── .github/
│   └── workflows/
│       └── build-and-publish.yml    # CI/CD pipeline
├── source/
│   └── HelloDotnetTen/
│       ├── HelloDotnetTen.Console/  # Console application
│       └── HelloDotnetTen.slnx      # Solution file
├── docs/
│   └── llm/
│       └── dump.txt                 # Project export for LLM context
├── publish.ps1                      # Cross-platform publish script
├── export.ps1                       # Project export script
└── README.md
```

## Scripts

### publish.ps1

Generates self-contained single-file executables for all supported platforms.

```powershell
# Basic usage
./publish.ps1

# With options
./publish.ps1 -Clean           # Clean output directory first
./publish.ps1 -SkipRestore     # Skip restore (use with caution)
./publish.ps1 -OutputDir dist  # Custom output directory
```

### export.ps1

Exports all project source files to a single text file for LLM context.

```powershell
./export.ps1
# Output: docs/llm/dump.txt
```

## CI/CD

This project uses GitHub Actions for continuous integration. Every push and pull request triggers:

1. Build verification
2. Cross-platform publish for all 10 target platforms
3. Artifact upload (binaries available for download)

## License

This project is provided as-is for educational and demonstration purposes.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

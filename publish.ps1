# Publish Console Applications as Self-Contained Single-File Executables
# PowerShell 5 compatible script
# Generates standalone executables for all major platforms - NO .NET runtime required on target

param(
    [string]$ProjectPath = "source\HelloDotnetTen",
    [string]$OutputDir = "publish",
    [switch]$Clean,
    [switch]$SkipRestore
)

# Runtime Identifiers for all major platforms
# These create FULLY self-contained executables with NO external dependencies
$RuntimeIdentifiers = @(
    # Windows
    "win-x64",          # Windows 64-bit (most common)
    "win-x86",          # Windows 32-bit (legacy support)
    "win-arm64",        # Windows ARM64 (Surface Pro X, etc.)
    
    # Linux
    "linux-x64",        # Linux 64-bit (most servers/desktops)
    "linux-arm64",      # Linux ARM64 (Raspberry Pi 4, AWS Graviton, etc.)
    "linux-arm",        # Linux ARM32 (older Raspberry Pi)
    "linux-musl-x64",   # Alpine Linux (Docker containers)
    "linux-musl-arm64", # Alpine Linux ARM64
    
    # macOS
    "osx-x64",          # macOS Intel
    "osx-arm64"         # macOS Apple Silicon (M1/M2/M3)
)

# Console projects to publish (add more as needed)
$ConsoleProjects = @(
    "HelloDotnetTen.Console"
)

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Self-Contained Executable Publisher" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Project Path: $ProjectPath" -ForegroundColor Yellow
Write-Host "Output Directory: $OutputDir" -ForegroundColor Yellow
Write-Host "Platforms: $($RuntimeIdentifiers.Count)" -ForegroundColor Yellow
Write-Host ""

# Clean output directory if requested
if ($Clean -and (Test-Path $OutputDir)) {
    Write-Host "Cleaning output directory..." -ForegroundColor Magenta
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Track results
$results = @()
$totalBuilds = $ConsoleProjects.Count * $RuntimeIdentifiers.Count
$currentBuild = 0
$startTime = Get-Date

foreach ($project in $ConsoleProjects) {
    $projectFile = Join-Path $ProjectPath "$project\$project.csproj"
    
    if (-not (Test-Path $projectFile)) {
        Write-Host "ERROR: Project file not found: $projectFile" -ForegroundColor Red
        continue
    }
    
    Write-Host ""
    Write-Host "Publishing: $project" -ForegroundColor Green
    Write-Host "=" * 50 -ForegroundColor DarkGray
    
    foreach ($rid in $RuntimeIdentifiers) {
        $currentBuild++
        $percentComplete = [math]::Round(($currentBuild / $totalBuilds) * 100)
        
        Write-Host "  [$currentBuild/$totalBuilds] $rid... " -NoNewline -ForegroundColor White
        
        # Create platform-specific output directory
        $platformOutputDir = Join-Path $OutputDir "$project\$rid"
        
        # Build the publish command
        # Key flags explained:
        #   -c Release                    : Release configuration (optimized)
        #   -r $rid                       : Target runtime identifier
        #   --self-contained true         : Include .NET runtime (NO .NET needed on target)
        #   -p:PublishSingleFile=true     : Bundle everything into ONE executable
        #   -p:PublishTrimmed=true        : Remove unused code (smaller file size)
        #   -p:EnableCompressionInSingleFile=true : Compress the single file
        #   -p:IncludeNativeLibrariesForSelfExtract=true : Include native libs in single file
        #   -p:DebugType=None             : No debug symbols (smaller file)
        #   -p:DebugSymbols=false         : No PDB files
        
        $publishArgs = @(
            "publish"
            $projectFile
            "-c", "Release"
            "-r", $rid
            "-o", $platformOutputDir
            "--self-contained", "true"
            "-p:PublishSingleFile=true"
            "-p:PublishTrimmed=true"
            "-p:EnableCompressionInSingleFile=true"
            "-p:IncludeNativeLibrariesForSelfExtract=true"
            "-p:DebugType=None"
            "-p:DebugSymbols=false"
        )
        
        if ($SkipRestore) {
            $publishArgs += "--no-restore"
        }
        
        # Execute publish
        $output = & dotnet @publishArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            # Find the executable
            $exePattern = if ($rid -like "win-*") { "*.exe" } else { $project }
            $executable = Get-ChildItem -Path $platformOutputDir -Filter $exePattern -File | 
                          Where-Object { $_.Name -notlike "*.pdb" -and $_.Name -notlike "*.json" } |
                          Select-Object -First 1
            
            if ($executable) {
                $sizeMB = [math]::Round($executable.Length / 1MB, 2)
                Write-Host "OK " -ForegroundColor Green -NoNewline
                Write-Host "($sizeMB MB)" -ForegroundColor DarkGray
                
                $results += [PSCustomObject]@{
                    Project = $project
                    Platform = $rid
                    Status = "Success"
                    Size = "$sizeMB MB"
                    Path = $executable.FullName
                }
            } else {
                Write-Host "OK (no exe found)" -ForegroundColor Yellow
                $results += [PSCustomObject]@{
                    Project = $project
                    Platform = $rid
                    Status = "Success (no exe)"
                    Size = "N/A"
                    Path = $platformOutputDir
                }
            }
        } else {
            Write-Host "FAILED" -ForegroundColor Red
            
            # Show error details
            $errorLines = $output | Where-Object { $_ -match "error" } | Select-Object -First 3
            foreach ($line in $errorLines) {
                Write-Host "    $line" -ForegroundColor DarkRed
            }
            
            $results += [PSCustomObject]@{
                Project = $project
                Platform = $rid
                Status = "Failed"
                Size = "N/A"
                Path = "N/A"
            }
        }
    }
}

# Calculate elapsed time
$endTime = Get-Date
$elapsed = $endTime - $startTime

# Summary
Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  PUBLISH SUMMARY" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Elapsed Time: $([math]::Round($elapsed.TotalMinutes, 2)) minutes" -ForegroundColor Yellow
Write-Host ""

$successCount = ($results | Where-Object { $_.Status -like "Success*" }).Count
$failedCount = ($results | Where-Object { $_.Status -eq "Failed" }).Count

Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failedCount" -ForegroundColor $(if ($failedCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

# Display results table
Write-Host "Platform Results:" -ForegroundColor White
Write-Host "-" * 80 -ForegroundColor DarkGray

$results | ForEach-Object {
    $statusColor = if ($_.Status -like "Success*") { "Green" } else { "Red" }
    $platformPadded = $_.Platform.PadRight(20)
    $sizePadded = $_.Size.PadRight(10)
    
    Write-Host "  $platformPadded" -NoNewline -ForegroundColor White
    Write-Host "$sizePadded" -NoNewline -ForegroundColor DarkGray
    Write-Host $_.Status -ForegroundColor $statusColor
}

Write-Host ""
Write-Host "Output Location: $(Resolve-Path $OutputDir)" -ForegroundColor Yellow
Write-Host ""

# Create a manifest file
$manifestPath = Join-Path $OutputDir "publish-manifest.json"
$manifest = @{
    GeneratedAt = (Get-Date).ToString("o")
    ElapsedSeconds = [math]::Round($elapsed.TotalSeconds, 2)
    Results = $results | ForEach-Object {
        @{
            Project = $_.Project
            Platform = $_.Platform
            Status = $_.Status
            Size = $_.Size
            Path = $_.Path
        }
    }
}

$manifest | ConvertTo-Json -Depth 3 | Out-File -FilePath $manifestPath -Encoding UTF8
Write-Host "Manifest saved: $manifestPath" -ForegroundColor DarkGray

# Exit with error code if any builds failed
if ($failedCount -gt 0) {
    Write-Host ""
    Write-Host "WARNING: Some builds failed. Check the output above for details." -ForegroundColor Yellow
    exit 1
}

Write-Host "All builds completed successfully!" -ForegroundColor Green

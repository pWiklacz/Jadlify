[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$Version = "",

    [string]$ReleaseDir = "$PSScriptRoot/../releases",

    [switch]$Help
)

$ErrorActionPreference = "Stop"

function Write-And-Exit {
    param(
        [string]$Message,
        [int]$Code
    )

    Write-Output $Message
    exit $Code
}

function Write-Help {
    Write-Output @'
pack-min.ps1 - minimal Velopack pack pipeline for the phrAIse desktop app

Prerequisite:
  The 'vpk' global tool must be installed:
    dotnet tool install --global Velopack.Vpk
  This script DOES NOT auto-install vpk.

Usage:
  pwsh ./.scripts/pack-min.ps1
  pwsh ./.scripts/pack-min.ps1 -Version 0.6.0-dev
  pwsh ./.scripts/pack-min.ps1 -Configuration Release -Version 0.6.0-dev

What it does (in order):
  1. Ensures 'vpk' is on PATH (exits 2 with an install hint otherwise).
  2. If -Version is empty, derives it from `git describe --tags --always --dirty`
     and normalizes the result to a SemVer2 string before invoking vpk.
  3. Runs ./.scripts/build-min.ps1 -Project ./phrAIse.Desktop/phrAIse.Desktop.csproj -Configuration <cfg>.
  4. Runs `dotnet publish phrAIse.Desktop -c <cfg> -r win-x64 --self-contained -o ./publish/win-x64`.
  5. Runs `vpk pack --packId phrAIse --packVersion <ver> --packDir ./publish/win-x64
     --mainExe phrAIse.Desktop.exe --outputDir <ReleaseDir> --delta None`.
  6. Asserts that <ReleaseDir>/phrAIse-<ver>-full.nupkg exists.

Output:
  [PackSuccess] on success.
  [PackFailed]  with one diagnostic line on failure.
  Full raw output is appended to .logs/pack-min.log.

Options:
  -Configuration Debug|Release  default Release
  -Version <semver2>            optional; if empty, derived from git describe and normalized
  -ReleaseDir <path>            output directory for vpk artifacts; default ./releases
'@
}

function Test-SemVer2 {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    $pattern = '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)' +
        '(?:-(?<pre>(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*)' +
        '(?:\.(?:0|[1-9]\d*|\d*[A-Za-z-][0-9A-Za-z-]*))*))?' +
        '(?:\+(?<build>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$'
    return $Value -match $pattern
}

function Resolve-VersionFromGit {
    $describe = & git describe --tags --always --dirty 2>$null
    if (-not $describe) {
        Write-And-Exit "[PackFailed] git describe failed; provide -Version explicitly." 2
    }

    $raw = $describe.Trim()
    $candidate = $raw

    # Strip leading 'v'.
    if ($candidate -match '^v\d') {
        $candidate = $candidate.Substring(1)
    }

    # Form: <tag>-<count>-g<sha>[-dirty]
    $match = [regex]::Match($candidate, '^(?<base>\d+\.\d+\.\d+(?:-[0-9A-Za-z\.-]+)?)-(?<count>\d+)-g(?<sha>[0-9a-fA-F]+)(?:-(?<dirty>dirty))?$')
    if ($match.Success) {
        $base = $match.Groups["base"].Value
        $count = $match.Groups["count"].Value
        $sha = $match.Groups["sha"].Value
        $dirty = $match.Groups["dirty"].Value

        if ($base.Contains('-')) {
            # Tag already has a prerelease segment (e.g. 0.6.0-rc1); append commit count to it.
            $prerelease = "$base.$count"
        }
        else {
            $prerelease = "$base-dev.$count"
        }

        $build = "g$sha"
        if (-not [string]::IsNullOrEmpty($dirty)) {
            $build = "$build.dirty"
        }

        $candidate = "$prerelease+$build"
        if (Test-SemVer2 $candidate) {
            return $candidate
        }
    }

    if (Test-SemVer2 $candidate) {
        return $candidate
    }

    # Bare commit sha (no tag yet).
    if ($raw -match '^(?<sha>[0-9a-fA-F]{4,})(?:-(?<dirty>dirty))?$') {
        $sha = $Matches["sha"]
        $dirty = $Matches["dirty"]
        $build = "g$sha"
        if (-not [string]::IsNullOrEmpty($dirty)) {
            $build = "$build.dirty"
        }
        $candidate = "0.0.0-dev+$build"
        if (Test-SemVer2 $candidate) {
            return $candidate
        }
    }

    Write-And-Exit "[PackFailed] git describe '$raw' could not be normalized to SemVer2; pass -Version explicitly." 2
}

if ($Help) {
    Write-Help
    exit 0
}

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$logDir = Join-Path $repoRoot ".logs"
$logFile = Join-Path $logDir "pack-min.log"

if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

# Reset the log for this invocation.
Set-Content -Path $logFile -Value "" -Encoding utf8

function Append-Log {
    param([string[]]$Lines)

    if ($null -ne $Lines) {
        $Lines | Out-File -FilePath $logFile -Append -Encoding utf8
    }
}

# 1. Verify vpk is available.
$vpkCommand = Get-Command vpk -ErrorAction SilentlyContinue
if ($null -eq $vpkCommand) {
    Append-Log @("vpk: not found on PATH")
    Write-Output "[PackFailed] 'vpk' global tool is not on PATH."
    Write-Output "Install it with: dotnet tool install --global Velopack.Vpk"
    Write-Output "[FullLog: $logFile]"
    exit 2
}

$vpk = & vpk --help 2>&1
$vpkExit = $LASTEXITCODE
Append-Log @("vpk --help (exit=$vpkExit):", "$vpk")
if ($vpkExit -ne 0) {
    Write-Output "[PackFailed] 'vpk --help' failed with exit code $vpkExit."
    Write-Output "[FullLog: $logFile]"
    exit 2
}

# 2. Resolve / validate version.
if ([string]::IsNullOrWhiteSpace($Version)) {
    Push-Location $repoRoot
    try {
        $Version = Resolve-VersionFromGit
    }
    finally {
        Pop-Location
    }
}
elseif ($Version.StartsWith("v")) {
    $Version = $Version.Substring(1)
}

if (-not (Test-SemVer2 $Version)) {
    Write-And-Exit "[PackFailed] Version '$Version' is not SemVer2-compatible; vpk pack would reject it." 2
}

Append-Log @("resolved version: $Version")

# 3. Build via the minimal build script.
Push-Location $repoRoot
try {
    Append-Log @("--- build-min ---")
    $buildOutput = & pwsh ./.scripts/build-min.ps1 -Project ./phrAIse.Desktop/phrAIse.Desktop.csproj -Configuration $Configuration 2>&1
    $buildExit = $LASTEXITCODE
    Append-Log $buildOutput
    if ($buildExit -ne 0) {
        Write-Output "[PackFailed] build-min.ps1 exited with code $buildExit."
        Write-Output "[FullLog: $logFile]"
        exit $buildExit
    }

    # 4. dotnet publish for the runtime layout vpk consumes.
    $publishDir = Join-Path $repoRoot "publish/win-x64"
    Append-Log @("--- dotnet publish ---")
    $publishOutput = & dotnet publish phrAIse.Desktop -c $Configuration -r win-x64 --self-contained -o $publishDir --nologo 2>&1
    $publishExit = $LASTEXITCODE
    Append-Log $publishOutput
    if ($publishExit -ne 0) {
        Write-Output "[PackFailed] dotnet publish exited with code $publishExit."
        Write-Output "[FullLog: $logFile]"
        exit $publishExit
    }

    if (-not (Test-Path $ReleaseDir)) {
        New-Item -ItemType Directory -Path $ReleaseDir | Out-Null
    }

    # 5. vpk pack.
    Append-Log @("--- vpk pack ---")
    $packOutput = & vpk pack `
        --packId phrAIse `
        --packVersion $Version `
        --packDir $publishDir `
        --mainExe phrAIse.Desktop.exe `
        --outputDir $ReleaseDir `
        --delta None 2>&1
    $packExit = $LASTEXITCODE
    Append-Log $packOutput
    if ($packExit -ne 0) {
        Write-Output "[PackFailed] vpk pack exited with code $packExit."
        Write-Output "[FullLog: $logFile]"
        exit $packExit
    }

    # 6. Verify the expected artifact exists.
    $expectedNupkg = Join-Path $ReleaseDir "phrAIse-$Version-full.nupkg"
    if (-not (Test-Path $expectedNupkg)) {
        Write-Output "[PackFailed] Expected artifact not found: $expectedNupkg"
        Write-Output "[FullLog: $logFile]"
        exit 3
    }

    Write-Output "[PackSuccess] $expectedNupkg"
    exit 0
}
finally {
    Pop-Location
}

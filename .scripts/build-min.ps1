param(
    [switch]$Help,

    [string]$Project,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$Framework,

    [string]$Runtime,

    [switch]$NoRestore,

    [switch]$TreatWarningsAsErrors,

    [string[]]$DotnetArgs = @()
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
    Write-Output @"
build-min.ps1 - minimal dotnet build output for agents

Usage:
  pwsh ./.scripts/build-min.ps1
  pwsh ./.scripts/build-min.ps1 -Project ./phrAIse.Api/phrAIse.Api.csproj
  pwsh ./.scripts/build-min.ps1 -Project ./phrAIse.Shared/phrAIse.Shared.csproj -Configuration Release
  pwsh ./.scripts/build-min.ps1 -NoRestore

Output:
  [BuildSuccess] on success.
  [BuildFailed] plus up to 60 minimal diagnostics on failure.
  Full raw output is saved to .logs/build.log.

Options:
  -Project <path>             Build a .sln, .slnx, .csproj, .fsproj or .vbproj path.
  -Configuration Debug|Release
  -Framework <tfm>
  -Runtime <rid>
  -NoRestore
  -TreatWarningsAsErrors
  -DotnetArgs <args[]>        Extra safe MSBuild args. Prefer /p:Name=value syntax in PowerShell.

Do not pass dotnet commands or script-controlled options through -DotnetArgs:
  build, test, restore, publish, pack, run, clean
  -c, --configuration, -f, --framework, -r, --runtime, --no-restore, --verbosity, --nologo, -clp
"@
}

function Get-UniquePreservingOrder {
    param([string[]]$Items)

    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $result = New-Object System.Collections.Generic.List[string]

    foreach ($item in $Items) {
        if ([string]::IsNullOrWhiteSpace($item)) {
            continue
        }

        $normalized = $item.Trim()

        if ($seen.Add($normalized)) {
            $result.Add($normalized)
        }
    }

    return $result.ToArray()
}

function Assert-ValidPath {
    param(
        [string]$PathValue,
        [string]$ParamName
    )

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return
    }

    if (-not (Test-Path $PathValue)) {
        Write-And-Exit "[$ParamName Invalid] Path does not exist: $PathValue" 2
    }

    $allowedExtensions = @(".sln", ".slnx", ".csproj", ".fsproj", ".vbproj")
    $extension = [System.IO.Path]::GetExtension($PathValue)

    if ($allowedExtensions -notcontains $extension) {
        Write-And-Exit "[$ParamName Invalid] Expected .sln, .slnx, .csproj, .fsproj or .vbproj: $PathValue" 2
    }
}

function Assert-SafeExtraArgs {
    param([string[]]$ExtraArgs)

    $blocked = @(
        "build",
        "test",
        "restore",
        "publish",
        "pack",
        "run",
        "clean"
    )

    $blockedOptions = @(
        "-v",
        "--verbosity",
        "--nologo",
        "-clp",
        "--consoleloggerparameters",
        "-c",
        "--configuration",
        "-f",
        "--framework",
        "-r",
        "--runtime",
        "--no-restore"
    )

    foreach ($arg in $ExtraArgs) {
        if ([string]::IsNullOrWhiteSpace($arg)) {
            continue
        }

        $trimmed = $arg.Trim()

        if ($blocked -contains $trimmed.ToLowerInvariant()) {
            Write-And-Exit "[BuildArgsInvalid] Do not pass dotnet command '$trimmed' through -DotnetArgs." 2
        }

        foreach ($blockedOption in $blockedOptions) {
            if (
                $trimmed.Equals($blockedOption, [System.StringComparison]::OrdinalIgnoreCase) -or
                $trimmed.StartsWith("$blockedOption=", [System.StringComparison]::OrdinalIgnoreCase) -or
                $trimmed.StartsWith("$blockedOption`:", [System.StringComparison]::OrdinalIgnoreCase)
            ) {
                Write-And-Exit "[BuildArgsInvalid] Option '$trimmed' is controlled by the script. Use the dedicated script parameter instead." 2
            }
        }
    }
}

function Get-DisplayPath {
    param([string]$PathValue)

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return $PathValue
    }

    $trimmed = $PathValue.Trim()

    if (-not [System.IO.Path]::IsPathRooted($trimmed)) {
        return $trimmed
    }

    try {
        return [System.IO.Path]::GetRelativePath((Get-Location).Path, $trimmed)
    }
    catch {
        return $trimmed
    }
}

function Format-BuildDiagnostic {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) {
        return $null
    }

    $clean = $Line.Trim()
    $clean = $clean -replace '\s+\(https?://[^)]*\)', ''
    $clean = $clean -replace '\s+\[[^\]]+\]\s*$', ''

    if (
        $clean -match '^Build (FAILED|succeeded)\.$' -or
        $clean -match '^\d+\s+(Warning|Error)\(s\)$' -or
        $clean -match '^Time Elapsed\s+' -or
        $clean -match '^\d+\s+ms$'
    ) {
        return $null
    }

    $fileDiagnosticPattern = '^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\):\s*(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s*(?<message>.*)$'
    if ($clean -match $fileDiagnosticPattern) {
        $path = Get-DisplayPath $Matches["file"]
        return "$path`:$($Matches["line"]):$($Matches["column"]) $($Matches["code"]) $($Matches["message"])"
    }

    $generalDiagnosticPattern = '^(?<source>.*?):\s*(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s*(?<message>.*)$'
    if ($clean -match $generalDiagnosticPattern) {
        $source = $Matches["source"].Trim()
        if ([string]::IsNullOrWhiteSpace($source)) {
            return "$($Matches["code"]) $($Matches["message"])"
        }

        return "$source $($Matches["code"]) $($Matches["message"])"
    }

    return $clean
}

function Get-MinimalBuildOutput {
    param([string[]]$Lines)

    $result = New-Object System.Collections.Generic.List[string]

    foreach ($line in $Lines) {
        $formatted = Format-BuildDiagnostic $line

        if (-not [string]::IsNullOrWhiteSpace($formatted)) {
            $result.Add($formatted)
        }
    }

    return $result.ToArray()
}

if ($Help) {
    Write-Help
    exit 0
}

Assert-ValidPath -PathValue $Project -ParamName "Project"

$DotnetArgs = Get-UniquePreservingOrder $DotnetArgs
Assert-SafeExtraArgs $DotnetArgs

$logDir = ".logs"
$logFile = Join-Path $logDir "build.log"

if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

$argsList = New-Object System.Collections.Generic.List[string]

$argsList.Add("build")

if (-not [string]::IsNullOrWhiteSpace($Project)) {
    $argsList.Add($Project)
}

$argsList.Add("--nologo")
$argsList.Add("--configuration")
$argsList.Add($Configuration)
$argsList.Add("--verbosity")
$argsList.Add("quiet")
$argsList.Add("-clp:ErrorsOnly;NoSummary")

if (-not [string]::IsNullOrWhiteSpace($Framework)) {
    $argsList.Add("--framework")
    $argsList.Add($Framework)
}

if (-not [string]::IsNullOrWhiteSpace($Runtime)) {
    $argsList.Add("--runtime")
    $argsList.Add($Runtime)
}

if ($NoRestore) {
    $argsList.Add("--no-restore")
}

if ($TreatWarningsAsErrors) {
    $argsList.Add("-p:TreatWarningsAsErrors=true")
}

foreach ($arg in $DotnetArgs) {
    $argsList.Add($arg)
}

$output = & dotnet @argsList 2>&1
$exitCode = $LASTEXITCODE

$output | Out-File -FilePath $logFile -Encoding utf8

if ($exitCode -eq 0) {
    Write-Output "[BuildSuccess]"
    exit 0
}

Write-Output "[BuildFailed]"

Get-MinimalBuildOutput $output |
    Select-Object -First 60 |
    ForEach-Object { Write-Output $_ }

Write-Output "[FullLog: $logFile]"

exit $exitCode

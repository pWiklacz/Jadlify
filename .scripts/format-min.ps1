param(
    [switch]$Help,

    [string]$Project,

    [ValidateSet("info", "warn", "error")]
    [string]$Severity = "error",

    [switch]$NoRestore,

    [string[]]$Include = @(),

    [string[]]$Exclude = @(),

    [string[]]$Diagnostics = @(),

    [string[]]$ExcludeDiagnostics = @(),

    [switch]$IncludeGenerated,

    [int]$MaxFailureLines = 80,

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
format-min.ps1 - minimal dotnet format verification output for agents

Usage:
  pwsh ./.scripts/format-min.ps1
  pwsh ./.scripts/format-min.ps1 -Project ./phrAIse.Api/phrAIse.Api.csproj
  pwsh ./.scripts/format-min.ps1 -NoRestore
  pwsh ./.scripts/format-min.ps1 -Diagnostics IDE0055

Output:
  [FormatSuccess] on success.
  [FormatFailed] plus up to 80 minimal diagnostics on failure.
  Full raw output is saved to .logs/format.log.

Options:
  -Project <path>             Format-check a .sln, .slnx, .csproj, .fsproj or .vbproj path.
  -Severity info|warn|error
  -NoRestore
  -Include <paths[]>          Existing relative file or folder paths to include.
  -Exclude <paths[]>          Existing relative file or folder paths to exclude.
  -Diagnostics <ids[]>        Diagnostic IDs to include.
  -ExcludeDiagnostics <ids[]> Diagnostic IDs to exclude.
  -IncludeGenerated
  -MaxFailureLines <10-500>
  -DotnetArgs <args[]>        Extra safe dotnet format args.

Do not pass dotnet commands or script-controlled options through -DotnetArgs:
  format, build, test, restore, run, publish, pack, clean
  --verify-no-changes, --severity, --no-restore, --include, --exclude, --diagnostics, --exclude-diagnostics, --include-generated, --verbosity, --binarylog, --report
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

function Assert-ValidProjectPath {
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

function Assert-ExistingPaths {
    param(
        [string[]]$PathValues,
        [string]$ParamName
    )

    foreach ($pathValue in $PathValues) {
        if ([string]::IsNullOrWhiteSpace($pathValue)) {
            continue
        }

        if (-not (Test-Path $pathValue)) {
            Write-And-Exit "[$ParamName Invalid] Path does not exist: $pathValue" 2
        }
    }
}

function Assert-SafeText {
    param(
        [string]$Value,
        [string]$ParamName
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    if ($Value -match "[`r`n]") {
        Write-And-Exit "[$ParamName Invalid] Value cannot contain new lines." 2
    }

    if ($Value.Length -gt 200) {
        Write-And-Exit "[$ParamName Invalid] Value is too long." 2
    }
}

function Assert-SafeTextItems {
    param(
        [string[]]$Values,
        [string]$ParamName
    )

    foreach ($value in $Values) {
        Assert-SafeText -Value $value -ParamName $ParamName
    }
}

function Assert-SafeExtraArgs {
    param([string[]]$ExtraArgs)

    $blocked = @(
        "format",
        "build",
        "test",
        "restore",
        "run",
        "publish",
        "pack",
        "clean"
    )

    $blockedOptions = @(
        "-v",
        "--verbosity",
        "--binarylog",
        "--report",
        "--verify-no-changes",
        "--severity",
        "--no-restore",
        "--include",
        "--exclude",
        "--diagnostics",
        "--exclude-diagnostics",
        "--include-generated"
    )

    foreach ($arg in $ExtraArgs) {
        if ([string]::IsNullOrWhiteSpace($arg)) {
            continue
        }

        $trimmed = $arg.Trim()

        if ($blocked -contains $trimmed.ToLowerInvariant()) {
            Write-And-Exit "[FormatArgsInvalid] Do not pass dotnet command '$trimmed' through -DotnetArgs." 2
        }

        foreach ($blockedOption in $blockedOptions) {
            if (
                $trimmed.Equals($blockedOption, [System.StringComparison]::OrdinalIgnoreCase) -or
                $trimmed.StartsWith("$blockedOption=", [System.StringComparison]::OrdinalIgnoreCase) -or
                $trimmed.StartsWith("$blockedOption`:", [System.StringComparison]::OrdinalIgnoreCase)
            ) {
                Write-And-Exit "[FormatArgsInvalid] Option '$trimmed' is controlled by the script. Use the dedicated script parameter instead." 2
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

function Format-FormatDiagnostic {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) {
        return $null
    }

    $clean = $Line.Trim()
    $clean = $clean -replace '\s+\(https?://[^)]*\)', ''
    $clean = $clean -replace '\s+\[[^\]]+\]\s*$', ''

    if (
        $clean -match '^Restore complete' -or
        $clean -match '^The dotnet format command' -or
        $clean -match '^Formatting code files in workspace' -or
        $clean -match '^Formatted code file' -or
        $clean -match '^Format complete' -or
        $clean -match '^Warnings were encountered' -or
        $clean -match '^\d+\s+ms$'
    ) {
        return $null
    }

    $fileDiagnosticPattern = '^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\):\s*(?<severity>error|warning)\s+(?<code>[A-Z]+\d+):\s*(?<message>.*)$'
    if ($clean -match $fileDiagnosticPattern) {
        $path = Get-DisplayPath $Matches["file"]
        return "$path`:$($Matches["line"]):$($Matches["column"]) $($Matches["code"]) $($Matches["message"])"
    }

    $formatFilePattern = '^The file ''(?<file>.+?)'' is not formatted\.$'
    if ($clean -match $formatFilePattern) {
        $path = Get-DisplayPath $Matches["file"]
        return "$path is not formatted."
    }

    return $clean
}

function Get-MinimalFormatOutput {
    param([string[]]$Lines)

    $result = New-Object System.Collections.Generic.List[string]

    foreach ($line in $Lines) {
        $formatted = Format-FormatDiagnostic $line

        if (-not [string]::IsNullOrWhiteSpace($formatted)) {
            $result.Add($formatted)
        }
    }

    return $result.ToArray()
}

function Add-MultiValueOption {
    param(
        [System.Collections.Generic.List[string]]$ArgsList,
        [string]$OptionName,
        [string[]]$Values
    )

    if ($Values.Count -eq 0) {
        return
    }

    $ArgsList.Add($OptionName)

    foreach ($value in $Values) {
        $ArgsList.Add($value)
    }
}

if ($Help) {
    Write-Help
    exit 0
}

Assert-ValidProjectPath -PathValue $Project -ParamName "Project"

$Include = Get-UniquePreservingOrder $Include
$Exclude = Get-UniquePreservingOrder $Exclude
$Diagnostics = Get-UniquePreservingOrder $Diagnostics
$ExcludeDiagnostics = Get-UniquePreservingOrder $ExcludeDiagnostics
$DotnetArgs = Get-UniquePreservingOrder $DotnetArgs

Assert-ExistingPaths -PathValues $Include -ParamName "Include"
Assert-ExistingPaths -PathValues $Exclude -ParamName "Exclude"
Assert-SafeTextItems -Values $Diagnostics -ParamName "Diagnostics"
Assert-SafeTextItems -Values $ExcludeDiagnostics -ParamName "ExcludeDiagnostics"
Assert-SafeExtraArgs $DotnetArgs

if ($MaxFailureLines -lt 10 -or $MaxFailureLines -gt 500) {
    Write-And-Exit "[MaxFailureLinesInvalid] Expected value between 10 and 500." 2
}

$logDir = ".logs"
$logFile = Join-Path $logDir "format.log"

if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

$argsList = New-Object System.Collections.Generic.List[string]

$argsList.Add("format")

if (-not [string]::IsNullOrWhiteSpace($Project)) {
    $argsList.Add($Project)
}

$argsList.Add("--verify-no-changes")
$argsList.Add("--severity")
$argsList.Add($Severity)

if ($NoRestore) {
    $argsList.Add("--no-restore")
}

Add-MultiValueOption -ArgsList $argsList -OptionName "--include" -Values $Include
Add-MultiValueOption -ArgsList $argsList -OptionName "--exclude" -Values $Exclude
Add-MultiValueOption -ArgsList $argsList -OptionName "--diagnostics" -Values $Diagnostics
Add-MultiValueOption -ArgsList $argsList -OptionName "--exclude-diagnostics" -Values $ExcludeDiagnostics

if ($IncludeGenerated) {
    $argsList.Add("--include-generated")
}

foreach ($arg in $DotnetArgs) {
    $argsList.Add($arg)
}

$rawOutput = & dotnet @argsList 2>&1
$exitCode = $LASTEXITCODE
$output = @($rawOutput | ForEach-Object { $_.ToString() })

$output | Out-File -FilePath $logFile -Encoding utf8

if ($exitCode -eq 0) {
    Write-Output "[FormatSuccess]"
    exit 0
}

Write-Output "[FormatFailed]"

$minimalOutput = Get-MinimalFormatOutput $output

if ($minimalOutput.Count -eq 0) {
    Write-Output "dotnet format exited with code $exitCode."
}
else {
    $minimalOutput |
        Select-Object -First $MaxFailureLines |
        ForEach-Object { Write-Output $_ }
}

Write-Output "[FullLog: $logFile]"

exit $exitCode

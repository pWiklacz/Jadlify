param(
    [switch]$Help,

    [string]$Project,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$Framework,

    [string]$Runtime,

    [switch]$NoRestore,

    [switch]$NoBuild,

    [string]$Filter,

    [string]$Name,

    [string]$Class,

    [string]$FullyQualifiedNameContains,

    [string[]]$Trait = @(),

    [int]$MaxFailureLines = 100,

    [int]$TimeoutSeconds = 300,

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
test-min.ps1 - minimal dotnet test output for agents

Usage:
  pwsh ./.scripts/test-min.ps1
  pwsh ./.scripts/test-min.ps1 -Project ./phrAIse.Api.Tests/phrAIse.Api.Tests.csproj
  pwsh ./.scripts/test-min.ps1 -Project ./phrAIse.Api.Tests/phrAIse.Api.Tests.csproj -FullyQualifiedNameContains Healthz
  pwsh ./.scripts/test-min.ps1 -NoRestore -NoBuild

Output:
  [TestSuccess Total=N] on success.
  [TestFailed Failed=N Passed=N Total=N] plus minimal failed test/build diagnostics on failure.
  [TestNoMatches Filter='...'] when a filter runs zero tests.
  Full raw output is saved to .logs/test.log.

Options:
  -Project <path>                     Test a .sln, .slnx, .csproj, .fsproj or .vbproj path.
  -Configuration Debug|Release
  -Framework <tfm>
  -Runtime <rid>
  -NoRestore
  -NoBuild                           Skip build only after a fresh successful build.
  -Filter <vstest-filter>
  -Name <text>                       Shortcut for FullyQualifiedName~text.
  -Class <text>                      Shortcut for FullyQualifiedName~text.
  -FullyQualifiedNameContains <text> Shortcut for FullyQualifiedName~text.
  -Trait Key=Value
  -MaxFailureLines <10-500>
  -TimeoutSeconds <30-3600>           Hard timeout for the dotnet test process.
  -DotnetArgs <args[]>               Extra safe MSBuild args. Prefer /p:Name=value syntax in PowerShell.

Do not pass dotnet commands or script-controlled options through -DotnetArgs:
  build, test, restore, publish, pack, run, clean
  -c, --configuration, -f, --framework, -r, --runtime, --filter, --no-restore, --no-build, --logger, --verbosity, --nologo, -m, --maxcpucount
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

    if ($Value.Length -gt 500) {
        Write-And-Exit "[$ParamName Invalid] Value is too long." 2
    }
}

function Assert-TraitFormat {
    param([string[]]$Traits)

    foreach ($traitValue in $Traits) {
        if ([string]::IsNullOrWhiteSpace($traitValue)) {
            continue
        }

        if ($traitValue -notmatch "^[A-Za-z0-9_.-]+=[^=]+$") {
            Write-And-Exit "[TraitInvalid] Expected format: Key=Value. Got: $traitValue" 2
        }
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
        "--logger",
        "-c",
        "--configuration",
        "-f",
        "--framework",
        "-r",
        "--runtime",
        "--filter",
        "--no-restore",
        "--no-build",
        "-m",
        "--maxcpucount",
        "/m",
        "/maxcpucount",
        "/nr"
    )

    foreach ($arg in $ExtraArgs) {
        if ([string]::IsNullOrWhiteSpace($arg)) {
            continue
        }

        $trimmed = $arg.Trim()

        if ($blocked -contains $trimmed.ToLowerInvariant()) {
            Write-And-Exit "[TestArgsInvalid] Do not pass dotnet command '$trimmed' through -DotnetArgs." 2
        }

        foreach ($blockedOption in $blockedOptions) {
            if (
                $trimmed.Equals($blockedOption, [System.StringComparison]::OrdinalIgnoreCase) -or
                $trimmed.StartsWith("$blockedOption=", [System.StringComparison]::OrdinalIgnoreCase) -or
                $trimmed.StartsWith("$blockedOption`:", [System.StringComparison]::OrdinalIgnoreCase)
            ) {
                Write-And-Exit "[TestArgsInvalid] Option '$trimmed' is controlled by the script. Use the dedicated script parameter instead." 2
            }
        }
    }
}

function Join-TestFilters {
    param(
        [string]$RawFilter,
        [string]$TestName,
        [string]$ClassName,
        [string]$FqnContains,
        [string[]]$Traits
    )

    $parts = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($RawFilter)) {
        $parts.Add("($RawFilter)")
    }

    if (-not [string]::IsNullOrWhiteSpace($TestName)) {
        $parts.Add("FullyQualifiedName~$TestName")
    }

    if (-not [string]::IsNullOrWhiteSpace($ClassName)) {
        $parts.Add("FullyQualifiedName~$ClassName")
    }

    if (-not [string]::IsNullOrWhiteSpace($FqnContains)) {
        $parts.Add("FullyQualifiedName~$FqnContains")
    }

    foreach ($traitValue in $Traits) {
        if ([string]::IsNullOrWhiteSpace($traitValue)) {
            continue
        }

        $parts.Add($traitValue.Trim())
    }

    $uniqueParts = Get-UniquePreservingOrder $parts.ToArray()

    if ($uniqueParts.Count -eq 0) {
        return $null
    }

    return ($uniqueParts -join "&")
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

function Format-DiagnosticLine {
    param([string]$Line)

    if ([string]::IsNullOrWhiteSpace($Line)) {
        return $null
    }

    $clean = $Line.Trim()

    $xunitFailurePattern = '^\[xUnit\.net [^\]]+\]\s+(?<test>.+)\s+\[FAIL\]$'
    if ($clean -match $xunitFailurePattern) {
        return "[FailedTest] $($Matches["test"])"
    }

    $clean = $clean -replace '\s+\(https?://[^)]*\)', ''
    $clean = $clean -replace '\s+\[[^\]]+\]\s*$', ''

    if (
        $clean -match '^Build (FAILED|succeeded)\.$' -or
        $clean -match '^Test Run (Failed|Successful)\.$' -or
        $clean -match '^\d+\s+(Warning|Error)\(s\)$' -or
        $clean -match '^Time Elapsed\s+' -or
        $clean -match '^Total time:\s+' -or
        $clean -match '^Total tests:\s+' -or
        $clean -match '^\s*(Passed|Failed|Skipped):\s+\d+$'
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

    if ($clean -match 'No test matches|No test is available') {
        return $clean
    }

    return $null
}

function Get-TestSummary {
    param([string[]]$Lines)

    $summary = $null

    foreach ($line in $Lines) {
        if (
            $line -match '(?<outcome>Passed|Failed)!\s+-\s+Failed:\s*(?<failed>\d+),\s*Passed:\s*(?<passed>\d+),\s*Skipped:\s*(?<skipped>\d+),\s*Total:\s*(?<total>\d+)'
        ) {
            $summary = [pscustomobject]@{
                Outcome = $Matches["outcome"]
                Failed = [int]$Matches["failed"]
                Passed = [int]$Matches["passed"]
                Skipped = [int]$Matches["skipped"]
                Total = [int]$Matches["total"]
            }
        }
    }

    return $summary
}

function Get-MinimalFailureOutput {
    param([string[]]$Lines)

    $result = New-Object System.Collections.Generic.List[string]

    foreach ($line in $Lines) {
        $formatted = Format-DiagnosticLine $line

        if (-not [string]::IsNullOrWhiteSpace($formatted)) {
            $result.Add($formatted)
        }
    }

    return $result.ToArray()
}

function Write-TestSuccess {
    param(
        [object]$Summary,
        [string]$EffectiveFilter
    )

    if (-not [string]::IsNullOrWhiteSpace($EffectiveFilter)) {
        Write-Output "[TestSuccess Filter='$EffectiveFilter' Total=$($Summary.Total)]"
        return
    }

    Write-Output "[TestSuccess Total=$($Summary.Total)]"
}

function Write-TestFailure {
    param(
        [object]$Summary,
        [string]$EffectiveFilter
    )

    if ($null -eq $Summary) {
        Write-Output "[TestFailed]"
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($EffectiveFilter)) {
        Write-Output "[TestFailed Filter='$EffectiveFilter' Failed=$($Summary.Failed) Passed=$($Summary.Passed) Total=$($Summary.Total)]"
        return
    }

    Write-Output "[TestFailed Failed=$($Summary.Failed) Passed=$($Summary.Passed) Total=$($Summary.Total)]"
}

function ConvertTo-ProcessArgument {
    param([string]$Argument)

    if ($null -eq $Argument) {
        return '""'
    }

    if ($Argument -notmatch '[\s"]') {
        return $Argument
    }

    return '"' + ($Argument -replace '"', '\"') + '"'
}

function Invoke-DotnetBuildServerShutdown {
    param([string]$LogDirectory)

    $shutdownOut = Join-Path $LogDirectory "build-server-shutdown.out.log"
    $shutdownErr = Join-Path $LogDirectory "build-server-shutdown.err.log"

    Remove-Item -LiteralPath $shutdownOut -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $shutdownErr -Force -ErrorAction SilentlyContinue

    try {
        $shutdown = Start-Process `
            -FilePath "dotnet" `
            -ArgumentList @("build-server", "shutdown") `
            -PassThru `
            -WindowStyle Hidden `
            -RedirectStandardOutput $shutdownOut `
            -RedirectStandardError $shutdownErr

        if (-not $shutdown.WaitForExit(30000)) {
            Stop-Process -Id $shutdown.Id -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        # Best effort cleanup only. The test result should still be reported.
    }
}

function Stop-NewDotnetProcesses {
    param([int[]]$ExistingProcessIds)

    $existing = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($processId in $ExistingProcessIds) {
        [void]$existing.Add($processId)
    }

    Get-Process dotnet -ErrorAction SilentlyContinue |
        Where-Object { -not $existing.Contains($_.Id) } |
        ForEach-Object {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        }
}

function Invoke-DotnetTest {
    param(
        [string[]]$ArgumentList,
        [string]$LogDirectory,
        [string]$LogPath,
        [int]$Timeout
    )

    $stdoutFile = Join-Path $LogDirectory "test.stdout.log"
    $stderrFile = Join-Path $LogDirectory "test.stderr.log"

    Remove-Item -LiteralPath $stdoutFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $stderrFile -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $LogPath -Force -ErrorAction SilentlyContinue

    $processArgs = @($ArgumentList | ForEach-Object { ConvertTo-ProcessArgument $_ })
    $existingDotnetProcessIds = @(Get-Process dotnet -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })

    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList $processArgs `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutFile `
        -RedirectStandardError $stderrFile

    $timedOut = -not $process.WaitForExit($Timeout * 1000)
    if ($timedOut) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        $exitCode = 124
    }
    else {
        $exitCode = $process.ExitCode
    }

    Invoke-DotnetBuildServerShutdown -LogDirectory $LogDirectory
    Stop-NewDotnetProcesses -ExistingProcessIds $existingDotnetProcessIds

    $stdout = @()
    $stderr = @()

    if (Test-Path $stdoutFile) {
        $stdout = @(Get-Content -Path $stdoutFile -ErrorAction SilentlyContinue | ForEach-Object { $_.ToString() })
    }

    if (Test-Path $stderrFile) {
        $stderr = @(Get-Content -Path $stderrFile -ErrorAction SilentlyContinue | ForEach-Object { $_.ToString() })
    }

    $combinedOutput = @($stdout + $stderr)
    $combinedOutput | Out-File -FilePath $LogPath -Encoding utf8

    return [pscustomobject]@{
        ExitCode = $exitCode
        TimedOut = $timedOut
        Output = $combinedOutput
    }
}

if ($Help) {
    Write-Help
    exit 0
}

Assert-ValidPath -PathValue $Project -ParamName "Project"

Assert-SafeText -Value $Filter -ParamName "Filter"
Assert-SafeText -Value $Name -ParamName "Name"
Assert-SafeText -Value $Class -ParamName "Class"
Assert-SafeText -Value $FullyQualifiedNameContains -ParamName "FullyQualifiedNameContains"

$Trait = Get-UniquePreservingOrder $Trait
Assert-TraitFormat $Trait

$DotnetArgs = Get-UniquePreservingOrder $DotnetArgs
Assert-SafeExtraArgs $DotnetArgs

if ($MaxFailureLines -lt 10 -or $MaxFailureLines -gt 500) {
    Write-And-Exit "[MaxFailureLinesInvalid] Expected value between 10 and 500." 2
}

if ($TimeoutSeconds -lt 30 -or $TimeoutSeconds -gt 3600) {
    Write-And-Exit "[TimeoutSecondsInvalid] Expected value between 30 and 3600." 2
}

$effectiveFilter = Join-TestFilters `
    -RawFilter $Filter `
    -TestName $Name `
    -ClassName $Class `
    -FqnContains $FullyQualifiedNameContains `
    -Traits $Trait

$logDir = ".logs"
$logFile = Join-Path $logDir "test.log"

if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

$argsList = New-Object System.Collections.Generic.List[string]

$argsList.Add("test")

if (-not [string]::IsNullOrWhiteSpace($Project)) {
    $argsList.Add($Project)
}

$argsList.Add("--nologo")
$argsList.Add("--configuration")
$argsList.Add($Configuration)
$argsList.Add("--logger")
$argsList.Add("console;verbosity=quiet")
$argsList.Add("--disable-build-servers")
$argsList.Add("/m:1")
$argsList.Add("/nr:false")

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

if ($NoBuild) {
    $argsList.Add("--no-build")
}

if (-not [string]::IsNullOrWhiteSpace($effectiveFilter)) {
    $argsList.Add("--filter")
    $argsList.Add($effectiveFilter)
}

foreach ($arg in $DotnetArgs) {
    $argsList.Add($arg)
}

$testRun = Invoke-DotnetTest `
    -ArgumentList $argsList.ToArray() `
    -LogDirectory $logDir `
    -LogPath $logFile `
    -Timeout $TimeoutSeconds

$exitCode = $testRun.ExitCode
$output = @($testRun.Output)

$summary = Get-TestSummary $output

if ($testRun.TimedOut) {
    Write-Output "[TestTimedOut Seconds=$TimeoutSeconds]"

    Get-MinimalFailureOutput $output |
        Select-Object -First $MaxFailureLines |
        ForEach-Object { Write-Output $_ }

    Write-Output "[FullLog: $logFile]"
    exit $exitCode
}

if ($exitCode -eq 0 -and $null -ne $summary -and $summary.Total -gt 0) {
    Write-TestSuccess -Summary $summary -EffectiveFilter $effectiveFilter
    exit 0
}

if ($exitCode -eq 0 -and -not [string]::IsNullOrWhiteSpace($effectiveFilter)) {
    Write-Output "[TestNoMatches Filter='$effectiveFilter']"
    Write-Output "[FullLog: $logFile]"
    exit 1
}

if ($exitCode -eq 0) {
    Write-Output "[TestNoTests]"
    Write-Output "[FullLog: $logFile]"
    exit 1
}

Write-TestFailure -Summary $summary -EffectiveFilter $effectiveFilter

Get-MinimalFailureOutput $output |
    Select-Object -First $MaxFailureLines |
    ForEach-Object { Write-Output $_ }

Write-Output "[FullLog: $logFile]"

exit $exitCode

param(
    [switch]$Help,

    [string]$BuildProject,

    [string]$TestProject,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$Framework,

    [string]$Runtime,

    [string]$TestFilter,

    [string]$TestName,

    [string]$TestClass,

    [string]$TestFullyQualifiedNameContains,

    [string[]]$TestTrait = @(),

    [switch]$TreatWarningsAsErrors,

    [int]$MaxFailureLines = 100
)

$ErrorActionPreference = "Stop"

function Write-Help {
    Write-Output @"
verify-min.ps1 - minimal build plus test verification for agents

Usage:
  pwsh ./.scripts/verify-min.ps1
  pwsh ./.scripts/verify-min.ps1 -BuildProject ./phrAIse.Api/phrAIse.Api.csproj -TestProject ./phrAIse.Api.Tests/phrAIse.Api.Tests.csproj
  pwsh ./.scripts/verify-min.ps1 -TestFullyQualifiedNameContains Healthz

Output:
  Runs build-min.ps1 first, then test-min.ps1 with -NoRestore -NoBuild.
  Stops after the first failure.
  Prints [VerifySuccess] only when both build and tests succeed.
  Full raw outputs are saved to .logs/build.log and .logs/test.log.

Options:
  -BuildProject <path>
  -TestProject <path>
  -Configuration Debug|Release
  -Framework <tfm>
  -Runtime <rid>
  -TestFilter <vstest-filter>
  -TestName <text>
  -TestClass <text>
  -TestFullyQualifiedNameContains <text>
  -TestTrait Key=Value
  -TreatWarningsAsErrors
  -MaxFailureLines <10-500>

Run these scripts sequentially. They share .logs files and integration tests may share local resources.
"@
}

if ($Help) {
    Write-Help
    exit 0
}

$buildScript = Join-Path $PSScriptRoot "build-min.ps1"
$testScript = Join-Path $PSScriptRoot "test-min.ps1"

if (-not (Test-Path $buildScript)) {
    Write-Output "[VerifyInvalid] Missing script: $buildScript"
    exit 2
}

if (-not (Test-Path $testScript)) {
    Write-Output "[VerifyInvalid] Missing script: $testScript"
    exit 2
}

$buildArgs = New-Object System.Collections.Generic.List[string]

$buildArgs.Add("-Configuration")
$buildArgs.Add($Configuration)

if (-not [string]::IsNullOrWhiteSpace($BuildProject)) {
    $buildArgs.Add("-Project")
    $buildArgs.Add($BuildProject)
}

if (-not [string]::IsNullOrWhiteSpace($Framework)) {
    $buildArgs.Add("-Framework")
    $buildArgs.Add($Framework)
}

if (-not [string]::IsNullOrWhiteSpace($Runtime)) {
    $buildArgs.Add("-Runtime")
    $buildArgs.Add($Runtime)
}

if ($TreatWarningsAsErrors) {
    $buildArgs.Add("-TreatWarningsAsErrors")
}

& pwsh $buildScript @buildArgs
$buildExitCode = $LASTEXITCODE

if ($buildExitCode -ne 0) {
    exit $buildExitCode
}

$testArgs = New-Object System.Collections.Generic.List[string]

$testArgs.Add("-Configuration")
$testArgs.Add($Configuration)
$testArgs.Add("-NoRestore")
$testArgs.Add("-NoBuild")
$testArgs.Add("-MaxFailureLines")
$testArgs.Add($MaxFailureLines.ToString())

if (-not [string]::IsNullOrWhiteSpace($TestProject)) {
    $testArgs.Add("-Project")
    $testArgs.Add($TestProject)
}

if (-not [string]::IsNullOrWhiteSpace($Framework)) {
    $testArgs.Add("-Framework")
    $testArgs.Add($Framework)
}

if (-not [string]::IsNullOrWhiteSpace($Runtime)) {
    $testArgs.Add("-Runtime")
    $testArgs.Add($Runtime)
}

if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $testArgs.Add("-Filter")
    $testArgs.Add($TestFilter)
}

if (-not [string]::IsNullOrWhiteSpace($TestName)) {
    $testArgs.Add("-Name")
    $testArgs.Add($TestName)
}

if (-not [string]::IsNullOrWhiteSpace($TestClass)) {
    $testArgs.Add("-Class")
    $testArgs.Add($TestClass)
}

if (-not [string]::IsNullOrWhiteSpace($TestFullyQualifiedNameContains)) {
    $testArgs.Add("-FullyQualifiedNameContains")
    $testArgs.Add($TestFullyQualifiedNameContains)
}

foreach ($traitValue in $TestTrait) {
    if (-not [string]::IsNullOrWhiteSpace($traitValue)) {
        $testArgs.Add("-Trait")
        $testArgs.Add($traitValue)
    }
}

& pwsh $testScript @testArgs
$testExitCode = $LASTEXITCODE

if ($testExitCode -eq 0) {
    Write-Output "[VerifySuccess]"
}

exit $testExitCode

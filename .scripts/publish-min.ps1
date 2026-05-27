[CmdletBinding()]
param(
    [string]$Bucket,

    [string]$Endpoint,

    [string]$ReleaseDir = "$PSScriptRoot/../releases",

    [string]$Region = "auto",

    [int]$KeepMaxReleases = 5,

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
publish-min.ps1 - upload Velopack release artifacts to Cloudflare R2 (S3 API)

Prerequisite:
  The 'vpk' global tool must be installed:
    dotnet tool install --global Velopack.Vpk
  This script DOES NOT auto-install vpk.

Required environment variables:
  AWS_ACCESS_KEY_ID      R2 access key id
  AWS_SECRET_ACCESS_KEY  R2 secret access key
  Both are mapped to Velopack's VPK_KEY_ID / VPK_SECRET for the child process.
  Credentials are NEVER passed on the command line.

Usage:
  pwsh ./.scripts/publish-min.ps1 -Bucket phraise-dev -Endpoint https://<ACCOUNT_ID>.r2.cloudflarestorage.com
  pwsh ./.scripts/publish-min.ps1 -Bucket phraise-dev -Endpoint https://<ACCOUNT_ID>.r2.cloudflarestorage.com -KeepMaxReleases 10

Output:
  [PublishSuccess] on success.
  [PublishFailed]  with one diagnostic line on failure.
  Full raw output is appended to .logs/publish-min.log.

Options:
  -Bucket <name>             R2 bucket name (mandatory)
  -Endpoint <url>            S3 API endpoint (mandatory; e.g. https://<ACCOUNT_ID>.r2.cloudflarestorage.com)
  -ReleaseDir <path>         Local Velopack output dir; default ./releases
  -Region <name>             retained for script compatibility; not passed when -Endpoint is used
  -KeepMaxReleases <n>       prune older releases on the bucket; default 5
'@
}

if ($Help) {
    Write-Help
    exit 0
}

if ([string]::IsNullOrWhiteSpace($Bucket)) {
    Write-And-Exit "[PublishFailed] -Bucket is required." 2
}

if ([string]::IsNullOrWhiteSpace($Endpoint)) {
    Write-And-Exit "[PublishFailed] -Endpoint is required." 2
}

$repoRoot = Resolve-Path "$PSScriptRoot/.."
$logDir = Join-Path $repoRoot ".logs"
$logFile = Join-Path $logDir "publish-min.log"

if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

Set-Content -Path $logFile -Value "" -Encoding utf8

function Append-Log {
    param([string[]]$Lines)

    if ($null -ne $Lines) {
        $Lines | Out-File -FilePath $logFile -Append -Encoding utf8
    }
}

# 1. Verify vpk on PATH.
$vpkCommand = Get-Command vpk -ErrorAction SilentlyContinue
if ($null -eq $vpkCommand) {
    Append-Log @("vpk: not found on PATH")
    Write-Output "[PublishFailed] 'vpk' global tool is not on PATH."
    Write-Output "Install it with: dotnet tool install --global Velopack.Vpk"
    Write-Output "[FullLog: $logFile]"
    exit 2
}

$vpk = & vpk --help 2>&1
$vpkExit = $LASTEXITCODE
Append-Log @("vpk --help (exit=$vpkExit):", "$vpk")
if ($vpkExit -ne 0) {
    Write-Output "[PublishFailed] 'vpk --help' failed with exit code $vpkExit."
    Write-Output "[FullLog: $logFile]"
    exit 2
}

# 2. Read AWS-style credentials from the environment.
$accessKey = $env:AWS_ACCESS_KEY_ID
$secretKey = $env:AWS_SECRET_ACCESS_KEY

if ([string]::IsNullOrWhiteSpace($accessKey)) {
    Write-And-Exit "[PublishFailed] AWS_ACCESS_KEY_ID is not set; cannot authenticate to R2." 2
}

if ([string]::IsNullOrWhiteSpace($secretKey)) {
    Write-And-Exit "[PublishFailed] AWS_SECRET_ACCESS_KEY is not set; cannot authenticate to R2." 2
}

if (-not (Test-Path $ReleaseDir)) {
    Write-And-Exit "[PublishFailed] Release directory does not exist: $ReleaseDir" 2
}

# 3. Map AWS_* env vars to VPK_* for the child process and run upload.
# Credentials are NOT passed as command-line arguments.
$previousVpkKeyId = $env:VPK_KEY_ID
$previousVpkSecret = $env:VPK_SECRET

try {
    $env:VPK_KEY_ID = $accessKey
    $env:VPK_SECRET = $secretKey

    Append-Log @("--- vpk upload s3 (bucket=$Bucket, endpoint=$Endpoint, keepMaxReleases=$KeepMaxReleases) ---")
    $uploadOutput = & vpk upload s3 `
        --bucket $Bucket `
        --endpoint $Endpoint `
        --keepMaxReleases $KeepMaxReleases `
        --outputDir $ReleaseDir 2>&1
    $uploadExit = $LASTEXITCODE
    Append-Log $uploadOutput
}
finally {
    $env:VPK_KEY_ID = $previousVpkKeyId
    $env:VPK_SECRET = $previousVpkSecret
}

if ($uploadExit -ne 0) {
    Write-Output "[PublishFailed] vpk upload s3 exited with code $uploadExit."
    Write-Output "[FullLog: $logFile]"
    exit $uploadExit
}

Write-Output "[PublishSuccess] $Bucket"
exit 0

# Jadlify Deployment Runbook

Prepared at: 2026-05-24

This runbook records the current Jadlify Azure App Service target and the operational path for future deployments. It is not a provisioning plan.

## Current Azure Target

- Resource group: `rg-jadlify-dev`
- App Service plan: `asp-jadlify-f1`
- Web App: `jadlify-mvp-wikla`
- Region: `West Europe`
- Tier: `Free`
- Runtime: `dotnet:10`
- Public URL: `https://jadlify-mvp-wikla.azurewebsites.net`
- API project: `src/Jadlify.API/Jadlify.API.csproj`
- Publish folder: `.artifacts/publish/jadlify`
- ZIP package: `.artifacts/jadlify.zip`

The target is an MVP/course deployment on Azure App Service F1. F1 has no deployment slots and no Always On, so cold starts and direct production deploys are expected constraints.

## Default Future Deployment Path

Use GitHub Actions from `main` once the GitHub repository has the publish profile secret:

- Secret name: `AZURE_WEBAPP_PUBLISH_PROFILE`
- Secret value: publish profile downloaded from Azure for `jadlify-mvp-wikla`
- Expected workflow behavior: restore, build, test, publish `src/Jadlify.API/Jadlify.API.csproj`, then deploy with `azure/webapps-deploy`

Before relying on the workflow, confirm the secret exists and that deploying on every successful `main` build is still acceptable for the current branch model.

## Required Environment Preflight

Run these commands before Azure CLI, GitHub, or other network-dependent deployment commands in this Windows environment:

```powershell
Remove-Item Env:HTTP_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:HTTPS_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:ALL_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:GIT_HTTP_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:GIT_HTTPS_PROXY -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Force .artifacts/azure-cli | Out-Null
$env:AZURE_CONFIG_DIR = (Resolve-Path .artifacts/azure-cli).Path

New-Item -ItemType Directory -Force .tmp-msbuild | Out-Null
$env:TEMP = (Resolve-Path .tmp-msbuild).Path
$env:TMP = $env:TEMP

az account show -o table
```

If `az account show` fails because no user is logged in, run the login visibly in the foreground:

```powershell
az login --use-device-code
```

Do not run device-code login hidden in the background. The device code must be visible to the user.

## Manual ZIP Deployment Fallback

Use the manual path when GitHub Actions is not ready, the publish-profile secret is missing, or a one-off production smoke deploy is explicitly requested.

Verify locally:

```powershell
dotnet build Jadlify.slnx --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal
dotnet test Jadlify.slnx --no-restore -m:1 -p:UseSharedCompilation=false --verbosity minimal
```

Publish and package:

```powershell
dotnet publish src/Jadlify.API/Jadlify.API.csproj -c Release -o .artifacts/publish/jadlify

Compress-Archive `
  -Path .artifacts/publish/jadlify/* `
  -DestinationPath .artifacts/jadlify.zip `
  -Force
```

Deploy:

```powershell
az webapp deploy `
  --resource-group rg-jadlify-dev `
  --name jadlify-mvp-wikla `
  --src-path .artifacts/jadlify.zip `
  --type zip
```

## Smoke Test

Preferred endpoint:

```text
https://jadlify-mvp-wikla.azurewebsites.net/health
```

If PowerShell, `curl`, or `Invoke-WebRequest` fails on HTTPS with a Schannel error, prefer the Azure CLI bundled Python and `requests` instead of treating the app as broken:

```powershell
$azCommand = Get-Command az
$azRoot = Split-Path (Split-Path $azCommand.Source -Parent) -Parent
$azPython = Join-Path $azRoot "python.exe"
& $azPython -c "import requests; r = requests.get('https://jadlify-mvp-wikla.azurewebsites.net/health', timeout=30); print(r.status_code); print(r.text[:500]); r.raise_for_status()"
```

Log check:

```powershell
az webapp log tail --resource-group rg-jadlify-dev --name jadlify-mvp-wikla
```

Success criteria:

- `/health` returns HTTP 200.
- App Service starts without runtime errors.
- Logs do not show startup exceptions.

## Rollback

Azure App Service F1 has no deployment slots. Rollback means redeploying a known-good ZIP artifact:

```powershell
az webapp deploy `
  --resource-group rg-jadlify-dev `
  --name jadlify-mvp-wikla `
  --src-path .artifacts/jadlify-known-good.zip `
  --type zip
```

Keep known-good ZIPs under `.artifacts/` or another local artifact store. Do not write deployment artifacts to `context/archive/`.

## One-Time Follow-Ups

- Add the GitHub repository secret `AZURE_WEBAPP_PUBLISH_PROFILE`.
- Optionally force HTTPS only:

```powershell
az webapp update `
  --resource-group rg-jadlify-dev `
  --name jadlify-mvp-wikla `
  --set httpsOnly=true
```

## Safety Boundaries

- Do not delete Azure resources without explicit user confirmation.
- Do not rotate secrets without explicit user confirmation.
- Do not upgrade tiers or change billing-affecting resources without explicit user confirmation.
- Keep writes out of `context/archive/`; archived changes are immutable.

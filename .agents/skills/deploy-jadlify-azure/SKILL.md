---
name: deploy-jadlify-azure
description: >
  Deploy Jadlify to Azure App Service and operate future production smoke
  deployments. Use when the user asks to deploy Jadlify, run an Azure App
  Service deploy, update or follow context/deployment/deploy-plan.md, says
  "wdroz Jadlify", or asks for future production smoke deploys.
---

# Deploy Jadlify Azure

This repo-local skill is specific to the Jadlify Azure App Service target. Load it only for deployment work.

## Required First Step

Read `context/deployment/deploy-plan.md` before running deployment commands. Treat it as the current deployment contract for resource names, artifact paths, smoke tests, rollback, and environment preflight.

## Deployment Path

- Prefer the GitHub Actions path once `AZURE_WEBAPP_PUBLISH_PROFILE` exists in the GitHub repository secrets.
- Use the manual ZIP deployment path when the secret is missing, GitHub Actions is not ready, or the user explicitly asks for a manual deploy.
- Do not run an actual deployment when the user only asks to update documentation, plans, or this skill.

## Environment Rules

Always clear broken proxy variables before Azure, GitHub, or other network commands in this environment:

```powershell
Remove-Item Env:HTTP_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:HTTPS_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:ALL_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:GIT_HTTP_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:GIT_HTTPS_PROXY -ErrorAction SilentlyContinue
```

Always set Azure CLI state to the repo-local directory:

```powershell
New-Item -ItemType Directory -Force .artifacts/azure-cli | Out-Null
$env:AZURE_CONFIG_DIR = (Resolve-Path .artifacts/azure-cli).Path
```

Use repo-local temp directories for .NET build, test, and publish commands when running in this Windows sandbox:

```powershell
New-Item -ItemType Directory -Force .tmp-msbuild | Out-Null
$env:TEMP = (Resolve-Path .tmp-msbuild).Path
$env:TMP = $env:TEMP
```

Verify the Azure account before deployment:

```powershell
az account show -o table
```

If login is needed, run it visibly:

```powershell
az login --use-device-code
```

Never run `az login --use-device-code` hidden in the background. The device code must be visible to the user.

## Smoke Test Rule

Smoke-test `https://jadlify-mvp-wikla.azurewebsites.net/health`. If PowerShell, `curl`, or `Invoke-WebRequest` fails with a Schannel HTTPS error, prefer Azure CLI bundled Python with `requests` before diagnosing the deployed app as broken.

## Safety Boundaries

Never delete Azure resources, rotate secrets, upgrade tiers, or change billing-affecting resources without explicit user confirmation. Keep writes out of `context/archive/`; archived changes are immutable.

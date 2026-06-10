Push-Location src/Jadlify.Web
$output = npx tsc -p tsconfig.app.json 2>&1
$ec = $LASTEXITCODE
Pop-Location
$output | ForEach-Object { [Console]::Error.WriteLine($_) }
if ($ec -ne 0) { exit 2 }
exit 0

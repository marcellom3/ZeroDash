# Compila o ZeroDash em dist\ZeroDash\ (pasta fixa, sem versão; requer .NET 8 SDK).
# O "Iniciar com Windows" é feito pelo próprio app (clique direito no ícone da bandeja).
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $root "src\ZeroDash\ZeroDash.csproj"
[xml]$x = Get-Content $proj
$ver = @($x.Project.PropertyGroup.Version)[0]
$out = Join-Path $root "dist\ZeroDash"   # pasta fixa (sem versão): o autostart aponta para ela
if (Get-Process ZeroDash -ErrorAction SilentlyContinue) {
    Write-Host "O ZeroDash está aberto. Feche-o (ícone da bandeja > Sair) e rode de novo." -ForegroundColor Yellow
    exit 1
}
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet publish $proj -c Release -o $out
if ($LASTEXITCODE -ne 0) { Write-Host "Falha na compilação." -ForegroundColor Red; exit $LASTEXITCODE }
Write-Host "`nZeroDash v$ver compilado em: $out" -ForegroundColor Green
Write-Host "Abra o ZeroDash.exe e ative 'Iniciar com Windows' no menu do ícone da bandeja."

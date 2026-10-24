@echo off
setlocal EnableExtensions
title ZeroDash - testar, enviar para dev e abrir Pull Request
pushd "%~dp0.." || exit /b 1
set "REPO=https://github.com/marcellom3/ZeroDash.git"
set "PRURL=https://github.com/marcellom3/ZeroDash/compare/main...dev?expand=1"

where git >nul 2>&1 || ( echo [ERRO] Git nao encontrado. Instale: https://git-scm.com/download/win & goto :fim )
where dotnet >nul 2>&1 || ( echo [ERRO] .NET 8 SDK nao encontrado. Instale: https://dotnet.microsoft.com/download/dotnet/8.0 & goto :fim )

echo.
echo [1/5] Preparando o repositorio Git (branch dev)...
if not exist ".git" (
  git init -b dev || goto :erro
  git remote add origin %REPO% || goto :erro
)
git config user.name "marcellom3"
git config user.email "41266454+marcellom3@users.noreply.github.com"
git fetch origin || goto :erro
git rev-parse --verify HEAD >nul 2>&1
if errorlevel 1 (
  git reset origin/dev >nul || goto :erro
  git branch --set-upstream-to=origin/dev dev >nul 2>&1
  git checkout -- docs/screenshots >nul 2>&1
)
git checkout dev >nul 2>&1

for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content 'src\ZeroDash\ZeroDash.csproj')).Project.PropertyGroup.Version | Select-Object -First 1"`) do set "VER=%%v"
echo.
echo [2/5] Compilando a versao %VER%...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if errorlevel 1 goto :erro
set "EXE=%CD%\dist\ZeroDash-v%VER%\ZeroDash.exe"
if not exist "%EXE%" ( echo [ERRO] Executavel nao encontrado: %EXE% & goto :erro )

echo.
echo [3/5] Abrindo o ZeroDash (confirme o UAC). Teste no ZeroMOD: skins, bandeja, FPS...
start "" "%EXE%"
echo.
echo Arquivos que serao enviados:
git status --short
echo.
choice /c SN /n /m "Funcionou? Fazer commit e push para a dev (S/N)? "
if errorlevel 2 ( echo Nada foi enviado. & goto :fim )

echo.
echo [4/5] Commit e push para a dev...
git add -A || goto :erro
git diff --cached --quiet || git commit -m "feat: skins e novo layout completo (v%VER%)" || goto :erro
git push -u origin dev || goto :erro

echo.
echo [5/5] Pull Request dev para main
choice /c SN /n /m "Abrir a pagina do Pull Request no navegador (S/N)? "
if errorlevel 2 goto :fim
start "" "%PRURL%"
echo Na pagina: confira os arquivos, clique em "Create pull request" e depois em "Merge pull request".
goto :fim

:erro
echo.
echo [ERRO] O passo anterior falhou. Copie as mensagens acima e envie para analise.

:fim
echo.
pause
popd
endlocal

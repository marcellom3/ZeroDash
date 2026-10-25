@echo off
setlocal EnableExtensions
title ZeroDash - testar, enviar para dev e abrir Pull Request
pushd "%~dp0.." || exit /b 1
set "REPO=https://github.com/marcellom3/ZeroDash.git"
set "PRURL=https://github.com/marcellom3/ZeroDash/compare/main...dev?expand=1"

where git >nul 2>&1 || ( echo [ERRO] Git nao encontrado. Instale: https://git-scm.com/download/win & goto :fim )
where dotnet >nul 2>&1 || ( echo [ERRO] .NET 8 SDK nao encontrado. Instale: https://dotnet.microsoft.com/download/dotnet/8.0 & goto :fim )

echo.
echo [1/6] Preparando o repositorio Git (branch dev)...
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
if exist "src\ZeroDash\skins" rmdir /s /q "src\ZeroDash\skins"
if exist "src\ZeroDash\temas\dourado.html" del /q "src\ZeroDash\temas\dourado.html"

for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content 'src\ZeroDash\ZeroDash.csproj')).Project.PropertyGroup.Version | Select-Object -First 1"`) do set "VER=%%v"
echo.
echo [2/6] Compilando a versao %VER%...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if errorlevel 1 goto :erro
set "EXE=%CD%\dist\ZeroDash\ZeroDash.exe"
if not exist "%EXE%" ( echo [ERRO] Executavel nao encontrado: %EXE% & goto :erro )

echo.
echo [3/6] Abrindo o ZeroDash (confirme o UAC). Teste no ZeroMOD: temas, bandeja, FPS...
start "" "%EXE%"
echo.
echo Arquivos que serao enviados:
git status --short
echo.
choice /c SN /n /m "Funcionou? Fazer commit e push para a dev (S/N)? "
if errorlevel 2 ( echo Nada foi enviado. & goto :fim )

echo.
echo [4/6] Commit e push para a dev...
git add -A || goto :erro
git diff --cached --quiet || git commit -m "feat: temas de cor, novo icone e layout completo (v%VER%)" || goto :erro
git push -u origin dev || goto :erro

echo.
echo [5/6] Pull Request dev para main
choice /c SN /n /m "Abrir a pagina do Pull Request no navegador (S/N)? "
if errorlevel 2 goto :fim
start "" "%PRURL%"
echo Na pagina: confira os arquivos, clique em "Create pull request" e depois em "Merge pull request".

echo.
echo [6/6] Tag v%VER% na main
choice /c SN /n /m "O Pull Request ja foi mergeado na main? Criar a tag v%VER% (S/N)? "
if errorlevel 2 ( echo Tag nao criada. Rode este .bat de novo depois do merge. & goto :fim )
git fetch origin --tags --force || goto :erro
git diff --quiet origin/main origin/dev || ( echo [AVISO] A main ainda nao tem o mesmo conteudo da dev. Conclua o merge e rode de novo. & goto :fim )
git ls-remote --exit-code --tags origin refs/tags/v%VER% >nul 2>&1
if not errorlevel 1 (
  echo A tag v%VER% ja existe no GitHub.
  choice /c SN /n /m "Mover a tag v%VER% para o commit atual da main (S/N)? "
  if errorlevel 2 goto :fim
  git push origin :refs/tags/v%VER% || goto :erro
  git tag -d v%VER% >nul 2>&1
)
git tag -a v%VER% -m "ZeroDash v%VER%" origin/main || goto :erro
git push origin v%VER% || goto :erro
echo Tag v%VER% enviada. O GitHub Actions vai gerar a release com o .zip.
goto :fim

:erro
echo.
echo [ERRO] O passo anterior falhou. Copie as mensagens acima e envie para analise.

:fim
echo.
pause
popd
endlocal

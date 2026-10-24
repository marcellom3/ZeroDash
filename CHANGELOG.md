# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

## [1.0.0] - 2026-10-24
### Adicionado
- Primeira versão estável do ZeroDash (antes chamado ZeroModDash).
- Coletor de sensores com LibreHardwareMonitorLib: CPU, GPU, placa-mãe/fans, memória (Windows), discos.
- FPS via memória compartilhada do RTSS.
- Janela WebView2 sem borda, encaixada automaticamente no monitor 1920x480 (ZeroMOD); oculta se ele não estiver conectado.
- Layout padrão preto e dourado; layout alternativo preto/cinza/branco em `layouts/black.html`.
- Ícone na bandeja do sistema com menu: nome/versão, Iniciar com Windows (liga/desliga), Recarregar e Sair; ícone embutido no executável.
- Instância única, `error.log` para falhas, autostart por tarefa agendada (administrador, sem UAC) controlado pelo próprio app.
- Endpoints locais `/` e `/api/metrics` (somente 127.0.0.1).

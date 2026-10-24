# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

## [1.0.1] - 2026-10-25
### Adicionado
- Menu **Skins** na bandeja (Dourado [completo], Dourado [clássico], Preto e Cinza [clássico]): troca de skin em tempo real, com a escolha salva; skins extras podem ser colocadas na pasta `skins` ao lado do exe.
- Nova skin padrão **Dourado [completo]** em pt-BR, com 10 blocos e histórico de FPS com destaque para o valor em tempo real.
- Novas métricas na API: potência da CPU e da GPU, nomes de CPU/GPU, RAM livre/total, tipo e velocidade da RAM, uptime e sistema operacional.
### Alterado
- Os layouts passaram a ser skins (`wwwroot/index.html` e `layouts/` foram substituídos por `src/ZeroDash/skins/`).
- Tipo/velocidade da RAM via WMI (`System.Management`, já incluído pela LibreHardwareMonitorLib).

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

# Changelog

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e versionamento [SemVer](https://semver.org/lang/pt-BR/).

## [1.0.2] - 2026-10-25
### Adicionado
- Menu **Temas** na bandeja (antes "Skins"), com troca em tempo real e escolha salva: Âmbar (padrão), Ciano, Lima, Rosa e Turquesa. Temas extras podem ser colocados na pasta `temas` ao lado do exe.
- O ícone (`.ico`) passa a ser gerado automaticamente a cada compilação a partir de `assets/icons/zerodash.png`.
### Alterado
- O painel completo é o único layout; as skins "Dourado [clássico]" e "Preto e Cinza [clássico]" e o tema Dourado provisório foram removidos. Os temas mudam apenas as cores, mantendo degradês e efeitos.

## [1.0.1] - 2026-10-25
### Adicionado
- Painel completo em pt-BR (CPU, GPU, RAM, armazenamento, ventoinhas, data/hora, FPS, temperatura, energia e informações do sistema), com ícones nos cabeçalhos e legendas Usado/Livre/Total.
- Menu **Skins** na bandeja, com troca em tempo real e escolha salva.
- Novas métricas na API: potência da CPU e da GPU, nomes de CPU/GPU, RAM livre/total, tipo e velocidade da RAM, uptime e sistema operacional.
### Alterado
- Pasta de saída da compilação fixa (`dist\ZeroDash`, sem a versão no nome), para o autostart não quebrar ao atualizar; o `.zip` da release continua levando a versão no nome.
- Os layouts passaram a ser skins (`wwwroot/index.html` e `layouts/` foram substituídos por `src/ZeroDash/skins/`).

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

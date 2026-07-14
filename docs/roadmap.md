# Roadmap

Este roadmap organiza as proximas evolucoes do GhostTrace. Ele descreve direcao e
prioridade, nao uma garantia de data ou de escopo fechado. A seguranca forense e a
compatibilidade com Windows continuam sendo criterios de aceite para qualquer item.

## Principios

- Coleta primeiro: modulos devem preservar evidencia e reportar limites de cobertura.
- Nenhuma acao destrutiva sem opt-in explicito, confirmacao e log auditavel.
- Saidas devem ser reproduziveis, locais e uteis para analise offline.
- Mudancas de comportamento precisam de testes e passar pelo gate de PR.

## Proxima entrega: confiabilidade e testes

Prioridade: alta.

- Implementar suites reais em `GhostTrace.Tests.Integration` para validar fluxos CLI,
  relatorios e pipeline em ambiente Windows controlado.
- Implementar `GhostTrace.Tests.ForensicSafety` para exercitar as protecoes da limpeza:
  caminhos fora da allowlist, junctions/symlinks, arquivos trocados entre coleta e
  remocao e valores de Registro inesperados.
- Cobrir gravacao atomica e cancelamento de relatorios JSON, incluindo preservacao do
  arquivo anterior quando a escrita falhar ou for cancelada.
- Executar a publicacao self-contained e a construcao do MSI como validacao de release
  em uma etapa separada da CI de PR.

## Depois: relatorios e operacao

Prioridade: media.

- Definir um contrato unico para exportacoes TXT, CSV e HTML, com escrita atomica,
  codificacao consistente e testes de erro de I/O.
- Adicionar metadados de cobertura ao relatorio: modulos executados, modulos ignorados,
  privilegios, erros por fonte e limites de enumeracao atingidos.
- Criar exemplos de relatorios anonimizados e perfis de coleta por tipo de investigacao.
- Melhorar o modo nao interativo com codigos de saida documentados e exemplos para
  automacao local/EDR.

## Evolucao forense

Prioridade: media, condicionada a datasets de teste representativos.

- Ampliar a cobertura de artefatos com parsers isolados e testes de formatos validos,
  corrompidos e legados.
- Evoluir a correlacao de tarefas agendadas para explicar evidencia ausente, conflito
  entre COM e TaskCache e grau de confianca do achado.
- Adicionar coleta opcional a partir de fontes offline, sem modificar a maquina alvo.
- Manter mapeamento MITRE ATT&CK e playbooks de interpretacao junto de cada modulo novo.

## Distribuicao e manutencao

Prioridade: continua.

- Fixar e revisar dependencias antes de cada release; migrar APIs prerelease somente
  com teste de compatibilidade da CLI.
- Publicar checksums e instrucoes de verificacao do MSI para cadeia de custodia.
- Avaliar assinatura de codigo para o instalador e binario distribuido.
- Medir cobertura de testes na CI e definir uma meta progressiva, sem bloquear a
  recuperacao de cobertura legada de uma so vez.

## Fora de escopo

- Remocao automatica de malware, limpeza em lote ou qualquer acao destrutiva sem
  revisao humana.
- Telemetria, upload de evidencia ou dependencia de servicos cloud para executar um scan.
- Declarar um artefato como prova conclusiva de execucao ou comprometimento sem contexto
  analitico adicional.

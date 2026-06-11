# Scheduled Tasks Correlation Playbook

## Objetivo

Tarefa agendada no Windows e igual barata depois da dedetizacao: se voce viu uma, provavelmente tem vestigio em mais lugar.

Este playbook existe para correlacionar achados de tarefas agendadas com **Task Scheduler**, **TaskCache\Tree**, **registro**, **persistencia vizinha** e **residuos deixados apos desinstalacao**.

Traduzindo: quando um software fala *"fui removido"* mas ainda deixou agendamento, cache, binding ou sujeira no registro, este guia ajuda a separar:

- tarefa legitima
- tarefa quebrada
- tarefa abandonada
- tarefa suspeita
- **Ghost Task**

Sem misticismo. So correlacao. Porque no Windows, se voce perguntar educadamente, ele mente. Se voce cruza artefato, ele entrega.

---

## Quando usar

Use este playbook quando:

- o `ScheduledTasksScanModule` encontrar tarefas relacionadas ao alvo investigado
- o `TaskCacheScanModule` apontar inconsistencias em `TaskCache\Tree`
- um software aparentemente desinstalado ainda deixar execucao recorrente
- houver suspeita de persistencia via tarefa agendada
- o nome, caminho, autor ou acao da tarefa parecer estranho demais pra ser coincidencia
- a tarefa nao aparecer direito na interface do Windows, mas aparecer no registro ou vice-versa

Se a sensacao for **"tem coisa rodando aqui que ja devia ter morrido"**, esse e o playbook certo.

---

## Fontes de evidencia correlacionadas

A ideia nao e confiar em um artefato sozinho. Um indicador isolado pode ser bug, resto de uninstall ou so mais uma invencao criativa do Windows.

Correlacione, no minimo, estas fontes:

### 1. ScheduledTasksScanModule

Use para identificar:

- nome da tarefa
- caminho logico da tarefa
- acao configurada
- trigger
- usuario/contexto
- estado visivel na infraestrutura do Task Scheduler

Pergunta-chave: **a tarefa existe como configuracao operacional visivel?**

### 2. TaskCacheScanModule

Use para validar residuos e anomalias em:

- `TaskCache\Tree`
- referencias quebradas
- entradas sem pareamento esperado
- caminhos de tarefa inconsistentes
- vestigios que continuam existindo mesmo quando a tarefa nao aparece como deveria

Pergunta-chave: **o registro ainda guarda o esqueleto da tarefa?**

### 3. UninstallEntriesScanModule

Use para verificar:

- se o software associado ainda esta oficialmente instalado
- publisher, versao e uninstall string
- nome de produto compativel com o nome da tarefa

Pergunta-chave: **a tarefa pertence a algo que ainda deveria existir?**

### 4. FileSystemTraceScanModule

Use para confirmar se o binario apontado pela tarefa:

- ainda existe em disco
- foi removido
- foi movido
- esta em pasta incomum
- ficou perdido em `ProgramData`, `AppData`, `%Temp%` ou outro canto suspeito

Pergunta-chave: **a acao da tarefa ainda aponta para algo real ou para fantasma com CRM?**

### 5. PersistenceScanModule / AsepScanModule / ServicesScanModule / WmiPersistenceScanModule

Use para descobrir se a tarefa faz parte de um conjunto maior de persistencia.

Pergunta-chave: **a tarefa esta sozinha ou tem comparsa?**

Se o mesmo nome, caminho, fornecedor ou binario aparece em mais de um mecanismo de persistencia, o caso sobe de categoria bem rapido.

---

## O que procurar

### Tarefa legitima

Sinais comuns:

- nome coerente com software conhecido
- binario existente e assinado ou esperado
- publisher compativel
- uninstall entry presente
- caminhos padrao em `Program Files` ou componentes do proprio Windows
- sem inconsistencias entre Task Scheduler e TaskCache

Resumo tecnico: existe, bate, faz sentido.

### Tarefa quebrada

Sinais comuns:

- tarefa ainda registrada
- binario removido ou caminho invalido
- acao apontando para executavel inexistente
- software relacionado ja nao esta instalado
- residuos de uninstall evidentes

Resumo tecnico: nao e necessariamente malicia. As vezes e so software mal educado largando prato na pia do sistema.

### Tarefa abandonada

Sinais comuns:

- nome remete a produto antigo
- vendor nao aparece mais nas entradas instaladas
- acao referencia diretorio vazio ou descontinuado
- trigger continua configurado, mas sem alvo viavel

Resumo tecnico: morto no marketing, vivo no registro.

### Tarefa suspeita

Sinais comuns:

- nome imitando componente legitimo com pequena variacao
- execucao em `AppData`, `%Temp%`, `C:\Users\Public`, caminhos ofuscados ou aleatorios
- argumentos estranhos, scripts inline, `powershell`, `cmd /c`, `wscript`, `mshta`, `rundll32`
- usuario incomum ou contexto inesperado
- combinacao com outros artefatos de persistencia

Resumo tecnico: ainda nao e sentenca, mas ja perdeu o beneficio da duvida.

### Ghost Task

Aqui mora a fofoca tecnica boa.

Em termos praticos, trate como **Ghost Task** quando existir forte sinal de resquicio ou inconsistencia estrutural entre o que o Windows deveria expor e o que o registro ainda mantem.

Sinais tipicos:

- entrada em `TaskCache\Tree` sem representacao operacional consistente
- referencia quebrada entre componentes esperados da tarefa
- nome/caminho presente no registro, mas nao visivel como tarefa normal
- residuos persistindo apos remocao parcial
- indicios de manipulacao para esconder ou corromper a representacao da tarefa

Resumo tecnico: a tarefa nao esta limpa o suficiente para sumir, nem integra o suficiente para existir direito.

---

## Fluxo de investigacao

Siga esta ordem para nao cair no golpe do artefato isolado.

### Etapa 1 — Identifique a tarefa

Comece pelo que o GhostTrace encontrou:

- nome
n- caminho
- acao
- trigger
- contexto de execucao

Pergunte:

- isso parece software legitimo?
- isso parece resquicio?
- isso parece persistencia?

### Etapa 2 — Valide no TaskCache

Cruze com os achados do `TaskCacheScanModule`.

Procure:

- entradas com nome equivalente
- chaves inconsistentes
- referencias quebradas
- vestigios sem par esperado

Se bater no cache mas nao bater na representacao operacional, ligue o alerta.

### Etapa 3 — Valide o binario ou script referenciado

Use o caminho da acao e confira se o alvo:

- existe
- ainda pertence ao software investigado
- esta no local esperado
- foi removido parcialmente
- parece improvisado ou suspeito

Uma tarefa apontando para arquivo inexistente costuma dizer duas coisas:

1. uninstall porco
2. persistencia mal resolvida

E as duas merecem documentacao.

### Etapa 4 — Correlacione com software instalado

Use `UninstallEntriesScanModule` para responder:

- o produto ainda consta como instalado?
- nome e publisher batem?
- a versao encontrada faz sentido?

Se a tarefa existe, mas o produto nao, isso fortalece a hipotese de residuo ou persistencia indevida.

### Etapa 5 — Veja se ha persistencia vizinha

Cruze com:

- `PersistenceScanModule`
- `AsepScanModule`
- `ServicesScanModule`
- `WmiPersistenceScanModule`

Se o mesmo binario, pasta, vendor ou familia de nome aparece em outros mecanismos, voce nao tem so uma tarefa. Voce tem ecossistema. E ecossistema no Windows raramente nasce sozinho.

### Etapa 6 — Classifique o achado

Classifique como uma destas categorias:

- legitima
- quebrada
- abandonada
- suspeita
- Ghost Task

O importante nao e parecer inteligente. E ser consistente. Se outro analista ler depois, ele precisa entender **por que** voce classificou assim.

---

## Sinais fortes vs sinais fracos

### Sinais fortes

Considere como sinais fortes:

- tarefa + TaskCache inconsistentes entre si
- acao apontando para artefato inexistente apos desinstalacao
- tarefa referenciando binario em local atipico e persistencia correlata em outro modulo
- residuos em registro sem correspondencia operacional limpa
- combinacao de task suspeita com PowerShell, WMI ou servicos relacionados

### Sinais fracos

Considere como sinais fracos isoladamente:

- nome estranho sozinho
- autor vazio sozinho
- trigger incomum sozinho
- tarefa antiga sem contexto adicional
- entradas de software mal padronizadas por vendor pequeno

No GhostTrace, a regra de ouro e simples:

> **um artefato grita. correlacao prova.**

---

## Falsos positivos e cuidados

Nem toda anomalia e malicia. As vezes e so software enterprise tendo um dia particularmente vergonhoso.

Cuidado com:

- atualizadores legitimos que deixam tarefas antigas
- suites de seguranca que usam nomes feios e caminhos pouco amigaveis
- instaladores que quebram remocao e deixam XML, cache ou registro pela metade
- tarefas desabilitadas que ainda existem por design
- software corporativo com scripts e wrappers pouco elegantes, mas legitimos

Antes de chamar de persistencia maliciosa, confirme:

- publisher
- caminho real
- presenca do binario
- relacao com produto instalado
- correlacao com outros modulos

Se a unica prova for **"esse nome me irritou"**, isso ainda nao fecha caso.

---

## Exemplo de raciocinio investigativo

### Cenario A — residuo pos-desinstalacao

O GhostTrace encontra uma tarefa chamada `VendorUpdaterDaily`.

Correlacao:

- `ScheduledTasksScanModule`: tarefa existe
- `TaskCacheScanModule`: entrada coerente
- `UninstallEntriesScanModule`: produto nao aparece mais instalado
- `FileSystemTraceScanModule`: binario alvo nao existe mais em `Program Files\Vendor`

Leitura:

- forte indicio de residuo apos desinstalacao
- provavelmente tarefa quebrada ou abandonada
- prioridade media, mas vale limpeza orientada

Traducao informal: o software foi embora e esqueceu de avisar a propria agenda.

### Cenario B — Ghost Task

O GhostTrace encontra indicio em `TaskCache\Tree` para uma tarefa com nome semelhante a componente de sistema, mas sem representacao operacional consistente.

Correlacao:

- `TaskCacheScanModule`: vestigio presente e inconsistente
- `ScheduledTasksScanModule`: tarefa nao aparece de forma normal ou aparece incompleta
- `FileSystemTraceScanModule`: acao aponta para caminho atipico em `AppData`
- `WmiPersistenceScanModule`: artefato relacionado ao mesmo executavel

Leitura:

- alta suspeita de manipulacao ou persistencia anomala
- classificar como possivel Ghost Task
- escalonar triagem e preservar evidencia

Traducao informal: nao e so sujeira. E sujeira tentando parecer decoracao.

---

## Como documentar o achado

Ao registrar um caso, inclua pelo menos:

- nome da tarefa
- caminho da tarefa
- acao configurada
- status do binario referenciado
- presenca ou ausencia em `TaskCache\Tree`
- relacao com produto instalado ou removido
- modulos correlatos que reforcam a conclusao
- classificacao final

Formato recomendado:

```text
Tarefa: <nome>
Categoria: legitima | quebrada | abandonada | suspeita | Ghost Task
Acao: <binario/comando>
Binario existe: sim | nao
Produto instalado relacionado: sim | nao | inconclusivo
TaskCache coerente: sim | nao
Persistencia correlata: sim | nao
Conclusao: <resumo objetivo>
```

Se nao estiver documentado, daqui a dois dias vira lenda urbana de incidente.

---

## Relacao com outros modulos do GhostTrace

Este playbook fica melhor quando usado com:

- `ScheduledTasksScanModule`
- `TaskCacheScanModule`
- `FileSystemTraceScanModule`
- `UninstallEntriesScanModule`
- `PersistenceScanModule`
- `AsepScanModule`
- `ServicesScanModule`
- `WmiPersistenceScanModule`
- `PowerShellHistoryScanModule`

Se a tarefa chamar script, payload ou binario ja visto por outros modulos, a investigacao sai de **"curioso"** para **"acionavel"** bem rapido.

---

## Fechamento

Tarefa agendada nao deve ser analisada como item decorativo do sistema.

Ela pode ser:

- automacao legitima
- sobra de uninstall mal feito
- persistencia oportunista
- inconsistencia estrutural
- tentativa de se esconder em estado quebrado

O trabalho do analista nao e entrar em panico nem passar pano.
E correlacionar.

Porque no Windows, desaparecer de um lugar nao significa morrer.
As vezes significa so que ele mudou de armario.

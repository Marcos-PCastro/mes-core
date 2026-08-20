# Requirements Document

**Feature:** MES Core (`mes-core`)

> **Derivado do `design.md`.** Este documento formaliza, em critérios verificáveis,
> o que o design já decidiu. Onde houver divergência aparente, o `design.md` é a
> fonte da verdade e este documento deve ser corrigido.
>
> **Idioma:** documento em português. Nomes de entidade, endpoint, token de UI e
> termos de código em inglês (decisão R3 do design).

## Introduction

O `mes-core` é um núcleo mínimo de MES construído para demonstrar decisões de
engenharia defensáveis em entrevista técnica de nível pleno: modelagem de domínio
com invariantes, idempotência em canal não confiável, concorrência otimista,
indicador derivado de eventos, consulta recursiva de grafo e testes que provam
propriedades.

Os requisitos 1 a 9 são funcionais e seguem os blocos do design §4.1. Os
requisitos 10 a 16 são não funcionais. A numeração `X.Y` é referenciada pelas
propriedades executáveis do design §25 (`Validates: Requirements X.Y`) e **não
deve ser alterada** sem atualizar aquelas referências.

### Convenções

- Critérios no formato `X.Y` — `X` é o requisito, `Y` o critério de aceitação.
- `WHEN … THEN … SHALL …` para comportamento acionado por evento.
- `IF … THEN … SHALL …` para comportamento condicional.
- `THE SYSTEM SHALL …` para regra invariante.
- Todo critério é verificável por teste automatizado, por inspeção de artefato ou
  por comando executável. Critério não verificável não entra.

## Glossary

Ver design §3 para o glossário completo PT↔EN. Termos usados neste documento:

| Termo (código, inglês) | Significado (planta, português) |
|---|---|
| `WorkOrder` | Ordem de produção (OP) |
| `ProductionEntry` | Apontamento de produção |
| `DowntimeEvent` | Parada de máquina |
| `DowntimeReason` | Motivo de parada |
| `ScrapReason` | Motivo de refugo |
| `Batch` | Lote |
| `BatchConsumption` | Consumo de lote componente |
| `Resource` | Recurso, máquina ou linha |
| `Product` | Produto, item ou SKU |
| `Shift` | Turno |
| OEE | Eficiência global do equipamento (`Availability × Performance × Quality`) |
| `IdempotencyKey` | Chave de idempotência |
| Genealogy | Genealogia, rastreabilidade de lote |
| Recall | Recolhimento; busca reversa de lotes afetados |

---

## Requirements

## Requisito 1 — Cadastros

**User story:** Como supervisor de produção, quero cadastrar produtos, recursos,
motivos de parada e motivos de refugo, para que a operação registre eventos usando
um vocabulário controlado em vez de texto livre.

### Critérios de aceitação

1.1. THE SYSTEM SHALL expor operações de listagem, consulta por id, criação,
atualização e desativação para `Product`, `Resource`, `DowntimeReason` e
`ScrapReason`, conforme os contratos do design §22.2.

1.2. WHEN uma entidade de catálogo é criada com um `code` já existente THEN o
sistema SHALL rejeitar com `409` e `problem.type` sufixo `duplicate-code`.

1.3. THE SYSTEM SHALL tratar exclusão como desativação lógica
(`is_active = false`), preservando o histórico que referencia a entidade.

1.4. WHEN a desativação é solicitada para uma entidade referenciada por registro
ativo THEN o sistema SHALL rejeitar com `409` e sufixo `in-use`.

1.5. THE SYSTEM SHALL exigir `ideal_cycle_time_seconds` em `Product` como valor
maior que zero para que o produto seja elegível a cálculo de performance.

1.6. THE SYSTEM SHALL classificar `DowntimeReason` em categoria `Planned` ou
`Unplanned` e expor o campo `counts_against_availability`, que determina se a
parada penaliza a disponibilidade.

1.7. THE SYSTEM SHALL paginar listagens com `page` e `pageSize`, retornando
`items`, `total`, `page` e `pageSize`.

1.8. THE SYSTEM SHALL popular, no seed de demonstração, exclusivamente os nomes
fictícios do design §8.6.

---

## Requisito 2 — Ordem de produção: ciclo de vida e máquina de estados

**User story:** Como supervisor, quero que a ordem de produção só transite entre
estados válidos, para que o sistema seja a autoridade sobre o que pode acontecer e
não dependa da disciplina do operador.

### Critérios de aceitação

2.1. THE SYSTEM SHALL permitir exatamente as transições declaradas na matriz do
design §10.1 e SHALL rejeitar toda combinação `(status, action)` ausente da matriz
com `InvalidStateTransitionException`, respondida como `422` e sufixo
`invalid-state-transition`.

2.2. THE SYSTEM SHALL tratar `Completed` e `Cancelled` como estados terminais
absorventes: nenhuma ação é aceita e o estado permanece inalterado após a
tentativa.

2.3. THE SYSTEM SHALL implementar a matriz de transição como tabela declarativa
única, e o teste parametrizado SHALL cobrir as 48 combinações de 6 estados por
8 ações, cada uma com resultado esperado declarado.

2.4. WHEN `Release` é solicitado THEN o sistema SHALL exigir
`PlannedQuantity > 0`, `Resource.IsActive` e `Product.IsActive`, rejeitando com
`422` e sufixo específico por guarda quando qualquer condição falhar.

2.5. WHEN `Unrelease` é solicitado THEN o sistema SHALL rejeitar com `422` e
sufixo `cannot-unrelease-with-entries` caso a ordem já possua apontamento.

2.6. WHEN `Start` é solicitado THEN o sistema SHALL rejeitar com `409` e sufixo
`resource-busy` caso exista outra `WorkOrder` em `InProgress` no mesmo `Resource`,
e SHALL preencher `StartedAt` uma única vez.

2.7. WHEN `Complete` é solicitado THEN o sistema SHALL rejeitar com `409` e
sufixo `open-downtime-must-be-closed` caso exista `DowntimeEvent` aberto no
recurso, e SHALL preencher `CompletedAt` na transição bem-sucedida.

2.8. THE SYSTEM SHALL exigir a permissão `workorder:cancel` para `Cancel`,
respondendo `403` quando ausente.

2.9. THE SYSTEM SHALL expor `GET /api/work-orders/{id}/allowed-actions`
retornando as ações válidas para o estado atual, derivadas da mesma tabela de
transição, sem duplicar a regra.

2.10. THE SYSTEM SHALL modelar ações como sub-recursos `POST`
(`/release`, `/start`, `/complete`) e SHALL NOT aceitar mudança de estado via
`PATCH { status }`.

---

## Requisito 3 — Apontamento de produção: quantidades, idempotência e concorrência

**User story:** Como operador, quero apontar produção sem risco de duplicar
registro quando a tela demora, o Wi-Fi cai ou o coletor reenvia, para que o
indicador reflita a realidade da linha.

### Critérios de aceitação

3.1. THE SYSTEM SHALL manter, para toda `WorkOrder`,
`ProducedGoodQuantity == Σ entries.GoodQuantity` e
`ProducedScrapQuantity == Σ entries.ScrapQuantity` (invariante WO-2), sob qualquer
sequência de operações aceitas, inclusive concorrentes.

3.2. THE SYSTEM SHALL rejeitar apontamento que faria
`ProducedGoodQuantity` exceder `PlannedQuantity × (1 + OverproductionTolerance)`,
com `422` e sufixo `overproduction-not-allowed`. `OverproductionTolerance` SHALL
ter valor default de 5% e SHALL ser configurável por produto.

3.3. THE SYSTEM SHALL exigir o header `Idempotency-Key` no POST de apontamento e
SHALL aplicar a semântica HTTP do design §22.4:

| Situação | Status | `problem.type` sufixo |
|---|---|---|
| Primeiro POST aceito | `201` + `Location` | — |
| Replay, mesma chave e mesmo payload | `200`, `wasReplay: true`, corpo idêntico | — |
| Header ausente | `400` | `missing-idempotency-key` |
| Mesma chave, payload diferente | `409` | `idempotency-key-reused` |
| Corrida de INSERT (`23505`) na mesma chave | `200` | — |

3.3.1. WHEN um replay é detectado THEN o sistema SHALL NOT produzir efeito
colateral e SHALL NOT publicar evento de domínio.

3.3.2. THE SYSTEM SHALL garantir unicidade de `idempotency_key` por índice único
`(work_order_id, idempotency_key)` no banco, não apenas na aplicação.

3.3.3. THE SYSTEM SHALL tratar `PostgresException.SqlState == "23505"` no índice
de idempotência como replay, e SHALL NOT responder `500`.

3.3.4. THE SYSTEM SHALL calcular o hash de payload sobre os campos que definem a
intenção de negócio (`workOrderId`, `goodQuantity`, `scrapQuantity`,
`scrapReasonId`, `occurredAt`) e SHALL NOT incluir metadado de transporte
(`recordedAt`, `userId`).

3.4. THE SYSTEM SHALL aplicar concorrência otimista na `WorkOrder` usando `xmin`
do PostgreSQL como token, sem coluna adicional.

3.4.1. WHEN o `xmin` mudou entre leitura e escrita THEN o sistema SHALL responder
`409` com sufixo `concurrency-conflict` e header `Retry-After: 0`.

3.4.2. THE SYSTEM SHALL tentar novamente até 3 vezes no servidor, com backoff
50/100/200 ms mais jitter, recarregando o agregado a cada tentativa. `DomainException`
SHALL NOT ser retentável.

3.4.3. THE SYSTEM SHALL garantir que 20 apontamentos concorrentes na mesma ordem
resultem em totais iguais à soma dos apontamentos aceitos, sem perda de atualização.

3.5. WHEN `GoodQuantity + ScrapQuantity == 0` THEN o sistema SHALL rejeitar com
`422` e sufixo `empty-production-entry`.

3.6. WHEN `ScrapQuantity > 0` e `ScrapReasonId` é nulo THEN o sistema SHALL
rejeitar com `422` e sufixo `scrap-reason-required`.

3.7. THE SYSTEM SHALL aceitar apontamento apenas quando
`Status == InProgress`, rejeitando com `422` e sufixo
`work-order-not-in-progress` em qualquer outro estado.

3.8. THE SYSTEM SHALL exigir `StartedAt <= OccurredAt <= Now + 5 min`, rejeitando
com `422` e sufixo `occurred-at-out-of-range`. A tolerância de 5 minutos cobre
desvio de relógio entre coletor e servidor, e o `detail` do erro SHALL informar o
horário do servidor.

---

## Requisito 4 — Paradas de máquina

**User story:** Como operador, quero registrar quando e por que a máquina parou,
para que a perda de disponibilidade seja explicada por motivo e não apareça como
número inexplicável no indicador.

### Critérios de aceitação

4.1. THE SYSTEM SHALL permitir abrir parada em um `Resource` informando
`DowntimeReasonId` obrigatório (invariante DT-3) e `startedAt`, e SHALL exigir
`Idempotency-Key` com a mesma semântica do critério 3.3.

4.2. THE SYSTEM SHALL garantir no máximo um `DowntimeEvent` aberto por `Resource`
(invariante DT-2), por índice único parcial
`UNIQUE (resource_id) WHERE ended_at IS NULL`, respondendo `409` com sufixo
`resource-already-down` na segunda abertura.

4.3. WHEN o fechamento é solicitado THEN o sistema SHALL exigir
`endedAt > startedAt` (invariante DT-1), rejeitando com `422` e sufixo
`ended-before-started`.

4.4. WHEN o fechamento é solicitado em parada já fechada THEN o sistema SHALL
rejeitar com `422` e sufixo `downtime-already-closed`. Parada fechada SHALL ser
imutável (invariante DT-4).

4.5. THE SYSTEM SHALL expor `GET /api/resources/{id}/downtimes/open` retornando a
parada aberta ou `204` quando não houver.

4.6. THE SYSTEM SHALL expor
`GET /api/resources/downtimes/stale?olderThanHours=N` listando paradas abertas há
mais de N horas, e SHALL NOT fechar parada automaticamente — inventar `ended_at`
falsificaria o histórico.

4.7. THE SYSTEM SHALL permitir parada sem `WorkOrder` associada, porque a máquina
pode parar em setup ou manutenção sem ordem aberta.

---

## Requisito 5 — OEE derivado de eventos

**User story:** Como supervisor, quero que o OEE seja calculado a partir dos
eventos registrados e nunca digitado, para que o número seja sempre auditável e
explicável ao operador.

### Critérios de aceitação

5.1. THE SYSTEM SHALL calcular `OEE = Availability × Performance × Quality` como
função pura dos eventos da janela, e SHALL NOT persistir o OEE nem seus fatores
como coluna de verdade em nenhuma tabela.

5.1.1. THE SYSTEM SHALL garantir que todo fator retornado esteja em `[0, 1]` ou
seja nulo, e que `OEE` nunca exceda nenhum de seus fatores.

5.1.2. THE SYSTEM SHALL produzir resultado idêntico independentemente da ordem
dos eventos de entrada.

5.1.3. THE SYSTEM SHALL calcular `Performance` sobre `Q_total` (boas mais
refugo), e SHALL NOT usar apenas `Q_good`, para que a perda de qualidade seja
contabilizada uma única vez no fator `Quality`.

5.2. THE SYSTEM SHALL derivar os tempos conforme as definições do design §17.2:

- `T_planned` = tempo da janela intersectado com os turnos definidos
- `T_down` = duração da **união** dos intervalos de parada contáveis, recortada na
  janela — nunca a soma
- `T_run` = `max(0, T_planned − T_down)`
- `Availability` = `T_run / T_planned`

5.2.1. THE SYSTEM SHALL garantir `T_planned == T_run + T_down` (com tolerância
`1e-6`) e `0 <= T_run <= T_planned`.

5.2.2. THE SYSTEM SHALL garantir que adicionar uma parada nunca aumente a
`Availability`.

5.2.3. WHEN nenhum `Shift` está configurado THEN o sistema SHALL adotar
`T_planned = T_total` (janela 24×7) como comportamento default documentado.

5.2.4. THE SYSTEM SHALL desconsiderar, no cálculo de `T_down`, as paradas cuja
`DowntimeReason` tenha `counts_against_availability = false`.

5.3. THE SYSTEM SHALL classificar apontamentos na janela por `occurred_at`, e
SHALL NOT usar `recorded_at`, para que apontamento retroativo caia no período em
que a produção aconteceu.

5.4. THE SYSTEM SHALL usar janela semiaberta `[from, to)` em toda consulta de
período, para que um evento na borda não seja contado em duas janelas
consecutivas.

5.5. THE SYSTEM SHALL tratar os casos de borda da tabela do design §17.3:

| Condição | Comportamento exigido |
|---|---|
| `T_planned == 0` | `hasData: false`, fatores `null`, `reason: "NoPlannedProductionTime"`. **Não** retornar 0 |
| `Q_total == 0` | `Availability` normal, `Performance = 0`, `Quality = null`, `OEE = 0` |
| Paradas sobrepostas | União de intervalos |
| Parada cruzando a borda da janela | Recorte no limite da janela |
| Parada aberta | Tratada como terminando em `min(now, W₁)` |
| `T_down > T_planned` | `T_run = 0`, log de warning, nunca tempo negativo |
| `Performance > 1` | Clampada em `1.0` **e** `performanceWasClamped: true` |
| `C_ideal <= 0` | `Performance = null`, `OEE = null`, `reason: "MissingIdealCycleTime"` |
| `to <= from` | `400` com sufixo `invalid-time-window` |
| Janela maior que 90 dias | `400` com sufixo `time-window-too-large` |
| Múltiplos produtos na janela | `C_ideal` ponderado por produto |

5.6. THE SYSTEM SHALL responder `200` com `hasData: false` quando não houver
dados na janela, e SHALL NOT responder `404` — ausência de produção é resposta
legítima.

5.7. THE SYSTEM SHALL expor, além do OEE pontual, `oee/timeseries` com bucket
configurável e `downtime-pareto` com segundos e ocorrências por motivo.

5.8. THE SYSTEM SHALL garantir, por teste automatizado, que a implementação SQL
de leitura e o calculador puro em C# produzem o mesmo resultado para o mesmo
conjunto de eventos.

5.9. THE SYSTEM SHALL injetar o tempo por `IClock`, e SHALL NOT invocar
`DateTime.UtcNow` dentro de regra de domínio, para que o cálculo por período seja
determinístico em teste.

---

## Requisito 6 — Rastreabilidade: genealogia e recall

**User story:** Como analista de qualidade, quero descobrir em segundos de quais
lotes um produto veio e para onde um lote suspeito foi, para que um recall recolha
exatamente o que precisa e nada além.

### Critérios de aceitação

6.1. THE SYSTEM SHALL manter o grafo de consumo acíclico (invariante B-4).

6.1.1. WHEN `producedBatchId == consumedBatchId` THEN o sistema SHALL rejeitar
com `422` e sufixo `self-consumption` (invariante B-3).

6.1.2. WHEN a aresta a ser criada fecharia um ciclo THEN o sistema SHALL rejeitar
com `422` e sufixo `cycle-detected`, verificando **antes** da escrita, e o
`detail` SHALL informar o caminho encontrado.

6.1.3. THE SYSTEM SHALL exigir `quantity > 0` em `BatchConsumption` (invariante
B-2).

6.2. THE SYSTEM SHALL expor consulta backward retornando toda a ancestralidade do
lote, em todos os níveis, com `depth` por nó.

6.2.1. THE SYSTEM SHALL respeitar o parâmetro `maxDepth`, garantindo
`node.Depth <= maxDepth` em todo nó retornado.

6.2.2. THE SYSTEM SHALL NOT incluir o próprio lote consultado no resultado da
ancestralidade, e SHALL retornar ids distintos.

6.2.3. THE SYSTEM SHALL retornar o mesmo conjunto em chamadas repetidas para o
mesmo lote e a mesma profundidade.

6.3. THE SYSTEM SHALL expor consulta forward e `recall-impact` retornando todos
os lotes que consumiram o lote suspeito, direta ou indiretamente.

6.3.1. THE SYSTEM SHALL retornar cada lote afetado uma única vez, com a menor
profundidade, mesmo em grafo com convergência (diamante), para que o volume do
recall não seja superestimado.

6.3.2. THE SYSTEM SHALL garantir que backward e forward sejam inversas: para
quaisquer lotes `a` e `b`, `b ∈ Ancestry(a) ⟺ a ∈ Impact(b)`.

6.3.3. THE SYSTEM SHALL marcar nó já materializado em outro ramo com
`isReference: true`, para que a árvore não expanda exponencialmente em grafo
convergente.

6.4. THE SYSTEM SHALL implementar as consultas com CTE recursiva parametrizada no
PostgreSQL, com guarda de ciclo por array de caminho e limite de profundidade.

6.5. THE SYSTEM SHALL manter índice em `consumed_batch_id` e em
`produced_batch_id` em `batch_consumption`, e o teste de performance SHALL
confirmar uso de índice em vez de varredura sequencial.

6.6. THE SYSTEM SHALL permitir bloquear um lote (`POST /api/batches/{id}/block`)
com motivo, para uso no fluxo de recall.

---

## Requisito 7 — Simulador de equipamento e tempo real

**User story:** Como avaliador do projeto, quero abrir o dashboard e ver o
indicador se mover sozinho, para entender em 30 segundos que o sistema está vivo
e não é uma tela estática.

### Critérios de aceitação

7.1. THE SYSTEM SHALL incluir um worker `Mes.Simulator` que publica apontamentos
de produção e eventos de parada periodicamente.

7.2. THE SIMULATOR SHALL comunicar-se exclusivamente pela API pública HTTP,
autenticando como conta de serviço, e SHALL NOT escrever diretamente no banco,
para que nenhuma invariante seja contornada.

7.3. THE SIMULATOR SHALL gerar parada aleatória com MTBF e MTTR configuráveis e
fechá-la, e SHALL aceitar semente fixa para execução reprodutível.

7.4. THE SIMULATOR SHALL tentar novamente em `409 concurrency-conflict`
reusando a **mesma** `Idempotency-Key`, e SHALL NOT tentar novamente em `422`.

7.5. THE SYSTEM SHALL expor o hub SignalR `/hubs/shop-floor` autenticado, com
grupos por recurso, emitindo `productionRecorded`, `downtimeStarted`,
`downtimeClosed` e `workOrderStatusChanged`.

7.6. THE SYSTEM SHALL publicar evento de domínio somente **após** o commit da
transação, e SHALL NOT notificar produção que a transação possa desfazer.

7.7. THE SYSTEM SHALL usar o evento de tempo real como **notificação**, e o
cliente SHALL invalidar a query e refazer a leitura, para que o valor exibido
venha sempre do mesmo caminho de cálculo.

7.8. WHEN o simulador está habilitado THEN o dashboard SHALL apresentar mudança
visível em menos de 30 segundos sem interação do usuário.

7.9. THE SYSTEM SHALL entregar o evento ao cliente conectado em menos de
1 segundo após o commit.

---

## Requisito 8 — RBAC: usuário, papel e permissão

**User story:** Como administrador, quero que cada ação exija uma permissão
específica, para que um operador não consiga liberar ou cancelar ordem de
produção.

### Critérios de aceitação

8.1. THE SYSTEM SHALL modelar `User`, `Role` e `Permission`, com os papéis
`Operator`, `Supervisor` e `Admin` no seed.

8.2. THE SYSTEM SHALL autorizar endpoints **por permissão**, não por papel
(`.RequireAuthorization("workorder:release")`), com papel mapeando para conjunto
de permissões.

8.3. WHEN um usuário com papel `Operator` tenta liberar ordem de produção THEN o
sistema SHALL responder `403`.

8.4. THE SYSTEM SHALL manter públicos apenas `POST /api/auth/login` e
`GET /health`; todo o restante SHALL exigir token válido, inclusive os endpoints
usados pelo simulador.

8.5. THE SYSTEM SHALL expor `GET /api/auth/me` retornando papéis e permissões
efetivas do usuário autenticado.

8.6. THE FRONTEND SHALL ocultar ação sem permissão, e o servidor SHALL rejeitar
a mesma ação independentemente da UI — ocultar é conveniência, não controle.

---

## Requisito 9 — Etiqueta de lote

**User story:** Como operador, quero gerar a etiqueta do lote com QR code, para
que o lote seja identificável na próxima etapa sem digitação manual do código.

### Critérios de aceitação

9.1. THE SYSTEM SHALL expor `GET /api/batches/{id}/label?format=png|svg`
retornando o QR code do lote com `Content-Type` correspondente
(`image/png` ou `image/svg+xml`).

9.2. THE QR CODE SHALL codificar o `code` do lote em formato legível por leitor
comum.

9.3. WHEN o lote não existe THEN o sistema SHALL responder `404`.

9.4. THE SYSTEM SHALL permitir cache HTTP da imagem, dado que a etiqueta de um
lote é imutável.

---

# Requisitos não funcionais

---

## Requisito 10 — Setup e execução

**User story:** Como recrutador técnico com dez minutos disponíveis, quero rodar o
projeto com um comando, para avaliar o sistema em vez de depurar o ambiente.

### Critérios de aceitação

10.1. WHEN um avaliador executa `git clone` seguido de `docker compose up` em
máquina limpa com Docker instalado THEN o sistema SHALL estar operacional em menos
de 2 minutos, sem passo manual adicional.

10.2. THE SYSTEM SHALL subir quatro serviços no Compose: API, PostgreSQL,
frontend e simulador.

10.3. THE SYSTEM SHALL declarar `healthcheck` no PostgreSQL e
`depends_on: condition: service_healthy` na API, para que a API não inicie antes
do banco estar pronto.

10.4. THE SYSTEM SHALL aplicar migrations no startup apenas quando
`Mes__ApplyMigrationsOnStartup=true`, e o README SHALL registrar que em produção
isso seria um job separado.

10.5. THE SEED de demonstração SHALL popular os 4 catálogos, ao menos 3 recursos,
5 ordens de produção e um grafo de lotes com 3 níveis incluindo convergência, para
que nenhuma tela abra vazia.

10.6. THE SYSTEM SHALL expor `GET /health` respondendo `200` quando API e banco
estão saudáveis.

10.7. O `docker compose up` SHALL ser validado em máquina limpa antes da
publicação do repositório.

---

## Requisito 11 — Qualidade e testes

**User story:** Como entrevistador técnico, quero ver testes que provam
propriedades e não apenas exemplos felizes, para avaliar se o candidato pensa em
corretude.

### Critérios de aceitação

11.1. THE PROJECT SHALL manter cobertura de linha superior a 90% em `Mes.Domain`.

11.2. THE SUÍTE de testes de domínio SHALL executar em menos de 3 segundos,
comprovando ausência de I/O na camada de domínio.

11.3. THE PROJECT SHALL implementar no mínimo 10 das 16 propriedades executáveis
do design §25, incluindo obrigatoriamente P-1 (fatores em `[0,1]`),
P-6 (idempotência: N replays equivalem a 1 execução), P-7 (alcançabilidade da
máquina de estados), P-9 (backward e forward inversas) e P-10 (união de intervalos
limitada, monotônica e comutativa).

11.4. THE INTEGRATION TESTS SHALL usar PostgreSQL real via Testcontainers, e o
projeto SHALL NOT usar `UseInMemoryDatabase`, porque o provider em memória não
possui índice único parcial, CTE recursiva nem `xmin` — exatamente as três
capacidades que precisam ser testadas.

11.5. THE PROJECT SHALL executar CI no GitHub Actions com as etapas restore,
build com `TreatWarningsAsErrors`, testes de unidade e propriedade, testes de
integração, lint e testes do frontend, e build das imagens Docker.

11.6. THE BRANCH `main` SHALL permanecer com CI verde ao fim de cada sprint.

11.7. THE PROJECT SHALL NOT conter testes de getter, de mapeamento de DTO, de
configuração de EF Core ou de componente de layout — teste sem valor é dívida de
manutenção.

11.8. WHEN uma propriedade executável falha THEN o contraexemplo minimizado SHALL
ser registrado no README como evidência do valor da técnica.

---

## Requisito 12 — Observabilidade

**User story:** Como quem sustenta o sistema, quero correlacionar um erro relatado
pelo usuário com o log correspondente, para investigar sem adivinhação.

### Critérios de aceitação

12.1. THE SYSTEM SHALL emitir log estruturado com propriedades nomeadas
(`workOrderId`, `resourceId`, `idempotencyKey`, `traceId`), e SHALL NOT depender de
interpolação de string para dado pesquisável.

12.2. THE SYSTEM SHALL incluir `traceId` em toda resposta de erro, correlacionado
com o log.

12.3. THE SYSTEM SHALL respeitar a taxonomia de nível do design §24.1: erro de
cliente (4xx) em `Debug` ou `Information`; erro de sistema (5xx) em `Error`.

12.4. THE SYSTEM SHALL registrar warning quando `T_down > T_planned`, indicando
provável má classificação de turno.

12.5. THE SYSTEM SHALL retornar `ProblemDetails` conforme RFC 9457, sem stack
trace, com `type`, `title`, `status`, `detail`, `instance` e `traceId`.

---

## Requisito 13 — Segurança

**User story:** Como revisor do código, quero não encontrar erro elementar de
segurança, para que o projeto some credibilidade em vez de subtrair.

### Critérios de aceitação

13.1. THE SYSTEM SHALL armazenar senha com Argon2id ou PBKDF2 com salt e fator de
trabalho adequado, e SHALL NOT usar MD5, SHA1 ou SHA256 sem salt.

13.2. THE SYSTEM SHALL emitir JWT com expiração de 60 minutos e claims `sub`,
`role` e `permissions`.

13.3. THE SIGNING KEY SHALL vir de variável de ambiente. O repositório SHALL NOT
conter segredo real; valores em `appsettings.json` SHALL ser exclusivamente de
desenvolvimento e comentados como tal.

13.4. THE SYSTEM SHALL configurar CORS com origem explícita do frontend, e SHALL
NOT usar `AllowAnyOrigin`.

13.5. THE SYSTEM SHALL parametrizar todo SQL, incluindo as CTEs recursivas em
Dapper, e SHALL NOT interpolar string em consulta.

13.6. THE SYSTEM SHALL aplicar rate limit no endpoint de login.

13.7. THE PROJECT SHALL fixar versões de dependência e habilitar Dependabot.

13.8. THE SYSTEM SHALL exigir autenticação no hub SignalR.

---

## Requisito 14 — Restrição de propriedade intelectual

**User story:** Como profissional, quero publicar o projeto sem expor
propriedade intelectual do meu empregador, para que o portfólio comunique
integridade em vez de risco.

### Critérios de aceitação

14.1. THE REPOSITORY SHALL NOT conter qualquer trecho de código proveniente do
sistema MES da empresa.

14.2. THE REPOSITORY SHALL NOT conter a nomenclatura real do sistema legado,
incluindo identificadores de aplicação, códigos de área ou nomes de package de
banco.

14.3. THE REPOSITORY SHALL NOT conter nome real de tabela, máquina, linha,
produto ou cliente da operação real.

14.4. THE REPOSITORY SHALL NOT conter dado real, dump de banco ou captura de tela
com dado real.

14.5. THE REPOSITORY SHALL NOT nomear a empresa empregadora. Referências à
experiência profissional SHALL usar formulação genérica do tipo
"a manufacturing company".

14.6. ALL nomes de seed SHALL provir exclusivamente da tabela de nomes fictícios
do design §8.6.

14.7. WHEN questionado sobre a relação com o sistema real THEN o autor SHALL
poder responder com a formulação do design §28.5, sem ambiguidade.

---

## Requisito 15 — Idioma e convenções de repositório

**User story:** Como recrutador de empresa estrangeira, quero ler o repositório
sem barreira de idioma, para avaliar o trabalho em vez de traduzi-lo.

### Critérios de aceitação

15.1. THE REPOSITORY SHALL estar 100% em inglês: README, ADRs, mensagens de
commit, nomes de classe, propriedade, método, variável, comentário, mensagem de
erro, nome de tabela e de coluna.

15.2. THE COMMITS SHALL seguir Conventional Commits em inglês.

15.3. THE NAMING SHALL seguir as convenções do design §3.3: `PascalCase` em
classes e propriedades, `snake_case` em tabelas e colunas, kebab plural em
endpoints, verbo no passado em eventos de domínio.

15.4. THE REPOSITORY SHALL NOT conter arquivo de build, `.bak`, `.vs`, `bin`,
`obj` ou artefato gerado, conforme `.gitignore` adequado.

15.5. THE `git log` SHALL ser legível e SHALL fazer parte deliberada do portfólio.

---

## Requisito 16 — Interface: layout, cores e acessibilidade

**User story:** Como operador de chão de fábrica, quero enxergar o estado da
máquina numa olhada de dois segundos, com luva na mão e sob luz forte de galpão,
para registrar o que aconteceu sem parar a linha.

> **Nota de escopo:** os critérios abaixo são decisões de produto, não de gosto.
> Cada um deriva de restrição operacional ou de norma de acessibilidade, e por isso
> é defensável em entrevista.

### Critérios de aceitação

16.1. THE FRONTEND SHALL adotar uma biblioteca de componentes — Mantine **ou**
shadcn/ui — e SHALL aceitar seus defaults visuais. O projeto SHALL NOT investir
esforço em CSS artesanal extenso, porque o orçamento de esforço pertence ao
backend.

16.2. THE FRONTEND SHALL definir cores por **token semântico**, não cromático:
`--status-running`, `--status-down`, `--status-idle`, `--oee-above-target`,
`--oee-warning`, `--oee-below-target`.

16.2.1. THE CODEBASE SHALL NOT conter valor hexadecimal de cor fora do arquivo de
tokens.

16.2.2. Justificativa a registrar no `docs/ui-guidelines.md`: no sistema legado do
autor as classes são nomeadas pela cor (`svg-sensor-verde`), de modo que o nome
passa a mentir quando a paleta muda. Token semântico mantém o código falando de
estado, não de pigmento, e reduz troca de paleta a uma alteração pontual.

16.3. THE UI SHALL NOT usar cor como único canal de informação. Todo indicador de
estado SHALL combinar cor, ícone e texto.

16.3.1. Justificativa: aproximadamente 8% dos homens apresentam alguma forma de
daltonismo, e o eixo verde-vermelho é justamente o mais afetado. Num painel em que
a cor é a única informação, esse operador não consegue operar.

16.4. THE UI SHALL atender contraste mínimo WCAG 2.2 AA: 4.5:1 para texto normal,
3:1 para texto grande e para componentes de interface.

16.5. THE FORMULÁRIOS operacionais (apontamento e parada) SHALL ser navegáveis por
teclado, com ordem de foco previsível e indicador de foco visível.

16.6. THE ATUALIZAÇÕES em tempo real SHALL ser anunciadas por região `aria-live`,
para que a mudança de estado não seja perceptível apenas visualmente.

> **Ressalva obrigatória no README:** conformidade WCAG completa exige teste manual
> com tecnologia assistiva e revisão por especialista em acessibilidade. Os
> critérios acima cobrem o que é verificável automaticamente e por inspeção, e o
> README SHALL declarar essa limitação em vez de afirmar conformidade total.

16.7. THE TELAS de apontamento e de parada SHALL usar alvo de toque mínimo de
44×44 px, preferencialmente 48×48 px, considerando operação com luva.

16.8. THE VALOR de produção e o OEE SHALL ser legíveis a aproximadamente 2 metros
de distância na tela de operação.

16.9. THE TELAS operacionais SHALL apresentar o mínimo de campos necessário por
tela, priorizando conclusão rápida sobre completude de informação.

16.10. THE LIMIARES de OEE SHALL derivar de referência de domínio — 85% como
patamar de classe mundial e 60% como típico de manufatura — SHALL ser configuráveis
e SHALL estar documentados. O projeto SHALL NOT usar limiar arbitrário.

16.10.1. THE VALOR numérico do indicador SHALL estar sempre visível ao lado da
representação colorida, e a cor SHALL NOT ser a única forma de leitura do
indicador.

16.11. THE DASHBOARD SHALL usar tema escuro e alta densidade de informação, padrão
de sala de controle. THE TELAS de cadastro e administração SHALL usar tema claro.

16.12. THE LAYOUT SHALL ser funcional em tablet de 10 polegadas na orientação
horizontal.

16.13. EVERY tela que carrega dados SHALL implementar quatro estados explícitos:
carregando, vazio, erro e sucesso. THE UI SHALL NOT apresentar tela em branco.

16.13.1. WHEN a API retorna `ProblemDetails` THEN a UI SHALL renderizar `title` e
`detail` de forma legível ao usuário, e SHALL NOT exibir JSON cru.

16.14. THE PROJECT SHALL produzir `docs/ui-guidelines.md` no **início do sprint 9**,
contendo paleta, tokens semânticos, tipografia, escala de espaçamento e tamanhos de
alvo de toque.

16.14.1. THE PALETA e os tokens SHALL ser congelados ao fim do sprint 9. Revisão de
cor durante a implementação SHALL NOT ocorrer, por ser a causa mais frequente de o
frontend consumir o dobro do esforço estimado.

16.15. THE PALETA SHALL partir de escala existente (Tailwind ou a da biblioteca de
componentes adotada). O projeto SHALL NOT inventar cores.

---

# Rastreabilidade

## Requisito → seção do design → propriedade executável

| Requisito | Seção do design | Propriedade |
|---|---|---|
| 1 — Cadastros | §4.1, §8.6, §9, §22.2 | — |
| 2.1, 2.2 — Máquina de estados | §10, §10.1, §20 | P-7, P-14 |
| 2.3–2.10 — Guardas de transição | §20.2, §20.3, §22.3 | P-7 |
| 3.1 — Conservação de quantidade | §8.2 (WO-2), §20.3 | P-5 |
| 3.2 — Superprodução | §8.2 (WO-4), §20.2 | P-13 |
| 3.3 — Idempotência | §19, §22.4 | P-6, P-15 |
| 3.4 — Concorrência otimista | §23 | P-5 sob concorrência |
| 3.5–3.8 — Guardas de apontamento | §8.2 (WO-8, WO-9), §20.2 | — |
| 4.1–4.7 — Paradas | §8.3 (DT-1..4), §22.5, §24.2 | P-11 |
| 5.1 — OEE como função pura | §17.1, §17.2, §17.5 | P-1, P-2, P-12 |
| 5.2 — Derivação dos tempos | §17.2, §17.4 | P-3, P-4, P-10, P-11 |
| 5.3–5.6 — Janela e casos de borda | §17.3, §17.7, §22.6 | P-1, P-3 |
| 5.7–5.9 — Séries, paridade SQL/C#, `IClock` | §14.4, §17.6, §22.6 | — |
| 6.1 — Aciclicidade | §8.4 (B-3, B-4), §18.5 | P-8 |
| 6.2 — Genealogia backward | §18.2, §18.4 | P-16 |
| 6.3 — Genealogia forward e recall | §18.3, §22.7 | P-9, P-16 |
| 6.4–6.6 — CTE, índices, bloqueio | §9.1, §18.2, §18.3 | — |
| 7 — Simulador e tempo real | §7, §11.1, §22.8 | — |
| 8 — RBAC | §16, §22.1, §22.3 | — |
| 9 — Etiqueta QR | §22.7 | — |
| 10 — Setup e execução | §15.1 | — |
| 11 — Qualidade e testes | §14, §25 | P-1..P-16 |
| 12 — Observabilidade | §22.9, §24.1 | — |
| 13 — Segurança | §16 | — |
| 14 — Propriedade intelectual | §2 (R4), §8.6, §28.5 | — |
| 15 — Idioma e convenções | §2 (R3), §3.3 | — |
| 16 — Interface e acessibilidade | §13, §26 (S9, S10) | — |

## Confirmação das referências do design §25

A numeração planejada no design §25 foi mantida integralmente. As referências
`Validates: Requirements X.Y` existentes permanecem válidas:

| Propriedade | Referência no design | Requisito correspondente | Situação |
|---|---|---|---|
| P-1 | Requirements 5.1 | 5.1, 5.1.1 | ✔ válida |
| P-2 | Requirements 5.1 | 5.1.1 | ✔ válida |
| P-3 | Requirements 5.2 | 5.2.1 | ✔ válida |
| P-4 | Requirements 5.2 | 5.2 | ✔ válida |
| P-5 | Requirements 3.1 | 3.1 | ✔ válida |
| P-6 | Requirements 3.3 | 3.3, 3.3.1 | ✔ válida |
| P-7 | Requirements 2.1 | 2.1, 2.3 | ✔ válida |
| P-8 | Requirements 6.1 | 6.1, 6.1.2 | ✔ válida |
| P-9 | Requirements 6.2, 6.3 | 6.3.2 | ✔ válida |
| P-10 | Requirements 5.2, 4.2 | 5.2, 5.2.1 | ✔ válida |
| P-11 | Requirements 5.2, 4.2 | 5.2.2, 4.2 | ✔ válida |
| P-12 | Requirements 5.1 | 5.1.2 | ✔ válida |
| P-13 | Requirements 3.2 | 3.2 | ✔ válida |
| P-14 | Requirements 2.2 | 2.2 | ✔ válida |
| P-15 | Requirements 3.4 | 3.4.3 | ✔ válida |
| P-16 | Requirements 6.2 | 6.2.1, 6.2.2, 6.2.3 | ✔ válida |

Nenhum ajuste necessário no `design.md`.

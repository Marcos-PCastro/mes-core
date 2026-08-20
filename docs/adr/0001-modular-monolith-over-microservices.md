# ADR-0001: Build MES Core as a modular monolith

- **Status:** Accepted
- **Date:** 2026-__-__
- **Sprint:** S1

## Context

MES Core has five bounded areas: work orders, downtime, OEE, traceability and
identity. <descreva o tamanho real: nº de agregados, nº de endpoints previstos,
um único time — você — e um único banco>

<descreva a restrição operacional: o avaliador precisa rodar tudo com um
comando, em menos de 2 minutos (requirements.md 10.1)>

## Decision

<uma frase: monólito modular, separado por projeto, com regra de dependência
garantida pelo compilador>

## Consequences

### What we gain

- <deploy único, transação local, refactor de fronteira sem versionar contrato>
- <regra de dependência verificada por teste — cite DependencyRuleTests>
- <`docker compose up` em 4 serviços em vez de 12>

### What we give up

- <escala independente por módulo>
- <isolamento de falha entre áreas>
- <liberdade de stack por serviço>

## Alternatives considered

| Alternative | Why it was rejected |
|---|---|
| Microservices per bounded context | <custo operacional desproporcional ao tamanho; transação distribuída onde hoje há transação local — cite a invariante WO-2 do design.md §8.2> |
| Single project, no layer separation | <nada impediria EF Core no domínio; perde a garantia do compilador> |

## Natural seams if this ever needs to split

| Candidate service | Why it is a clean seam |
|---|---|
| Traceability | <lê e escreve só batch/batch_consumption; nenhuma invariante compartilhada com WorkOrder> |
| OEE / reporting | <somente leitura; já isolado atrás de IOeeQueryService> |
| Identity | <fronteira clássica; já isolado atrás de porta> |

## Revisions

| Date | Change |
|---|---|
| 2026-__-__ | Initial version |
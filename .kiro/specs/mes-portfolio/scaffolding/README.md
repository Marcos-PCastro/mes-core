# Scaffolding — Índice Geral

> **Para que serve este diretório:**
> O `scaffolding.md` (na pasta acima) te levou de "repositório vazio" até
> "solução .NET compilando com CI verde". A partir daqui, cada documento é um
> **sprint completo**: quais arquivos criar, em que ordem, o que vai dentro de
> cada um, e como validar antes de avançar.
>
> **Regra de ouro:** não abra dois sprints ao mesmo tempo. Feche um, faça commit,
> confirme o CI verde, e só então abra o próximo.

---

## Como usar

Cada guia de sprint segue a mesma estrutura:

| Seção | O que contém |
|---|---|
| **Objetivo** | O que existe no fim do sprint que não existia no começo |
| **Critério de pronto** | Checklist verificável, copiado do `design.md §26` |
| **Ordem de criação** | Tabela numerada: arquivo → propósito → seção de referência |
| **Passo N** | Um passo por arquivo (ou grupo coeso), com código e o *porquê* |
| **Como saber que deu certo** | Comandos concretos e resultado esperado |
| **Commits sugeridos** | Mensagens em Conventional Commits, em inglês |
| **O que você aprendeu** | Vocabulário para a entrevista |

---

## Mapa dos sprints

| # | Guia | Duração | Foco | Núcleo? |
|---|---|---|---|---|
| 0 | [`../scaffolding.md`](../scaffolding.md) | — | Repositório, solução, Docker, CI | — |
| 1 | [`sprint-01-foundation.md`](sprint-01-foundation.md) | 1 sem | Fechar a fundação: `/health`, primeiro teste, ADR-0001 | — |
| 2 | [`sprint-02-domain.md`](sprint-02-domain.md) | 2 sem | Domínio puro: `WorkOrder`, máquina de estados, `OeeCalculator` | ⭐ |
| 3 | `sprint-03-persistence.md` | 1 sem | EF Core, migrations, índices, seed, Testcontainers | — |
| 4 | `sprint-04-api-auth.md` | 2 sem | Minimal API, OpenAPI, JWT, ProblemDetails | — |
| 5 | `sprint-05-idempotency-concurrency.md` | 2 sem | Apontamento idempotente + `xmin` | 🔒 |
| 6 | `sprint-06-oee.md` | 2 sem | OEE derivado de eventos, Dapper, paridade SQL/C# | 🔒 |
| 7 | `sprint-07-genealogy.md` | 1 sem | CTE recursiva, recall, guarda de ciclo | 🔒 |
| 8 | `sprint-08-simulator-realtime.md` | 1 sem | `BackgroundService`, Polly, SignalR | — |
| 9 | `sprint-09-frontend.md` | 2 sem | React, TanStack Query, telas operacionais | — |
| 10 | `sprint-10-dashboard.md` | 1 sem | Dashboard, Recharts, tempo real na tela | — |
| 11 | `sprint-11-rbac-qr-properties.md` | 1 sem | RBAC por permissão, QR code, FsCheck | 🔒 |
| 12 | `sprint-12-polish.md` | 1 sem | README, ADRs, GIF, auditoria de publicação | 🔒 |

🔒 = **nunca cortar** · ⭐ = sprint que sustenta todos os outros

> **Guias escritos até agora:** Sprint 0 (`../scaffolding.md`), Sprint 1 e Sprint 2.
> Os demais são escritos sob demanda, um por vez, quando você chegar nele — assim
> cada guia reflete as decisões reais tomadas nos sprints anteriores em vez de
> supor o que vai acontecer.

---

## Mapa: "quero implementar X, onde está documentado?"

| O que implementar | `design.md` | `requirements.md` | Guia de sprint |
|---|---|---|---|
| Endpoint `/health` | §15.1 | 10.6 | Sprint 1 |
| Regra de dependência entre camadas | §6.1 | — | Sprint 1 |
| Entidades e invariantes | §8 | 1, 2, 3, 4, 6 | Sprint 2 |
| Máquina de estados da `WorkOrder` | §10, §10.1, §20 | 2.1–2.10 | Sprint 2 |
| Cálculo de OEE (função pura) | §17.1–17.6 | 5.1–5.6 | Sprint 2 |
| Merge de intervalos | §17.4 | 5.2 | Sprint 2 |
| Schema do banco | §9 | — | Sprint 3 |
| Índices que importam | §9.1 | 3.3.2, 4.2, 6.5 | Sprint 3 |
| `xmin` como token de concorrência | §23.2 | 3.4 | Sprint 3 (config) / 5 (uso) |
| Seed com nomes fictícios | §8.6 | 1.8, 10.5, 14.6 | Sprint 3 |
| Contratos de API | §22 | 1.1, 2.10, 3.3, 4.1–4.6 | Sprint 4 |
| `ProblemDetails` RFC 9457 | §22.9, §24.1 | 12.2, 12.5 | Sprint 4 |
| JWT e hash de senha | §16 | 13.1, 13.2, 13.3 | Sprint 4 |
| Idempotência de apontamento | §19 | 3.3, 3.3.1–3.3.4 | Sprint 5 |
| Concorrência otimista + retry | §23 | 3.4, 3.4.1–3.4.3 | Sprint 5 |
| SQL de OEE (Dapper) | §17.7 | 5.3, 5.4, 5.8 | Sprint 6 |
| Casos de borda do OEE | §17.3 | 5.5 | Sprint 6 |
| Genealogia backward/forward | §18.2, §18.3 | 6.2, 6.3 | Sprint 7 |
| Guarda de aciclicidade | §18.5 | 6.1, 6.1.1, 6.1.2 | Sprint 7 |
| Simulador de equipamento | §11.1 | 7.1–7.4 | Sprint 8 |
| SignalR hub e grupos | §22.8 | 7.5–7.9 | Sprint 8 |
| Telas operacionais | §13 | 16.5, 16.7, 16.9, 16.13 | Sprint 9 |
| Idempotency-Key no cliente | §13 (nota 1) | 3.3 | Sprint 9 |
| Dashboard e gráficos | §13 | 16.8, 16.10, 16.11 | Sprint 10 |
| Tokens semânticos de cor | — | 16.2, 16.2.1, 16.14 | Sprint 10 |
| RBAC por permissão | §16 | 8.1–8.6 | Sprint 11 |
| Etiqueta QR | §22.7 | 9.1–9.4 | Sprint 11 |
| Propriedades executáveis | §25 | 11.3 | Sprint 11 |
| README e ADRs | §27, §28.1 | 15.1, 15.2 | Sprint 12 |
| Auditoria de IP antes de publicar | §2 (R4), §28.5 | 14.1–14.7 | Sprint 12 |

---

## Estrutura final do repositório

Este é o destino. Nenhum sprint cria tudo isso — cada guia cria a sua fatia.

```
mes-core/
├─ README.md                                  # S12
├─ LICENSE                                    # S0
├─ docker-compose.yml                         # S0
├─ Directory.Build.props                      # S0
├─ .editorconfig                              # S0 / corrigido em S1
├─ .config/dotnet-tools.json                  # S3
├─ .github/
│  ├─ workflows/ci.yml                        # S0
│  └─ dependabot.yml                          # S12
├─ docs/
│  ├─ adr/000{1..5}-*.md                      # S1, S3, S4, S5, S2/S6
│  ├─ domain-primer.md                        # S12
│  ├─ ui-guidelines.md                        # S9 (início)
│  ├─ api-collection/                          # S4
│  └─ screenshots/ + demo.gif                 # S12
├─ src/
│  ├─ Mes.Domain/                             # S2
│  ├─ Mes.Application/                        # S3 (ports), S4–S7 (use cases)
│  ├─ Mes.Infrastructure/                     # S3 (EF), S6–S7 (Dapper), S8 (SignalR)
│  ├─ Mes.Api/                                # S1 (/health), S4–S8 (endpoints)
│  └─ Mes.Simulator/                          # S8
├─ tests/
│  ├─ Mes.Domain.UnitTests/                   # S1 (arquitetura), S2 (domínio)
│  ├─ Mes.Domain.PropertyTests/               # S11
│  └─ Mes.Api.IntegrationTests/               # S3 (fixture), S4–S7
└─ web/                                       # S9, S10
```

---

## Se o tempo apertar

Ordem de corte do `design.md §26.3`, resumida:

1. **Nunca corte** Sprints 5, 6, 7 e 12 — são o projeto.
2. **Nunca corte** os testes de domínio nem o mínimo de 8 propriedades.
3. **Reduza** o Sprint 9 (4 telas em vez de 9) e o Sprint 10 (cards sem gráfico).
4. **Corte** QR code, `timeseries`, Pareto e deploy em nuvem.

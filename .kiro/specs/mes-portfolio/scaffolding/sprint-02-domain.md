# Sprint 2 — Domínio puro e testes de domínio

> **Duração:** 2 semanas · **Esforço:** 16–20 h
> **Referências:** `design.md §8` (entidades e invariantes), `§10` e `§10.1`
> (máquina de estados), `§17` (OEE), `§20` (guardas), `§21` (assinaturas),
> `requirements.md 2`, `3`, `4`, `5`, `11.1`, `11.2`

---

## Objetivo

O coração do sistema, sem banco, sem HTTP, sem mock. No fim deste sprint você
tem a máquina de estados completa e o `OeeCalculator` funcionando, cobertos por
testes que rodam em menos de 3 segundos.

> **Este é o sprint mais importante do projeto.** É o código que você vai abrir
> na tela numa entrevista. Se algum sprint merecer atrasar, é este — mas por
> qualidade, não por procrastinação.

---

## Critério de pronto

- [ ] Matriz de transição do `design.md §10.1` implementada como tabela declarativa
- [ ] 48 casos de transição passando (teste parametrizado)
- [ ] `OeeCalculator.Calculate` com **todos** os casos de borda do `§17.3`
- [ ] `MergeAndMeasure` implementado e testado
- [ ] Cobertura de `Mes.Domain` > 90% (`requirements.md 11.1`)
- [ ] `dotnet test tests/Mes.Domain.UnitTests` roda em menos de 3 s (`11.2`)
- [ ] `docs/adr/0005-oee-derived-from-events.md` commitado
- [ ] `Mes.Domain` continua com **zero** `PackageReference` (teste do Sprint 1 verde)

---

## As duas regras que governam este sprint

**Regra 1 — o domínio não faz I/O.** Nada de `DbContext`, `HttpClient`,
`IRepository`. Se um método precisa de dado que não está no agregado, ele recebe
esse dado como parâmetro.

**Regra 2 — o domínio não lê o relógio.** Nenhum `DateTime.UtcNow` ou
`DateTimeOffset.Now` dentro de regra de negócio. O instante atual chega sempre
como parâmetro `DateTimeOffset now`. Sem isso, todo teste de janela de tempo
fica não determinístico.

> **Onde fica o `IClock` então?**
> Em `Mes.Application/Abstractions/IClock.cs`, criado no Sprint 3. O domínio não
> conhece nem a interface — quem chama o domínio resolve o tempo e passa o valor.
> Essa é a razão pela qual as assinaturas do `design.md §20.3` terminam em
> `DateTimeOffset now`.

---

## Ordem de criação

Siga esta ordem. Ela é topológica: cada arquivo só depende dos anteriores.

| # | Arquivo | Propósito | Referência |
|---|---|---|---|
| **Bloco A — Fundação do domínio** | | | |
| 1 | `Common/DomainException.cs` | Exceção base + `InvalidStateTransitionException` | `§24.1` |
| 2 | `Common/IDomainEvent.cs` | Contrato de evento de domínio | `§8` |
| 3 | `Common/Entity.cs` | Identidade e igualdade por id | — |
| 4 | `Common/AggregateRoot.cs` | Coleção de eventos pendentes | `§8.1` |
| **Bloco B — Catálogos e recursos** | | | |
| 5 | `Catalog/Product.cs` | `ideal_cycle_time_seconds` | `§9`, `req 1.5` |
| 6 | `Catalog/ScrapReason.cs` | Motivo de refugo | `§9` |
| 7 | `Downtimes/DowntimeReason.cs` | Categoria + `CountsAgainstAvailability` | `req 1.6` |
| 8 | `Resources/ResourceState.cs` | `Idle`/`Running`/`Down` | `§8.5` |
| 9 | `Resources/Resource.cs` | Estado como projeção | `§8.5` |
| **Bloco C — Work Order (o agregado central)** | | | |
| 10 | `WorkOrders/WorkOrderStatus.cs` | 6 estados | `§10` |
| 11 | `WorkOrders/WorkOrderAction.cs` | 8 ações | `§20.1` |
| 12 | `WorkOrders/WorkOrderTransitions.cs` | **A tabela** — fonte da verdade | `§10.1`, `§20.1` |
| 13 | `WorkOrders/ProductionSource.cs` | `Operator`/`Equipment` | `§9` |
| 14 | `WorkOrders/ProductionEntry.cs` | Entidade interna do agregado | `§8.1` |
| 15 | `WorkOrders/Events/*.cs` | `ProductionEntryRecorded`, `WorkOrderStatusChanged` | `§3.3` |
| 16 | `WorkOrders/WorkOrder.cs` | Raiz + invariantes WO-1..WO-9 | `§8.2`, `§20.2`, `§20.3` |
| **Bloco D — Paradas** | | | |
| 17 | `Downtimes/Events/*.cs` | `DowntimeStarted`, `DowntimeClosed` | `§21` |
| 18 | `Downtimes/DowntimeEvent.cs` | Invariantes DT-1..DT-4 | `§8.3`, `§21` |
| **Bloco E — Rastreabilidade** | | | |
| 19 | `Traceability/BatchStatus.cs` | `Available`/`Consumed`/`Blocked` | `§9` |
| 20 | `Traceability/Batch.cs` | Lote produzido ou comprado | `§21` |
| 21 | `Traceability/BatchConsumption.cs` | Aresta do DAG, guardas B-2/B-3 | `§8.4` |
| **Bloco F — OEE (o coração testável)** | | | |
| 22 | `Oee/TimeInterval.cs` | Value object + `ClipTo` | `§17.6` |
| 23 | `Oee/Shift.cs` | Janela de turno | `§17.2` |
| 24 | `Oee/DowntimeSlice.cs` | Intervalo + contabilidade | `§17.6` |
| 25 | `Oee/ProductionSlice.cs` | Quantidades + `C_ideal` por produto | `§17.6` |
| 26 | `Oee/OeeInput.cs` | Entrada da função pura | `§17.6` |
| 27 | `Oee/OeeResult.cs` | Saída + `Reason` + `PerformanceWasClamped` | `§17.6` |
| 28 | `Oee/IntervalMath.cs` | `MergeAndMeasure` — algoritmo do `§17.4` | `§17.4` |
| 29 | `Oee/OeeCalculator.cs` | `Calculate` — algoritmo do `§17.5` | `§17.5` |
| **Bloco G — Testes** | | | |
| 30 | `Builders/WorkOrderTestBuilder.cs` | Constrói OP em qualquer estado alcançável | `§14.2` |
| 31 | `WorkOrders/WorkOrderTransitionTests.cs` | **As 48 combinações** | `§14.2` |
| 32 | `WorkOrders/RecordProductionTests.cs` | Guardas WO-2/4/8/9 | `§20.2` |
| 33 | `Oee/IntervalMathTests.cs` | Merge, clip, borda | `§17.4` |
| 34 | `Oee/OeeCalculatorTests.cs` | Exemplos + todos os casos de borda | `§17.3` |
| 35 | `Downtimes/DowntimeEventTests.cs` | DT-1, DT-4 | `§8.3` |
| **Bloco H — Fechamento** | | | |
| 36 | `docs/adr/0005-oee-derived-from-events.md` | ADR do sprint | `§27` |

---

## Passo 1 — `Common/DomainException.cs`

Toda violação de invariante do domínio sobe como `DomainException`. No Sprint 4
o middleware traduz isso em `422` com `problem.type`. O domínio não sabe o que é
HTTP — ele só sabe que a regra foi violada e qual é o código dela.

```csharp
namespace Mes.Domain.Common;

/// <summary>
/// Raised when a domain invariant would be violated.
/// Maps to HTTP 422 Unprocessable Entity (design.md §24.1).
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Stable, kebab-case identifier used as the ProblemDetails type suffix,
    /// e.g. "overproduction-not-allowed" (design.md §22.4).
    /// Kept in the domain on purpose: the code is part of the business contract,
    /// not a presentation detail.
    /// </summary>
    public string Code { get; }

    public DomainException(string code, string message) : base(message) => Code = code;
}
```

Na mesma pasta, `Common/InvalidStateTransitionException.cs`:

```csharp
namespace Mes.Domain.Common;

/// <summary>
/// Raised when a (status, action) pair is absent from the transition table
/// (design.md §10.1). Maps to HTTP 422 / invalid-state-transition.
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public string FromStatus { get; }
    public string Action { get; }

    public InvalidStateTransitionException(string fromStatus, string action)
        : base("invalid-state-transition",
               $"Action '{action}' is not allowed while the work order is in status '{fromStatus}'.")
    {
        FromStatus = fromStatus;
        Action = action;
    }
}
```

> **Por que `Code` como string e não `enum`?**
> O código viaja para o cliente HTTP como parte da URL do `problem.type`. Um
> `enum` exigiria um mapa `enum → string` em algum lugar, e esse mapa é uma
> segunda fonte da verdade esperando divergir. String estável, escrita uma vez.

> **Por que o código de erro vive no domínio e não na API?**
> Porque `overproduction-not-allowed` é uma regra de negócio, não uma escolha de
> apresentação. Quando o cliente trata `422 overproduction-not-allowed`, ele está
> reagindo à regra. Manter o identificador junto da regra é o que garante que os
> dois nunca se separem.

---

## Passo 2 — `Common/IDomainEvent.cs`

```csharp
namespace Mes.Domain.Common;

/// <summary>
/// Something that happened in the domain, named in the past tense
/// (design.md §3.3). Published only after the transaction commits
/// (design.md §21, requirements.md 7.6).
/// </summary>
public interface IDomainEvent
{
    /// <summary>When the fact occurred, as decided by the caller — never DateTime.UtcNow.</summary>
    DateTimeOffset OccurredAt { get; }
}
```

> **Por que `OccurredAt` no evento e não `DateTime.UtcNow` no construtor?**
> Regra 2 deste sprint. Se o evento capturasse o relógio, um teste que verifica
> "o evento foi levantado com o instante X" não teria como afirmar nada.

---

## Passo 3 — `Common/Entity.cs`

```csharp
namespace Mes.Domain.Common;

/// <summary>
/// Base for entities with identity. Two entities are equal when their ids match,
/// regardless of the state of their other properties.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id.Equals(other.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
```

> **Por que `Guid.CreateVersion7()` e não `Guid.NewGuid()`?**
> UUID v7 embute o timestamp nos bits mais significativos, então os ids saem
> **ordenáveis por tempo de criação**. Num índice B-tree do Postgres, isso
> significa inserção no fim da árvore em vez de páginas aleatórias — menos
> fragmentação e menos page split. `Guid.NewGuid()` (v4) é puramente aleatório.
>
> É um detalhe pequeno com efeito real em tabela que só cresce, como
> `production_entry`. E é uma resposta muito boa quando o entrevistador pergunta
> "por que não usar `int identity` como PK?": você escolheu GUID pela geração no
> cliente (necessária para idempotência) e mitigou o custo de índice com v7.

> **Por que igualdade por id e não por valor?**
> Entidade tem identidade: a mesma `WorkOrder` continua sendo a mesma depois de
> receber um apontamento. Value object (como `TimeInterval`) é o contrário — dois
> intervalos com o mesmo início e fim **são** o mesmo intervalo. Por isso value
> object vira `record` e entidade vira `class` com `Equals` por id.

---

## Passo 4 — `Common/AggregateRoot.cs`

```csharp
namespace Mes.Domain.Common;

/// <summary>
/// An entity that is the transactional boundary of its aggregate (design.md §8.1).
/// Collects domain events; the infrastructure drains and publishes them
/// AFTER the transaction commits (design.md §21).
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called by the unit of work after a successful commit.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

> **Por que o agregado acumula eventos em vez de publicar direto?**
> Porque publicar dentro do método significa notificar antes do commit. Se a
> transação der rollback — por conflito de `xmin`, por exemplo — o dashboard já
> recebeu a notificação de uma produção que nunca existiu. Acumular e drenar
> depois do commit é o que o `requirements.md 7.6` exige.

---

## Passo 5 — `Catalog/Product.cs`

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Catalog;

/// <summary>
/// What is produced or consumed. The ideal cycle time is what makes the product
/// eligible for a performance calculation (requirements.md 1.5).
/// </summary>
public sealed class Product : Entity
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    /// <summary>
    /// Theoretical seconds per unit at nominal speed. Feeds C_ideal in the OEE
    /// performance factor (design.md §17.2).
    /// double, not decimal: this is a physical measurement, not money or a count.
    /// </summary>
    public double IdealCycleTimeSeconds { get; private set; }

    /// <summary>
    /// Per-product override of the overproduction tolerance (requirements.md 3.2).
    /// null means "use the system default".
    /// </summary>
    public decimal? OverproductionTolerance { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Product() { }   // EF Core

    public static Product Create(
        string code,
        string description,
        double idealCycleTimeSeconds,
        decimal? overproductionTolerance = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("product-code-required", "Product code is required.");

        // requirements.md 1.5 — must be greater than zero to be eligible for performance.
        if (idealCycleTimeSeconds <= 0)
            throw new DomainException(
                "ideal-cycle-time-must-be-positive",
                "Ideal cycle time must be greater than zero.");

        if (overproductionTolerance is < 0)
            throw new DomainException(
                "tolerance-must-not-be-negative",
                "Overproduction tolerance must not be negative.");

        return new Product
        {
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim() ?? string.Empty,
            IdealCycleTimeSeconds = idealCycleTimeSeconds,
            OverproductionTolerance = overproductionTolerance
        };
    }

    public void Update(string description, double idealCycleTimeSeconds)
    {
        if (idealCycleTimeSeconds <= 0)
            throw new DomainException(
                "ideal-cycle-time-must-be-positive",
                "Ideal cycle time must be greater than zero.");

        Description = description?.Trim() ?? string.Empty;
        IdealCycleTimeSeconds = idealCycleTimeSeconds;
    }

    /// <summary>Soft delete (requirements.md 1.3). History that references it stays intact.</summary>
    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
```

> **Por que `double` para segundos e `decimal` para quantidade?**
> `decimal` é base 10 e exato — é o tipo certo para contagem e dinheiro, onde
> `0.1 + 0.2` tem que dar exatamente `0.3`. `double` é base 2 e mais rápido, e é
> o tipo certo para medição física, onde já existe erro de medição maior que o
> erro de representação.
>
> A regra prática: **quantidade de peça é `decimal`, duração em segundos é
> `double`**. Misturar os dois no mesmo cálculo exige conversão explícita — e no
> `OeeCalculator` você vai ver exatamente onde essa fronteira está.

> **Por que `= null!` nas strings e `private Product()`?**
> O construtor privado sem parâmetros existe para o EF Core materializar a
> entidade ao ler do banco. O `null!` diz ao compilador "eu garanto que a factory
> preenche isso" — sem ele, o `Nullable=enable` do Sprint 1 reclamaria. É o padrão
> canônico para entidade com factory + ORM.

> **Por que `ToUpperInvariant()` no código?**
> `WIDGET-100` e `widget-100` são o mesmo produto para o operador. Normalizar na
> entrada evita duplicata que o índice único não pega. `Invariant` e não
> `ToUpper()` porque a cultura do servidor não deve influenciar — em turco,
> `ToUpper()` de `i` não é `I`.

---

## Passo 6 — `Catalog/ScrapReason.cs`

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Catalog;

/// <summary>Controlled vocabulary for scrap: DIMENSIONAL, VISUAL, CONTAMINATION...</summary>
public sealed class ScrapReason : Entity
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private ScrapReason() { }

    public static ScrapReason Create(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("scrap-reason-code-required", "Scrap reason code is required.");

        return new ScrapReason
        {
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim() ?? string.Empty
        };
    }

    public void Update(string description) => Description = description?.Trim() ?? string.Empty;
    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
```

> **Por que catálogo em vez de campo de texto livre?**
> `requirements.md 1` inteiro existe por isso. Com texto livre, o Pareto de
> refugo do Sprint 10 fica inútil: `dimensional`, `Dimensional`, `dimencional` e
> `dim.` viram quatro categorias. Vocabulário controlado é o que torna o
> indicador agregável.

---

## Passo 7 — `Downtimes/DowntimeReason.cs`

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Downtimes;

public enum DowntimeCategory
{
    Planned = 1,
    Unplanned = 2
}

/// <summary>
/// Controlled vocabulary for downtime: SETUP, TOOL-CHANGE, MECH-FAILURE...
/// requirements.md 1.6.
/// </summary>
public sealed class DowntimeReason : Entity
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DowntimeCategory Category { get; private set; }

    /// <summary>
    /// Whether this downtime penalises Availability (design.md §17.2).
    /// A planned maintenance window scheduled outside the shift should NOT count;
    /// a mechanical failure during the shift must.
    /// This flag is what stops OEE from punishing the plant for planned work.
    /// </summary>
    public bool CountsAgainstAvailability { get; private set; }

    public bool IsActive { get; private set; } = true;

    private DowntimeReason() { }

    public static DowntimeReason Create(
        string code,
        string description,
        DowntimeCategory category,
        bool countsAgainstAvailability)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("downtime-reason-code-required", "Downtime reason code is required.");

        return new DowntimeReason
        {
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim() ?? string.Empty,
            Category = category,
            CountsAgainstAvailability = countsAgainstAvailability
        };
    }

    public void Update(string description, DowntimeCategory category, bool countsAgainstAvailability)
    {
        Description = description?.Trim() ?? string.Empty;
        Category = category;
        CountsAgainstAvailability = countsAgainstAvailability;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
```

> **`Category` e `CountsAgainstAvailability` são a mesma coisa?**
> Não, e essa é a pegadinha. `Category` é classificação para relatório
> (quanto do tempo perdido foi planejado?). `CountsAgainstAvailability` é
> comportamento de cálculo. Existe parada `Planned` que **conta** — uma troca de
> ferramenta programada dentro do turno consome tempo de produção real. Amarrar
> os dois num único campo é o tipo de acoplamento que obriga a alterar o cálculo
> quando muda a classificação. Separados, cada um muda por conta própria.

---

## Passo 8 — `Resources/ResourceState.cs` e `Resources/Resource.cs`

```csharp
namespace Mes.Domain.Resources;

public enum ResourceState
{
    Idle = 1,
    Running = 2,
    Down = 3
}

public enum ResourceType
{
    Line = 1,
    Machine = 2,
    Cell = 3
}
```

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Resources;

/// <summary>
/// Where production happens. State is a PROJECTION, never the source of truth
/// (design.md §8.5):
///   Down    ⟺ an open DowntimeEvent exists for this resource
///   Running ⟺ not Down and a WorkOrder is InProgress on this resource
///   Idle    ⟺ neither
/// The column is a cache for fast dashboards. An integration test must prove it
/// is always reconstructible from the events.
/// </summary>
public sealed class Resource : Entity
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public ResourceType ResourceType { get; private set; }
    public ResourceState State { get; private set; } = ResourceState.Idle;
    public bool IsActive { get; private set; } = true;

    private Resource() { }

    public static Resource Create(string code, string description, ResourceType resourceType)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("resource-code-required", "Resource code is required.");

        return new Resource
        {
            Code = code.Trim().ToUpperInvariant(),
            Description = description?.Trim() ?? string.Empty,
            ResourceType = resourceType
        };
    }

    public void Update(string description, ResourceType resourceType)
    {
        Description = description?.Trim() ?? string.Empty;
        ResourceType = resourceType;
    }

    /// <summary>
    /// Recomputes the cached state from the facts. Pure function of its arguments:
    /// the caller (a handler) queries the two conditions and passes the answers in.
    /// The resource never queries anything itself.
    /// </summary>
    public void ProjectState(bool hasOpenDowntime, bool hasWorkOrderInProgress)
    {
        State = hasOpenDowntime
            ? ResourceState.Down
            : hasWorkOrderInProgress
                ? ResourceState.Running
                : ResourceState.Idle;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
```

> **Por que `ProjectState` recebe dois booleanos em vez de consultar o banco?**
> Regra 1 do sprint. As duas condições vivem em outros agregados
> (`DowntimeEvent` e `WorkOrder`), então quem sabe respondê-las é o handler, que
> tem repositório. O agregado recebe as respostas e aplica a regra de precedência
> — que é a única parte que é conhecimento de domínio.
>
> Esse padrão se repete em `Start` e `Complete` da `WorkOrder`, e é uma pergunta
> de entrevista frequente: *"onde você põe uma regra que precisa consultar outro
> agregado?"* Resposta: a **decisão** fica no domínio, a **consulta** fica no
> handler.

> **Por que persistir um cache que pode divergir?**
> Porque o dashboard precisa listar 20 recursos com o estado de cada um sem
> disparar 40 subconsultas. É uma otimização consciente, e o `design.md §8.5`
> exige um teste de integração provando que a coluna é sempre reconstruível. A
> armadilha do MES legado é justamente estado digitado que divergiu e ninguém
> consegue mais reconciliar — aqui o estado é derivável, e isso é testado.

---

## Passo 9 — `WorkOrders/WorkOrderStatus.cs` e `WorkOrderAction.cs`

```csharp
namespace Mes.Domain.WorkOrders;

/// <summary>The six states of design.md §10. Cast to string when persisted.</summary>
public enum WorkOrderStatus
{
    Draft = 1,
    Released = 2,
    InProgress = 3,
    Paused = 4,
    Completed = 5,
    Cancelled = 6
}
```

```csharp
namespace Mes.Domain.WorkOrders;

/// <summary>The eight actions of design.md §10.1.</summary>
public enum WorkOrderAction
{
    Release = 1,
    Unrelease = 2,
    Start = 3,
    RecordProduction = 4,
    Pause = 5,
    Resume = 6,
    Complete = 7,
    Cancel = 8
}
```

> **Por que valores explícitos (`= 1`) em vez do default do C#?**
> Sem valor explícito, inserir `Paused` no meio do enum renumera tudo que vem
> depois. Se em algum momento o valor numérico for persistido ou serializado,
> dados antigos passam a significar outra coisa. Vamos persistir como string
> (`design.md §23.2`), mas fixar o número custa zero e remove a classe inteira de
> bug.

---

## Passo 10 — `WorkOrders/WorkOrderTransitions.cs` — a tabela

Este é o arquivo mais importante do sprint. É a matriz do `design.md §10.1`
transcrita como dado, não como código de controle.

```csharp
using System.Collections.Frozen;
using Mes.Domain.Common;
using static Mes.Domain.WorkOrders.WorkOrderAction;
using static Mes.Domain.WorkOrders.WorkOrderStatus;

namespace Mes.Domain.WorkOrders;

/// <summary>
/// The state machine of design.md §10.1, as a single declarative table.
///
/// One table, not a cascade of switch statements. Consequences:
///   - the parametrised test generates all 6 x 8 = 48 cases straight from the enums
///   - GET /api/work-orders/{id}/allowed-actions derives from here (requirements.md 2.9)
///   - the frontend enables buttons from that endpoint, so the rule lives in one place
///
/// Absence of a (from, action) key means the transition is forbidden.
/// </summary>
internal static class WorkOrderTransitions
{
    private static readonly FrozenDictionary<(WorkOrderStatus From, WorkOrderAction Action), WorkOrderStatus> Map =
        new Dictionary<(WorkOrderStatus, WorkOrderAction), WorkOrderStatus>
        {
            // Draft
            [(Draft, Release)] = Released,
            [(Draft, Cancel)] = Cancelled,

            // Released
            [(Released, Unrelease)] = Draft,        // guard: zero entries
            [(Released, Start)] = InProgress,       // guard: no other InProgress on the resource
            [(Released, Cancel)] = Cancelled,

            // InProgress
            [(InProgress, RecordProduction)] = InProgress,   // self-loop
            [(InProgress, Pause)] = Paused,
            [(InProgress, Complete)] = Completed,   // guard: no open downtime on the resource
            [(InProgress, Cancel)] = Cancelled,

            // Paused
            [(Paused, Resume)] = InProgress,
            [(Paused, Complete)] = Completed,       // guard: no open downtime on the resource
            [(Paused, Cancel)] = Cancelled,

            // Completed and Cancelled are absorbing terminal states:
            // no key at all, so every action is rejected (requirements.md 2.2).
        }.ToFrozenDictionary();

    public static bool IsAllowed(WorkOrderStatus from, WorkOrderAction action) =>
        Map.ContainsKey((from, action));

    public static WorkOrderStatus Target(WorkOrderStatus from, WorkOrderAction action) =>
        Map.TryGetValue((from, action), out var to)
            ? to
            : throw new InvalidStateTransitionException(from.ToString(), action.ToString());

    /// <summary>Actions valid for the current status. Backs requirements.md 2.9.</summary>
    public static IReadOnlySet<WorkOrderAction> AllowedActionsFor(WorkOrderStatus from) =>
        Enum.GetValues<WorkOrderAction>()
            .Where(action => IsAllowed(from, action))
            .ToHashSet();
}
```

Como a tabela é `internal` e o teste da matriz precisa lê-la, adicione ao
`src/Mes.Domain/Mes.Domain.csproj`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Mes.Domain.UnitTests" />
</ItemGroup>
```

> **Por que `internal` em vez de `public`?**
> Porque a tabela é mecânica interna do agregado. Se ela fosse pública, um
> handler poderia consultá-la e decidir a transição por conta própria, e a
> `WorkOrder` deixaria de ser a autoridade sobre o próprio estado. A superfície
> pública é `WorkOrder.AllowedActions()` — quem precisa saber as ações válidas
> pergunta ao agregado, não à tabela.
>
> O `InternalsVisibleTo` para o teste é o mínimo necessário para provar a tabela
> diretamente. Tradeoff assumido: o teste conhece um detalhe interno, em troca de
> testar a fonte da verdade em vez de um proxy.

> **Por que `FrozenDictionary`?**
> É uma coleção imutável otimizada para leitura, introduzida no .NET 8. Ela paga
> um custo maior de construção (uma vez, no `static`) e devolve lookup mais rápido
> que `Dictionary`. Para uma tabela lida em toda transição e nunca modificada, é
> exatamente o caso de uso. `ReadOnlyDictionary` só embrulharia um `Dictionary`
> sem ganho de performance.

> **Por que `Completed` e `Cancelled` simplesmente não aparecem?**
> Porque estado absorvente é a **ausência** de transição, não uma lista de
> proibições. Se você escrevesse 8 entradas `[(Completed, X)] = throw`, teria 16
> linhas dizendo "não" — e a próxima pessoa a adicionar uma ação teria que
> lembrar de adicionar duas negações. Com ausência, a proibição é automática:
> nova ação já nasce proibida nos terminais. `requirements.md 2.2` sai de graça.

---

## Passo 11 — `WorkOrders/ProductionSource.cs` e `ProductionEntry.cs`

```csharp
namespace Mes.Domain.WorkOrders;

/// <summary>Who reported the production. Feeds the source column (design.md §9).</summary>
public enum ProductionSource
{
    Operator = 1,
    Equipment = 2
}
```

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.WorkOrders;

/// <summary>
/// A single production report: good units + scrap at a point in time.
///
/// Internal entity of the WorkOrder aggregate (design.md §8.1): it has no
/// repository of its own and is never created outside WorkOrder.RecordProduction.
/// That single write path is what makes invariant WO-2 hold.
/// </summary>
public sealed class ProductionEntry : Entity
{
    public Guid WorkOrderId { get; private set; }
    public decimal GoodQuantity { get; private set; }
    public decimal ScrapQuantity { get; private set; }
    public Guid? ScrapReasonId { get; private set; }

    /// <summary>
    /// When the production actually happened. This is what classifies the entry
    /// into an OEE window (design.md §17.7, requirements.md 5.3).
    /// </summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// When the system received the report. Transport metadata: deliberately NOT
    /// part of the idempotency payload hash (design.md §19.6).
    /// </summary>
    public DateTimeOffset RecordedAt { get; private set; }

    public Guid RecordedByUserId { get; private set; }
    public ProductionSource Source { get; private set; }

    /// <summary>
    /// Client-generated key, unique per work order. The row IS the idempotency
    /// record — there is no separate table, so the insert is atomic by construction
    /// (design.md §19.2).
    /// </summary>
    public string IdempotencyKey { get; private set; } = null!;

    /// <summary>
    /// SHA-256 of the canonical business payload. Same key + different hash → 409
    /// (design.md §19.6). Computed by the handler, stored here.
    /// </summary>
    public string PayloadHash { get; private set; } = null!;

    public decimal TotalQuantity => GoodQuantity + ScrapQuantity;

    private ProductionEntry() { }

    /// <summary>
    /// internal on purpose: only WorkOrder.RecordProduction may build one.
    /// All guards live in the aggregate root, so there is exactly one place
    /// where an entry can come into existence.
    /// </summary>
    internal static ProductionEntry Create(
        Guid workOrderId,
        decimal goodQuantity,
        decimal scrapQuantity,
        Guid? scrapReasonId,
        DateTimeOffset occurredAt,
        DateTimeOffset recordedAt,
        Guid recordedByUserId,
        ProductionSource source,
        string idempotencyKey,
        string payloadHash) => new()
        {
            WorkOrderId = workOrderId,
            GoodQuantity = goodQuantity,
            ScrapQuantity = scrapQuantity,
            ScrapReasonId = scrapReasonId,
            OccurredAt = occurredAt,
            RecordedAt = recordedAt,
            RecordedByUserId = recordedByUserId,
            Source = source,
            IdempotencyKey = idempotencyKey,
            PayloadHash = payloadHash
        };
}
```

> **Por que `internal static Create` em vez de `public`?**
> Se qualquer código pudesse instanciar `ProductionEntry`, alguém eventualmente
> criaria uma sem passar pela raiz — e os totais da `WorkOrder` divergiriam da
> soma dos apontamentos. A invariante WO-2 (`design.md §8.2`) só é garantida
> porque existe **um único caminho de escrita**. `internal` é o compilador
> segurando essa porta.

> **Por que `OccurredAt` e `RecordedAt` separados?**
> Porque são fatos diferentes. `OccurredAt` é quando a peça saiu da máquina;
> `RecordedAt` é quando o sistema soube. Num apontamento retroativo, eles diferem
> em horas. Todo cálculo por período usa `OccurredAt` (`requirements.md 5.3`), e o
> hash de idempotência usa `OccurredAt` e ignora `RecordedAt`
> (`requirements.md 3.3.4`) — porque o mesmo apontamento re-tentado 3 s depois é o
> mesmo apontamento.
>
> Colapsar os dois num único campo é o erro que faz relatório retroativo cair no
> período errado, e é praticamente impossível de descobrir depois.

---

## Passo 12 — `WorkOrders/Events/*.cs`

Nome no passado, sempre (`design.md §3.3`). Cada evento carrega o mínimo para o
consumidor decidir o que invalidar — não o dado calculado (`design.md §7`).

`src/Mes.Domain/WorkOrders/Events/ProductionEntryRecorded.cs`:

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.WorkOrders.Events;

/// <summary>
/// Raised exactly once per accepted production entry. A replay of an
/// idempotency key raises NOTHING (requirements.md 3.3.1).
///
/// Payload is a notification, not computed data: the client invalidates its
/// query and refetches, so the displayed OEE always comes from the same code
/// path (design.md §7, requirements.md 7.7).
/// </summary>
public sealed record ProductionEntryRecorded(
    Guid WorkOrderId,
    Guid ResourceId,
    Guid EntryId,
    decimal GoodQuantity,
    decimal ScrapQuantity,
    DateTimeOffset OccurredAt) : IDomainEvent;
```

`src/Mes.Domain/WorkOrders/Events/WorkOrderStatusChanged.cs`:

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.WorkOrders.Events;

public sealed record WorkOrderStatusChanged(
    Guid WorkOrderId,
    Guid ResourceId,
    WorkOrderStatus FromStatus,
    WorkOrderStatus ToStatus,
    DateTimeOffset OccurredAt) : IDomainEvent;
```

> **Por que `record` para evento?**
> Evento é fato imutável. `record` te dá imutabilidade, igualdade por valor e
> `ToString()` legível de graça — o que ajuda muito quando o evento aparece num
> log ou numa asserção de teste falhando.

> **Por que o evento não carrega o OEE novo?**
> Porque aí existiriam dois caminhos de cálculo: o do evento e o da consulta.
> Eles divergem, e você descobre quando o supervisor vê números diferentes em
> duas telas. O evento diz "mudou algo no recurso X"; o cliente refaz a leitura.
> Uma fonte de verdade, testada uma vez.
>
> Essa é a resposta pronta para *"como você garante que o tempo real não divirja
> da consulta?"*.

---

## Passo 13 — `WorkOrders/WorkOrder.cs` — a raiz do agregado

O arquivo central do domínio. Implementa as invariantes WO-1 a WO-9 do
`design.md §8.2` e as guardas do `§20.2`.

```csharp
using Mes.Domain.Catalog;
using Mes.Domain.Common;
using Mes.Domain.Resources;
using Mes.Domain.WorkOrders.Events;

namespace Mes.Domain.WorkOrders;

/// <summary>
/// Authorisation to produce a quantity of a product on a resource.
/// Aggregate root: ProductionEntry lives inside this boundary and is never
/// written without going through RecordProduction (design.md §8.1).
///
/// Invariants WO-1..WO-9 — design.md §8.2.
/// Transition guards      — design.md §20.2.
/// </summary>
public sealed class WorkOrder : AggregateRoot
{
    /// <summary>Default overproduction tolerance: 5% (requirements.md 3.2).</summary>
    public const decimal DefaultOverproductionTolerance = 0.05m;

    private readonly List<ProductionEntry> _entries = [];

    public string Code { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Guid ResourceId { get; private set; }

    public decimal PlannedQuantity { get; private set; }
    public decimal ProducedGoodQuantity { get; private set; }
    public decimal ProducedScrapQuantity { get; private set; }

    public WorkOrderStatus Status { get; private set; } = WorkOrderStatus.Draft;

    public DateTimeOffset? ScheduledStart { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public decimal OverproductionTolerance { get; private set; } = DefaultOverproductionTolerance;

    public string? CancellationReason { get; private set; }

    public IReadOnlyList<ProductionEntry> Entries => _entries;

    /// <summary>Maximum good quantity accepted, tolerance included (invariant WO-4).</summary>
    public decimal MaxAcceptableGoodQuantity => PlannedQuantity * (1 + OverproductionTolerance);

    private WorkOrder() { }   // EF Core

    // ─────────────────────────────────────────────────────────────────────
    //  Creation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Preconditions:  code is non-empty; plannedQuantity > 0 (invariant WO-1)
    /// Postconditions: Status == Draft; totals are zero; no events raised
    /// </summary>
    public static WorkOrder Create(
        string code,
        Guid productId,
        Guid resourceId,
        decimal plannedQuantity,
        DateTimeOffset? scheduledStart = null,
        decimal? overproductionTolerance = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("work-order-code-required", "Work order code is required.");

        // Invariant WO-1
        if (plannedQuantity <= 0)
            throw new DomainException(
                "planned-quantity-must-be-positive",
                "Planned quantity must be greater than zero.");

        if (overproductionTolerance is < 0)
            throw new DomainException(
                "tolerance-must-not-be-negative",
                "Overproduction tolerance must not be negative.");

        return new WorkOrder
        {
            Code = code.Trim().ToUpperInvariant(),
            ProductId = productId,
            ResourceId = resourceId,
            PlannedQuantity = plannedQuantity,
            ScheduledStart = scheduledStart,
            OverproductionTolerance = overproductionTolerance ?? DefaultOverproductionTolerance
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Guards (design.md §20.2): plannedQuantity > 0, resource active, product active.
    /// The resource and the product are passed in because the aggregate does no I/O.
    /// </summary>
    public void Release(Resource resource, Product product, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(product);

        if (!resource.IsActive)
            throw new DomainException("resource-inactive", "The resource is inactive.");

        if (!product.IsActive)
            throw new DomainException("product-inactive", "The product is inactive.");

        if (PlannedQuantity <= 0)
            throw new DomainException(
                "planned-quantity-must-be-positive",
                "Planned quantity must be greater than zero.");

        TransitionTo(WorkOrderAction.Release, now);
    }

    /// <summary>Guard: no entries yet (requirements.md 2.5).</summary>
    public void Unrelease(DateTimeOffset now)
    {
        if (_entries.Count > 0)
            throw new DomainException(
                "cannot-unrelease-with-entries",
                "The work order already has production entries and cannot be unreleased.");

        TransitionTo(WorkOrderAction.Unrelease, now);
    }

    /// <summary>
    /// Guard checked by the HANDLER, not here: no other work order InProgress on
    /// the same resource (409 resource-busy). That guard needs a repository query,
    /// and the aggregate does no I/O — see design.md §20.3.
    ///
    /// Postconditions: Status == InProgress; StartedAt set exactly once (requirements.md 2.6)
    /// </summary>
    public void Start(DateTimeOffset now)
    {
        TransitionTo(WorkOrderAction.Start, now);
        StartedAt ??= now;   // never overwritten on a later Resume
    }

    public void Pause(string? note, DateTimeOffset now) =>
        TransitionTo(WorkOrderAction.Pause, now);

    public void Resume(DateTimeOffset now) =>
        TransitionTo(WorkOrderAction.Resume, now);

    /// <summary>
    /// Guard checked by the HANDLER: no open DowntimeEvent on the resource
    /// (409 open-downtime-must-be-closed, requirements.md 2.7).
    ///
    /// Postconditions: Status == Completed; CompletedAt == now; aggregate is terminal
    /// </summary>
    public void Complete(DateTimeOffset now)
    {
        TransitionTo(WorkOrderAction.Complete, now);
        CompletedAt = now;
    }

    /// <summary>
    /// Permission workorder:cancel is enforced at the endpoint (requirements.md 2.8).
    /// Authorisation is not a domain concern — the domain does not know who is calling.
    /// </summary>
    public void Cancel(string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("cancellation-reason-required", "A cancellation reason is required.");

        TransitionTo(WorkOrderAction.Cancel, now);
        CancellationReason = reason.Trim();
        CompletedAt = now;   // invariant WO-6: terminal states carry a closing timestamp
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Production reporting — the heart of invariant WO-2
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Preconditions:
    ///   Status == InProgress                                        (WO-3)
    ///   goodQty >= 0, scrapQty >= 0, goodQty + scrapQty > 0         (WO-8 partial)
    ///   scrapQty > 0  =>  scrapReasonId is not null                 (WO-8)
    ///   StartedAt <= occurredAt <= now + clockSkewTolerance         (WO-9)
    ///   ProducedGoodQuantity + goodQty <= MaxAcceptableGoodQuantity (WO-4)
    /// Postconditions:
    ///   _entries.Count == old + 1
    ///   ProducedGoodQuantity  == old + goodQty
    ///   ProducedScrapQuantity == old + scrapQty                     (WO-2)
    ///   exactly one ProductionEntryRecorded raised
    ///   Status unchanged (InProgress self-loop)
    /// </summary>
    public ProductionEntry RecordProduction(
        decimal goodQuantity,
        decimal scrapQuantity,
        Guid? scrapReasonId,
        DateTimeOffset occurredAt,
        string idempotencyKey,
        string payloadHash,
        ProductionSource source,
        Guid userId,
        DateTimeOffset now,
        TimeSpan clockSkewTolerance)
    {
        // WO-3 — only InProgress accepts production.
        // Goes through the table so the error is identical to any other bad transition.
        if (!WorkOrderTransitions.IsAllowed(Status, WorkOrderAction.RecordProduction))
            throw new DomainException(
                "work-order-not-in-progress",
                $"Production can only be recorded while the work order is InProgress. Current status: {Status}.");

        if (goodQuantity < 0 || scrapQuantity < 0)
            throw new DomainException(
                "quantity-must-not-be-negative",
                "Quantities must not be negative.");

        // requirements.md 3.5
        if (goodQuantity + scrapQuantity == 0)
            throw new DomainException(
                "empty-production-entry",
                "A production entry must report at least one good or scrap unit.");

        // WO-8 / requirements.md 3.6
        if (scrapQuantity > 0 && scrapReasonId is null)
            throw new DomainException(
                "scrap-reason-required",
                "A scrap reason is required when scrap quantity is greater than zero.");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainException("missing-idempotency-key", "An idempotency key is required.");

        // WO-9 / requirements.md 3.8 — the tolerance covers clock skew between
        // the shop-floor collector and the server.
        var latestAcceptable = now + clockSkewTolerance;

        if (StartedAt is { } startedAt && occurredAt < startedAt)
            throw new DomainException(
                "occurred-at-out-of-range",
                $"occurredAt {occurredAt:O} is before the work order started at {startedAt:O}.");

        if (occurredAt > latestAcceptable)
            throw new DomainException(
                "occurred-at-out-of-range",
                $"occurredAt {occurredAt:O} is in the future. Server time is {now:O}.");

        // WO-4 / requirements.md 3.2
        var newGoodTotal = ProducedGoodQuantity + goodQuantity;
        if (newGoodTotal > MaxAcceptableGoodQuantity)
            throw new DomainException(
                "overproduction-not-allowed",
                $"Recording {goodQuantity} good units would bring the total to {newGoodTotal}, " +
                $"exceeding the planned quantity of {PlannedQuantity} by more than the " +
                $"{OverproductionTolerance:P0} tolerance.");

        var entry = ProductionEntry.Create(
            workOrderId: Id,
            goodQuantity: goodQuantity,
            scrapQuantity: scrapQuantity,
            scrapReasonId: scrapReasonId,
            occurredAt: occurredAt,
            recordedAt: now,
            recordedByUserId: userId,
            source: source,
            idempotencyKey: idempotencyKey,
            payloadHash: payloadHash);

        _entries.Add(entry);

        // WO-2: the totals and the sum of entries move together, always.
        ProducedGoodQuantity = newGoodTotal;
        ProducedScrapQuantity += scrapQuantity;

        Raise(new ProductionEntryRecorded(
            WorkOrderId: Id,
            ResourceId: ResourceId,
            EntryId: entry.Id,
            GoodQuantity: goodQuantity,
            ScrapQuantity: scrapQuantity,
            OccurredAt: occurredAt));

        return entry;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  State machine
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Backs GET /api/work-orders/{id}/allowed-actions (requirements.md 2.9).</summary>
    public IReadOnlySet<WorkOrderAction> AllowedActions() =>
        WorkOrderTransitions.AllowedActionsFor(Status);

    /// <summary>
    /// The ONLY place Status is assigned. Every public lifecycle method funnels
    /// through here, so no transition can bypass the table (invariant WO-5).
    /// </summary>
    private void TransitionTo(WorkOrderAction action, DateTimeOffset now)
    {
        var from = Status;
        var to = WorkOrderTransitions.Target(from, action);

        if (from == to)
            return;   // self-loop (RecordProduction): no status change, no event

        Status = to;

        Raise(new WorkOrderStatusChanged(
            WorkOrderId: Id,
            ResourceId: ResourceId,
            FromStatus: from,
            ToStatus: to,
            OccurredAt: now));
    }
}
```

### Três decisões neste arquivo que valem defender

**1. As guardas que consultam outro agregado não estão aqui.**
`Start` não verifica se o recurso está ocupado; `Complete` não verifica se há
parada aberta. Essas duas perguntas exigem repositório, e o agregado não faz I/O.
O handler pergunta, e só chama o agregado se a resposta permitir.

Isso é o que mantém os testes de domínio em milissegundos. E é a resposta para
*"onde você põe uma regra que precisa consultar outro agregado?"*: a decisão fica
no domínio, a consulta fica no handler.

**2. `TransitionTo` é o único lugar que atribui `Status`.**
Procure por `Status =` no arquivo: aparece uma vez. Isso é o que torna a
invariante WO-5 verificável por leitura, não por confiança. Se alguém adicionar
um método novo, ele **tem** que passar pela tabela — não existe outro caminho.

**3. A tolerância de superprodução é 5% e configurável, não zero.**
Rejeitar qualquer excesso irrita o operador: a última caixa quase sempre tem peça
extra. Aceitar qualquer valor perde o controle. Tolerância explícita e
configurável é a resposta madura, e o `detail` da exceção informa o valor exato —
o operador entende o que aconteceu sem abrir ticket.

---

## Passo 14 — `Downtimes/Events/*.cs` e `Downtimes/DowntimeEvent.cs`

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Downtimes.Events;

public sealed record DowntimeStarted(
    Guid DowntimeId,
    Guid ResourceId,
    Guid DowntimeReasonId,
    DateTimeOffset StartedAt,
    DateTimeOffset OccurredAt) : IDomainEvent;
```

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Downtimes.Events;

public sealed record DowntimeClosed(
    Guid DowntimeId,
    Guid ResourceId,
    DateTimeOffset EndedAt,
    double DurationSeconds,
    DateTimeOffset OccurredAt) : IDomainEvent;
```

`src/Mes.Domain/Downtimes/DowntimeEvent.cs`:

```csharp
using Mes.Domain.Common;
using Mes.Domain.Downtimes.Events;

namespace Mes.Domain.Downtimes;

/// <summary>
/// An interval in which the resource did not produce, with a reason.
///
/// Its own aggregate, independent of the work order (design.md §8.1):
/// a machine can stop during setup or maintenance with no order open
/// (requirements.md 4.7).
///
/// Invariants DT-1..DT-4 — design.md §8.3.
/// </summary>
public sealed class DowntimeEvent : AggregateRoot
{
    public Guid ResourceId { get; private set; }
    public Guid DowntimeReasonId { get; private set; }

    /// <summary>Optional: the machine may stop with no order open (requirements.md 4.7).</summary>
    public Guid? WorkOrderId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>null means the downtime is still open.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    public string? Note { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;

    /// <summary>Invariant DT-2 is enforced in the database by a unique partial index:
    /// UNIQUE (resource_id) WHERE ended_at IS NULL — see design.md §9.1.</summary>
    public bool IsOpen => EndedAt is null;

    private DowntimeEvent() { }

    /// <summary>
    /// Preconditions:  reasonId is not empty (invariant DT-3)
    ///                 startedAt <= now + clockSkewTolerance
    ///                 no open downtime for resourceId — enforced by the unique
    ///                 partial index and checked by the handler
    /// Postconditions: IsOpen == true; DowntimeStarted raised
    /// </summary>
    public static DowntimeEvent Open(
        Guid resourceId,
        Guid reasonId,
        Guid? workOrderId,
        DateTimeOffset startedAt,
        string? note,
        string idempotencyKey,
        DateTimeOffset now,
        TimeSpan clockSkewTolerance)
    {
        // Invariant DT-3 — requirements.md 4.1
        if (reasonId == Guid.Empty)
            throw new DomainException(
                "downtime-reason-required",
                "A downtime reason is required when opening a downtime.");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainException("missing-idempotency-key", "An idempotency key is required.");

        if (startedAt > now + clockSkewTolerance)
            throw new DomainException(
                "started-at-out-of-range",
                $"startedAt {startedAt:O} is in the future. Server time is {now:O}.");

        var downtime = new DowntimeEvent
        {
            ResourceId = resourceId,
            DowntimeReasonId = reasonId,
            WorkOrderId = workOrderId,
            StartedAt = startedAt,
            Note = note?.Trim(),
            IdempotencyKey = idempotencyKey
        };

        downtime.Raise(new DowntimeStarted(
            DowntimeId: downtime.Id,
            ResourceId: resourceId,
            DowntimeReasonId: reasonId,
            StartedAt: startedAt,
            OccurredAt: now));

        return downtime;
    }

    /// <summary>
    /// Preconditions:  IsOpen == true (invariant DT-4)
    ///                 endedAt > StartedAt (invariant DT-1)
    /// Postconditions: IsOpen == false; DurationSeconds > 0; DowntimeClosed raised;
    ///                 the aggregate is immutable from here on
    /// </summary>
    public void Close(DateTimeOffset endedAt, DateTimeOffset now)
    {
        // Invariant DT-4 — requirements.md 4.4. A closed downtime is immutable:
        // rewriting ended_at would falsify the availability history.
        if (!IsOpen)
            throw new DomainException(
                "downtime-already-closed",
                $"The downtime was already closed at {EndedAt:O}.");

        // Invariant DT-1 — requirements.md 4.3
        if (endedAt <= StartedAt)
            throw new DomainException(
                "ended-before-started",
                $"endedAt {endedAt:O} must be after startedAt {StartedAt:O}.");

        EndedAt = endedAt;

        Raise(new DowntimeClosed(
            DowntimeId: Id,
            ResourceId: ResourceId,
            EndedAt: endedAt,
            DurationSeconds: DurationSeconds(now),
            OccurredAt: now));
    }

    /// <summary>
    /// Duration so far. An OPEN downtime counts up to `now` — a stoppage in
    /// progress is real lost time, and the OEE must reflect it (design.md §17.3).
    /// </summary>
    public double DurationSeconds(DateTimeOffset now) =>
        ((EndedAt ?? now) - StartedAt).TotalSeconds;
}
```

> **Por que não existe um método para reabrir ou corrigir uma parada fechada?**
> Invariante DT-4. Se `ended_at` pudesse ser reescrito, a disponibilidade
> histórica passaria a ser uma opinião. Correção de dado errado é caso de
> registro compensatório com trilha, não de edição silenciosa — e isso está fora
> do escopo (`design.md §4.2`).
>
> Relacionado: `requirements.md 4.6` exige um endpoint que **lista** paradas
> abertas há muito tempo, e proíbe fechá-las automaticamente. Inventar um
> `ended_at` falsificaria o histórico. Listar e deixar o supervisor decidir é a
> escolha correta, e é um bom exemplo de "o sistema não adivinha".

> **Por que `IsOpen` não é uma coluna `is_open`?**
> Porque seria derivável de `ended_at IS NULL`, e duas fontes para o mesmo fato
> divergem. Além disso, o índice único parcial do Postgres
> (`UNIQUE (resource_id) WHERE ended_at IS NULL`) só funciona porque a condição
> está na coluna real.

---

## Passo 15 — `Traceability/BatchStatus.cs`, `Batch.cs`, `BatchConsumption.cs`

```csharp
namespace Mes.Domain.Traceability;

public enum BatchStatus
{
    Available = 1,
    Consumed = 2,
    Blocked = 3
}
```

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Traceability;

/// <summary>
/// A traceable quantity of material with its own identity.
/// Invariant B-1: Code is globally unique (unique index).
/// Invariant B-5: a produced batch belongs to exactly one WorkOrder;
///                a purchased batch has no work order.
/// </summary>
public sealed class Batch : AggregateRoot
{
    public string Code { get; private set; } = null!;
    public Guid ProductId { get; private set; }

    /// <summary>null for a purchased batch (raw material bought, not produced).</summary>
    public Guid? WorkOrderId { get; private set; }

    public decimal Quantity { get; private set; }
    public BatchStatus Status { get; private set; } = BatchStatus.Available;
    public DateTimeOffset ProducedAt { get; private set; }
    public string? BlockReason { get; private set; }

    private Batch() { }

    public static Batch Produce(
        string code, Guid productId, Guid workOrderId, decimal quantity, DateTimeOffset producedAt) =>
        CreateCore(code, productId, workOrderId, quantity, producedAt);

    public static Batch Purchase(
        string code, Guid productId, decimal quantity, DateTimeOffset receivedAt) =>
        CreateCore(code, productId, workOrderId: null, quantity, receivedAt);

    private static Batch CreateCore(
        string code, Guid productId, Guid? workOrderId, decimal quantity, DateTimeOffset producedAt)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("batch-code-required", "Batch code is required.");

        if (quantity <= 0)
            throw new DomainException(
                "batch-quantity-must-be-positive",
                "Batch quantity must be greater than zero.");

        return new Batch
        {
            Code = code.Trim().ToUpperInvariant(),
            ProductId = productId,
            WorkOrderId = workOrderId,
            Quantity = quantity,
            ProducedAt = producedAt
        };
    }

    /// <summary>Used by the recall flow (requirements.md 6.6).</summary>
    public void Block(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("block-reason-required", "A block reason is required.");

        Status = BatchStatus.Blocked;
        BlockReason = reason.Trim();
    }

    public void Unblock()
    {
        Status = BatchStatus.Available;
        BlockReason = null;
    }
}
```

```csharp
using Mes.Domain.Common;

namespace Mes.Domain.Traceability;

/// <summary>
/// One edge of the genealogy DAG: producedBatchId consumed consumedBatchId.
///
/// Invariant B-2: quantity > 0                    (requirements.md 6.1.3)
/// Invariant B-3: a batch cannot consume itself   (requirements.md 6.1.1)
/// Invariant B-4: the graph stays acyclic         — checked by the handler BEFORE
///                the write, because it needs a reachability query (design.md §18.5)
/// </summary>
public sealed class BatchConsumption : Entity
{
    /// <summary>The batch that consumed — the child in the "produced from" direction.</summary>
    public Guid ProducedBatchId { get; private set; }

    /// <summary>The component that was consumed.</summary>
    public Guid ConsumedBatchId { get; private set; }

    public decimal Quantity { get; private set; }
    public DateTimeOffset ConsumedAt { get; private set; }

    private BatchConsumption() { }

    /// <summary>
    /// Preconditions:  producedBatchId != consumedBatchId       (B-3)
    ///                 quantity > 0                            (B-2)
    ///                 no path consumedBatchId -> producedBatchId (B-4, handler)
    /// </summary>
    public static BatchConsumption Create(
        Guid producedBatchId, Guid consumedBatchId, decimal quantity, DateTimeOffset consumedAt)
    {
        // Invariant B-3 — requirements.md 6.1.1
        if (producedBatchId == consumedBatchId)
            throw new DomainException("self-consumption", "A batch cannot consume itself.");

        // Invariant B-2 — requirements.md 6.1.3
        if (quantity <= 0)
            throw new DomainException(
                "consumed-quantity-must-be-positive",
                "Consumed quantity must be greater than zero.");

        return new BatchConsumption
        {
            ProducedBatchId = producedBatchId,
            ConsumedBatchId = consumedBatchId,
            Quantity = quantity,
            ConsumedAt = consumedAt
        };
    }
}
```

> **Por que a guarda de ciclo (B-4) não está aqui, junto das outras duas?**
> Porque detectar ciclo exige percorrer o grafo, e o grafo está no banco.
> Autoconsumo (B-3) é verificável olhando dois `Guid`; ciclo indireto
> (`A → B → C → A`) não é.
>
> Mesma linha das guardas de `Start` e `Complete`: o que dá para verificar com o
> que está em memória fica no domínio; o que exige consulta fica no handler. O
> Sprint 7 implementa o `ExistsPath` que fecha essa guarda.

> **Por que aresta como linha em vez de campo `parent_id` no lote?**
> Porque um lote é produzido a partir de **vários** componentes. `parent_id`
> modelaria uma árvore com um pai só. Aresta como linha modela um DAG com
> múltiplos pais e múltiplos filhos, que é o que genealogia de fato é. Escolher a
> modelagem certa aqui é o que faz o recall do Sprint 7 ser uma CTE limpa em vez
> de uma gambiarra.

---

## Passo 16 — `Oee/TimeInterval.cs`

Value object, não entidade: dois intervalos com o mesmo início e fim **são** o
mesmo intervalo. Por isso `record`.

```csharp
namespace Mes.Domain.Oee;

/// <summary>
/// A half-open interval [Start, End). Half-open on purpose: with an inclusive
/// upper bound, an event exactly on the boundary would be counted in two
/// consecutive windows and the sum of the periods would not match the total
/// (design.md §17.7, requirements.md 5.4). That bug is real and very hard to
/// find after the fact.
/// </summary>
public sealed record TimeInterval
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public TimeInterval(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
            throw new ArgumentException(
                $"Interval end {end:O} must not be before start {start:O}.", nameof(end));

        Start = start;
        End = end;
    }

    public double DurationSeconds => (End - Start).TotalSeconds;

    public bool IsEmpty => End <= Start;

    /// <summary>True when the two intervals share at least one instant.
    /// Uses strict inequality on both sides, consistent with half-open semantics:
    /// [10:00, 11:00) and [11:00, 12:00) do NOT intersect.</summary>
    public bool Intersects(TimeInterval other) => Start < other.End && other.Start < End;

    public bool Contains(DateTimeOffset instant) => instant >= Start && instant < End;

    /// <summary>
    /// This interval restricted to the window, or null when they do not overlap.
    /// Used to clip a downtime that crosses the window boundary (design.md §17.3).
    /// </summary>
    public TimeInterval? ClipTo(TimeInterval window)
    {
        var start = Start > window.Start ? Start : window.Start;
        var end = End < window.End ? End : window.End;

        return start < end ? new TimeInterval(start, end) : null;
    }

    public override string ToString() => $"[{Start:O}, {End:O})";
}
```

> **Por que `ClipTo` devolve `null` em vez de um intervalo vazio?**
> Porque `null` é impossível de somar por acidente. Um intervalo vazio ainda é um
> objeto que entra na lista e participa do merge; `null` força o chamador a
> decidir o que fazer, e o compilador cobra isso com `Nullable=enable`. Tornar o
> estado inválido inexpressável é melhor que representá-lo e lembrar de filtrar.

> **Por que `ToString()` customizado?**
> Quando uma asserção de merge de intervalos falha, a mensagem mostra
> `[2026-03-14T06:00:00Z, 2026-03-14T14:00:00Z)` em vez de
> `Mes.Domain.Oee.TimeInterval`. Cinco linhas que economizam muito tempo de
> depuração — e mais ainda quando o FsCheck do Sprint 11 imprimir um
> contraexemplo minimizado.

---

## Passo 17 — `Oee/Shift.cs`, `DowntimeSlice.cs`, `ProductionSlice.cs`

```csharp
namespace Mes.Domain.Oee;

/// <summary>
/// A scheduled work window. The union of the shifts intersected with the analysis
/// window is T_planned (design.md §17.2).
/// When no shift is configured, T_planned = T_total, i.e. a 24x7 window —
/// documented default behaviour (requirements.md 5.2.3).
/// </summary>
public sealed record Shift(TimeInterval Interval, string? Name = null);
```

```csharp
namespace Mes.Domain.Oee;

/// <summary>
/// A downtime interval plus whether it penalises availability.
/// Slices with CountsAgainstAvailability = false are excluded from T_down
/// (requirements.md 5.2.4).
/// </summary>
public sealed record DowntimeSlice(
    TimeInterval Interval,
    bool CountsAgainstAvailability,
    string? ReasonCode = null);
```

```csharp
namespace Mes.Domain.Oee;

/// <summary>
/// Production aggregated BY PRODUCT, because C_ideal differs per product and the
/// performance factor has to be weighted (design.md §17.3, last row):
///     P = SUM(C_ideal_i * Q_total_i) / T_run
/// A single C_ideal for a resource that ran two products would simply be wrong.
/// </summary>
public sealed record ProductionSlice(
    decimal GoodQuantity,
    decimal ScrapQuantity,
    double IdealCycleTimeSeconds,
    string? ProductCode = null)
{
    public decimal TotalQuantity => GoodQuantity + ScrapQuantity;
}
```

---

## Passo 18 — `Oee/OeeInput.cs` e `Oee/OeeResult.cs`

```csharp
namespace Mes.Domain.Oee;

/// <summary>
/// Everything CalculateOee needs. Note `Now`: the current instant arrives as
/// DATA, never read from the ambient clock. That is what makes the calculation
/// deterministic in a test (requirements.md 5.9).
/// </summary>
public sealed record OeeInput(
    TimeInterval Window,
    IReadOnlyList<Shift> Shifts,
    IReadOnlyList<DowntimeSlice> Downtimes,
    IReadOnlyList<ProductionSlice> ProductionByProduct,
    DateTimeOffset Now);
```

```csharp
namespace Mes.Domain.Oee;

/// <summary>Why a factor came back null. Surfaced to the client as `reason`.</summary>
public static class OeeReason
{
    public const string NoPlannedProductionTime = "NoPlannedProductionTime";
    public const string MissingIdealCycleTime = "MissingIdealCycleTime";
}

/// <summary>
/// Result of the pure calculation. Nothing here is ever persisted as a column of
/// truth (requirements.md 5.1) — the OEE is always recomputed from the events.
///
/// decimal for the factors: they are ratios shown to a human and compared against
/// a threshold, so exactness beats speed.
/// double for the seconds: physical measurement.
/// </summary>
public sealed record OeeResult
{
    public bool HasData { get; init; }

    public decimal? Availability { get; init; }
    public decimal? Performance { get; init; }
    public decimal? Quality { get; init; }
    public decimal? Oee { get; init; }

    public double PlannedSeconds { get; init; }
    public double DownSeconds { get; init; }
    public double RunSeconds { get; init; }

    public decimal GoodQuantity { get; init; }
    public decimal ScrapQuantity { get; init; }
    public decimal TotalQuantity => GoodQuantity + ScrapQuantity;

    /// <summary>
    /// true means the plant produced faster than the ideal cycle time, which means
    /// the product's ideal_cycle_time_seconds is misconfigured. Clamping alone
    /// would hide bad data; the flag turns it into information (requirements.md 5.5).
    /// </summary>
    public bool PerformanceWasClamped { get; init; }

    public string? Reason { get; init; }

    /// <summary>
    /// No planned production time in the window. Factors are null, NOT zero:
    /// zero means "the machine performed terribly", null means "nothing was
    /// scheduled". That distinction is what the supervisor needs
    /// (requirements.md 5.5, first row).
    /// </summary>
    public static OeeResult NoData(string reason, double plannedSeconds = 0) => new()
    {
        HasData = false,
        Reason = reason,
        PlannedSeconds = plannedSeconds
    };

    /// <summary>Availability and quality are known; performance is not (missing C_ideal).</summary>
    public static OeeResult Partial(
        decimal availability,
        decimal? quality,
        string reason,
        double plannedSeconds,
        double downSeconds,
        double runSeconds,
        decimal goodQuantity,
        decimal scrapQuantity) => new()
        {
            HasData = true,
            Availability = availability,
            Performance = null,
            Quality = quality,
            Oee = null,
            Reason = reason,
            PlannedSeconds = plannedSeconds,
            DownSeconds = downSeconds,
            RunSeconds = runSeconds,
            GoodQuantity = goodQuantity,
            ScrapQuantity = scrapQuantity
        };
}
```

> **Por que `HasData: false` e não uma exceção, quando não há produção?**
> `requirements.md 5.6`. Ausência de produção é resposta legítima, não erro. Um
> turno que não foi programado tem OEE indefinido, e o endpoint responde `200` com
> `hasData: false`. Responder `404` diria "o recurso não existe", que é falso.
>
> A distinção `null` vs `0` é o detalhe que mais rende conversa em entrevista:
> `0` significa "produziu muito mal"; `null` significa "não havia o que produzir".
> Um supervisor toma decisões opostas nos dois casos.

---

## Passo 19 — `Oee/IntervalMath.cs` — o merge de intervalos

Implementação do algoritmo do `design.md §17.4`. É a base de `T_down` e de
`T_planned`, e é o algoritmo mais testável do projeto.

```csharp
namespace Mes.Domain.Oee;

/// <summary>
/// Interval set operations. Pure functions, no ambient time, no I/O.
///
/// Why this file exists at all: T_down is the length of the UNION of the downtime
/// intervals, never the SUM. Summing would count the same minute twice whenever
/// two stoppages overlap, and could produce T_down > T_planned — a negative run
/// time (design.md §17.3).
/// </summary>
public static class IntervalMath
{
    /// <summary>
    /// Length of the union of `intervals`, clipped to `window`, in seconds.
    ///
    /// Complexity: O(n log n), dominated by the sort.
    ///
    /// Preconditions:  window.Start &lt; window.End
    /// Postconditions: 0 &lt;= result &lt;= window.DurationSeconds
    ///                 result &lt;= sum of the clipped durations   (union &lt;= sum)
    ///                 monotonic: adding an interval never decreases the result
    ///                 commutative: independent of input order
    /// Each of those four is a property test — see design.md §25 (P-10).
    /// </summary>
    public static double MergeAndMeasure(
        IReadOnlyList<TimeInterval> intervals,
        TimeInterval window)
    {
        ArgumentNullException.ThrowIfNull(intervals);
        ArgumentNullException.ThrowIfNull(window);

        var merged = Merge(intervals, window);

        var total = 0d;
        foreach (var interval in merged)
            total += interval.DurationSeconds;

        // Defensive: floating point accumulation must never exceed the window.
        return Math.Min(total, window.DurationSeconds);
    }

    /// <summary>
    /// The union of `intervals` clipped to `window`, as a minimal ordered list of
    /// disjoint intervals. Returned (rather than just the length) because the OEE
    /// calculator needs to intersect downtimes with shifts.
    /// </summary>
    public static IReadOnlyList<TimeInterval> Merge(
        IReadOnlyList<TimeInterval> intervals,
        TimeInterval window)
    {
        // ── Step 1: clip each interval to the window, drop the ones that miss ──
        var clipped = new List<TimeInterval>(intervals.Count);
        foreach (var interval in intervals)
        {
            if (interval.ClipTo(window) is { } piece)
                clipped.Add(piece);
        }

        if (clipped.Count == 0)
            return [];

        // ── Step 2: order by start ─────────────────────────────────────────
        clipped.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        // ── Step 3: linear sweep accumulating the union ────────────────────
        var result = new List<TimeInterval>(clipped.Count);
        var currentStart = clipped[0].Start;
        var currentEnd = clipped[0].End;

        for (var i = 1; i < clipped.Count; i++)
        {
            // LOOP INVARIANT:
            //   (a) `result` holds the union of clipped[0..i-1] minus the current block
            //   (b) [currentStart, currentEnd) is the block being merged, and
            //       currentStart <= clipped[i].Start  (guaranteed by the sort)
            var interval = clipped[i];

            if (interval.Start <= currentEnd)
            {
                // Overlaps or touches: extend the current block.
                // `<=` and not `<` on purpose: [10:00,11:00) and [11:00,12:00)
                // are contiguous, so their union is [10:00,12:00) — one block.
                if (interval.End > currentEnd)
                    currentEnd = interval.End;
            }
            else
            {
                // Gap: close the current block and start a new one.
                result.Add(new TimeInterval(currentStart, currentEnd));
                currentStart = interval.Start;
                currentEnd = interval.End;
            }
        }

        result.Add(new TimeInterval(currentStart, currentEnd));
        return result;
    }

    /// <summary>
    /// Intersection of two interval sets. Used to discard downtime that falls
    /// outside any shift: a stoppage at 3am on an unmanned line must not penalise
    /// availability (design.md §17.5, step 2).
    /// </summary>
    public static IReadOnlyList<TimeInterval> Intersect(
        IReadOnlyList<TimeInterval> left,
        IReadOnlyList<TimeInterval> right)
    {
        var result = new List<TimeInterval>();

        foreach (var a in left)
        {
            foreach (var b in right)
            {
                if (a.ClipTo(b) is { } overlap)
                    result.Add(overlap);
            }
        }

        return result;
    }
}
```

> **Por que `interval.Start <= currentEnd` e não `<`?**
> Com `<`, os intervalos `[10:00, 11:00)` e `[11:00, 12:00)` seriam tratados como
> dois blocos separados, e a união daria 7200 s em dois pedaços em vez de um bloco
> de 7200 s. O total é o mesmo, mas `Merge` deixaria de devolver a lista
> **mínima** de intervalos disjuntos — e o `Intersect` com turnos passaria a
> gerar fragmentos desnecessários. Contíguo e sobreposto se fundem igual.

> **Por que este algoritmo é bom de mencionar em entrevista?**
> "Merge de intervalos sobrepostos" é exercício clássico de entrevista, normalmente
> feito sem contexto. Aqui ele tem motivo real: é o que impede o OEE de reportar
> tempo de parada maior que o tempo planejado. Poder dizer *"implementei merge de
> intervalos porque somar paradas sobrepostas gerava disponibilidade negativa"* é
> muito mais forte que ter resolvido o mesmo problema num quadro branco.

> **Por que devolver a lista e também ter `MergeAndMeasure`?**
> Porque o `OeeCalculator` precisa dos dois: a lista para intersectar paradas com
> turnos, e o total para calcular `Availability`. Separar as duas
> responsabilidades mantém cada função com uma tarefa e ambas testáveis
> isoladamente.

---

## Passo 20 — `Oee/OeeCalculator.cs`

Implementação do algoritmo do `design.md §17.5`. Função pura: mesma entrada,
mesma saída, sempre. Sem I/O, sem relógio ambiente, sem estado.

```csharp
namespace Mes.Domain.Oee;

/// <summary>
/// OEE = Availability x Performance x Quality, computed as a PURE FUNCTION of the
/// events in the window (design.md §17, ADR-0005).
///
/// There is no `oee` column anywhere in the schema. The number is always
/// recomputable, always auditable, and always explainable to the operator
/// ("availability dropped because of the 40-minute TOOL-CHANGE stoppage").
///
/// A retroactive production entry needs no special handling: the next query
/// already reflects it. That falls out for free from not persisting the indicator.
/// </summary>
public static class OeeCalculator
{
    private const double TimeTolerance = 1e-6;

    public static OeeResult Calculate(OeeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // ── PRECONDITIONS ────────────────────────────────────────────────
        // Inverted window is a programming error, not a data problem, so it
        // throws instead of returning NoData. The endpoint maps it to
        // 400 invalid-time-window (requirements.md 5.5).
        if (input.Window.End <= input.Window.Start)
            throw new ArgumentException(
                $"Window end {input.Window.End:O} must be after start {input.Window.Start:O}.",
                nameof(input));

        // ── STEP 1: planned production time ──────────────────────────────
        var shiftIntervals = input.Shifts.Select(s => s.Interval).ToArray();

        // No shift configured => 24x7 window. Documented default (requirements.md 5.2.3).
        var plannedSeconds = shiftIntervals.Length == 0
            ? input.Window.DurationSeconds
            : IntervalMath.MergeAndMeasure(shiftIntervals, input.Window);

        if (plannedSeconds <= 0)
            return OeeResult.NoData(OeeReason.NoPlannedProductionTime);

        // ── STEP 2: countable downtime (union, clipped) ───────────────────
        var countable = input.Downtimes
            .Where(d => d.CountsAgainstAvailability)              // requirements.md 5.2.4
            .Select(d => ClampOpenDowntime(d.Interval, input))
            .ToArray();

        // A stoppage outside any shift must not penalise availability.
        if (shiftIntervals.Length > 0)
        {
            var shiftUnion = IntervalMath.Merge(shiftIntervals, input.Window);
            countable = IntervalMath.Intersect(countable, shiftUnion).ToArray();
        }

        var downSeconds = IntervalMath.MergeAndMeasure(countable, input.Window);

        // Defensive clamp. If this fires, a stoppage outside the shift was
        // misclassified — the caller logs a warning (requirements.md 12.4).
        if (downSeconds > plannedSeconds)
            downSeconds = plannedSeconds;

        // ── STEP 3: run time ─────────────────────────────────────────────
        var runSeconds = Math.Max(0d, plannedSeconds - downSeconds);

        // ── STEP 4: quantities ───────────────────────────────────────────
        var goodQuantity = 0m;
        var scrapQuantity = 0m;
        foreach (var slice in input.ProductionByProduct)
        {
            if (slice.GoodQuantity < 0 || slice.ScrapQuantity < 0)
                throw new ArgumentException("Production quantities must not be negative.", nameof(input));

            goodQuantity += slice.GoodQuantity;
            scrapQuantity += slice.ScrapQuantity;
        }

        var totalQuantity = goodQuantity + scrapQuantity;

        // ── STEP 5: Availability ─────────────────────────────────────────
        var availability = Ratio(runSeconds, plannedSeconds);

        // ── STEP 6: Quality ──────────────────────────────────────────────
        // Computed before Performance so the Partial() path below can carry it.
        // null, not zero: there is no quality without a single unit produced.
        decimal? quality = totalQuantity == 0
            ? null
            : goodQuantity / totalQuantity;

        // ── STEP 7: Performance ──────────────────────────────────────────
        // A product that produced something but has no ideal cycle time makes the
        // performance factor unknowable. Do not estimate — missing data is missing
        // data (requirements.md 5.5).
        var hasProducedWithoutCycleTime = input.ProductionByProduct
            .Any(p => p.TotalQuantity > 0 && p.IdealCycleTimeSeconds <= 0);

        if (hasProducedWithoutCycleTime)
        {
            return OeeResult.Partial(
                availability: availability,
                quality: quality,
                reason: OeeReason.MissingIdealCycleTime,
                plannedSeconds: plannedSeconds,
                downSeconds: downSeconds,
                runSeconds: runSeconds,
                goodQuantity: goodQuantity,
                scrapQuantity: scrapQuantity);
        }

        decimal performance;
        var performanceWasClamped = false;

        if (runSeconds <= 0)
        {
            // The whole window was downtime: no run time, so no performance.
            performance = 0m;
        }
        else
        {
            // Theoretical time weighted per product. Performance uses Q_TOTAL
            // (good + scrap), not Q_good: the machine spent a cycle producing the
            // scrapped unit too. The quality loss is counted once, in the Q factor.
            // Using Q_good here would penalise scrap twice — this is the canonical
            // Nakajima definition (design.md §17.2).
            var theoreticalSeconds = 0d;
            foreach (var slice in input.ProductionByProduct)
                theoreticalSeconds += slice.IdealCycleTimeSeconds * (double)slice.TotalQuantity;

            performance = Ratio(theoreticalSeconds, runSeconds);

            if (performance > 1m)
            {
                performance = 1m;
                performanceWasClamped = true;
            }
        }

        // ── STEP 8: OEE ──────────────────────────────────────────────────
        // Quality is null when nothing was produced, but the period's OEE is 0,
        // not null: a shift that produced nothing did perform at zero
        // (requirements.md 5.5, second row).
        var oee = quality is { } q
            ? availability * performance * q
            : 0m;

        // ── POSTCONDITIONS ───────────────────────────────────────────────
        // Kept as asserts so a future "optimisation" that changes the semantics
        // fails loudly in Debug. The property tests of Sprint 11 assert the same
        // facts over generated input (design.md §25, P-1..P-4).
        System.Diagnostics.Debug.Assert(availability is >= 0m and <= 1m);
        System.Diagnostics.Debug.Assert(performance is >= 0m and <= 1m);
        System.Diagnostics.Debug.Assert(quality is null or (>= 0m and <= 1m));
        System.Diagnostics.Debug.Assert(oee is >= 0m and <= 1m);
        System.Diagnostics.Debug.Assert(oee <= availability);
        System.Diagnostics.Debug.Assert(oee <= performance);
        System.Diagnostics.Debug.Assert(
            Math.Abs(plannedSeconds - (runSeconds + downSeconds)) < TimeTolerance);

        return new OeeResult
        {
            HasData = true,
            Availability = availability,
            Performance = performance,
            Quality = quality,
            Oee = oee,
            PlannedSeconds = plannedSeconds,
            DownSeconds = downSeconds,
            RunSeconds = runSeconds,
            GoodQuantity = goodQuantity,
            ScrapQuantity = scrapQuantity,
            PerformanceWasClamped = performanceWasClamped,
            Reason = null
        };
    }

    /// <summary>
    /// An open downtime (ended_at IS NULL) counts up to min(now, windowEnd).
    /// The query layer already applies COALESCE, but doing it here as well keeps
    /// the calculator correct when called directly from a test
    /// (design.md §17.3, "Parada aberta").
    /// </summary>
    private static TimeInterval ClampOpenDowntime(TimeInterval interval, OeeInput input)
    {
        var cap = input.Now < input.Window.End ? input.Now : input.Window.End;
        return interval.End > cap && interval.Start < cap
            ? new TimeInterval(interval.Start, cap)
            : interval;
    }

    /// <summary>
    /// Ratio as decimal, clamped to [0,1]. The conversion double -> decimal happens
    /// here and nowhere else: seconds are measured (double), factors are reported
    /// (decimal). One conversion point means one place to reason about precision.
    /// </summary>
    private static decimal Ratio(double numerator, double denominator)
    {
        if (denominator <= 0)
            return 0m;

        var value = (decimal)(numerator / denominator);
        return value < 0m ? 0m : value > 1m ? 1m : value;
    }
}
```

### O que defender neste arquivo

**`Performance` usa `Q_total`, não `Q_good`.** A máquina gastou ciclo para
produzir a peça refugada também. A perda de qualidade é contabilizada uma única
vez, no fator `Q`. Se `P` usasse `Q_good`, o refugo seria penalizado duas vezes e
o OEE ficaria artificialmente baixo. Essa é a definição canônica (Nakajima), e é
o detalhe que separa quem entendeu o conceito de quem copiou a fórmula.

**`T_planned == T_run + T_down`, sempre.** Está no `Debug.Assert` e vira
propriedade executável no Sprint 11 (P-3). É a conservação de tempo: se ela
quebra, algum recorte ou clamp está errado, e o número reportado não fecha.

**`null` e `0` significam coisas diferentes, em três lugares.**

| Situação | `Availability` | `Performance` | `Quality` | `Oee` |
|---|---|---|---|---|
| Nada programado na janela | `null` | `null` | `null` | `null` |
| Programado, nada produzido | calculado | `0` | `null` | `0` |
| Produziu, mas falta `C_ideal` | calculado | `null` | calculado | `null` |

Cada linha responde uma pergunta diferente do supervisor. Colapsar as três em
`0` transformaria "não havia turno" em "a máquina foi péssima".

**`PerformanceWasClamped` transforma dado ruim em informação.** Performance acima
de 1 significa que o `ideal_cycle_time_seconds` do produto está cadastrado errado.
Clampar sem sinalizar esconderia o problema para sempre; clampar **e** sinalizar
faz o cadastro errado aparecer na tela.

> **Por que `Debug.Assert` e não `if (...) throw`?**
> `Debug.Assert` é removido em Release, então não custa nada em produção. Ele
> documenta a postcondição no código e falha alto durante desenvolvimento e nos
> testes. A verificação que **precisa** valer em produção — janela invertida,
> quantidade negativa — é `throw` de verdade, alguns blocos acima.

---

## Passo 21 — `Builders/WorkOrderTestBuilder.cs`

Antes dos testes, o builder. Ele monta uma `WorkOrder` em qualquer estado
**usando apenas os métodos públicos** — ou seja, caminhando pelas transições
reais.

Primeiro, amplie o `tests/Mes.Domain.UnitTests/GlobalUsings.cs`. `DomainException`
e `InvalidStateTransitionException` aparecem em quase toda asserção deste sprint,
então vale um `global using`:

```csharp
global using FluentAssertions;
global using Mes.Domain.Common;
global using Xunit;
```

```csharp
using Mes.Domain.Catalog;
using Mes.Domain.Resources;
using Mes.Domain.WorkOrders;

namespace Mes.Domain.UnitTests.Builders;

/// <summary>
/// Builds a WorkOrder in any reachable status by walking the REAL transitions.
///
/// Deliberate choice: no reflection, no internal setter, no test-only backdoor.
/// If a status cannot be reached through the public API, it is not reachable in
/// production either — and the builder failing tells you that. This is the same
/// idea as property P-7 (state machine reachability, design.md §25).
/// </summary>
public sealed class WorkOrderTestBuilder
{
    // Fixed instant: every time-dependent assertion is deterministic.
    public static readonly DateTimeOffset Now =
        new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);

    public static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);

    private string _code = "WO-2026-0001";
    private decimal _plannedQuantity = 1_000m;
    private decimal? _tolerance;
    private readonly Guid _productId = Guid.CreateVersion7();
    private readonly Guid _resourceId = Guid.CreateVersion7();

    public static WorkOrderTestBuilder A() => new();

    public WorkOrderTestBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public WorkOrderTestBuilder WithPlannedQuantity(decimal quantity)
    {
        _plannedQuantity = quantity;
        return this;
    }

    public WorkOrderTestBuilder WithTolerance(decimal tolerance)
    {
        _tolerance = tolerance;
        return this;
    }

    public WorkOrder Build() =>
        WorkOrder.Create(_code, _productId, _resourceId, _plannedQuantity, Now, _tolerance);

    /// <summary>
    /// A work order in the requested status, reached through real transitions.
    /// Throws for a status that cannot be reached — which is itself information.
    /// </summary>
    public WorkOrder InStatus(WorkOrderStatus status)
    {
        var workOrder = Build();
        var product = ActiveProduct();
        var resource = ActiveResource();

        switch (status)
        {
            case WorkOrderStatus.Draft:
                break;

            case WorkOrderStatus.Released:
                workOrder.Release(resource, product, Now);
                break;

            case WorkOrderStatus.InProgress:
                workOrder.Release(resource, product, Now);
                workOrder.Start(Now);
                break;

            case WorkOrderStatus.Paused:
                workOrder.Release(resource, product, Now);
                workOrder.Start(Now);
                workOrder.Pause(note: null, Now);
                break;

            case WorkOrderStatus.Completed:
                workOrder.Release(resource, product, Now);
                workOrder.Start(Now);
                workOrder.Complete(Now);
                break;

            case WorkOrderStatus.Cancelled:
                workOrder.Cancel("cancelled by test setup", Now);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unhandled status.");
        }

        workOrder.ClearDomainEvents();   // setup noise must not pollute assertions
        return workOrder;
    }

    /// <summary>An InProgress work order that already has one production entry.
    /// Needed to exercise the Unrelease guard (requirements.md 2.5).</summary>
    public WorkOrder InProgressWithEntry(decimal good = 10m, decimal scrap = 0m)
    {
        var workOrder = InStatus(WorkOrderStatus.InProgress);

        workOrder.RecordProduction(
            goodQuantity: good,
            scrapQuantity: scrap,
            scrapReasonId: scrap > 0 ? Guid.CreateVersion7() : null,
            occurredAt: Now,
            idempotencyKey: Guid.CreateVersion7().ToString(),
            payloadHash: "test-hash",
            source: ProductionSource.Operator,
            userId: Guid.CreateVersion7(),
            now: Now,
            clockSkewTolerance: ClockSkew);

        workOrder.ClearDomainEvents();
        return workOrder;
    }

    public static Product ActiveProduct(double idealCycleTimeSeconds = 30d) =>
        Product.Create("WIDGET-100", "Test widget", idealCycleTimeSeconds);

    public static Product InactiveProduct()
    {
        var product = ActiveProduct();
        product.Deactivate();
        return product;
    }

    public static Resource ActiveResource() =>
        Resource.Create("LINE-A", "Test line", ResourceType.Line);

    public static Resource InactiveResource()
    {
        var resource = ActiveResource();
        resource.Deactivate();
        return resource;
    }
}
```

> **Por que `Now` é uma constante e não `DateTimeOffset.UtcNow`?**
> Porque a Regra 2 do sprint só tem valor se o teste também respeitar. Com
> instante fixo, a asserção "`occurredAt` fora da janela é rejeitado" tem sempre o
> mesmo resultado. Com `UtcNow`, ela passaria 364 dias por ano e falharia na
> virada do horário de verão.

> **Por que `ClearDomainEvents()` depois do setup?**
> Porque montar uma OP em `InProgress` levanta dois `WorkOrderStatusChanged`. Se o
> teste depois afirmar "exatamente um evento foi levantado", ele contaria os do
> setup. Limpar deixa a asserção falar só sobre o que o teste está exercitando.

> **Por que nomes fictícios (`WIDGET-100`, `LINE-A`) mesmo em teste?**
> `requirements.md 14.3` e `14.6`. A restrição de propriedade intelectual vale para
> o repositório inteiro, e código de teste é código commitado. Usar a tabela do
> `design.md §8.6` desde o começo evita ter que auditar e reescrever no Sprint 12.

---

## Passo 22 — `WorkOrders/WorkOrderTransitionTests.cs` — as 48 combinações

O teste mais rentável do projeto: 6 estados × 8 ações, gerados a partir dos
`enum`, cada combinação com resultado esperado declarado.

```csharp
using Mes.Domain.UnitTests.Builders;
using Mes.Domain.WorkOrders;

namespace Mes.Domain.UnitTests.WorkOrders;

/// <summary>
/// Validates requirements.md 2.1, 2.2 and 2.3 — the transition matrix of
/// design.md §10.1.
///
/// The expected results live in ONE place (ExpectedAllowed) and the 48 cases are
/// generated from the enums. Adding a status or an action makes this test fail
/// until the matrix is updated on purpose — which is exactly what you want.
/// </summary>
public sealed class WorkOrderTransitionTests
{
    // The matrix of design.md §10.1, transcribed once.
    // Only the ALLOWED pairs are listed; everything else must be rejected.
    private static readonly HashSet<(WorkOrderStatus, WorkOrderAction)> ExpectedAllowed =
    [
        (WorkOrderStatus.Draft,      WorkOrderAction.Release),
        (WorkOrderStatus.Draft,      WorkOrderAction.Cancel),

        (WorkOrderStatus.Released,   WorkOrderAction.Unrelease),
        (WorkOrderStatus.Released,   WorkOrderAction.Start),
        (WorkOrderStatus.Released,   WorkOrderAction.Cancel),

        (WorkOrderStatus.InProgress, WorkOrderAction.RecordProduction),
        (WorkOrderStatus.InProgress, WorkOrderAction.Pause),
        (WorkOrderStatus.InProgress, WorkOrderAction.Complete),
        (WorkOrderStatus.InProgress, WorkOrderAction.Cancel),

        (WorkOrderStatus.Paused,     WorkOrderAction.Resume),
        (WorkOrderStatus.Paused,     WorkOrderAction.Complete),
        (WorkOrderStatus.Paused,     WorkOrderAction.Cancel),

        // Completed and Cancelled appear nowhere: absorbing terminal states.
    ];

    public static TheoryData<WorkOrderStatus, WorkOrderAction, bool> TransitionMatrix()
    {
        var data = new TheoryData<WorkOrderStatus, WorkOrderAction, bool>();

        foreach (var status in Enum.GetValues<WorkOrderStatus>())
            foreach (var action in Enum.GetValues<WorkOrderAction>())
                data.Add(status, action, ExpectedAllowed.Contains((status, action)));

        return data;
    }

    [Theory]
    [MemberData(nameof(TransitionMatrix))]
    public void Transition_table_matches_the_specification(
        WorkOrderStatus from, WorkOrderAction action, bool expectedAllowed)
    {
        WorkOrderTransitions.IsAllowed(from, action).Should().Be(expectedAllowed);
    }

    [Fact]
    public void Matrix_covers_exactly_48_combinations()
    {
        // Guards the test itself: if a status or an action is added without
        // revisiting the matrix, this count changes and the test says so.
        var statuses = Enum.GetValues<WorkOrderStatus>().Length;
        var actions = Enum.GetValues<WorkOrderAction>().Length;

        (statuses * actions).Should().Be(48);
        TransitionMatrix().Should().HaveCount(48);
    }

    [Theory]
    [MemberData(nameof(TransitionMatrix))]
    public void Target_throws_exactly_when_the_transition_is_forbidden(
        WorkOrderStatus from, WorkOrderAction action, bool expectedAllowed)
    {
        var act = () => WorkOrderTransitions.Target(from, action);

        if (expectedAllowed)
            act.Should().NotThrow();
        else
            act.Should().Throw<InvalidStateTransitionException>();
    }

    // ── Terminal states are absorbing (requirements.md 2.2, property P-14) ──

    [Theory]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public void Terminal_states_reject_every_action(WorkOrderStatus terminal)
    {
        foreach (var action in Enum.GetValues<WorkOrderAction>())
            WorkOrderTransitions.IsAllowed(terminal, action).Should().BeFalse(
                "terminal state {0} must not accept {1}", terminal, action);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public void Terminal_states_leave_the_status_unchanged_after_a_failed_attempt(
        WorkOrderStatus terminal)
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(terminal);

        var act = () => workOrder.Complete(WorkOrderTestBuilder.Now);

        act.Should().Throw<InvalidStateTransitionException>();
        workOrder.Status.Should().Be(terminal);   // no partial mutation
    }

    // ── AllowedActions derives from the same table (requirements.md 2.9) ──

    [Theory]
    [InlineData(WorkOrderStatus.Draft, 2)]        // Release, Cancel
    [InlineData(WorkOrderStatus.Released, 3)]     // Unrelease, Start, Cancel
    [InlineData(WorkOrderStatus.InProgress, 4)]   // RecordProduction, Pause, Complete, Cancel
    [InlineData(WorkOrderStatus.Paused, 3)]       // Resume, Complete, Cancel
    [InlineData(WorkOrderStatus.Completed, 0)]
    [InlineData(WorkOrderStatus.Cancelled, 0)]
    public void AllowedActions_reports_what_the_table_permits(
        WorkOrderStatus status, int expectedCount)
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(status);

        var allowed = workOrder.AllowedActions();

        allowed.Should().HaveCount(expectedCount);
        allowed.Should().OnlyContain(action => WorkOrderTransitions.IsAllowed(status, action));
    }

    // ── Guards that depend only on the aggregate ──

    [Fact]
    public void Release_rejects_an_inactive_resource()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.Draft);

        var act = () => workOrder.Release(
            WorkOrderTestBuilder.InactiveResource(),
            WorkOrderTestBuilder.ActiveProduct(),
            WorkOrderTestBuilder.Now);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("resource-inactive");
    }

    [Fact]
    public void Release_rejects_an_inactive_product()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.Draft);

        var act = () => workOrder.Release(
            WorkOrderTestBuilder.ActiveResource(),
            WorkOrderTestBuilder.InactiveProduct(),
            WorkOrderTestBuilder.Now);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("product-inactive");
    }

    [Fact]
    public void Unrelease_is_unreachable_once_production_has_started()
    {
        // requirements.md 2.5 has two layers of defence:
        //   1. the matrix forbids Unrelease from InProgress, so a work order that
        //      is producing cannot even attempt it;
        //   2. the guard inside Unrelease covers the Released-with-entries case,
        //      which becomes reachable only if a future change allows it.
        // This test pins layer 1; the next one pins layer 2.
        WorkOrderTransitions.IsAllowed(WorkOrderStatus.InProgress, WorkOrderAction.Unrelease)
            .Should().BeFalse();

        var workOrder = WorkOrderTestBuilder.A().InProgressWithEntry();

        var act = () => workOrder.Unrelease(WorkOrderTestBuilder.Now);

        // The guard fires first: the aggregate has entries.
        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("cannot-unrelease-with-entries");
    }

    [Fact]
    public void Unrelease_from_released_without_entries_returns_to_draft()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.Released);

        workOrder.Unrelease(WorkOrderTestBuilder.Now);

        workOrder.Status.Should().Be(WorkOrderStatus.Draft);
    }

    [Fact]
    public void Start_sets_StartedAt_once_and_Resume_does_not_overwrite_it()
    {
        // requirements.md 2.6
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.Released);

        workOrder.Start(WorkOrderTestBuilder.Now);
        var firstStart = workOrder.StartedAt;

        workOrder.Pause(note: null, WorkOrderTestBuilder.Now);
        workOrder.Resume(WorkOrderTestBuilder.Now.AddHours(2));

        workOrder.StartedAt.Should().Be(firstStart);
    }

    [Fact]
    public void Complete_stamps_CompletedAt()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        workOrder.Complete(WorkOrderTestBuilder.Now);

        workOrder.Status.Should().Be(WorkOrderStatus.Completed);
        workOrder.CompletedAt.Should().Be(WorkOrderTestBuilder.Now);
    }

    [Fact]
    public void Cancel_requires_a_reason()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.Draft);

        var act = () => workOrder.Cancel("  ", WorkOrderTestBuilder.Now);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("cancellation-reason-required");
    }
}
```

> **Por que testar `WorkOrderTransitions` e não só os métodos públicos?**
> Porque a tabela **é** a máquina de estados. Testar pelos métodos públicos
> cobriria as transições alcançáveis, mas não provaria que as 36 combinações
> proibidas estão proibidas — muitas delas não têm método correspondente
> (`Release` numa OP `Completed` não tem como ser chamado sem antes construir uma
> OP `Completed`). Testar a tabela cobre o espaço inteiro; testar os métodos cobre
> o comportamento. Os dois têm valor e o custo é baixo.

> **Por que `Matrix_covers_exactly_48_combinations` existe?**
> É um teste sobre o teste. Se alguém adicionar um estado `Scheduled` sem revisar
> a matriz, esse teste falha imediatamente com uma mensagem clara, em vez de a
> suíte continuar verde cobrindo 56 combinações onde 8 nunca foram analisadas.

---

## Passo 23 — `WorkOrders/RecordProductionTests.cs`

```csharp
using Mes.Domain.UnitTests.Builders;
using Mes.Domain.WorkOrders;
using Mes.Domain.WorkOrders.Events;

namespace Mes.Domain.UnitTests.WorkOrders;

/// <summary>
/// Validates requirements.md 3.1, 3.2, 3.5, 3.6, 3.7, 3.8
/// and invariants WO-2, WO-3, WO-4, WO-8, WO-9 (design.md §8.2).
/// </summary>
public sealed class RecordProductionTests
{
    private static readonly DateTimeOffset Now = WorkOrderTestBuilder.Now;
    private static readonly TimeSpan Skew = WorkOrderTestBuilder.ClockSkew;

    private static ProductionEntry Record(
        WorkOrder workOrder,
        decimal good = 10m,
        decimal scrap = 0m,
        Guid? scrapReasonId = null,
        DateTimeOffset? occurredAt = null,
        string? key = null) =>
        workOrder.RecordProduction(
            goodQuantity: good,
            scrapQuantity: scrap,
            scrapReasonId: scrapReasonId ?? (scrap > 0 ? Guid.CreateVersion7() : null),
            occurredAt: occurredAt ?? Now,
            idempotencyKey: key ?? Guid.CreateVersion7().ToString(),
            payloadHash: "hash",
            source: ProductionSource.Operator,
            userId: Guid.CreateVersion7(),
            now: Now,
            clockSkewTolerance: Skew);

    // ── WO-2: conservation of quantity (property P-5) ──

    [Fact]
    public void Totals_always_equal_the_sum_of_the_entries()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        Record(workOrder, good: 10m, scrap: 1m);
        Record(workOrder, good: 7m, scrap: 0m);
        Record(workOrder, good: 3m, scrap: 2m);

        workOrder.ProducedGoodQuantity.Should().Be(workOrder.Entries.Sum(e => e.GoodQuantity));
        workOrder.ProducedScrapQuantity.Should().Be(workOrder.Entries.Sum(e => e.ScrapQuantity));
        workOrder.ProducedGoodQuantity.Should().Be(20m);
        workOrder.ProducedScrapQuantity.Should().Be(3m);
    }

    [Fact]
    public void Recording_raises_exactly_one_event()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        Record(workOrder, good: 8m, scrap: 1m);

        workOrder.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductionEntryRecorded>();
    }

    [Fact]
    public void Recording_does_not_change_the_status()
    {
        // InProgress -> InProgress is a self-loop: no status change, no status event.
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        Record(workOrder);

        workOrder.Status.Should().Be(WorkOrderStatus.InProgress);
        workOrder.DomainEvents.Should().NotContain(e => e is WorkOrderStatusChanged);
    }

    // ── WO-3 / requirements.md 3.7 ──

    [Theory]
    [InlineData(WorkOrderStatus.Draft)]
    [InlineData(WorkOrderStatus.Released)]
    [InlineData(WorkOrderStatus.Paused)]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public void Recording_is_rejected_outside_InProgress(WorkOrderStatus status)
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(status);

        var act = () => Record(workOrder);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("work-order-not-in-progress");
    }

    // ── requirements.md 3.5 ──

    [Fact]
    public void An_entry_with_no_quantity_at_all_is_rejected()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, good: 0m, scrap: 0m);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("empty-production-entry");
    }

    [Fact]
    public void Negative_quantities_are_rejected()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, good: -1m, scrap: 0m);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("quantity-must-not-be-negative");
    }

    // ── WO-8 / requirements.md 3.6 ──

    [Fact]
    public void Scrap_without_a_reason_is_rejected()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        var act = () => workOrder.RecordProduction(
            goodQuantity: 5m,
            scrapQuantity: 2m,
            scrapReasonId: null,       // ← the point of the test
            occurredAt: Now,
            idempotencyKey: "k",
            payloadHash: "h",
            source: ProductionSource.Operator,
            userId: Guid.CreateVersion7(),
            now: Now,
            clockSkewTolerance: Skew);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("scrap-reason-required");
    }

    [Fact]
    public void Good_only_entry_does_not_require_a_scrap_reason()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, good: 5m, scrap: 0m, scrapReasonId: null);

        act.Should().NotThrow();
    }

    // ── WO-4 / requirements.md 3.2 ──

    [Fact]
    public void Production_up_to_the_tolerance_is_accepted()
    {
        // 100 planned, 5% tolerance => 105 accepted
        var workOrder = WorkOrderTestBuilder.A()
            .WithPlannedQuantity(100m)
            .WithTolerance(0.05m)
            .InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, good: 105m);

        act.Should().NotThrow();
        workOrder.ProducedGoodQuantity.Should().Be(105m);
    }

    [Fact]
    public void Production_beyond_the_tolerance_is_rejected()
    {
        var workOrder = WorkOrderTestBuilder.A()
            .WithPlannedQuantity(100m)
            .WithTolerance(0.05m)
            .InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, good: 106m);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("overproduction-not-allowed");
    }

    [Fact]
    public void Overproduction_is_evaluated_on_the_accumulated_total()
    {
        var workOrder = WorkOrderTestBuilder.A()
            .WithPlannedQuantity(100m)
            .WithTolerance(0.05m)
            .InStatus(WorkOrderStatus.InProgress);

        Record(workOrder, good: 100m);

        var act = () => Record(workOrder, good: 6m);   // 106 total

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("overproduction-not-allowed");

        workOrder.ProducedGoodQuantity.Should().Be(100m, "a rejected entry must not mutate the total");
    }

    [Fact]
    public void Scrap_does_not_count_against_the_overproduction_limit()
    {
        // Only good quantity is capped: scrap is loss, not delivery.
        var workOrder = WorkOrderTestBuilder.A()
            .WithPlannedQuantity(100m)
            .WithTolerance(0m)
            .InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, good: 100m, scrap: 50m);

        act.Should().NotThrow();
    }

    // ── WO-9 / requirements.md 3.8 ──

    [Fact]
    public void OccurredAt_before_the_start_is_rejected()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, occurredAt: Now.AddMinutes(-1));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("occurred-at-out-of-range");
    }

    [Fact]
    public void OccurredAt_within_the_clock_skew_tolerance_is_accepted()
    {
        // A collector running 3 minutes fast must not have its entry rejected.
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, occurredAt: Now.AddMinutes(3));

        act.Should().NotThrow();
    }

    [Fact]
    public void OccurredAt_beyond_the_clock_skew_tolerance_is_rejected()
    {
        var workOrder = WorkOrderTestBuilder.A().InStatus(WorkOrderStatus.InProgress);

        var act = () => Record(workOrder, occurredAt: Now.AddMinutes(6));

        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain(Now.ToString("O"),
                "the error detail must report the server time (requirements.md 3.8)");
    }
}
```

> **Por que `Scrap_does_not_count_against_the_overproduction_limit`?**
> Porque é uma decisão de negócio que precisa estar explícita. O limite existe
> para impedir entregar mais do que foi pedido; refugo não é entrega. Se o teste
> não existisse, alguém "corrigiria" o cálculo para incluir refugo achando que era
> bug. Teste como documentação executável da intenção.

> **Por que a asserção `"a rejected entry must not mutate the total"`?**
> Porque é o tipo de bug que passa despercebido: o total é incrementado antes da
> validação, a exceção sobe, e o agregado fica num estado inconsistente na memória.
> Se o handler capturar a exceção e continuar usando o objeto, o próximo cálculo
> parte de um número errado. Verificar atomicidade da rejeição é baratíssimo.

---

## Passo 24 — `Oee/IntervalMathTests.cs`

```csharp
using Mes.Domain.Oee;

namespace Mes.Domain.UnitTests.Oee;

/// <summary>
/// Validates the merge algorithm of design.md §17.4 and its postconditions.
/// These same facts become property P-10 in Sprint 11 (design.md §25).
/// </summary>
public sealed class IntervalMathTests
{
    private static readonly DateTimeOffset T0 = new(2026, 3, 14, 6, 0, 0, TimeSpan.Zero);

    /// <summary>Interval from hour H1 to hour H2, relative to T0.</summary>
    private static TimeInterval H(double from, double to) =>
        new(T0.AddHours(from), T0.AddHours(to));

    private static readonly TimeInterval Window = H(0, 8);   // 8 hours = 28 800 s

    [Fact]
    public void Empty_input_measures_zero()
    {
        IntervalMath.MergeAndMeasure([], Window).Should().Be(0d);
    }

    [Fact]
    public void A_single_interval_inside_the_window_measures_its_own_duration()
    {
        IntervalMath.MergeAndMeasure([H(1, 3)], Window).Should().Be(7_200d);
    }

    [Fact]
    public void Disjoint_intervals_add_up()
    {
        IntervalMath.MergeAndMeasure([H(1, 2), H(4, 5)], Window).Should().Be(7_200d);
    }

    [Fact]
    public void Overlapping_intervals_are_unioned_not_summed()
    {
        // [1,3) and [2,4) overlap by one hour.
        // Union = 3 h = 10 800 s. Sum would be 4 h = 14 400 s — the bug this prevents.
        IntervalMath.MergeAndMeasure([H(1, 3), H(2, 4)], Window).Should().Be(10_800d);
    }

    [Fact]
    public void A_fully_contained_interval_adds_nothing()
    {
        IntervalMath.MergeAndMeasure([H(1, 5), H(2, 3)], Window).Should().Be(14_400d);
    }

    [Fact]
    public void Contiguous_intervals_merge_into_one_block()
    {
        var merged = IntervalMath.Merge([H(1, 2), H(2, 3)], Window);

        merged.Should().ContainSingle();
        merged[0].Should().Be(H(1, 3));
    }

    [Fact]
    public void An_interval_crossing_the_window_start_is_clipped()
    {
        // Starts 2 h before the window; only the part inside counts.
        IntervalMath.MergeAndMeasure([H(-2, 1)], Window).Should().Be(3_600d);
    }

    [Fact]
    public void An_interval_crossing_the_window_end_is_clipped()
    {
        IntervalMath.MergeAndMeasure([H(7, 12)], Window).Should().Be(3_600d);
    }

    [Fact]
    public void An_interval_entirely_outside_the_window_is_discarded()
    {
        IntervalMath.MergeAndMeasure([H(10, 12)], Window).Should().Be(0d);
    }

    [Fact]
    public void An_interval_touching_the_window_end_contributes_nothing()
    {
        // Half-open window: [8,9) does not intersect [0,8).
        IntervalMath.MergeAndMeasure([H(8, 9)], Window).Should().Be(0d);
    }

    // ── The four postconditions of design.md §17.4 ──

    [Fact]
    public void Result_is_never_greater_than_the_window()
    {
        IntervalMath.MergeAndMeasure([H(-100, 100)], Window)
            .Should().Be(Window.DurationSeconds);
    }

    [Fact]
    public void Union_is_never_greater_than_the_sum()
    {
        TimeInterval[] intervals = [H(1, 4), H(2, 5), H(3, 6)];

        var union = IntervalMath.MergeAndMeasure(intervals, Window);
        var sum = intervals.Sum(i => i.ClipTo(Window)!.DurationSeconds);

        union.Should().BeLessThanOrEqualTo(sum);
        union.Should().Be(18_000d);   // [1,6) = 5 h
        sum.Should().Be(32_400d);     // 3 x 3 h
    }

    [Fact]
    public void Adding_an_interval_never_decreases_the_result()
    {
        var before = IntervalMath.MergeAndMeasure([H(1, 2)], Window);
        var after = IntervalMath.MergeAndMeasure([H(1, 2), H(5, 6)], Window);

        after.Should().BeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Result_is_independent_of_input_order()
    {
        TimeInterval[] ordered = [H(1, 2), H(3, 5), H(4, 6)];
        TimeInterval[] shuffled = [H(4, 6), H(1, 2), H(3, 5)];

        IntervalMath.MergeAndMeasure(shuffled, Window)
            .Should().Be(IntervalMath.MergeAndMeasure(ordered, Window));
    }

    // ── Intersect ──

    [Fact]
    public void Intersect_keeps_only_the_overlapping_parts()
    {
        // Downtime [1,7) intersected with two shifts [0,3) and [5,8) => [1,3) + [5,7)
        var result = IntervalMath.Intersect([H(1, 7)], [H(0, 3), H(5, 8)]);

        result.Should().HaveCount(2);
        result.Sum(i => i.DurationSeconds).Should().Be(14_400d);   // 2 h + 2 h
    }

    [Fact]
    public void Intersect_with_no_overlap_returns_empty()
    {
        IntervalMath.Intersect([H(1, 2)], [H(5, 6)]).Should().BeEmpty();
    }

    // ── TimeInterval ──

    [Fact]
    public void Interval_with_end_before_start_is_rejected()
    {
        var act = () => new TimeInterval(T0.AddHours(2), T0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Half_open_intervals_that_only_touch_do_not_intersect()
    {
        H(0, 1).Intersects(H(1, 2)).Should().BeFalse();
    }

    [Fact]
    public void Contains_excludes_the_upper_bound()
    {
        H(0, 1).Contains(T0).Should().BeTrue();
        H(0, 1).Contains(T0.AddHours(1)).Should().BeFalse();
    }
}
```

> **Por que o helper `H(from, to)`?**
> Sem ele, cada teste teria três linhas de construção de `DateTimeOffset` e a
> intenção ficaria enterrada. Com ele, `H(1, 3)` e `H(2, 4)` deixam a sobreposição
> óbvia à leitura. Legibilidade de teste é o que faz o teste ser mantido em vez de
> deletado quando incomoda.

---

## Passo 25 — `Oee/OeeCalculatorTests.cs`

Cobre os casos de borda do `design.md §17.3`. Cada linha daquela tabela é um
teste nomeado.

```csharp
using Mes.Domain.Oee;

namespace Mes.Domain.UnitTests.Oee;

/// <summary>
/// Validates requirements.md 5.1 to 5.6 and every edge case in design.md §17.3.
/// </summary>
public sealed class OeeCalculatorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 3, 14, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = T0.AddHours(8);

    private static TimeInterval H(double from, double to) => new(T0.AddHours(from), T0.AddHours(to));

    private static readonly TimeInterval EightHourWindow = H(0, 8);   // 28 800 s

    private static OeeInput Input(
        TimeInterval? window = null,
        IReadOnlyList<Shift>? shifts = null,
        IReadOnlyList<DowntimeSlice>? downtimes = null,
        IReadOnlyList<ProductionSlice>? production = null,
        DateTimeOffset? now = null) =>
        new(
            Window: window ?? EightHourWindow,
            Shifts: shifts ?? [],
            Downtimes: downtimes ?? [],
            ProductionByProduct: production ?? [],
            Now: now ?? Now);

    private static DowntimeSlice Down(double from, double to, bool counts = true) =>
        new(H(from, to), counts);

    private static ProductionSlice Produced(
        decimal good, decimal scrap = 0m, double cycleSeconds = 30d) =>
        new(good, scrap, cycleSeconds);

    // ── Happy path ──

    [Fact]
    public void A_clean_shift_produces_the_expected_factors()
    {
        // 8 h window, no shift configured => T_planned = 28 800 s
        // 1 h downtime                    => T_down    =  3 600 s, T_run = 25 200 s
        // 800 units at 30 s               => theoretical = 24 000 s
        // A = 25200/28800 = 0.875
        // P = 24000/25200 = 0.952380...
        // Q = 780/800     = 0.975
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 2)],
            production: [Produced(good: 780m, scrap: 20m, cycleSeconds: 30d)]));

        result.HasData.Should().BeTrue();
        result.PlannedSeconds.Should().Be(28_800d);
        result.DownSeconds.Should().Be(3_600d);
        result.RunSeconds.Should().Be(25_200d);

        result.Availability.Should().BeApproximately(0.875m, 0.0001m);
        result.Performance.Should().BeApproximately(0.9524m, 0.0001m);
        result.Quality.Should().BeApproximately(0.975m, 0.0001m);
        result.Oee.Should().BeApproximately(0.8129m, 0.0001m);
        result.PerformanceWasClamped.Should().BeFalse();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public void Oee_equals_the_product_of_its_factors()
    {
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 3)],
            production: [Produced(good: 500m, scrap: 25m)]));

        var expected = result.Availability!.Value * result.Performance!.Value * result.Quality!.Value;

        result.Oee.Should().BeApproximately(expected, 0.000001m);
    }

    [Fact]
    public void Oee_never_exceeds_any_of_its_factors()
    {
        // Property P-2, as an example test here and as FsCheck in Sprint 11.
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(2, 4)],
            production: [Produced(good: 400m, scrap: 40m)]));

        result.Oee.Should().BeLessThanOrEqualTo(result.Availability!.Value);
        result.Oee.Should().BeLessThanOrEqualTo(result.Performance!.Value);
        result.Oee.Should().BeLessThanOrEqualTo(result.Quality!.Value);
    }

    [Fact]
    public void Time_is_conserved()
    {
        // Property P-3
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 2), Down(3, 5)],
            production: [Produced(good: 300m)]));

        (result.RunSeconds + result.DownSeconds).Should()
            .BeApproximately(result.PlannedSeconds, 1e-6);
    }

    // ── design.md §17.3, row by row ──

    [Fact]
    public void No_planned_time_returns_no_data_with_null_factors()
    {
        // A shift that does not intersect the window at all.
        var result = OeeCalculator.Calculate(Input(
            shifts: [new Shift(H(20, 24))],
            production: [Produced(good: 100m)]));

        result.HasData.Should().BeFalse();
        result.Reason.Should().Be(OeeReason.NoPlannedProductionTime);

        // The critical distinction: null, not zero. Zero would mean
        // "the machine performed terribly"; null means "nothing was scheduled".
        result.Availability.Should().BeNull();
        result.Performance.Should().BeNull();
        result.Quality.Should().BeNull();
        result.Oee.Should().BeNull();
    }

    [Fact]
    public void No_production_gives_availability_but_zero_oee()
    {
        var result = OeeCalculator.Calculate(Input(downtimes: [Down(1, 2)]));

        result.HasData.Should().BeTrue();
        result.Availability.Should().BeApproximately(0.875m, 0.0001m);
        result.Performance.Should().Be(0m);
        result.Quality.Should().BeNull();     // no quality without a single unit
        result.Oee.Should().Be(0m);           // but the period's OEE is zero, not null
    }

    [Fact]
    public void Overlapping_downtimes_are_unioned()
    {
        // [1,3) and [2,4) => union 3 h, not 4 h.
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 3), Down(2, 4)],
            production: [Produced(good: 100m)]));

        result.DownSeconds.Should().Be(10_800d);
    }

    [Fact]
    public void Downtime_crossing_the_window_boundary_is_clipped()
    {
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(-2, 1), Down(7, 12)],
            production: [Produced(good: 100m)]));

        result.DownSeconds.Should().Be(7_200d);   // 1 h + 1 h
    }

    [Fact]
    public void An_open_downtime_counts_up_to_now()
    {
        // Stoppage that started at hour 6 and has no end: `now` is hour 7,
        // so it contributes 1 h, not 2 h.
        var openEnd = T0.AddHours(100);   // far future = "still open" from the query layer
        var result = OeeCalculator.Calculate(Input(
            downtimes: [new DowntimeSlice(new TimeInterval(T0.AddHours(6), openEnd), true)],
            production: [Produced(good: 100m)],
            now: T0.AddHours(7)));

        result.DownSeconds.Should().Be(3_600d);
    }

    [Fact]
    public void Downtime_exceeding_planned_time_never_produces_negative_run_time()
    {
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(-10, 20)],
            production: [Produced(good: 10m)]));

        result.DownSeconds.Should().Be(result.PlannedSeconds);
        result.RunSeconds.Should().Be(0d);
        result.Availability.Should().Be(0m);
        result.Performance.Should().Be(0m);
        result.Oee.Should().Be(0m);
    }

    [Fact]
    public void Performance_above_one_is_clamped_and_flagged()
    {
        // 25 200 s of run time but 2 000 units at 30 s = 60 000 s of theoretical time.
        // Physically impossible: the product's ideal cycle time is misconfigured.
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 2)],
            production: [Produced(good: 2_000m, cycleSeconds: 30d)]));

        result.Performance.Should().Be(1m);
        result.PerformanceWasClamped.Should().BeTrue(
            "clamping alone would hide bad master data; the flag turns it into information");
    }

    [Fact]
    public void Missing_ideal_cycle_time_makes_performance_and_oee_unknown()
    {
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 2)],
            production: [Produced(good: 100m, cycleSeconds: 0d)]));

        result.HasData.Should().BeTrue();
        result.Reason.Should().Be(OeeReason.MissingIdealCycleTime);
        result.Availability.Should().NotBeNull();   // still knowable
        result.Quality.Should().NotBeNull();        // still knowable
        result.Performance.Should().BeNull();       // do not estimate
        result.Oee.Should().BeNull();
    }

    [Fact]
    public void A_product_with_no_output_does_not_trigger_missing_cycle_time()
    {
        // Only a product that PRODUCED something needs a cycle time.
        var result = OeeCalculator.Calculate(Input(
            production: [Produced(good: 100m, cycleSeconds: 30d), Produced(good: 0m, cycleSeconds: 0d)]));

        result.Reason.Should().BeNull();
        result.Performance.Should().NotBeNull();
    }

    [Fact]
    public void An_inverted_window_throws()
    {
        var act = () => OeeCalculator.Calculate(Input(
            window: new TimeInterval(T0, T0)));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void All_scrap_gives_zero_quality_and_zero_oee()
    {
        var result = OeeCalculator.Calculate(Input(
            production: [Produced(good: 0m, scrap: 500m)]));

        result.Quality.Should().Be(0m);
        result.Oee.Should().Be(0m);
        result.Performance.Should().NotBe(0m, "the machine did run cycles, they were just all scrap");
    }

    // ── Multiple products: weighted C_ideal ──

    [Fact]
    public void Ideal_cycle_time_is_weighted_across_products()
    {
        // 100 units at 10 s  = 1 000 s
        // 100 units at 50 s  = 5 000 s
        // theoretical        = 6 000 s over 28 800 s of run time
        var result = OeeCalculator.Calculate(Input(
            production: [Produced(good: 100m, cycleSeconds: 10d), Produced(good: 100m, cycleSeconds: 50d)]));

        result.RunSeconds.Should().Be(28_800d);
        result.Performance.Should().BeApproximately(6_000m / 28_800m, 0.0001m);
    }

    // ── Shifts and non-countable reasons ──

    [Fact]
    public void With_no_shift_configured_planned_time_is_the_whole_window()
    {
        // requirements.md 5.2.3 — documented 24x7 default.
        var result = OeeCalculator.Calculate(Input(production: [Produced(good: 100m)]));

        result.PlannedSeconds.Should().Be(EightHourWindow.DurationSeconds);
    }

    [Fact]
    public void Planned_time_is_the_window_intersected_with_the_shifts()
    {
        // Two 2 h shifts inside an 8 h window => 4 h planned.
        var result = OeeCalculator.Calculate(Input(
            shifts: [new Shift(H(0, 2)), new Shift(H(4, 6))],
            production: [Produced(good: 100m)]));

        result.PlannedSeconds.Should().Be(14_400d);
    }

    [Fact]
    public void Downtime_outside_any_shift_does_not_penalise_availability()
    {
        // Shift [0,2); stoppage at [3,4) happened on an unmanned line.
        var result = OeeCalculator.Calculate(Input(
            shifts: [new Shift(H(0, 2))],
            downtimes: [Down(3, 4)],
            production: [Produced(good: 100m)]));

        result.DownSeconds.Should().Be(0d);
        result.Availability.Should().Be(1m);
    }

    [Fact]
    public void A_reason_that_does_not_count_is_excluded_from_downtime()
    {
        // requirements.md 5.2.4
        var result = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 3, counts: false)],
            production: [Produced(good: 100m)]));

        result.DownSeconds.Should().Be(0d);
        result.Availability.Should().Be(1m);
    }

    [Fact]
    public void Adding_a_downtime_never_increases_availability()
    {
        // Property P-11
        var withoutExtra = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 2)], production: [Produced(good: 100m)]));

        var withExtra = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 2), Down(4, 5)], production: [Produced(good: 100m)]));

        withExtra.Availability.Should().BeLessThanOrEqualTo(withoutExtra.Availability!.Value);
    }

    [Fact]
    public void Result_is_invariant_to_event_order()
    {
        // Property P-12
        var ordered = OeeCalculator.Calculate(Input(
            downtimes: [Down(1, 2), Down(3, 4), Down(5, 6)],
            production: [Produced(good: 100m, cycleSeconds: 20d), Produced(good: 50m, cycleSeconds: 40d)]));

        var shuffled = OeeCalculator.Calculate(Input(
            downtimes: [Down(5, 6), Down(1, 2), Down(3, 4)],
            production: [Produced(good: 50m, cycleSeconds: 40d), Produced(good: 100m, cycleSeconds: 20d)]));

        shuffled.Should().BeEquivalentTo(ordered);
    }

    [Fact]
    public void Calculate_is_deterministic_for_the_same_input()
    {
        // requirements.md 5.9 — no ambient clock inside the calculation.
        var input = Input(downtimes: [Down(1, 2)], production: [Produced(good: 100m)]);

        OeeCalculator.Calculate(input).Should().BeEquivalentTo(OeeCalculator.Calculate(input));
    }
}
```

---

## Passo 26 — `Downtimes/DowntimeEventTests.cs`

```csharp
using Mes.Domain.Downtimes;
using Mes.Domain.Downtimes.Events;

namespace Mes.Domain.UnitTests.Downtimes;

/// <summary>Validates invariants DT-1, DT-3, DT-4 and requirements.md 4.1, 4.3, 4.4, 4.7.</summary>
public sealed class DowntimeEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Skew = TimeSpan.FromMinutes(5);

    private static DowntimeEvent Open(
        DateTimeOffset? startedAt = null, Guid? reasonId = null, Guid? workOrderId = null) =>
        DowntimeEvent.Open(
            resourceId: Guid.CreateVersion7(),
            reasonId: reasonId ?? Guid.CreateVersion7(),
            workOrderId: workOrderId,
            startedAt: startedAt ?? Now.AddHours(-1),
            note: null,
            idempotencyKey: Guid.CreateVersion7().ToString(),
            now: Now,
            clockSkewTolerance: Skew);

    [Fact]
    public void A_new_downtime_is_open_and_raises_DowntimeStarted()
    {
        var downtime = Open();

        downtime.IsOpen.Should().BeTrue();
        downtime.EndedAt.Should().BeNull();
        downtime.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<DowntimeStarted>();
    }

    [Fact]
    public void A_reason_is_mandatory()
    {
        // Invariant DT-3 — requirements.md 4.1
        var act = () => Open(reasonId: Guid.Empty);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("downtime-reason-required");
    }

    [Fact]
    public void A_downtime_may_exist_without_a_work_order()
    {
        // requirements.md 4.7 — setup and maintenance happen with no order open.
        var act = () => Open(workOrderId: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Closing_sets_the_end_and_raises_DowntimeClosed()
    {
        var downtime = Open(startedAt: Now.AddHours(-2));
        downtime.ClearDomainEvents();

        downtime.Close(Now.AddHours(-1), Now);

        downtime.IsOpen.Should().BeFalse();
        downtime.DurationSeconds(Now).Should().Be(3_600d);
        downtime.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<DowntimeClosed>();
    }

    [Fact]
    public void Closing_before_the_start_is_rejected()
    {
        // Invariant DT-1 — requirements.md 4.3
        var downtime = Open(startedAt: Now.AddHours(-1));

        var act = () => downtime.Close(Now.AddHours(-2), Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("ended-before-started");
    }

    [Fact]
    public void Closing_at_exactly_the_start_is_rejected()
    {
        var startedAt = Now.AddHours(-1);
        var downtime = Open(startedAt: startedAt);

        var act = () => downtime.Close(startedAt, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("ended-before-started");
    }

    [Fact]
    public void A_closed_downtime_is_immutable()
    {
        // Invariant DT-4 — requirements.md 4.4. Rewriting ended_at would falsify
        // the availability history.
        var downtime = Open(startedAt: Now.AddHours(-2));
        downtime.Close(Now.AddHours(-1), Now);

        var act = () => downtime.Close(Now, Now);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("downtime-already-closed");
    }

    [Fact]
    public void An_open_downtime_measures_up_to_now()
    {
        var downtime = Open(startedAt: Now.AddMinutes(-30));

        downtime.DurationSeconds(Now).Should().Be(1_800d);
        downtime.DurationSeconds(Now.AddMinutes(30)).Should().Be(3_600d);
    }

    [Fact]
    public void A_start_in_the_future_beyond_the_skew_tolerance_is_rejected()
    {
        var act = () => Open(startedAt: Now.AddMinutes(6));

        act.Should().Throw<DomainException>().Which.Code.Should().Be("started-at-out-of-range");
    }
}
```

---

## Passo 27 — `docs/adr/0005-oee-derived-from-events.md`

O ADR do sprint. `design.md §27` define o núcleo do argumento: o indicador nunca
é campo digitado; o custo é consulta mais caras; o benefício é auditabilidade,
apontamento retroativo de graça e explicabilidade ao operador.

Escreva com as suas palavras, usando o `_template.md` do Sprint 1. Pontos que o
ADR precisa cobrir:

**Context** — no MES legado, indicador tende a ser campo preenchido: alguém digita
"disponibilidade 87%". O número existe, ninguém sabe de onde veio, e quando
diverge do chão de fábrica não há como reconciliar.

**Decision** — OEE é função pura de três conjuntos de eventos (janela, paradas,
apontamentos) mais o tempo de ciclo do produto. Não existe coluna `oee` em
nenhuma tabela.

**What we gain**
- Sempre recalculável, sempre auditável
- Explicável ao operador: "seu OEE caiu porque a parada de 40 min por
  `TOOL-CHANGE` derrubou a disponibilidade"
- Apontamento retroativo funciona **de graça**: não há cache para invalidar
- Correção de dado se propaga sozinha ao indicador

**What we give up**
- Consulta mais caras: cada leitura agrega eventos em vez de ler uma coluna
- Sem histórico congelado: se a definição do cálculo mudar, os números do passado
  mudam junto. (Registre a mitigação: `T_planned`, `T_down` e `T_run` vêm na
  resposta, então o número é sempre auditável contra os insumos.)
- Necessidade de índice adequado em `production_entry` e `downtime_event`

**Alternatives considered**
| Alternative | Why rejected |
|---|---|
| Coluna `oee` atualizada por trigger | Divergência silenciosa; apontamento retroativo exigiria recálculo em cascata |
| Tabela de agregação materializada por hora | Complexidade de invalidação sem ganho neste volume; reavaliar se a janela de 90 dias virar gargalo |
| Indicador digitado pelo supervisor | É o anti-padrão que motivou o projeto |

> **Anote a data de revisão.** Este ADR nasce no Sprint 2 e é **revisado no
> Sprint 6**, depois de você implementar o SQL e sentir o custo real. ADR revisado
> após a implementação, com data, é sinal de honestidade intelectual — não de
> indecisão.

---

## Como saber que deu certo

### 1. Build limpo

```powershell
dotnet build
```

Esperado: `0 Warning(s). 0 Error(s)`.

### 2. Domínio ainda sem dependência externa

```powershell
dotnet test tests/Mes.Domain.UnitTests --filter "FullyQualifiedName~DependencyRuleTests"
```

Esperado: os 2 testes do Sprint 1 continuam verdes. Se você adicionou
`FluentValidation` ou `EFCore` no domínio por reflexo, é aqui que aparece.

### 3. As 48 combinações

```powershell
dotnet test tests/Mes.Domain.UnitTests --filter "FullyQualifiedName~WorkOrderTransitionTests"
```

Esperado: 48 casos do teste parametrizado + os testes de guarda, todos verdes.

### 4. Suíte completa em menos de 3 segundos

```powershell
dotnet test tests/Mes.Domain.UnitTests
```

Esperado: ~130 testes, tempo total abaixo de 3 s (`requirements.md 11.2`).

> **Se passar de 3 s**, algo faz I/O. Suspeitos usuais: `DateTime.UtcNow` num
> loop, `Thread.Sleep` esquecido num teste, ou um `Guid.NewGuid()` dentro de um
> `[Theory]` com centenas de casos.

### 5. Cobertura acima de 90%

```powershell
dotnet test tests/Mes.Domain.UnitTests --collect:"XPlat Code Coverage"
```

Para ler o resultado, instale a ferramenta de relatório uma vez:

```powershell
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"tests/Mes.Domain.UnitTests/TestResults/*/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

Abra `coveragereport/index.html`. Esperado: `Mes.Domain` acima de 90%
(`requirements.md 11.1`).

> **Onde a cobertura vai faltar, e o que fazer:** provavelmente nos `Update` e
> `Reactivate` dos catálogos. Duas opções honestas: escrever o teste (30 segundos
> cada) ou remover o método se nada o usa. **Não** escreva teste de getter para
> inflar o número — `requirements.md 11.7` proíbe explicitamente, e teste sem
> valor é dívida de manutenção.

Adicione `coveragereport/` e `TestResults/` ao `.gitignore`.

### 6. CI verde

```powershell
git push
```

---

## Commits sugeridos

```
feat(domain): add entity, aggregate root and domain exception primitives
feat(domain): add product, scrap reason and downtime reason catalogs
feat(domain): add resource with state as a projection
feat(domain): add work order state machine as a declarative table
feat(domain): add production entry with idempotency key and payload hash
feat(domain): enforce work order invariants WO-1 through WO-9
feat(domain): add downtime event with DT-1 through DT-4 invariants
feat(domain): add batch and batch consumption with acyclicity guards
feat(oee): add time interval value object with half-open semantics
feat(oee): add interval union algorithm for downtime measurement
feat(oee): derive OEE as a pure function of shop-floor events
test(domain): cover all 48 work order transition combinations
test(domain): cover production reporting guards and quantity conservation
test(oee): cover interval merge postconditions
test(oee): cover every edge case of the OEE calculation
test(domain): cover downtime open and close invariants
docs(adr): add ADR-0005 OEE derived from events
```

---

## O que você aprendeu neste sprint

| Conceito | O que dizer em entrevista |
|---|---|
| Agregado e fronteira transacional | `ProductionEntry` não tem repositório: o único caminho de escrita é a raiz, e é isso que garante a invariante WO-2 |
| Invariante vs validação de entrada | Invariante é regra de negócio no agregado (`422`); validação é forma do payload (`400`). Confundir os dois espalha regra pela borda |
| Máquina de estados como tabela | `FrozenDictionary` em vez de cascata de `switch`; 48 casos gerados do `enum`; estado terminal é ausência de entrada, não lista de negações |
| Injeção de tempo | `DateTimeOffset now` como parâmetro; sem isso todo teste de janela é não determinístico |
| Função pura | `OeeCalculator` não tem I/O nem estado; mesma entrada, mesma saída, sempre |
| Merge de intervalos | `O(n log n)` por varredura linear; união nunca é soma; é o que impede disponibilidade negativa |
| Janela semiaberta `[from, to)` | Com limite superior inclusivo, evento na borda conta em duas janelas e a soma dos períodos não fecha |
| `null` vs `0` em indicador | `0` = "produziu mal"; `null` = "não havia o que produzir". Um supervisor decide o oposto em cada caso |
| `decimal` vs `double` | Contagem exata em `decimal`; medição física em `double`; uma única fronteira de conversão |
| UUID v7 | Id ordenável por tempo; inserção no fim do B-tree em vez de página aleatória |
| Guarda que consulta outro agregado | A decisão fica no domínio, a consulta fica no handler — é o que mantém o domínio sem I/O |
| Clamp com sinalização | `PerformanceWasClamped` transforma cadastro errado em informação em vez de esconder |

---

## Próximo passo

👉 `sprint-03-persistence.md` — EF Core, migrations, os índices que importam
(único parcial, idempotência, genealogia), `xmin` como token de concorrência,
seed com nomes fictícios e o primeiro teste com Testcontainers.

Antes de abrir o próximo: confirme que a `main` está verde e que a suíte de
domínio roda abaixo de 3 s. Esse número é o seu termômetro pelo resto do projeto.

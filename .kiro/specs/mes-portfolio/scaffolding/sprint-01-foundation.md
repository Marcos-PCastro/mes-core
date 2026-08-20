# Sprint 1 — Fundação: fechar o scaffold

> **Duração:** 1 semana · **Esforço:** 8–10 h
> **Referências:** `design.md §26` (Sprint 1), `§6.1` (regra de dependência),
> `§15.1` (Compose), `requirements.md 10.1`, `10.6`, `11.5`, `15.4`

---

## Objetivo

Repositório vivo, com pipeline verde e `docker compose up` subindo a API + Postgres.
**Nada de domínio ainda.** O objetivo é que a infraestrutura de trabalho
(build, teste, container, CI) esteja provada antes de você escrever a primeira
regra de negócio.

Por que essa ordem importa: se você começa pelo domínio e só monta o CI no fim,
descobre problemas de ambiente quando já tem 3 mil linhas de código para
depurar junto. Fundação primeiro é barato; fundação depois é caro.

---

## Critério de pronto

- [ ] `git clone && docker compose up` → `GET http://localhost:5000/health` responde `200`
- [ ] `dotnet build` sem warnings (`TreatWarningsAsErrors=true`)
- [ ] `dotnet test` roda e passa (pelo menos 1 teste com valor real)
- [ ] Badge do GitHub Actions verde no README
- [ ] `docs/adr/0001-modular-monolith-over-microservices.md` commitado
- [ ] Zero arquivo de boilerplate do template no repositório

---

## O que já existe (não refazer)

Confirmado no repositório atual:

| Item | Situação |
|---|---|
| `MesCore.slnx` com os 5 projetos de `src/` | ✅ |
| `Directory.Build.props` (Nullable, LangVersion, TreatWarningsAsErrors) | ✅ |
| Referências entre projetos na direção correta | ✅ |
| `docker-compose.yml` com healthcheck e `service_healthy` | ✅ |
| `Dockerfile` da API, do Simulator e do frontend | ✅ |
| `.github/workflows/ci.yml` | ✅ |
| Frontend Vite + React 19 + TS | ✅ (esqueleto) |

---

## Ordem de criação

| # | Arquivo | Ação | Referência |
|---|---|---|---|
| 1 | `.editorconfig` | Corrigir (arquivo está corrompido) | `scaffolding.md §6.2` |
| 2 | `MesCore.slnx` | Adicionar os 3 projetos de teste | — |
| 3 | `src/Mes.Api/Mes.Api.csproj` | Atualizar pacotes para .NET 10 | `design.md §29.1` |
| 4 | `src/Mes.Api/Program.cs` | Substituir boilerplate por `/health` | `requirements.md 10.6` |
| 5 | `src/Mes.Api/appsettings.json` | Adicionar seções de configuração | `design.md §15.1` |
| 6 | `src/Mes.Api/Properties/launchSettings.json` | Ajustar portas locais | — |
| 7 | `src/Mes.Simulator/*` | Limpar `Worker.cs`, deixar inerte | `design.md §26` (S8) |
| 8 | `tests/*/GlobalUsings.cs` | Usings compartilhados | — |
| 9 | `tests/Mes.Domain.UnitTests/Architecture/DependencyRuleTests.cs` | Primeiro teste real | `design.md §6.1` |
| 10 | `tests/Mes.Api.IntegrationTests/HealthEndpointTests.cs` | Teste do `/health` | `requirements.md 10.6` |
| 11 | `docs/adr/_template.md` | Molde dos ADRs | `design.md §27` |
| 12 | `docs/adr/0001-modular-monolith-over-microservices.md` | ADR do sprint | `design.md §27` |
| 13 | `README.md` | Esqueleto com badge | `design.md §28.1` |
| 14 | `.gitignore` | Conferir cobertura | `requirements.md 15.4` |

---

## Passo 1 — Corrigir o `.editorconfig`

O arquivo atual tem linhas colapsadas (as regras de naming ficaram numa única
linha, o que invalida a seção). Substitua o conteúdo inteiro:

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx}]
indent_size = 4
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# ── Naming: campos privados em camelCase com underscore ──────────────
dotnet_naming_rule.private_fields_should_be_camel_case.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case.style = underscore_camel_case

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.underscore_camel_case.capitalization = camel_case
dotnet_naming_style.underscore_camel_case.required_prefix = _

# ── Analyzers que valem a pena tratar como erro ──────────────────────
dotnet_diagnostic.CA2007.severity = none      # ConfigureAwait: irrelevante em ASP.NET Core
dotnet_diagnostic.CA1848.severity = none      # LoggerMessage delegates: excesso p/ este porte
dotnet_diagnostic.IDE0055.severity = warning  # formatação

[*.{ts,tsx,js,jsx}]
indent_size = 2

[*.{json,yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

> **Por que `_camelCase` em campo privado?**
> É a convenção do próprio runtime do .NET e a mais comum em código open source
> C#. O ponto não é o estilo em si — é ter **uma** convenção aplicada
> automaticamente, para nunca gastar revisão de código discutindo isso.

> **Por que desligar CA2007 e CA1848?**
> Analyzer ligado sem critério gera ruído, e ruído com `TreatWarningsAsErrors`
> gera build vermelho por motivo irrelevante. Desligar **com comentário
> explicando** é diferente de ignorar: você tomou a decisão e ela está registrada.

---

## Passo 2 — Adicionar os projetos de teste à solução

Os três projetos de teste existem em `tests/` mas não estão na solução. Isso
significa que `dotnet build` na raiz não os compila, e você só descobre erro de
compilação de teste quando o CI roda.

Edite `MesCore.slnx`:

```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/Mes.Api/Mes.Api.csproj" />
    <Project Path="src/Mes.Application/Mes.Application.csproj" />
    <Project Path="src/Mes.Domain/Mes.Domain.csproj" />
    <Project Path="src/Mes.Infrastructure/Mes.Infrastructure.csproj" />
    <Project Path="src/Mes.Simulator/Mes.Simulator.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/Mes.Api.IntegrationTests/Mes.Api.IntegrationTests.csproj" />
    <Project Path="tests/Mes.Domain.PropertyTests/Mes.Domain.PropertyTests.csproj" />
    <Project Path="tests/Mes.Domain.UnitTests/Mes.Domain.UnitTests.csproj" />
  </Folder>
  <Folder Name="/solution-items/">
    <File Path="Directory.Build.props" />
    <File Path=".editorconfig" />
    <File Path="docker-compose.yml" />
    <File Path="README.md" />
  </Folder>
</Solution>
```

> **O que é `.slnx`?**
> É o formato novo de solução do .NET (XML legível) que substitui o `.sln`
> antigo, aquele formato com GUIDs ilegíveis que dava conflito de merge em toda
> alteração. Usar `.slnx` é um detalhe pequeno que mostra que você acompanha o
> ecossistema.

Confira:

```powershell
dotnet sln list
```

Deve listar 8 projetos.

---

## Passo 3 — Atualizar os pacotes da API

O `Mes.Api.csproj` atual referencia pacotes do .NET 8 e o `Swashbuckle`, que o
`design.md §11.1` descartou em favor do OpenAPI nativo + Scalar.

Substitua o conteúdo de `src/Mes.Api/Mes.Api.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <!-- TargetFramework, Nullable e LangVersion vêm do Directory.Build.props -->
  </PropertyGroup>

  <ItemGroup>
    <!-- Geração do documento OpenAPI (nativo do .NET, substitui Swashbuckle) -->
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />

    <!-- UI para navegar o OpenAPI. Substitui o Swagger UI -->
    <PackageReference Include="Scalar.AspNetCore" Version="2.0.0" />

    <!-- Log estruturado (requirements.md 12.1) -->
    <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mes.Application\Mes.Application.csproj" />
    <ProjectReference Include="..\Mes.Infrastructure\Mes.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

> **Por que remover `<TargetFramework>` e `<Nullable>` do `.csproj`?**
> Eles já estão no `Directory.Build.props`. Repetir a mesma propriedade em dois
> lugares é convite para elas divergirem. Um lugar só, sempre.

> **Por que Scalar e não Swagger UI?**
> Swashbuckle era a única opção até o .NET 8. No .NET 9+ a geração do documento
> OpenAPI é nativa (`Microsoft.AspNetCore.OpenApi`), e o Scalar é uma UI mais
> limpa que consome esse documento. Menos dependência, ferramenta atual.

Faça o mesmo tipo de limpeza nos `.csproj` de `Mes.Domain`, `Mes.Application`,
`Mes.Infrastructure` e `Mes.Simulator`: remova `TargetFramework` e `Nullable`,
deixe só `ImplicitUsings` e as referências.

**Confirme a invariante mais importante do projeto:** o
`src/Mes.Domain/Mes.Domain.csproj` **não pode ter nenhum `<PackageReference>`**.
Zero. Isso é verificável, e o Passo 9 escreve o teste que verifica.

---

## Passo 4 — `Program.cs`: substituir o boilerplate por `/health`

O `Program.cs` atual é o template `dotnet new webapi`, com `WeatherForecast`.
Substitua o conteúdo inteiro:

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging estruturado ───────────────────────────────────────────────
// Serilog lê a configuração de appsettings.json (seção "Serilog").
// Log estruturado significa propriedades nomeadas e pesquisáveis
// (workOrderId, resourceId, traceId) — ver requirements.md 12.1.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ── OpenAPI ───────────────────────────────────────────────────────────
// Gera o documento em /openapi/v1.json. A UI do Scalar consome esse documento.
builder.Services.AddOpenApi();

// ── Health checks ─────────────────────────────────────────────────────
// Neste sprint só o self-check. No Sprint 3, quando o DbContext existir,
// adicionamos a verificação do PostgreSQL aqui.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // UI navegável em /scalar
}

// GET /health — público, sem autenticação (requirements.md 8.4, 10.6).
// Responde 200 quando todos os checks registrados estão saudáveis, 503 quando não.
app.MapHealthChecks("/health");

app.Run();
```

> **Onde fica o `/health`, afinal?**
> Ele não é um arquivo. Health check no ASP.NET Core é um **serviço registrado**
> (`AddHealthChecks`) mais um **endpoint mapeado** (`MapHealthChecks`). Os dois
> vivem no `Program.cs`. A partir do Sprint 4, os endpoints de negócio saem do
> `Program.cs` para `Endpoints/*.cs`, mas o `/health` fica aqui — ele é
> infraestrutura, não domínio.

> **Por que não `app.UseHttpsRedirection()`?**
> Dentro de container a API escuta HTTP na 8080 e o TLS termina antes (proxy /
> ingress). Redirecionar para HTTPS dentro do container gera loop e é a causa
> mais comum de "meu Compose sobe mas nada responde".

> **Por que `tags: ["live"]`?**
> Health check tem dois sabores: *liveness* ("o processo está de pé?") e
> *readiness* ("está pronto para receber tráfego?"). No Sprint 3, o Postgres
> entra como `ready`. Separar por tag permite expor `/health/live` e
> `/health/ready` — é o vocabulário que Kubernetes usa, e saber a distinção é
> pergunta de entrevista.

**Delete o arquivo de boilerplate**, se ainda existir:

```powershell
Remove-Item -ErrorAction SilentlyContinue src/Mes.Api/Mes.Api.http
```

> Guarde a coleção de requests em `docs/api-collection/` (Bruno ou Insomnia) a
> partir do Sprint 4 — é mais útil para quem avalia que um `.http` solto.

---

## Passo 5 — `src/Mes.Api/appsettings.json`

Cada projeto executável tem o seu próprio `appsettings.json` dentro da sua pasta.
O Passo 5 cuida dos dois arquivos da **API** (`src/Mes.Api/`). O Passo 7 cuida
do Simulator (`src/Mes.Simulator/`).

Como identificar qual editar: o arquivo relevante é sempre o do projeto que
**recebe** as variáveis de ambiente do Compose. O serviço `api:` no
`docker-compose.yml` injeta `ConnectionStrings__Default`, `Jwt__SecretKey` etc.,
então o arquivo que declara essas seções é `src/Mes.Api/appsettings.json`.

O arquivo atual só tem a seção `Logging`. Substitua pelo conteúdo abaixo:

```jsonc
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName" ]
  },

  "ConnectionStrings": {
    // Sobrescrito pelo Compose via ConnectionStrings__Default.
    // Este valor serve para rodar a API local com `docker compose up postgres`.
    "Default": "Host=localhost;Port=5432;Database=mes_core;Username=mes_user;Password=mes_dev_password"
  },

  "Mes": {
    "ApplyMigrationsOnStartup": false,
    "SeedOnStartup": false,
    "OverproductionTolerance": 0.05,
    "ClockSkewToleranceMinutes": 5,
    "MaxOeeWindowDays": 90,
    "OeeTargets": {
      "WorldClass": 0.85,
      "Warning": 0.60
    }
  },

  "Jwt": {
    // DEVELOPMENT ONLY. Em produção vem de variável de ambiente / secret store.
    // Nunca comitar chave real — ver requirements.md 13.3.
    "SecretKey": "dev-only-secret-do-not-use-in-production-min-32-chars",
    "Issuer": "mes-core",
    "Audience": "mes-core-client",
    "ExpirationMinutes": 60
  },

  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173", "http://localhost" ]
  },

  "AllowedHosts": "*"
}
```

> **Por que `ApplyMigrationsOnStartup: false` aqui e `true` no Compose?**
> `requirements.md 10.4`. Aplicar migration no startup é conveniente para demo,
> mas errado em produção — duas instâncias subindo ao mesmo tempo podem tentar
> migrar em paralelo. O default do arquivo é o comportamento seguro; o Compose
> liga explicitamente porque é ambiente de demonstração. Essa assimetria é
> deliberada e vale um parágrafo no README.

> **Por que `OverproductionTolerance` e `OeeTargets` já aqui?**
> `requirements.md 3.2` e `16.10` exigem que sejam configuráveis e documentados.
> Declarar cedo evita que o valor apareça hardcoded no meio do código depois —
> que é exatamente o "limiar arbitrário" que `16.10` proíbe.

Reduza o `appsettings.Development.json` ao que realmente difere:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.AspNetCore": "Information"
      }
    }
  },
  "Mes": {
    "ApplyMigrationsOnStartup": true,
    "SeedOnStartup": true
  }
}
```

---

## Passo 6 — `launchSettings.json`

Alinhe a porta local com a que o Compose expõe (`5000`), para que o
`VITE_API_BASE_URL` do frontend funcione nos dois modos.

`src/Mes.Api/Properties/launchSettings.json`:

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

> **Por que `launchUrl: "scalar"`?**
> Quando você roda `dotnet run --project src/Mes.Api`, o browser abre direto na
> documentação navegável da API. Detalhe pequeno, economiza dezenas de cliques
> ao longo do projeto.

---

## Passo 7 — Deixar o Simulator inerte

O `Mes.Simulator` só ganha comportamento real no Sprint 8. Por enquanto ele não
pode ficar logando "Worker running at..." a cada segundo, poluindo o Compose.

`src/Mes.Simulator/Worker.cs`:

```csharp
namespace Mes.Simulator;

/// <summary>
/// Placeholder. The real equipment simulator is implemented in Sprint 8
/// (see design.md §11.1 and §26 Sprint 8). Until then this worker stays idle
/// so that the Compose stack starts cleanly.
/// </summary>
public sealed class Worker(ILogger<Worker> logger, IConfiguration configuration)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue("Simulator:Enabled", defaultValue: false);

        logger.LogInformation(
            "Simulator starting. Enabled={Enabled}. Equipment simulation lands in Sprint 8.",
            enabled);

        return Task.CompletedTask;
    }
}
```

Crie `src/Mes.Simulator/appsettings.json` com a forma final da configuração, já
documentada, mesmo sem uso ainda:

```jsonc
{
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [ { "Name": "Console" } ]
  },
  "Simulator": {
    "Enabled": false,
    "ApiBaseUrl": "http://localhost:5000",

    // Conta de serviço: o simulador autentica como qualquer outro cliente.
    // Não existe caminho privilegiado de escrita (design.md §16, requirements.md 7.2).
    "ServiceAccountUsername": "simulator",
    "ServiceAccountPassword": "dev-only-password",

    "ProductionIntervalSeconds": 5,
    "FailureMtbfMinutes": 10,
    "RepairMttrMinutes": 2,
    "ScrapRate": 0.03,

    // Semente fixa → execução reprodutível (requirements.md 7.3).
    // null = semente aleatória.
    "RandomSeed": 20260819
  }
}
```

> **Por que declarar configuração que ainda não é lida?**
> Porque `requirements.md 7.3` exige MTBF/MTTR configuráveis e semente fixa.
> Escrever a forma da configuração antes do código força você a decidir o
> contrato primeiro. É a mesma ideia de escrever a assinatura antes do corpo.

---

## Passo 8 — `GlobalUsings.cs` nos projetos de teste

Cada projeto de teste já tem `<Using Include="Xunit" />` no `.csproj`. Para o
que for além disso, use um arquivo explícito — fica visível no diretório em vez
de escondido no XML.

Crie `tests/Mes.Domain.UnitTests/GlobalUsings.cs`:

```csharp
global using Xunit;
```

Repita em `tests/Mes.Domain.PropertyTests/GlobalUsings.cs`:

```csharp
global using Xunit;
```

E em `tests/Mes.Api.IntegrationTests/GlobalUsings.cs` já inclua o `FluentAssertions`,
pois o teste do Passo 10 usa `Should()` diretamente:

```csharp
global using FluentAssertions;
global using Xunit;
```

---

## Passo 9 — O primeiro teste: a regra de dependência

Aqui está a decisão que diferencia este sprint de "criei um teste vazio para o
CI parar de reclamar". O `design.md §6.1` estabelece que `Mes.Domain` não
referencia nada. Essa regra é a fundação de tudo — é o que permite o domínio
rodar em milissegundos (`requirements.md 11.2`).

Regra em documento é intenção. Regra em teste é garantia.

Crie `tests/Mes.Domain.UnitTests/Architecture/DependencyRuleTests.cs`:

```csharp
using System.Reflection;

namespace Mes.Domain.UnitTests.Architecture;

/// <summary>
/// Enforces the dependency rule from design.md §6.1:
/// Domain depends on nothing. Application depends on Domain only.
/// This is what keeps the domain test suite under 3 seconds
/// (requirements.md 11.2) and the OEE calculator testable without a database.
/// </summary>
public sealed class DependencyRuleTests
{
    // Assemblies that every .NET assembly legitimately references.
    private static readonly string[] AllowedPrefixes =
    [
        "System",
        "netstandard",
        "mscorlib",
        "Microsoft.CSharp",
        "Microsoft.VisualBasic"
    ];

    [Fact]
    public void Domain_has_no_third_party_dependencies()
    {
        var domain = typeof(DomainAssemblyMarker).Assembly;

        var forbidden = domain
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => !AllowedPrefixes.Any(p =>
                name.StartsWith(p, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(forbidden);
    }

    [Fact]
    public void Domain_does_not_reference_any_other_Mes_project()
    {
        var domain = typeof(DomainAssemblyMarker).Assembly;

        var mesReferences = domain
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith("Mes.", StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(mesReferences);
    }
}
```

Isso exige um tipo âncora no domínio. Crie
`src/Mes.Domain/DomainAssemblyMarker.cs`:

```csharp
namespace Mes.Domain;

/// <summary>
/// Anchor type used to reference this assembly from tests and DI registration
/// without depending on any concrete domain type.
/// </summary>
public sealed class DomainAssemblyMarker
{
    private DomainAssemblyMarker() { }
}
```

> **Por que um "marker"?**
> Para pegar o `Assembly` você precisa de algum tipo dele. Usar uma entidade de
> negócio para isso acopla o teste de arquitetura a uma classe que vai mudar. Um
> tipo vazio e dedicado é estável. O mesmo padrão vai servir no Sprint 4 para
> registrar validators por assembly scanning.

> **Este teste vai realmente pegar algo?**
> Sim, e cedo. No Sprint 2, se você adicionar `FluentValidation` no domínio por
> reflexo (é o lugar "natural" para quem vem de MVC), o build vermelho te avisa
> na hora. Sem esse teste, você descobre no Sprint 6, quando os testes de
> domínio já estiverem levando 20 segundos porque alguém arrastou EF Core para
> lá.

Adicione ao `tests/Mes.Domain.UnitTests/Mes.Domain.UnitTests.csproj` o pacote de
asserções que o `design.md §11.3` escolheu:

```xml
<PackageReference Include="FluentAssertions" Version="7.0.0" />
```

E no `GlobalUsings.cs` do projeto:

```csharp
global using FluentAssertions;
global using Xunit;
```

---

## Passo 10 — Teste de integração do `/health`

O `requirements.md 10.6` diz que `GET /health` responde `200`. Isso é
verificável, então tem teste. Esse arquivo também estabelece o padrão de teste
de integração que os sprints seguintes vão reusar.

Adicione ao `tests/Mes.Api.IntegrationTests/Mes.Api.IntegrationTests.csproj`:

```xml
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
```

Crie `tests/Mes.Api.IntegrationTests/HealthEndpointTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mes.Api.IntegrationTests;

/// <summary>
/// Validates requirements.md 10.6 — GET /health returns 200 when the API is healthy.
/// Uses WebApplicationFactory, which boots the real HTTP pipeline in-process:
/// routing, middleware and DI all behave as in production.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_ok()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_endpoint_does_not_require_authentication()
    {
        // requirements.md 8.4 — only POST /api/auth/login and GET /health are public.
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health", CancellationToken.None);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
```

`WebApplicationFactory<Program>` exige que a classe `Program` seja visível ao
projeto de teste. Com top-level statements ela é `internal`. Adicione ao
`src/Mes.Api/Mes.Api.csproj`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="Mes.Api.IntegrationTests" />
</ItemGroup>
```

> **Por que `InternalsVisibleTo` e não tornar `Program` público?**
> Manter `Program` interno preserva o encapsulamento do host. Abrir só para o
> projeto de teste é o mínimo necessário, e é o padrão que a própria
> documentação do ASP.NET Core recomenda.

> **Por que este teste ainda não precisa de Testcontainers?**
> Porque `/health` neste sprint não toca o banco. No Sprint 3 o check do
> Postgres entra, e aí este teste passa a exigir o container — e você vai ver o
> teste falhar primeiro, o que é a forma certa de descobrir que o check está
> realmente ligado.

---

## Passo 11 — Template de ADR

Crie `docs/adr/_template.md`. Ter o molde pronto elimina a fricção de começar o
ADR do zero a cada sprint:

```markdown
# ADR-NNNN: <short title in the imperative>

- **Status:** Accepted
- **Date:** YYYY-MM-DD
- **Sprint:** SN

## Context

What forces are at play? What constraint, requirement or pain triggered this
decision? Describe the situation without naming the solution yet.

## Decision

What we decided, stated plainly. One paragraph.

## Consequences

### What we gain

### What we give up

An ADR that only lists benefits is marketing, not engineering. Name the cost.

## Alternatives considered

| Alternative | Why it was rejected |
|---|---|

## Revisions

| Date | Change |
|---|---|
```

> **Por que "what we give up" é seção obrigatória?**
> `design.md §27`: "Escolhi X" é fraco. "Escolhi X aceitando perder Y, porque
> neste contexto Z importa mais" é uma frase de Pleno. A seção existe para te
> forçar a escrever a segunda versão.

---

## Passo 12 — ADR-0001

Crie `docs/adr/0001-modular-monolith-over-microservices.md`. O `design.md §27`
define o núcleo do argumento: tamanho do sistema, custo operacional de
distribuição, onde estão os pontos de corte se algum dia precisar dividir.

Estrutura a preencher **com as suas palavras** (não copie do `design.md` — o
ADR precisa soar como você, porque você vai defendê-lo em voz alta):

```markdown
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
```

> **A seção "Natural seams" é o diferencial deste ADR.**
> Qualquer pessoa diz "escolhi monólito porque é mais simples". Mostrar que você
> já sabe **por onde cortaria** prova que a escolha foi analisada e não
> preguiçosa. É a resposta pronta para "e se crescer?".

---

## Passo 13 — README esqueleto

Não escreva o README final agora (isso é o Sprint 12). Escreva a **estrutura**,
com marcadores do que entra depois. Assim você nunca chega no Sprint 12 com
página em branco.

`README.md`:

```markdown
# MES Core

A minimal Manufacturing Execution System core: work orders with an explicit state
machine, idempotent production reporting, event-derived OEE and batch genealogy.

[![CI](https://github.com/SEU_USUARIO/mes-core/actions/workflows/ci.yml/badge.svg)](https://github.com/SEU_USUARIO/mes-core/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

<!-- TODO S12: demo.gif here — 20-30s of the dashboard moving on its own -->

## Run it in one command

```bash
git clone https://github.com/SEU_USUARIO/mes-core.git
cd mes-core
docker compose up --build
```

| What | Where |
|---|---|
| API health | http://localhost:5000/health |
| API reference | http://localhost:5000/scalar |
| Frontend | http://localhost |

<!-- TODO S4: demo credentials -->

## Why this project exists

<!-- TODO S12: the narrative — design.md §1.2, in English, no employer name -->

## What a MES does

<!-- TODO S12: the plain-language paragraph — design.md §5 -->

## Architecture

<!-- TODO S12: C4 level 2 Mermaid diagram + dependency rule -->

## The interesting parts

<!-- TODO S12: four short blocks, each linking to the code
     - Idempotent production reporting
     - OEE derived from events, never stored
     - Batch genealogy with recursive CTE
     - Optimistic concurrency with Postgres xmin -->

## Testing strategy

<!-- TODO S11/S12: the pyramid + the counterexample a property test found -->

## Decisions and tradeoffs

- [ADR-0001 — Modular monolith over microservices](docs/adr/0001-modular-monolith-over-microservices.md)
<!-- TODO: ADR-0002 (S3), 0003 (S4), 0004 (S5), 0005 (S2/S6) -->

## What's intentionally out of scope

<!-- TODO S12: the table from design.md §4.2 -->

## Tech stack

.NET 10 · ASP.NET Core Minimal API · EF Core · Dapper · PostgreSQL 17 · SignalR
React 19 · TypeScript · Vite · TanStack Query · Docker Compose · GitHub Actions
```

> **Por que "Run it in one command" tão em cima?**
> `design.md §28.1`. Quem avalia quer rodar, não ler. O README com badge, GIF e
> comando de execução nos primeiros 20 segundos de scroll é o que decide se a
> pessoa continua.

> **Por que TODOs explícitos no README?**
> Porque README é o artefato mais fácil de deixar para depois e o mais decisivo
> para o resultado. Marcador visível vira dívida visível.

---

## Passo 14 — Conferir o `.gitignore`

`requirements.md 15.4` proíbe `bin`, `obj`, `.vs`, `.bak` e artefato gerado no
repositório. Confirme que estas entradas existem:

```gitignore
# .NET
bin/
obj/
*.user
.vs/

# Node
node_modules/
web/dist/

# Env
.env
!.env.example

# IDE / OS
.idea/
.DS_Store
Thumbs.db

# Coverage
TestResults/
coverage/
*.coverage
*.trx
```

Verifique se algo já rastreado deveria estar ignorado:

```powershell
git ls-files | Select-String -Pattern "(bin/|obj/|node_modules/|/dist/)"
```

Se retornar linhas, remova do índice preservando o arquivo em disco:

```powershell
git rm -r --cached src/Mes.Api/obj
```

> **Atenção:** `git rm --cached` remove do controle de versão mas mantém em
> disco. Sem o `--cached` ele apaga o arquivo. Confira o comando antes de
> executar.

---

## Como saber que deu certo

Rode em sequência. Cada comando tem um resultado esperado explícito.

### 1. Build limpo

```powershell
dotnet build
```

Esperado: `Build succeeded. 0 Warning(s). 0 Error(s)`.

Se aparecer warning de nullable em arquivo do template, ele deveria ter sido
apagado. Se aparecer em código seu, corrija — com `TreatWarningsAsErrors` o
build já vai estar vermelho de qualquer forma.

### 2. Testes passando

```powershell
dotnet test
```

Esperado: 4 testes passando (2 de arquitetura, 2 de `/health`).

### 3. Domínio sem dependência — a verificação manual

```powershell
dotnet list src/Mes.Domain/Mes.Domain.csproj package
```

Esperado: nenhum pacote. Se listar algum, o teste do Passo 9 já falhou e o
motivo está ali.

### 4. Velocidade do domínio

```powershell
dotnet test tests/Mes.Domain.UnitTests
```

Esperado: bem abaixo de 3 s (`requirements.md 11.2`). Anote o número — no fim do
Sprint 2, com ~80 testes, ele ainda tem que estar abaixo de 3 s. Se subir, é
sinal de que I/O entrou no domínio.

### 5. Compose de ponta a ponta

```powershell
docker compose up --build
```

Em outro terminal:

```powershell
Invoke-WebRequest http://localhost:5000/health | Select-Object StatusCode
```

Esperado: `200`.

Verifique também que a API esperou o banco:

```powershell
docker compose logs api | Select-String "Now listening"
```

Não deve haver exceção de conexão antes dessa linha. Se houver, o
`depends_on: condition: service_healthy` não está funcionando.

Derrube:

```powershell
docker compose down
```

### 6. CI verde

```powershell
git push
```

Acesse `https://github.com/SEU_USUARIO/mes-core/actions`. Os três jobs
(`backend`, `frontend`, `docker`) devem passar.

> **Se o job `frontend` falhar em `npm run lint`:** o `package.json` usa
> `oxlint`. Confirme que existe `.oxlintrc.json` e que ele não está apontando
> para regra inexistente. Frontend real começa no Sprint 9; aqui basta o lint
> passar no esqueleto.

---

## Commits sugeridos

Um commit por unidade coerente, em inglês, Conventional Commits. O `git log` é
parte do portfólio (`requirements.md 15.5`).

```
chore: fix editorconfig naming rules and analyzer severities
chore: add test projects to the solution
chore(api): replace template boilerplate with health endpoint
chore(api): move to native OpenAPI and Scalar, drop Swashbuckle
chore(api): declare configuration sections for mes, jwt and cors
chore(simulator): keep worker idle until sprint 8
test(architecture): assert domain has no external dependencies
test(api): cover health endpoint contract
docs(adr): add ADR-0001 modular monolith over microservices
docs: add README skeleton with run instructions and CI badge
```

> **Por que tantos commits pequenos em vez de um "sprint 1 done"?**
> Porque o `git log` é lido. Dez commits com escopo claro mostram como você
> trabalha; um commit gigante esconde. E se algo quebrar, `git bisect` funciona.

---

## O que você aprendeu neste sprint

Vocabulário para usar em entrevista, com o "porquê" atrelado:

| Conceito | O que dizer |
|---|---|
| `Directory.Build.props` | Configuração de build centralizada; evita divergência entre `.csproj` |
| Regra de dependência | O compilador garante a arquitetura; `Domain` sem referência é o que mantém o teste em milissegundos |
| Teste de arquitetura | Regra em documento é intenção; regra em teste é garantia |
| `TreatWarningsAsErrors` | Warning ignorado hoje é bug amanhã; custa zero revisão de código |
| Multi-stage Dockerfile | SDK para build, runtime para executar; imagem final ~5× menor |
| `healthcheck` + `service_healthy` | Sem isso a API sobe antes do banco e falha; é o erro que faz o avaliador desistir na primeira tentativa |
| Liveness vs readiness | "Processo de pé" ≠ "pronto para tráfego"; separar por tag é o vocabulário de orquestrador |
| `WebApplicationFactory` | Testa pelo pipeline HTTP real, não por chamada direta ao handler |
| ADR | Registro de decisão com o custo assumido, não só o benefício |

---

## Próximo passo

👉 [`sprint-02-domain.md`](sprint-02-domain.md) — o coração do sistema:
`WorkOrder` com invariantes, máquina de estados como tabela declarativa, e o
`OeeCalculator` como função pura. É o sprint mais importante do projeto.

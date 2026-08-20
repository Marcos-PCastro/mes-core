# Scaffolding Guide: MES Core

> **Para quem é este documento:**  
> Para você — que conhece o domínio, mas está fazendo a jornada de aprender
> como estruturar um projeto .NET moderno do zero, criar o repositório certo
> e organizar o código do jeito que o mercado internacional espera.
>
> Trate este guia como se fosse um tech lead sentado do seu lado, explicando
> cada passo, o *porquê* de cada decisão e o que esperar de resultado.
>
> **Documentos complementares:**  
> - `design.md` — arquitetura completa, algoritmos, contratos de API  
> - `requirements.md` — critérios de aceitação verificáveis

---

## Como usar este guia

Cada seção é um **passo sequencial**. Não pule etapas — elas constroem umas
sobre as outras. No final de cada passo há uma seção **"Como saber que deu certo"**
para você validar antes de avançar.

Ao lado de cada comando você vai ver uma explicação do *porquê*, não só do *o quê*.
Esse é o diferencial de um tech lead: você não só sabe o que digitar, sabe
defender a escolha.

---

## Índice

1. [Pré-requisitos — o que instalar](#1-pré-requisitos)
2. [Criar o repositório no GitHub](#2-criar-o-repositório-no-github)
3. [Clonar e abrir no VS Code / Rider](#3-clonar-e-abrir)
4. [Criar a solução .NET e os projetos](#4-criar-a-solução-net-e-os-projetos)
5. [Configurar referências entre projetos](#5-configurar-referências-entre-projetos)
6. [Configurar qualidade de código](#6-configurar-qualidade-de-código)
7. [Criar o projeto React + Vite + TypeScript](#7-criar-o-frontend-react)
8. [Configurar o Docker Compose](#8-docker-compose)
9. [Configurar o CI no GitHub Actions](#9-ci-github-actions)
10. [Executar o projeto pela primeira vez](#10-primeira-execução)
11. [Próximos passos — onde o design.md entra](#11-próximos-passos)

---

## 1. Pré-requisitos

Antes de começar, confirme que tem instalado:

| Ferramenta | Versão mínima | Para que serve | Como verificar |
|---|---|---|---|
| **Git** | qualquer | Controle de versão | `git --version` |
| **.NET SDK** | 10.0 | Compilar e rodar o backend | `dotnet --version` |
| **Node.js** | 22 LTS | Compilar e rodar o frontend | `node --version` |
| **Docker Desktop** | qualquer recente | Subir PostgreSQL e os containers | `docker --version` |
| **GitHub CLI (`gh`)** | qualquer | Criar repositório pela linha de comando | `gh --version` |
| **VS Code** ou **Rider** | qualquer | Editor | — |

> **Por que .NET 10?**  
> É o LTS mais recente do ciclo. Demonstra que você não está preso em versões
> antigas. O `design.md §11.1` tem a justificativa completa se o entrevistador
> perguntar.

> **Por que GitHub CLI (`gh`)?**  
> Criar o repositório pela linha de comando é mais rápido e você aprende o
> fluxo que as equipes profissionais usam. Se preferir a interface web do GitHub,
> a seção 2 também explica o caminho visual.

### 1.1 Instalar o .NET 10

Se `dotnet --version` retornar algo abaixo de 10, acesse https://dot.net e baixe
o .NET 10 SDK. A instalação é um instalador gráfico no Windows.

### 1.2 Instalar o GitHub CLI

```powershell
winget install --id GitHub.cli
```

Depois de instalar, autentique:

```powershell
gh auth login
```

Escolha `GitHub.com → HTTPS → Login with a web browser`. O browser vai abrir e
pedir autorização.

---

## 2. Criar o repositório no GitHub

### O que vamos criar

Um repositório público chamado `mes-core`, com:
- `README.md` inicial
- `.gitignore` para .NET e Node
- Licença MIT

### Por que público

O objetivo do portfólio é ser visto. Repositório privado não aparece no perfil
do GitHub. O `design.md §14` (restrição de propriedade intelectual) já estabelece
que nenhum dado real vai entrar aqui — por isso público é seguro.

### Opção A — via linha de comando (recomendado)

```powershell
# Cria o repositório no GitHub já com README, .gitignore e MIT
gh repo create mes-core `
  --public `
  --description "Minimal MES core: work orders, OEE from events, batch genealogy, real-time dashboard" `
  --gitignore VisualStudio `
  --license mit `
  --clone
```

Isso cria o repositório no GitHub E já faz o clone na sua máquina. O resultado
é uma pasta `mes-core/` no diretório atual.

> **O que cada flag faz:**  
> `--public` → repositório visível. `--gitignore VisualStudio` → pré-carrega o
> `.gitignore` padrão para projetos .NET (inclui `bin/`, `obj/`, `.vs/`, etc.).
> `--license mit` → arquivo `LICENSE` já commitado. `--clone` → já faz o clone.

### Opção B — via interface web

1. Acesse https://github.com/new
2. **Repository name:** `mes-core`
3. **Description:** `Minimal MES core: work orders, OEE from events, batch genealogy, real-time dashboard`
4. Marque **Public**
5. Marque **Add a README file**
6. **Add .gitignore:** escolha `VisualStudio`
7. **Choose a license:** `MIT License`
8. Clique em **Create repository**
9. Depois clone: `git clone https://github.com/SEU_USUARIO/mes-core.git`

### Como saber que deu certo

```powershell
cd mes-core
git log --oneline
```

Deve mostrar um commit inicial, algo como `Initial commit`.

---

## 3. Clonar e abrir

Se usou a Opção A, você já está dentro da pasta `mes-core/`. Se usou a
Opção B e clonou separado, entre na pasta:

```powershell
cd mes-core
```

Abra no editor:

```powershell
# VS Code
code .

# Rider (se instalado via JetBrains Toolbox)
# abra pelo Rider → Open → selecione a pasta
```

A pasta ainda está quase vazia — só tem `README.md`, `.gitignore` e `LICENSE`.
Isso é correto. Vamos construir tudo a partir daqui.

---

## 4. Criar a solução .NET e os projetos

Esta é a etapa mais longa. Vamos criar a estrutura de pastas e projetos que o
`design.md §12` descreve.

### 4.1 O que é uma "solução" .NET

Uma solução (`.sln`) é um arquivo que agrupa vários projetos (`.csproj`).
Quando você abre a solução no Visual Studio ou Rider, todos os projetos aparecem
juntos. Quando você roda `dotnet build` na raiz, ele compila todos.

Em projetos com arquitetura em camadas (como este), cada camada vira um projeto
separado. Isso força o compilador a garantir que a dependência vai na direção
certa — se você tentar referenciar `Mes.Infrastructure` a partir de `Mes.Domain`,
o build quebra. Isso não é burocracia, é proteção de arquitetura.

### 4.2 Criar a estrutura de pastas

Execute tudo dentro da pasta `mes-core/`:

```powershell
# Pastas para o código-fonte
New-Item -ItemType Directory -Path src
New-Item -ItemType Directory -Path tests
New-Item -ItemType Directory -Path docs/adr
New-Item -ItemType Directory -Path web
```

### 4.3 Criar a solução

```powershell
dotnet new sln --name MesCore
```

Isso cria `MesCore.sln` na raiz. O nome `MesCore` é o identificador
interno; o repositório continua se chamando `mes-core`.

### 4.4 Criar os projetos de produção (src/)

Execute cada comando abaixo. Cada um cria um projeto do tipo especificado:

```powershell
# Domínio — biblioteca de classes pura, zero dependências externas
dotnet new classlib --name Mes.Domain --output src/Mes.Domain --framework net10.0

# Aplicação — casos de uso, interfaces (ports)
dotnet new classlib --name Mes.Application --output src/Mes.Application --framework net10.0

# Infraestrutura — EF Core, Dapper, repositórios concretos
dotnet new classlib --name Mes.Infrastructure --output src/Mes.Infrastructure --framework net10.0

# API — host ASP.NET Core, endpoints, SignalR hub
dotnet new webapi --name Mes.Api --output src/Mes.Api --framework net10.0

# Simulador — worker service que publica eventos via HTTP
dotnet new worker --name Mes.Simulator --output src/Mes.Simulator --framework net10.0
```

> **Por que `classlib` para Domain, Application e Infrastructure?**  
> Esses projetos não são executáveis — eles são bibliotecas que outros projetos
> referenciam. Só a `Mes.Api` e o `Mes.Simulator` são executáveis (`webapi` e
> `worker` geram um binário que você roda com `dotnet run`).

> **Por que `--framework net10.0` em todos?**  
> Para garantir que todos usam a mesma versão do .NET. Sem isso, o `dotnet new`
> usa o SDK instalado como padrão, que pode diferir entre máquinas.

### 4.5 Criar os projetos de teste (tests/)

```powershell
# Testes de unidade do domínio — rápidos, zero I/O
dotnet new xunit --name Mes.Domain.UnitTests --output tests/Mes.Domain.UnitTests --framework net10.0

# Testes de propriedade — property-based testing com FsCheck
dotnet new xunit --name Mes.Domain.PropertyTests --output tests/Mes.Domain.PropertyTests --framework net10.0

# Testes de integração — Testcontainers, banco real
dotnet new xunit --name Mes.Api.IntegrationTests --output tests/Mes.Api.IntegrationTests --framework net10.0
```

> **Por que xUnit?**  
> É o padrão de fato no ecossistema .NET moderno. NUnit e MSTest existem e
> funcionam, mas você vai encontrar xUnit em 90% dos projetos open source .NET.
> O `design.md §11.3` explica a escolha.

### 4.6 Adicionar todos os projetos à solução

```powershell
# Projetos de produção
dotnet sln add src/Mes.Domain/Mes.Domain.csproj
dotnet sln add src/Mes.Application/Mes.Application.csproj
dotnet sln add src/Mes.Infrastructure/Mes.Infrastructure.csproj
dotnet sln add src/Mes.Api/Mes.Api.csproj
dotnet sln add src/Mes.Simulator/Mes.Simulator.csproj

# Projetos de teste
dotnet sln add tests/Mes.Domain.UnitTests/Mes.Domain.UnitTests.csproj
dotnet sln add tests/Mes.Domain.PropertyTests/Mes.Domain.PropertyTests.csproj
dotnet sln add tests/Mes.Api.IntegrationTests/Mes.Api.IntegrationTests.csproj
```

### 4.7 Verificar que ficou certo

```powershell
dotnet sln list
```

Deve listar os 8 projetos. Se algum faltou, rode o `dotnet sln add` correspondente.

### Como saber que deu certo

```powershell
dotnet build
```

O build deve passar com **0 erros**. Pode ter warnings (vamos resolver na
seção 6), mas erros agora indicam que algo deu errado na criação dos projetos.

---

## 5. Configurar referências entre projetos

Esta é a etapa que garante que a arquitetura do `design.md §6.1` vai ser
aplicada pelo compilador. A regra é simples:

```
Domain  ←  Application  ←  Infrastructure  ←  Api
                                            ←  Simulator
```

A seta significa "referencia". `Domain` não referencia ninguém. `Api` referencia
tudo.

### Por que isso importa

Se `Mes.Domain` puder referenciar `Mes.Infrastructure`, você vai acabar colocando
código de banco de dados dentro do domínio — exatamente o que o MES legado faz
com os packages Oracle, e exatamente o que torna o legado difícil de testar.

Quando `Domain` não tem nenhuma referência externa, você consegue rodar os testes
de domínio em 2–3 segundos, sem banco, sem Docker, sem nada. Isso é o que o
`requirements.md §11.2` exige.

### Adicionar as referências

```powershell
# Application referencia Domain
dotnet add src/Mes.Application/Mes.Application.csproj reference src/Mes.Domain/Mes.Domain.csproj

# Infrastructure referencia Application (e por transitividade, Domain)
dotnet add src/Mes.Infrastructure/Mes.Infrastructure.csproj reference src/Mes.Application/Mes.Application.csproj

# Api referencia Infrastructure (para registrar os serviços) e Application
dotnet add src/Mes.Api/Mes.Api.csproj reference src/Mes.Infrastructure/Mes.Infrastructure.csproj
dotnet add src/Mes.Api/Mes.Api.csproj reference src/Mes.Application/Mes.Application.csproj

# Simulator referencia Application (chama handlers via HttpClient, não referência direta)
# Por enquanto não precisa de referência — vai comunicar pela API HTTP

# Testes de unidade referenciam Domain
dotnet add tests/Mes.Domain.UnitTests/Mes.Domain.UnitTests.csproj reference src/Mes.Domain/Mes.Domain.csproj
dotnet add tests/Mes.Domain.PropertyTests/Mes.Domain.PropertyTests.csproj reference src/Mes.Domain/Mes.Domain.csproj

# Testes de integração referenciam Api (para WebApplicationFactory)
dotnet add tests/Mes.Api.IntegrationTests/Mes.Api.IntegrationTests.csproj reference src/Mes.Api/Mes.Api.csproj
```

### Como saber que deu certo

```powershell
dotnet build
```

Deve continuar com 0 erros. Se aparecer "circular dependency", revise as
referências — provavelmente foi adicionada uma na direção errada.

---

## 6. Configurar qualidade de código

Antes de escrever uma linha de lógica, configure as regras que vão garantir
qualidade automaticamente. Isso evita discussões de estilo e pega problemas cedo.

### 6.1 Directory.Build.props — configuração global

Crie o arquivo `Directory.Build.props` na raiz do repositório.
Este arquivo é lido automaticamente pelo MSBuild para **todos os projetos** da
solução, sem precisar repetir configuração em cada `.csproj`.

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <!-- Nullable reference types: o compilador avisa sobre possíveis NullReferenceException -->
    <!-- Isso é o equivalente de um lint que pega null bugs em tempo de compilação -->
    <Nullable>enable</Nullable>

    <!-- Trata warnings como erros: o build quebra se houver warning não resolvido -->
    <!-- Garante que o repositório nunca acumule warnings ignorados -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- Versão da linguagem: sempre a mais recente disponível no SDK -->
    <LangVersion>latest</LangVersion>

    <!-- Versão do .NET para todos os projetos -->
    <TargetFramework>net10.0</TargetFramework>

    <!-- Metadados do pacote (útil se algum dia publicar no NuGet) -->
    <Authors>seu-nome-aqui</Authors>
    <Company></Company>
  </PropertyGroup>
</Project>
```

> **Por que `Nullable=enable`?**  
> Com nullable habilitado, o compilador consegue detectar em tempo de build
> situações como `string nome = null` (que causaria NullReferenceException em
> runtime). É a feature de C# moderno que mais reduz bugs silenciosos.

> **Por que `TreatWarningsAsErrors=true`?**  
> Warning ignorado hoje vira bug amanhã. A CI do `requirements.md §11.5` exige
> build com essa flag justamente para que nenhum warning entre no repositório.

### 6.2 .editorconfig — estilo de código

Crie `.editorconfig` na raiz. Este arquivo é reconhecido pelo VS Code, Rider e
Visual Studio automaticamente.

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

# Naming rules
dotnet_naming_rule.private_fields_should_be_camel_case.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case.style = camel_case_style

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_style.capitalization = camel_case

[*.{ts,tsx,js,jsx}]
indent_size = 2

[*.{json,yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

### 6.3 Verificar que ficou certo

```powershell
dotnet build
```

Se agora aparecerem warnings que antes não apareciam, é porque o `Nullable=enable`
encontrou potenciais null problems nos arquivos gerados pelo template. Abra o
arquivo apontado e corrija adicionando `?` onde apropriado ou inicializando a
propriedade. Exemplo comum:

```csharp
// Antes (nullable warning)
public string Name { get; set; }

// Depois (correto)
public string Name { get; set; } = string.Empty;
// ou
public required string Name { get; set; }
```

---

## 7. Criar o frontend React

O frontend é deliberadamente pequeno (ver `design.md §R1`). O objetivo é
demonstrar a integração com a API, não virar um projeto de frontend.

### 7.1 Criar o projeto Vite + React + TypeScript

```powershell
# Dentro da pasta mes-core/
npm create vite@latest web -- --template react-ts
```

> **O que esse comando faz:**  
> `npm create vite@latest` usa o scaffolding oficial do Vite. `web` é o nome da
> pasta de destino. `--template react-ts` configura React + TypeScript já
> integrados. Você vai precisar confirmar a instalação digitando `y`.

```powershell
# Entrar na pasta e instalar as dependências
cd web
npm install
```

### 7.2 Instalar as dependências do projeto

```powershell
# TanStack Query — estado de servidor (fetch, cache, invalidação)
npm install @tanstack/react-query

# React Router — navegação entre páginas
npm install react-router-dom

# SignalR — cliente para o hub em tempo real
npm install @microsoft/signalr

# Recharts — gráficos de OEE e Pareto
npm install recharts

# Mantine — biblioteca de componentes UI (ou shadcn/ui — ver design.md §16.1)
npm install @mantine/core @mantine/hooks @emotion/react

# Dependências de desenvolvimento
npm install -D openapi-typescript    # gera tipos TypeScript a partir do OpenAPI
npm install -D vitest @vitest/ui @testing-library/react @testing-library/user-event
```

### 7.3 Criar arquivo de variáveis de ambiente

```powershell
# De volta na raiz do repositório
cd ..
```

Crie o arquivo `web/.env.example`:

```env
# Copie este arquivo para .env e ajuste os valores
VITE_API_BASE_URL=http://localhost:5000
```

> **Por que `.env.example` e não `.env`?**  
> O arquivo `.env` contém valores reais (endereços, às vezes tokens) e **não
> deve ser commitado**. O `.env.example` mostra quais variáveis existem sem
> expor valores. O `.gitignore` gerado pelo GitHub para VisualStudio já ignora
> `.env`, mas confirme.

### Como saber que deu certo

```powershell
cd web
npm run dev
```

Deve abrir um servidor em `http://localhost:5173` com a tela padrão do Vite +
React. `Ctrl+C` para encerrar.

---

## 8. Docker Compose

O Docker Compose é o que permite que qualquer pessoa rode o projeto com um
comando, sem instalar PostgreSQL, sem configurar portas, sem saber nada do
ambiente.

### 8.1 Dockerfiles dos projetos .NET

Primeiro, cada projeto executável precisa de um `Dockerfile`. O .NET tem um
padrão de multi-stage build: uma stage para compilar, outra para rodar. Isso
mantém a imagem final pequena (não precisa do SDK completo, só do runtime).

Crie `src/Mes.Api/Dockerfile`:

```dockerfile
# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto para restaurar dependências primeiro
# (aproveitamos o cache do Docker: se o .csproj não mudou, não precisa restaurar)
COPY ["src/Mes.Api/Mes.Api.csproj", "src/Mes.Api/"]
COPY ["src/Mes.Application/Mes.Application.csproj", "src/Mes.Application/"]
COPY ["src/Mes.Domain/Mes.Domain.csproj", "src/Mes.Domain/"]
COPY ["src/Mes.Infrastructure/Mes.Infrastructure.csproj", "src/Mes.Infrastructure/"]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "src/Mes.Api/Mes.Api.csproj"

# Copia o resto do código e publica
COPY . .
RUN dotnet publish "src/Mes.Api/Mes.Api.csproj" -c Release -o /app/publish

# Stage 2: runtime (imagem final, muito menor)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expõe a porta 8080 (padrão do ASP.NET Core em container)
EXPOSE 8080
ENTRYPOINT ["dotnet", "Mes.Api.dll"]
```

Crie `src/Mes.Simulator/Dockerfile` (estrutura idêntica, só muda o projeto):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Mes.Simulator/Mes.Simulator.csproj", "src/Mes.Simulator/"]
COPY ["src/Mes.Application/Mes.Application.csproj", "src/Mes.Application/"]
COPY ["src/Mes.Domain/Mes.Domain.csproj", "src/Mes.Domain/"]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "src/Mes.Simulator/Mes.Simulator.csproj"

COPY . .
RUN dotnet publish "src/Mes.Simulator/Mes.Simulator.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mes.Simulator.dll"]
```

Crie `web/Dockerfile`:

```dockerfile
# Stage 1: build do frontend
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json .
RUN npm ci

COPY . .
# A variável VITE_API_BASE_URL precisa ser passada em build-time
ARG VITE_API_BASE_URL=http://localhost:5000
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

# Stage 2: servir com nginx
FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
# Configuração do nginx para suportar React Router (SPA)
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

Crie `web/nginx.conf`:

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    # Redireciona qualquer rota desconhecida para o index.html
    # Necessário para que o React Router funcione ao dar F5 em /work-orders/123
    location / {
        try_files $uri $uri/ /index.html;
    }

    gzip on;
    gzip_types text/plain text/css application/json application/javascript;
}
```

### 8.2 docker-compose.yml

Crie `docker-compose.yml` na raiz:

```yaml
# docker-compose.yml
# Sobe 4 serviços: PostgreSQL, API, Simulador e Frontend
# Uso: docker compose up --build
# Para rodar em background: docker compose up -d --build

services:

  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: mes_core
      POSTGRES_USER: mes_user
      # Em produção, nunca hardcode senha aqui — use secrets ou variável de ambiente
      # Para desenvolvimento local, isso é aceitável
      POSTGRES_PASSWORD: mes_dev_password
    ports:
      - "5432:5432"       # expõe a porta para você conectar pelo DBeaver/pgAdmin
    volumes:
      - postgres_data:/var/lib/postgresql/data   # dados persistem entre reinicializações
    healthcheck:
      # O healthcheck garante que a API só inicia quando o banco estiver pronto
      # Sem isso, a API tenta conectar antes do Postgres aceitar conexões e falha
      test: ["CMD-SHELL", "pg_isready -U mes_user -d mes_core"]
      interval: 5s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: src/Mes.Api/Dockerfile
    ports:
      - "5000:8080"       # API disponível em http://localhost:5000
    environment:
      # String de conexão para o banco dentro da rede Docker
      # "postgres" é o nome do serviço acima — o Docker resolve o DNS automaticamente
      ConnectionStrings__Default: "Host=postgres;Database=mes_core;Username=mes_user;Password=mes_dev_password"
      # Aplica migrations automaticamente na primeira inicialização
      Mes__ApplyMigrationsOnStartup: "true"
      # Seed de demonstração
      Mes__SeedOnStartup: "true"
      # ATENÇÃO: esta chave é só para desenvolvimento. Em produção, vem de variável de ambiente segura.
      Jwt__SecretKey: "dev-secret-key-change-in-production-min-32-chars"
      Jwt__Issuer: "mes-core"
      Jwt__Audience: "mes-core-client"
      # CORS: permite o frontend local
      Cors__AllowedOrigins__0: "http://localhost:80"
      Cors__AllowedOrigins__1: "http://localhost:5173"
    depends_on:
      postgres:
        condition: service_healthy    # ← só sobe quando o healthcheck do postgres passar

  simulator:
    build:
      context: .
      dockerfile: src/Mes.Simulator/Dockerfile
    environment:
      Simulator__ApiBaseUrl: "http://api:8080"    # comunica com a API pela rede interna Docker
      Simulator__Enabled: "true"
      Simulator__ProductionIntervalSeconds: "5"
      Simulator__FailureMtbfMinutes: "10"
      Simulator__RepairMttrMinutes: "2"
    depends_on:
      - api

  frontend:
    build:
      context: ./web
      dockerfile: Dockerfile
      args:
        VITE_API_BASE_URL: "http://localhost:5000"
    ports:
      - "80:80"           # frontend disponível em http://localhost
    depends_on:
      - api

volumes:
  postgres_data:
```

> **Ponto de aprendizado — `depends_on: condition: service_healthy`:**  
> Esta configuração é o requisito `requirements.md §10.3`. Sem ela, a API tenta
> conectar no banco antes do PostgreSQL estar pronto para aceitar conexões e o
> container da API falha na inicialização. O `healthcheck` no serviço `postgres`
> usa `pg_isready` — um utilitário que verifica se o banco está aceitando
> conexões. Só quando esse comando retorna sucesso é que a API sobe.

### Como saber que deu certo

```powershell
docker compose build
```

Deve completar sem erros. Ainda não rodamos, só verificamos se os Dockerfiles
estão corretos.

---

## 9. CI no GitHub Actions

O CI é o que garante que ninguém (inclusive você no cansaço) vai fazer push de
código que quebra o build ou os testes.

### O que o pipeline vai fazer

1. Instalar .NET e Node
2. Restaurar dependências
3. Build com `TreatWarningsAsErrors=true`
4. Rodar testes de unidade e propriedade (rápidos)
5. Rodar testes de integração com Testcontainers (precisam do Docker)
6. Lint do frontend
7. Build das imagens Docker (prova que os Dockerfiles estão corretos)

Crie `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  backend:
    name: Backend — build, test
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore
        run: dotnet restore

      - name: Build (warnings as errors)
        run: dotnet build --no-restore --configuration Release

      - name: Unit & Property tests
        run: |
          dotnet test tests/Mes.Domain.UnitTests --no-build --configuration Release
          dotnet test tests/Mes.Domain.PropertyTests --no-build --configuration Release

      # Os testes de integração precisam do Docker, que está disponível no ubuntu-latest
      - name: Integration tests
        run: dotnet test tests/Mes.Api.IntegrationTests --no-build --configuration Release

  frontend:
    name: Frontend — lint, build
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: web

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: "22"
          cache: "npm"
          cache-dependency-path: web/package-lock.json

      - name: Install
        run: npm ci

      - name: Lint
        run: npm run lint

      - name: Build
        run: npm run build

  docker:
    name: Docker — build images
    runs-on: ubuntu-latest
    needs: [backend, frontend]   # só roda se backend e frontend passarem

    steps:
      - uses: actions/checkout@v4

      - name: Build API image
        run: docker build -f src/Mes.Api/Dockerfile -t mes-api:ci .

      - name: Build Simulator image
        run: docker build -f src/Mes.Simulator/Dockerfile -t mes-simulator:ci .

      - name: Build Frontend image
        run: docker build -f web/Dockerfile web/ -t mes-frontend:ci
```

### Como saber que deu certo

Faça o primeiro push com todo esse scaffold:

```powershell
git add .
git commit -m "chore: initial project scaffold"
git push
```

Acesse `https://github.com/SEU_USUARIO/mes-core/actions`. O workflow vai
aparecer em execução. Como os projetos ainda estão vazios (sem código real), o
build vai passar, mas os testes podem falhar se os templates gerarem testes de
exemplo que dependem de namespace inexistente. Vamos resolver isso limpando os
arquivos de teste gerados automaticamente.

---

## 10. Primeira execução

### 10.1 Limpar os arquivos gerados pelos templates

Os templates do `dotnet new` geram arquivos de exemplo que não fazem parte do
nosso projeto. Delete-os:

```powershell
# Remove os arquivos de exemplo dos projetos de domínio e aplicação
Remove-Item src/Mes.Domain/Class1.cs
Remove-Item src/Mes.Application/Class1.cs
Remove-Item src/Mes.Infrastructure/Class1.cs

# Remove os testes de exemplo gerados pelo template xunit
Remove-Item tests/Mes.Domain.UnitTests/UnitTest1.cs
Remove-Item tests/Mes.Domain.PropertyTests/UnitTest1.cs
Remove-Item tests/Mes.Api.IntegrationTests/UnitTest1.cs

# O template webapi gera um controller de exemplo (WeatherForecast)
# Se tiver sido gerado, remova:
Remove-Item -ErrorAction SilentlyContinue src/Mes.Api/Controllers/WeatherForecastController.cs
Remove-Item -ErrorAction SilentlyContinue src/Mes.Api/WeatherForecast.cs
```

### 10.2 Build limpo

```powershell
dotnet build
```

Deve passar com 0 erros e 0 warnings. Se ainda houver warnings de nullable,
abra o arquivo indicado e corrija (geralmente uma propriedade `string` sem
inicialização).

### 10.3 Verificar a estrutura final

```powershell
Get-ChildItem -Recurse -Include "*.csproj" | Select-Object FullName
```

Deve listar os 8 projetos.

### 10.4 Rodar os testes (vazio, mas sem erro)

```powershell
dotnet test
```

Com os projetos de teste vazios, vai retornar `No tests found` ou similar — isso
é esperado. O importante é que não há erro de compilação.

### 10.5 Commit do scaffold limpo

```powershell
git add .
git commit -m "chore: clean up template boilerplate files"
git push
```

Acesse o GitHub Actions e confirme que o CI passa (mesmo com testes vazios).
Quando o CI tiver o badge verde, o scaffold está completo.

---

## 11. Próximos passos — onde o design.md entra

Com o scaffold pronto, começa a implementação real. A partir daqui, **cada sprint
tem o seu próprio guia**, no mesmo formato deste documento: qual arquivo criar,
em que ordem, o que vai dentro, e como validar.

### 👉 Continue em [`scaffolding/README.md`](scaffolding/README.md)

Esse é o índice geral. Ele contém:

- O mapa dos 12 sprints, com duração, foco e quais são intocáveis
- A tabela "quero implementar X, onde está documentado?"
- A estrutura final do repositório, com o sprint que cria cada pasta

### O próximo passo concreto

Abra [`scaffolding/sprint-01-foundation.md`](scaffolding/sprint-01-foundation.md).
Ele fecha o que falta da fundação: limpar o boilerplate do template, criar o
endpoint `GET /health`, escrever o primeiro teste com valor real e o ADR-0001.

### Referência rápida: onde cada decisão está documentada

| O que implementar | Onde está documentado |
|---|---|
| Entidades e invariantes | `design.md §8` |
| Máquina de estados da WorkOrder | `design.md §10` |
| Algoritmo de cálculo de OEE | `design.md §17` |
| Genealogia de lote (CTE recursiva) | `design.md §18` |
| Idempotência no apontamento | `design.md §19` |
| Concorrência otimista (`xmin`) | `design.md §23` |
| Contratos de API (endpoints, DTOs) | `design.md §22` |
| Propriedades para property-based testing | `design.md §25` |
| Critérios de aceitação verificáveis | `requirements.md` |

---

## Apêndice A — Comandos de referência rápida

```powershell
# Build
dotnet build

# Rodar todos os testes
dotnet test

# Rodar apenas testes de unidade (rápidos)
dotnet test tests/Mes.Domain.UnitTests

# Rodar a API localmente (sem Docker)
dotnet run --project src/Mes.Api

# Subir tudo com Docker
docker compose up --build

# Subir só o banco (para desenvolver a API localmente)
docker compose up postgres

# Adicionar migration do EF Core
dotnet ef migrations add NomeDaMigration --project src/Mes.Infrastructure --startup-project src/Mes.Api

# Aplicar migrations manualmente
dotnet ef database update --project src/Mes.Infrastructure --startup-project src/Mes.Api

# Gerar tipos TypeScript a partir do OpenAPI (rode a API primeiro)
cd web
npx openapi-typescript http://localhost:5000/openapi/v1.json -o src/api/schema.d.ts
```

## Apêndice B — Troubleshooting comum

| Problema | Causa provável | Solução |
|---|---|---|
| `Build failed: TreatWarningsAsErrors` | Propriedade `string` sem inicialização (nullable) | Adicione `= string.Empty;` ou use `required` |
| `dotnet ef` não encontrado | EF Core tools não instalado | `dotnet tool install -g dotnet-ef` |
| `docker compose up` falha na API | Postgres ainda não está pronto | Verifique o `healthcheck` no `docker-compose.yml` |
| Testes de integração falham localmente | Docker Desktop não está rodando | Inicie o Docker Desktop antes de rodar os testes |
| `npm run dev` não conecta na API | CORS não configurado ou URL errada | Verifique `VITE_API_BASE_URL` no `.env` e o CORS na API |
| `gh repo create` falha | Não autenticado | Rode `gh auth login` |

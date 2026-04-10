# Trail — Backend API

API REST do **Projeto Trail**, plataforma de gestão de trilhas de aprendizagem, desafios técnicos e fluxos de mentoria para o Programa Residência Porto Digital, com mentoria técnica da Avanade.

---

## Stack

| Camada | Tecnologia |
|--------|-----------|
| Runtime | ASP.NET Core (.NET 10) |
| ORM | Entity Framework Core |
| Banco | SQL Server |
| Auth | JWT Bearer |
| Docs | OpenAPI (nativo .NET 10) |
| Cloud | Azure App Service + Azure SQL |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server local **ou** Docker
- [`dotnet-ef`](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) CLI tool

```bash
dotnet tool install --global dotnet-ef
```

---

## Como rodar localmente

### 1. Clonar o repositório

```bash
git clone <url-do-repo>
cd trail-backend
```

### 2. Subir o SQL Server com Docker

```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=Trail@1234" \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

> Se já tiver SQL Server local instalado, ajuste a connection string em `appsettings.Development.json`.

### 3. Configurar o ambiente de desenvolvimento

O arquivo `Trail.Api/appsettings.Development.json` **não é versionado** (está no `.gitignore`).  
Crie-o na raiz do projeto `Trail.Api/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=TrailDb;User Id=sa;Password=Trail@1234;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "trail-super-secret-key-for-dev-only-32chars!!"
  }
}
```

> Em produção, use variáveis de ambiente ou Azure Key Vault — nunca versione segredos.

### 4. Aplicar as migrations

```bash
dotnet ef database update --project Trail.Api
```

### 5. Rodar a API

```bash
dotnet run --project Trail.Api
```

A API estará disponível em:
- `https://localhost:7xxx` — HTTPS
- `http://localhost:5xxx` — HTTP

Spec OpenAPI: `https://localhost:7xxx/openapi/v1.json`

---

## Endpoints do MVP

| Método | Rota | Role | Descrição |
|--------|------|------|-----------|
| `GET` | `/health` | Público | Health check da API |
| `POST` | `/auth/login` | Público | Autenticação, retorna JWT |
| `GET` | `/trails` | Autenticado | Lista trilhas |
| `GET` | `/trails/{id}/challenges` | Autenticado | Desafios de uma trilha |
| `POST` | `/submissions` | Student | Submete uma entrega |
| `GET` | `/submissions` | Mentor | Lista entregas pendentes |
| `PUT` | `/submissions/{id}/review` | Mentor | Avalia uma entrega |
| `GET` | `/students/{id}/progress` | Autenticado | Progresso do estudante |
| `GET` | `/metrics/overview` | Mentor / Manager | KPIs da turma |

---

## KPIs (calculados dinamicamente, nunca persistidos)

| KPI | Fórmula |
|-----|---------|
| Lead Time de Feedback | `ReviewedAt − SubmittedAt` |
| Taxa de Conclusão | `desafios concluídos ÷ total (%)` |
| Cobertura de Desafios | `submissões avaliadas ÷ submissões entregues (%)` |

---

## Modelo de dados

```
User        — Id, Name, Email, PasswordHash, Role, CreatedAt
Trail       — Id, Name, Description, CreatedAt
Challenge   — Id, TrailId (FK), Title, Description, Order, CreatedAt
Submission  — Id, StudentId (FK), ChallengeId (FK), DeliveryUrl,
              SubmittedAt, Status, ReviewerId (FK), Score, Feedback, ReviewedAt
```

> A avaliação está embutida na `Submission` — não existe entidade `Review` no MVP (decisão intencional).

---

## Estrutura de pastas

```
trail-backend/
│
├── Trail.Api/                         # Projeto principal da API
│   │
│   ├── Controllers/                   # Endpoints HTTP (controllers finos, sem lógica)
│   │   ├── AuthController.cs          # POST /auth/login
│   │   └── HealthController.cs        # GET /health
│   │
│   ├── Domain/                        # Núcleo do domínio (sem dependências externas)
│   │   ├── Entities/                  # Entidades persistidas no banco
│   │   │   ├── User.cs
│   │   │   ├── Trail.cs
│   │   │   ├── Challenge.cs
│   │   │   └── Submission.cs
│   │   └── Enums/                     # Tipos enumerados do domínio
│   │       ├── UserRole.cs            # Student | Mentor | Manager
│   │       └── SubmissionStatus.cs    # Submitted | Reviewed
│   │
│   ├── Application/                   # Lógica de negócio e orquestração
│   │   ├── Services/                  # Serviços de aplicação
│   │   │   └── AuthService.cs         # Login + geração de JWT
│   │   └── Interfaces/                # Contratos dos serviços (para injeção de dependência)
│   │
│   ├── Infrastructure/                # Detalhes de infraestrutura
│   │   └── Data/                      # Persistência
│   │       └── AppDbContext.cs        # DbContext com mapeamentos EF Core
│   │
│   ├── DTOs/                          # Objetos de transferência de dados (entrada/saída da API)
│   │   └── Auth/
│   │       ├── LoginRequest.cs        # { Email, Password }
│   │       └── LoginResponse.cs       # { Token, Role, Name }
│   │
│   ├── Migrations/                    # Migrations geradas pelo EF Core
│   ├── Program.cs                     # Configuração da aplicação (DI, middlewares, JWT)
│   ├── appsettings.json               # Configurações base (sem segredos)
│   └── appsettings.Development.json   # Configurações locais (NÃO versionado)
│
├── .github/                           # Configurações GitHub
│   ├── workflows/
│   │   └── ci.yml                     # Pipeline CI: build + testes
│   ├── PULL_REQUEST_TEMPLATE/         # Template de PR
│   ├── ISSUE_TEMPLATE/                # Templates de bug e feature
│   ├── CODEOWNERS                     # Responsáveis por revisão
│   └── copilot-instructions.md        # Instruções para o GitHub Copilot
│
├── project-infos/                     # Documentação da mentoria (sem código)
│   ├── discovery/                     # Briefing e discovery do produto
│   ├── encontros/                     # Material de cada encontro
│   ├── atividades/                    # Desafios técnicos e checklists
│   ├── entregas/                      # Artefatos produzidos pelos alunos
│   └── referencias/                   # Boas práticas, links, arquitetura
│
├── CLAUDE.md                          # Instruções para o Claude Code
├── Trail.slnx                         # Arquivo de solução .NET
└── .gitignore                         # Arquivos ignorados pelo git
```

---

## Regras de arquitetura

- **Controllers** são finos — sem lógica de negócio
- **Lógica de negócio** fica nos `Services` (Application layer)
- **KPIs** são sempre calculados dinamicamente, nunca persistidos no banco
- **Autorização** por Role é feita no backend (`[Authorize(Roles = "Mentor")]`)
- **Frontend** não implementa regras de negócio

---

## Fora do escopo do MVP

Não implementar: chat, gamificação, white-label, relatórios interprogramas, upload de arquivo (apenas `DeliveryUrl` como link).

---

## Convenções

- Rotas em inglês, snake_case
- Erros retornam `ProblemDetails` (RFC 7807)
- Toda migration deve ser revisada antes do commit
- Nunca commitar `appsettings.Development.json`
- Swagger/OpenAPI sempre atualizado

---

**Mentoria técnica: Avanade | Programa: Porto Digital — Residência**

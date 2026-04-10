# GitHub Copilot Instructions — Trail Backend

## Contexto do projeto

API REST do Projeto Trail (ASP.NET Core + EF Core + SQL Server + JWT).
Plataforma de gestão de trilhas de aprendizagem para o Programa Residência Porto Digital.

---

## Stack obrigatória

- ASP.NET Core (.NET 8+) com Controllers
- Entity Framework Core (SQL Server)
- ASP.NET Identity + JWT Bearer
- Swagger (Swashbuckle)

---

## Entidades principais

```csharp
// User — qualquer usuário do sistema
// Role: Student | Mentor | Manager

// Trail — trilha de aprendizagem
// Challenge — desafio dentro da trilha (tem Order para sequência)

// Submission — entidade central do MVP
// Contém tanto a entrega (StudentId, ChallengeId, DeliveryUrl, SubmittedAt, Status)
// quanto a avaliação (ReviewerId, Score, Feedback, ReviewedAt)
// NÃO existe entidade Review separada no MVP
```

---

## Padrões de código esperados

### Controllers
- Controllers finos — sem lógica de negócio
- Retornar `ActionResult<T>` tipado
- Usar `[Authorize(Roles = "...")]` para proteger endpoints
- Retornar `ProblemDetails` em erros

```csharp
// Exemplo de endpoint correto
[HttpPost]
[Authorize(Roles = "Student")]
public async Task<ActionResult<SubmissionDto>> Create(CreateSubmissionDto dto)
{
    var result = await _submissionService.CreateAsync(dto, User);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

### Services (Application Layer)
- Centralizar lógica de negócio aqui
- Calcular KPIs dinamicamente (nunca persistir métricas)
- Receber e retornar DTOs (não entidades)

### EF Core
- Usar Global Query Filters para multi-tenancy
- Sempre usar migrations (nunca EnsureCreated em produção)
- Evitar N+1 — usar Include() ou projeções

### DTOs
- DTOs separados para entrada (CreateXxxDto) e saída (XxxDto)
- Nunca expor entidades do domínio diretamente nas respostas

---

## KPIs — calcular assim

```csharp
// Lead Time (em horas)
var leadTime = (submission.ReviewedAt - submission.SubmittedAt)?.TotalHours;

// Taxa de Conclusão
var taxa = (double)concluidos / total * 100;

// Cobertura
var cobertura = (double)avaliadas / entregues * 100;
```

---

## Segurança

- Nunca armazenar senha em texto puro — usar `PasswordHasher<T>`
- JWT deve conter claims: `sub` (userId), `role`, `email`
- Validar Role no backend, nunca confiar apenas no frontend
- Connection strings em variáveis de ambiente ou secrets, nunca em código

---

## O que NÃO gerar

- Entidade `Review` separada (avaliação está na Submission)
- Persistência de métricas/KPIs em tabelas
- Upload de arquivo (apenas DeliveryUrl como string)
- Funcionalidades fora do backlog: chat, gamificação, white-label
- Lógica de negócio dentro de Controllers

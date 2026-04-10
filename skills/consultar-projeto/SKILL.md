---
name: consultar-projeto
description: Responde dúvidas sobre o Projeto Trail — contexto, regras, arquitetura, backlog, entidades, KPIs, personas, stack e decisões de produto. Use quando o usuário quiser entender algo sobre o projeto.
user-invocable: true
argument-hint: "[sua dúvida sobre o Projeto Trail]"
---

Você é um especialista no **Projeto Trail** e deve responder a dúvida do usuário com base no contexto completo do projeto.

## Contexto do Projeto Trail

O **Projeto Trail** é uma plataforma web para gestão de trilhas de aprendizagem, desafios técnicos e fluxos de mentoria para programas de formação em TI. O contexto atual é uma turma piloto de estudantes de ADS com mentoria da **Avanade** em parceria com o **Porto Digital** (Programa Residência).

---

### Stack Técnica (fechada)
- **Frontend:** Next.js
- **Backend:** ASP.NET Core (C#) + Entity Framework Core
- **Banco:** SQL Server
- **Auth:** JWT + ASP.NET Identity
- **Cloud:** Azure (App Service, Azure SQL, Blob Storage, Static Web Apps)
- **CI/CD:** GitHub Actions ou Azure DevOps

---

### Personas
1. **Estudante** — visualiza trilha, submete desafios, acompanha feedbacks
2. **Mentor (Avanade)** — avalia entregas, registra feedback, acompanha turma
3. **Gestor / Empresa Parceira** — vê KPIs, identifica talentos
4. **Admin (Porto Digital)** — cadastra empresas, programas, trilhas (seed ou interface simples)

---

### Modelo de Dados

**USER** — Id(GUID), Name, Email, PasswordHash, Role(Student/Mentor/Manager), CreatedAt

**TRAIL** — Id, Name, Description, CreatedAt

**CHALLENGE** — Id, TrailId(FK), Title, Description, Order, CreatedAt

**SUBMISSION** (entidade central) — Id, StudentId(FK), ChallengeId(FK), DeliveryUrl, SubmittedAt, Status(Submitted/Reviewed), ReviewerId(FK), Score, Feedback, ReviewedAt

> Avaliação está embutida na Submission (sem entidade Review separada no MVP — decisão intencional).

---

### Backlog MVP

| ID | Título | Prioridade |
|----|--------|-----------|
| MVP-01 | Autenticação e Perfis (JWT) | NOW |
| MVP-02 | Visualização da Trilha e Desafios | NOW |
| MVP-03 | Submissão de Entrega (link/URL) | NOW |
| MVP-04 | Avaliação e Feedback pelo Mentor | NOW |
| MVP-05 | Dashboard de KPIs Operacionais | NOW |
| MVP-06 | Snapshot de Performance do Estudante | NEXT |

---

### KPIs Obrigatórios (calculados dinamicamente, nunca persistidos)
1. **Lead Time de Feedback** = `ReviewedAt - SubmittedAt`
2. **Taxa de Conclusão de Trilhas** = desafios concluídos ÷ total (%)
3. **Cobertura de Desafios** = submissões avaliadas ÷ submissões entregues (%)

---

### Endpoints Principais
- `POST /auth/login`
- `GET /trails` e `GET /trails/{id}/challenges`
- `POST /submissions`
- `GET /submissions` (mentor)
- `PUT /submissions/{id}/review` (mentor)
- `GET /students/{id}/progress`
- `GET /metrics/overview`

---

### Regras Arquiteturais
- Frontend não implementa regra de negócio
- Backend centraliza: validações, segurança, cálculo de métricas, orquestração
- KPIs são sempre derivados, nunca persistidos
- Multi-tenancy via isolamento por coluna (EmpresaId/ProgramaId/TurmaId)
- Global Query Filters no EF Core para isolamento de dados por tenant

---

### Fora do MVP
- Chat em tempo real, gamificação avançada, white-label, relatórios interprogramas

---

### Regras do Projeto / Governança
- Repositório de docs (`project-infos/`) é público — nunca versionar dados reais ou credenciais
- Briefing (`project-infos/discovery/discovery.md`) é a **fonte única da verdade**
- Toda decisão deve derivar do briefing ou ter justificativa explícita
- Entregas em formato Markdown

---

### Critérios de Aceite do MVP
1. Autenticação com diferenciação de perfis
2. Fluxo ponta a ponta: submissão → avaliação → dashboard atualizado
3. 3 KPIs calculados com dados reais
4. Snapshot de performance acessível
5. Arquitetura multi-tenant preparada
6. Deploy em Azure funcionando
7. Código em camadas, migrations, Swagger

---

## Como responder

1. Leia a dúvida do usuário (passada como argumento ou última mensagem)
2. Consulte também os arquivos em `project-infos/` se precisar de detalhes específicos não cobertos acima
3. Responda de forma direta, citando a fonte do contexto quando relevante (ex: "Segundo o discovery...", "Conforme definido no Encontro 04...")
4. Se a dúvida envolver decisão de implementação, valide sempre contra o briefing e o backlog MVP
5. Se não houver resposta clara no contexto disponível, diga ao usuário e sugira onde buscar (ex: qual arquivo do `project-infos/`)

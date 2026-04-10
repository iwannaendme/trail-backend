# Pull Request — Trail Backend

## O que este PR faz?

<!-- Descreva em 1–3 linhas o que foi implementado ou corrigido. -->

---

## Item do backlog

- [ ] MVP-01 — Autenticação e Perfis
- [ ] MVP-02 — Visualização da Trilha e Desafios
- [ ] MVP-03 — Submissão de Entrega
- [ ] MVP-04 — Avaliação e Feedback
- [ ] MVP-05 — KPIs Operacionais
- [ ] MVP-06 — Snapshot de Performance
- [ ] Outro: ___________

## Tipo de mudança

- [ ] Feature nova
- [ ] Correção de bug
- [ ] Refatoração
- [ ] Migration / banco de dados
- [ ] Infra / pipeline
- [ ] Documentação

---

## Entidades impactadas

- [ ] User
- [ ] Trail
- [ ] Challenge
- [ ] Submission
- [ ] Nenhuma

**Campos adicionados/alterados:**
```
-
```

---

## Endpoints criados/alterados

```
Exemplo: POST /submissions, PUT /submissions/{id}/review
```

---

## Como validar

```bash
# Passos para testar este PR localmente
1.
2.
3.
```

---

## Uso de IA (Copilot / assistente)

- [ ] Não usei IA
- [ ] Usei IA — revisei o código e consigo explicar tudo que foi gerado

---

## Checklist

- [ ] O código compila sem erros
- [ ] Segue a arquitetura em camadas (Controller → Service → Repository)
- [ ] Lógica de negócio está no Service, não no Controller
- [ ] Endpoints protegidos com `[Authorize]` correto
- [ ] Migration gerada (se alterou banco)
- [ ] Swagger atualizado
- [ ] Sem connection strings ou credenciais no código
- [ ] Sem entidade ou funcionalidade fora do escopo do MVP

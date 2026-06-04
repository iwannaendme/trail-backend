using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trail.Api.Domain.Entities;
using Trail.Api.Domain.Enums;
using TrailEntity = Trail.Api.Domain.Entities.Trail;

namespace Trail.Api.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        await SeedUsersAsync(db, logger);
        await SeedTrailsAsync(db, logger);
        await SeedEnrollmentsAndActivityAsync(db, logger);
        await SeedSubmissionsAsync(db, logger);
    }

    private static async Task SeedUsersAsync(AppDbContext db, ILogger logger)
    {
        var hasher = new PasswordHasher<User>();

        var seeds = new List<(string Name, string Email, UserRole Role)>
        {
            ("Admin Manager",   "manager@trail.com", UserRole.Manager),
            ("Mentor Avanade",  "mentor@trail.com",  UserRole.Mentor),
            ("Estudante Teste", "student@trail.com", UserRole.Student),
        };

        var inserted = 0;
        foreach (var (name, email, role) in seeds)
        {
            if (await db.Users.AnyAsync(u => u.Email == email)) continue;

            var user = new User { Id = Guid.NewGuid(), Name = name, Email = email, Role = role };
            user.PasswordHash = hasher.HashPassword(user, "Senha@123");
            user.Settings = new UserSettings
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EmailNotifications = true,
                StudyReminder = true,
                AiSuggestions = true,
                WeeklyReport = true,
                Language = "pt-BR",
                DailyStudyGoal = "1h",
                Autoplay = true,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Users.Add(user);
            inserted++;
        }

        if (inserted > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("User seed: {Count} user(s) inserted.", inserted);
        }
        else
        {
            logger.LogInformation("User seed skipped: all seed users already exist.");
        }
    }

    // ── Trail seed — checks each trail by name so it's additive across restarts ──

    private static async Task SeedTrailsAsync(AppDbContext db, ILogger logger)
    {
        var catalog = BuildTrailCatalog();
        var inserted = 0;

        foreach (var (name, description, challenges) in catalog)
        {
            if (await db.Trails.AnyAsync(t => t.Name == name)) continue;

            var trail = new TrailEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                Challenges = challenges.Select((c, i) => new Challenge
                {
                    Id = Guid.NewGuid(),
                    Title = c.Title,
                    Description = c.Description,
                    Order = i + 1,
                    CreatedAt = DateTime.UtcNow,
                }).ToList(),
            };

            db.Trails.Add(trail);
            inserted++;
        }

        if (inserted > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("Trail seed: {Count} trail(s) inserted.", inserted);
        }
        else
        {
            logger.LogInformation("Trail seed skipped: all catalog trails already exist.");
        }
    }

    private static List<(string Name, string Description, (string Title, string Description)[] Challenges)> BuildTrailCatalog() =>
    [
        (
            "Fundamentos de .NET",
            "Do zero ao primeiro endpoint: C#, orientação a objetos, ASP.NET Core e Entity Framework Core com SQL Server.",
            [
                ("Hello World em C#", "Crie um console app que leia o nome do usuário e imprima 'Olá, {nome}!' com interpolação de strings."),
                ("Orientação a Objetos", "Modele uma classe Produto com propriedades, construtores e métodos. Crie instâncias e imprima seus dados."),
                ("Collections e LINQ", "Filtre, ordene e projete uma lista de produtos usando LINQ fluente e lambda expressions."),
                ("API REST mínima", "Implemente endpoints GET, POST, PUT e DELETE para Produto em uma Minimal API do ASP.NET Core."),
                ("Entity Framework Core", "Mapeie a entidade Produto para SQL Server com EF Core, configure migrations e persista dados reais."),
                ("Injeção de Dependência", "Extraia a lógica de negócio para um serviço IProdutoService e configure o container de DI."),
            ]
        ),
        (
            "React & Next.js do Zero",
            "Fundamentos de React moderno (hooks, context, RSC) e Next.js App Router — do setup ao deploy.",
            [
                ("Setup e JSX", "Inicialize um projeto Next.js com TypeScript. Crie componentes funcionais, passe props e renderize listas."),
                ("Estado com useState", "Construa um contador, um formulário controlado e uma todo list usando useState e events."),
                ("Efeitos e Fetch", "Use useEffect para buscar dados de uma API pública e exiba-os com loading e error states."),
                ("Context API", "Implemente um ThemeContext global que alterna entre dark e light mode em toda a aplicação."),
                ("App Router & Layouts", "Organize rotas com layouts aninhados, loading.tsx, error.tsx e route groups no App Router."),
                ("Server Components", "Migre páginas para React Server Components, busque dados server-side e use React cache()."),
                ("Deploy na Vercel", "Configure variáveis de ambiente, faça build de produção e publique o projeto na Vercel."),
            ]
        ),
        (
            "TypeScript na Prática",
            "Type-safety de verdade: tipos avançados, generics, decorators e integração com frameworks modernos.",
            [
                ("Tipos básicos e inferência", "Anote funções, objetos e arrays. Entenda quando TypeScript infere e quando você precisa anotar explicitamente."),
                ("Interfaces vs Types", "Defina contratos com interface e type alias. Explore extends, union, intersection e discriminated unions."),
                ("Generics", "Implemente uma função genérica identity, uma fila tipada Queue<T> e um hook useLocalStorage<T>."),
                ("Utility Types", "Use Partial, Required, Pick, Omit, Record e ReturnType para transformar tipos existentes."),
                ("Enums e Literal Types", "Modele status de pedido com const enum e string literal unions. Compare as abordagens."),
            ]
        ),
        (
            "Node.js & APIs REST",
            "Backend JavaScript profissional: Express, autenticação JWT, banco de dados, testes e boas práticas.",
            [
                ("Servidor Express", "Configure um servidor Express com TypeScript, middlewares de log e tratamento de erros centralizado."),
                ("Rotas e Controllers", "Organize rotas em módulos, valide inputs com Zod e retorne erros RFC 7807 (ProblemDetails)."),
                ("Autenticação JWT", "Implemente registro, login e refresh de tokens JWT com bcrypt para hashing de senhas."),
                ("ORM com Prisma", "Defina schema, rode migrations e execute queries tipadas com Prisma Client."),
                ("Upload de Arquivos", "Receba imagens via multipart/form-data, valide tipo e tamanho e salve no sistema de arquivos."),
                ("Testes com Vitest", "Escreva testes unitários para serviços e testes de integração para rotas usando Supertest."),
                ("Deploy com Docker", "Crie Dockerfile multi-stage, docker-compose com PostgreSQL e configure CI/CD básico."),
            ]
        ),
        (
            "Git & GitHub Avançado",
            "Controle de versão profissional: branching strategies, rebase interativo, hooks e GitHub Actions.",
            [
                ("Commits semânticos", "Adote Conventional Commits (feat, fix, chore…) e configure commitlint + Husky no projeto."),
                ("Branching com GitFlow", "Crie branches feature, release e hotfix seguindo GitFlow. Merge vs rebase — quando usar cada um."),
                ("Rebase interativo", "Use git rebase -i para squash, fixup e reword. Reescreva histórico sujo antes do PR."),
            ]
        ),
        (
            "Docker & DevOps",
            "Containerize aplicações, orquestre com Compose e automatize pipelines de CI/CD com GitHub Actions.",
            [
                ("Dockerfile na prática", "Escreva um Dockerfile multi-stage para uma API Node.js minimizando o tamanho da imagem final."),
                ("Docker Compose", "Suba API + banco de dados + Redis com docker-compose. Configure volumes e health checks."),
                ("Variáveis e secrets", "Gerencie .env, secrets no Docker e variáveis em diferentes ambientes (dev, staging, prod)."),
                ("GitHub Actions CI", "Crie workflow que roda lint, testes e build em cada PR. Reporte cobertura como comentário."),
                ("Deploy automatizado", "Configure CD para fazer deploy automaticamente na branch main usando SSH ou uma plataforma PaaS."),
            ]
        ),
    ];

    // ── Enrollments & Activity ────────────────────────────────────────────────

    private static async Task SeedEnrollmentsAndActivityAsync(AppDbContext db, ILogger logger)
    {
        var student = await db.Users.FirstOrDefaultAsync(u => u.Email == "student@trail.com");
        var trail = await db.Trails.OrderBy(t => t.Name).FirstOrDefaultAsync();

        if (student is null || trail is null)
        {
            logger.LogWarning("Enrollment seed skipped: prerequisites not found.");
            return;
        }

        if (!await db.TrailEnrollments.AnyAsync(e => e.UserId == student.Id && e.TrailId == trail.Id))
        {
            db.TrailEnrollments.Add(new TrailEnrollment
            {
                Id = Guid.NewGuid(),
                UserId = student.Id,
                TrailId = trail.Id,
                EnrolledAt = DateTime.UtcNow.AddDays(-14),
            });
        }

        if (!await db.UserActivities.AnyAsync(a => a.UserId == student.Id))
        {
            var activities = new[]
            {
                (Days: -6, Mins: 45), (Days: -5, Mins: 72), (Days: -4, Mins: 30),
                (Days: -3, Mins: 90), (Days: -2, Mins: 55), (Days: -1, Mins: 40), (Days: 0, Mins: 25),
            };
            foreach (var (days, mins) in activities)
            {
                db.UserActivities.Add(new UserActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = student.Id,
                    ActivityType = "study",
                    TargetType = "trail",
                    TargetId = trail.Id.ToString(),
                    Minutes = mins,
                    OccurredAt = DateTime.UtcNow.Date.AddDays(days),
                });
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Enrollment & activity seed completed.");
    }

    // ── Submissions ───────────────────────────────────────────────────────────

    private static async Task SeedSubmissionsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Submissions.AnyAsync())
        {
            logger.LogInformation("Submission seed skipped: submissions already exist.");
            return;
        }

        var student = await db.Users.FirstOrDefaultAsync(u => u.Email == "student@trail.com");
        var mentor = await db.Users.FirstOrDefaultAsync(u => u.Email == "mentor@trail.com");
        var firstChallenge = await db.Challenges.OrderBy(c => c.Order).FirstOrDefaultAsync();
        var secondChallenge = await db.Challenges.OrderBy(c => c.Order).Skip(1).FirstOrDefaultAsync();

        if (student is null || mentor is null || firstChallenge is null || secondChallenge is null)
        {
            logger.LogWarning("Submission seed skipped: required entities not found.");
            return;
        }

        db.Submissions.AddRange(
            new Submission
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                ChallengeId = firstChallenge.Id,
                GitHubUrl = "https://github.com/trail/student-hello-world",
                SubmittedAt = DateTime.UtcNow.AddDays(-3),
                Status = SubmissionStatus.Approved,
                ReviewerId = mentor.Id,
                MentorComment = "Excelente! Código limpo e bem estruturado. Pode avançar.",
                ReviewedAt = DateTime.UtcNow.AddDays(-2),
            },
            new Submission
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                ChallengeId = secondChallenge.Id,
                GitHubUrl = "https://github.com/trail/student-oo-challenge",
                SubmittedAt = DateTime.UtcNow.AddDays(-1),
                Status = SubmissionStatus.Submitted,
            }
        );

        await db.SaveChangesAsync();
        logger.LogInformation("Submission seed: 2 submissions inserted.");
    }
}

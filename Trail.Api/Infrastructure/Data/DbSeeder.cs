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
    }

    private static async Task SeedUsersAsync(AppDbContext db, ILogger logger)
    {
        var hasher = new PasswordHasher<User>();

        var seeds = new List<(string Name, string Email, UserRole Role)>
        {
            ("Admin Manager",    "manager@trail.com", UserRole.Manager),
            ("Mentor Avanade",   "mentor@trail.com",  UserRole.Mentor),
            ("Estudante Teste",  "student@trail.com", UserRole.Student),
        };

        var inserted = 0;

        foreach (var (name, email, role) in seeds)
        {
            if (await db.Users.AnyAsync(u => u.Email == email)) continue;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                Role = role
            };
            user.PasswordHash = hasher.HashPassword(user, "Senha@123");
            db.Users.Add(user);
            inserted++;
        }

        if (inserted > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("User seed completed: {Count} user(s) inserted.", inserted);
        }
        else
        {
            logger.LogInformation("User seed skipped: all seed users already exist.");
        }
    }

    private static async Task SeedTrailsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Trails.AnyAsync())
        {
            logger.LogInformation("Trail seed skipped: trails already exist.");
            return;
        }

        var fundamentos = new TrailEntity
        {
            Id = Guid.NewGuid(),
            Name = "Fundamentos de .NET",
            Description = "Trilha introdutória cobrindo C#, ASP.NET Core e Entity Framework.",
            Challenges =
            [
                new Challenge { Id = Guid.NewGuid(), Title = "Hello World em C#", Description = "Crie um console app que imprima 'Hello, Trail!'.", Order = 1 },
                new Challenge { Id = Guid.NewGuid(), Title = "API REST mínima",   Description = "Implemente um endpoint GET /ping retornando JSON.",     Order = 2 },
                new Challenge { Id = Guid.NewGuid(), Title = "CRUD com EF Core",  Description = "Modele uma entidade e exponha endpoints CRUD.",          Order = 3 },
            ]
        };

        var frontend = new TrailEntity
        {
            Id = Guid.NewGuid(),
            Name = "Frontend com Next.js",
            Description = "Trilha de frontend cobrindo React, Next.js e integração com APIs.",
            Challenges =
            [
                new Challenge { Id = Guid.NewGuid(), Title = "Setup do projeto Next.js", Description = "Inicialize um projeto Next.js com TypeScript.",      Order = 1 },
                new Challenge { Id = Guid.NewGuid(), Title = "Página de listagem",       Description = "Liste itens consumindo uma API pública.",            Order = 2 },
            ]
        };

        db.Trails.AddRange(fundamentos, frontend);
        await db.SaveChangesAsync();
        logger.LogInformation("Trail seed completed: 2 trails and 5 challenges inserted.");
    }
}

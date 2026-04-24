using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trail.Api.Domain.Entities;
using Trail.Api.Domain.Enums;

namespace Trail.Api.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
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
            logger.LogInformation("Seed completed: {Count} user(s) inserted.", inserted);
        }
        else
        {
            logger.LogInformation("Seed skipped: all seed users already exist.");
        }
    }
}

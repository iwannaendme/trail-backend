using Microsoft.AspNetCore.Identity;
using Trail.Api.Domain.Entities;
using Trail.Api.Domain.Enums;

namespace Trail.Api.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var hasher = new PasswordHasher<object>();

        var seeds = new List<(string Name, string Email, UserRole Role)>
        {
            ("Admin Manager",    "manager@trail.com", UserRole.Manager),
            ("Mentor Avanade",   "mentor@trail.com",  UserRole.Mentor),
            ("Estudante Teste",  "student@trail.com", UserRole.Student),
        };

        foreach (var (name, email, role) in seeds)
        {
            if (db.Users.Any(u => u.Email == email)) continue;

            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email,
                PasswordHash = hasher.HashPassword(null!, "Senha@123"),
                Role = role
            });
        }

        await db.SaveChangesAsync();
    }
}

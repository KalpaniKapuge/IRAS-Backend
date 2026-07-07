// IRAS.Application/Data/DataSeeder.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Domain.Entities.Identity;
using IRAS.Domain.Entities.Skills;
using IRAS.Domain.Enums;
using IRAS.Infrastructure.Data;

namespace IRAS.Application.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IrasDbContext db, string adminEmail, string adminPassword)
        {
            // ---- Admin account ----
            if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
            {
                db.Users.Add(new User
                {
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = UserRole.Admin,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }

            // ---- Skill taxonomy ----
            if (await db.Skills.AnyAsync()) return;   // already seeded

            // (name, category, aliases[])
            var seed = new (string Name, SkillCategory Cat, string[] Aliases)[]
            {
                // Programming languages
                ("JavaScript", SkillCategory.ProgrammingLanguage, new[]{"JS", "ECMAScript", "ES6"}),
                ("TypeScript", SkillCategory.ProgrammingLanguage, new[]{"TS"}),
                ("Python", SkillCategory.ProgrammingLanguage, new[]{"Python3", "Python 3"}),
                ("C#", SkillCategory.ProgrammingLanguage, new[]{"CSharp", "C Sharp", ".NET C#"}),
                ("Java", SkillCategory.ProgrammingLanguage, Array.Empty<string>()),
                ("PHP", SkillCategory.ProgrammingLanguage, Array.Empty<string>()),
                ("Go", SkillCategory.ProgrammingLanguage, new[]{"Golang"}),
                ("Kotlin", SkillCategory.ProgrammingLanguage, Array.Empty<string>()),
                ("Swift", SkillCategory.ProgrammingLanguage, Array.Empty<string>()),
                ("C++", SkillCategory.ProgrammingLanguage, new[]{"CPP", "Cplusplus"}),
                ("SQL", SkillCategory.ProgrammingLanguage, new[]{"T-SQL", "PL/SQL"}),
                ("Dart", SkillCategory.ProgrammingLanguage, Array.Empty<string>()),

                // Frameworks
                ("React", SkillCategory.Framework, new[]{"React.js", "ReactJS"}),
                ("Angular", SkillCategory.Framework, new[]{"AngularJS", "Angular 2+"}),
                ("Vue.js", SkillCategory.Framework, new[]{"Vue", "VueJS"}),
                ("Next.js", SkillCategory.Framework, new[]{"NextJS"}),
                ("Node.js", SkillCategory.Framework, new[]{"NodeJS", "Node"}),
                ("Express.js", SkillCategory.Framework, new[]{"Express", "ExpressJS"}),
                ("ASP.NET Core", SkillCategory.Framework, new[]{"ASP.NET", "ASP.NET Core Web API", "dotnet core"}),
                ("Entity Framework Core", SkillCategory.Framework, new[]{"EF Core", "EntityFramework"}),
                ("Spring Boot", SkillCategory.Framework, new[]{"Spring"}),
                ("Django", SkillCategory.Framework, Array.Empty<string>()),
                ("FastAPI", SkillCategory.Framework, Array.Empty<string>()),
                ("Flask", SkillCategory.Framework, Array.Empty<string>()),
                ("Laravel", SkillCategory.Framework, Array.Empty<string>()),
                ("Flutter", SkillCategory.Framework, Array.Empty<string>()),
                ("React Native", SkillCategory.Framework, Array.Empty<string>()),
                ("Tailwind CSS", SkillCategory.Framework, new[]{"TailwindCSS", "Tailwind"}),
                ("Bootstrap", SkillCategory.Framework, Array.Empty<string>()),
                (".NET", SkillCategory.Framework, new[]{"dotnet", ".NET Framework"}),

                // Databases
                ("SQL Server", SkillCategory.Database, new[]{"MSSQL", "Microsoft SQL Server"}),
                ("MySQL", SkillCategory.Database, Array.Empty<string>()),
                ("PostgreSQL", SkillCategory.Database, new[]{"Postgres"}),
                ("MongoDB", SkillCategory.Database, new[]{"Mongo"}),
                ("Redis", SkillCategory.Database, Array.Empty<string>()),
                ("SQLite", SkillCategory.Database, Array.Empty<string>()),
                ("Oracle Database", SkillCategory.Database, new[]{"Oracle DB", "Oracle"}),
                ("Elasticsearch", SkillCategory.Database, Array.Empty<string>()),

                // Cloud platforms
                ("AWS", SkillCategory.CloudPlatform, new[]{"Amazon Web Services"}),
                ("Microsoft Azure", SkillCategory.CloudPlatform, new[]{"Azure"}),
                ("Google Cloud Platform", SkillCategory.CloudPlatform, new[]{"GCP", "Google Cloud"}),
                ("Firebase", SkillCategory.CloudPlatform, Array.Empty<string>()),

                // Tools
                ("Git", SkillCategory.Tool, new[]{"GitHub", "GitLab"}),
                ("Docker", SkillCategory.Tool, Array.Empty<string>()),
                ("Kubernetes", SkillCategory.Tool, new[]{"K8s"}),
                ("Jenkins", SkillCategory.Tool, Array.Empty<string>()),
                ("Jira", SkillCategory.Tool, Array.Empty<string>()),
                ("Postman", SkillCategory.Tool, Array.Empty<string>()),
                ("Figma", SkillCategory.Tool, Array.Empty<string>()),
                ("Linux", SkillCategory.Tool, new[]{"Ubuntu", "Unix"}),
                ("CI/CD", SkillCategory.Tool, new[]{"Continuous Integration", "Continuous Deployment"}),
                ("REST API", SkillCategory.Tool, new[]{"RESTful API", "REST"}),
                ("GraphQL", SkillCategory.Tool, Array.Empty<string>()),
                ("Machine Learning", SkillCategory.Tool, new[]{"ML"}),
                ("TensorFlow", SkillCategory.Tool, Array.Empty<string>()),
                ("PyTorch", SkillCategory.Tool, Array.Empty<string>()),
                ("NLP", SkillCategory.Tool, new[]{"Natural Language Processing"}),

                // Soft skills
                ("Agile", SkillCategory.SoftSkill, new[]{"Scrum", "Agile Methodology"}),
                ("Team Leadership", SkillCategory.SoftSkill, new[]{"Leadership"}),
                ("Communication", SkillCategory.SoftSkill, Array.Empty<string>()),
                ("Problem Solving", SkillCategory.SoftSkill, Array.Empty<string>()),
            };

            foreach (var (name, cat, aliases) in seed)
            {
                var skill = new Skill { SkillName = name, Category = cat };
                db.Skills.Add(skill);
                await db.SaveChangesAsync();

                foreach (var alias in aliases)
                    db.SkillAliases.Add(new SkillAlias
                    {
                        SkillId = skill.SkillId,
                        AliasText = alias,
                        Source = AliasSource.SystemSeeded
                    });
            }
            await db.SaveChangesAsync();
        }
    }
}
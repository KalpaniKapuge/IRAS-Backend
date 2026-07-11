// IRAS.Application/Data/DataSeeder.cs
using Microsoft.EntityFrameworkCore;
using IRAS.Domain.Entities.Admin;
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
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
            if (admin is null)
            {
                admin = new User
                {
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = UserRole.Admin,
                    IsActive = true
                };
                db.Users.Add(admin);
                await db.SaveChangesAsync();
            }

            // ---- Skill taxonomy ----
            if (!await db.Skills.AnyAsync())
            {

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

            // ---- Knowledge base (chatbot content) ----
            if (!await db.KnowledgeBases.AnyAsync())
            {
                var kbSeed = new (string Title, string Content, KnowledgeCategory Category)[]
                {
                    ("How do I upload and parse my resume?",
                     "Go to your Resumes section and upload a PDF or DOCX file (up to 10 MB). The system automatically extracts your skills, email, and phone number. Review the suggested skills and confirm the ones that are accurate - they'll be added to your profile.",
                     KnowledgeCategory.PlatformHowTo),

                    ("How is my application score calculated?",
                     "Your total score combines two things: how well your confirmed skills match the job's required skills (weighted by importance), and how closely your resume's wording matches the job description, measured using AI semantic similarity. Experience and education are shown for context but aren't part of the final score.",
                     KnowledgeCategory.FAQ),

                    ("What is a skill gap?",
                     "A skill gap is a required or preferred skill for a job that isn't on your profile yet. Whenever you apply for a job, the system records any missing skills so you can see exactly what would strengthen a future application. You can view all your skill gaps, grouped by skill, from your dashboard.",
                     KnowledgeCategory.SkillAdvice),

                    ("How does automatic job matching work?",
                     "When an employer publishes a new job, the system automatically compares it against every candidate who has opted into matching and has a parsed resume. If your profile scores highly enough, you'll get a notification - you don't need to search for every job yourself.",
                     KnowledgeCategory.PlatformHowTo),

                    ("Why didn't I get matched to a job I'm interested in?",
                     "A few common reasons: matching may be turned off in your profile settings, your resume may not be parsed yet, or your score for that specific job may not have cleared the matching threshold. Applying directly is always possible even without an automatic match.",
                     KnowledgeCategory.FAQ),

                    ("How do I confirm skills the resume parser found?",
                     "After uploading a resume, you'll see a list of suggested skills detected in your document. Tick the ones that are accurate and confirm them - only confirmed skills are added to your profile and used for job matching and scoring.",
                     KnowledgeCategory.PlatformHowTo),

                    ("Will I get feedback if I'm rejected?",
                     "When an employer marks an application as rejected, the system prepares personalized feedback highlighting the skills that would have strengthened your application. The employer reviews and approves this feedback before it's sent to you, so it may take a little time to arrive.",
                     KnowledgeCategory.FAQ),

                    ("How do I post a job as an employer?",
                     "From your employer dashboard, create a job with its required skills, then generate a job description. Review and edit it if needed, then publish. Publishing also triggers automatic matching against opted-in candidates.",
                     KnowledgeCategory.PlatformHowTo),

                    ("What does must-have vs nice-to-have mean for job skills?",
                     "Must-have skills are required for the role and carry full weight in scoring; missing one shows up as a higher-priority skill gap. Nice-to-have skills add value but aren't required - missing them affects your score less.",
                     KnowledgeCategory.FAQ),

                    ("How do I update my candidate profile?",
                     "You can update your name, headline, education level, and matching preferences at any time from your profile page. Keeping your profile current improves both manual applications and automatic job matching.",
                     KnowledgeCategory.PlatformHowTo),
                };

                foreach (var (title, content, category) in kbSeed)
                {
                    db.KnowledgeBases.Add(new KnowledgeBase
                    {
                        Title = title,
                        Content = content,
                        Category = category,
                        IsActive = true,
                        UpdatedBy = admin.UserId
                    });
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
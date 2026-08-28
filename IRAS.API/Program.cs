// IRAS.API/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics;
using System.Text;
using IRAS.API.Filters;
using IRAS.Application.Common.Audit;
using IRAS.Application.Common.Email;
using IRAS.Application.Common.Notifications;
using IRAS.Application.Common.Options;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Data;
using IRAS.Application.Modules.Admin;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Auth;
using IRAS.Application.Modules.Candidates;
using IRAS.Application.Modules.Chat;
using IRAS.Application.Modules.Feedback;
using IRAS.Application.Modules.Interviews;
using IRAS.Application.Modules.Jobs;
using IRAS.Application.Modules.KnowledgeBase;
using IRAS.Application.Modules.Matching;
using IRAS.Application.Modules.Resumes;
using IRAS.Application.Modules.SkillDevelopment;
using IRAS.Application.Modules.SkillGaps;
using IRAS.Application.Modules.SkillImprovementPlans;
using IRAS.Application.Modules.SkillTaxonomy;
using IRAS.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Dev convenience: a previous `dotnet run` that didn't shut down cleanly (Ctrl+C not
// waited out, IDE stop, watch-triggered restart racing the old process) leaves
// IRAS.API.exe holding the Kestrel port, so the next run fails with "address already
// in use" before it ever reaches app.Run(). That happened often enough during active
// development that it's worth clearing automatically rather than a manual taskkill each
// time. Development-only, and this only ever targets a process literally named
// IRAS.API — never anything else that might be on the port.
if (builder.Environment.IsDevelopment())
{
    FreeStalePort(5048);
    FreeStalePort(7232);
}

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "IRAS API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter a JWT token obtained from /api/auth/login or /api/auth/register. Example: eyJhbGciOi...",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    // Only mark endpoints that actually require auth (has [Authorize], no [AllowAnonymous])
    // with the padlock — public endpoints like /register and /login stay open in the docs.
    options.OperationFilter<AuthorizeCheckOperationFilter>();

    // CandidateProfileController has JSON- and multipart-form actions sharing one route,
    // disambiguated at runtime by [Consumes] — Swashbuckle doesn't do that, so pick one
    // action's description per conflicting method/path pair for doc generation only.
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

builder.Services.AddDbContext<IrasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Audit logging (Module 11) — registered early since several admin-only services below
// depend on it. Needs HttpContext to capture the caller's IP address.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();

// CV builder — renders a candidate's existing profile data (education, experience,
// certifications, skills already collected via CandidateProfileService) into a downloadable
// PDF via QuestPDF, with template selection and per-CV section/item customization. No
// external API dependency, unlike the Gemini-backed generators above.
builder.Services.AddScoped<IRAS.Application.Modules.Cv.ICvPdfRenderer, IRAS.Application.Modules.Cv.CvPdfRenderer>();
builder.Services.AddScoped<IRAS.Application.Modules.Cv.ICvService, IRAS.Application.Modules.Cv.CvService>();
builder.Services.AddScoped<ISkillTaxonomyService, SkillTaxonomyService>();
builder.Services.AddScoped<IJobService, JobService>();


builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddHttpClient<IJdGenerator, GeminiJdGenerator>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Options
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection(FileStorageOptions.SectionName));
builder.Services.Configure<AiServiceOptions>(
    builder.Configuration.GetSection(AiServiceOptions.SectionName));

// Typed HTTP client for the AI service
builder.Services.AddHttpClient<IRAS.Application.Common.Ai.IAiServiceClient,
                               IRAS.Application.Common.Ai.AiServiceClient>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(AiServiceOptions.SectionName).Get<AiServiceOptions>()!;
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
});

// Storage + resume/profile upload modules
if (string.Equals(
        builder.Configuration[$"{FileStorageOptions.SectionName}:Provider"],
        "Supabase",
        StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IRAS.Application.Common.Storage.IFileStorage,
                                   IRAS.Application.Common.Storage.SupabaseFileStorage>();
}
else
{
    builder.Services.AddSingleton<IRAS.Application.Common.Storage.IFileStorage,
                                  IRAS.Application.Common.Storage.LocalDiskFileStorage>();
}
builder.Services.AddScoped<IResumeService, ResumeService>();

// Scoring — shared by Module 6 (application ranking) and Module 8 (proactive matching)
builder.Services.AddSingleton<IValidateOptions<ScoringOptions>, ScoringOptionsValidator>();
builder.Services.AddOptions<ScoringOptions>()
    .Bind(builder.Configuration.GetSection(ScoringOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddScoped<IScoringService, ScoringService>();

// Notifications — LogEmailSender is the dev-safe default (no SMTP credentials needed);
// swap in a real SmtpEmailSender/SendGridEmailSender behind the same IEmailSender later.
builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IJobMatchingService, JobMatchingService>();

builder.Services.AddScoped<ISkillGapService, SkillGapService>();
builder.Services.AddScoped<ISkillDevelopmentService, SkillDevelopmentService>();
builder.Services.AddScoped<ISkillImprovementPlanService, SkillImprovementPlanService>();

builder.Services.AddHttpClient<ISkillGapExplainer, GeminiSkillGapExplainer>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<ISkillPlanGenerator, GeminiSkillPlanGenerator>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient<IFeedbackGenerator, GeminiFeedbackGenerator>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

builder.Services.AddScoped<IInterviewService, InterviewService>();

builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddHttpClient<IChatResponder, GeminiChatResponder>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IJobModerationService, JobModerationService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<ISystemStatusService, SystemStatusService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
        ClockSkew = TimeSpan.Zero   // tokens expire exactly at ExpiryMinutes, no grace period
    };
});
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        // Vite dev server — allow the default port and the next few it falls back to
        // when 5173 is already taken by another running instance.
        policy.WithOrigins(
                  "http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "http://localhost:5176")
              .AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IrasDbContext>();
    await DataSeeder.SeedAsync(
        db,
        builder.Configuration["Seed:AdminEmail"] ?? "admin@iras.local",
        builder.Configuration["Seed:AdminPassword"] ?? "ChangeMe@123");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Prevent leaking stack traces / internal exception details in production.
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
    }));
}

// Serves files saved by LocalDiskFileStorage as real, fetchable URLs (logos,
// certificates, profile pictures) — mirrors Supabase Storage's public URLs so
// the two providers behave identically to everything that consumes IFileStorage.
var uploadsRoot = builder.Configuration["FileStorage:ResumeRootPath"];
if (!string.IsNullOrWhiteSpace(uploadsRoot))
{
    Directory.CreateDirectory(uploadsRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsRoot),
        RequestPath = "/uploads",
    });
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// See the FreeStalePort(...) calls near the top of this file — Development-only,
// finds whatever is LISTENING on the given port via `netstat -ano`, and kills it only if
// that process is literally named IRAS.API. Never touches unrelated processes, and any
// failure here (netstat missing, permission denied, etc.) is swallowed silently — this is
// a convenience, not something worth failing startup over.
static void FreeStalePort(int port)
{
    try
    {
        using var netstat = Process.Start(new ProcessStartInfo
        {
            FileName = "netstat",
            Arguments = "-ano",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });
        if (netstat is null) return;

        var output = netstat.StandardOutput.ReadToEnd();
        netstat.WaitForExit(2000);

        var currentPid = Environment.ProcessId;
        foreach (var line in output.Split('\n'))
        {
            if (!line.Contains($":{port} ") || !line.Contains("LISTENING")) continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !int.TryParse(parts[^1], out var pid) || pid == currentPid) continue;

            try
            {
                using var proc = Process.GetProcessById(pid);
                if (!proc.ProcessName.Equals("IRAS.API", StringComparison.OrdinalIgnoreCase)) continue;

                Console.WriteLine($"[dev] Port {port} was held by a stale IRAS.API.exe (PID {pid}) — stopping it.");
                proc.Kill();
                proc.WaitForExit(2000);
            }
            catch
            {
                // Already exited, access denied, etc. — Kestrel's own bind error is the fallback if this didn't work.
            }
        }
    }
    catch
    {
        // netstat unavailable or something else went wrong — not worth failing startup over.
    }
}

// Only requires the operation to carry the Bearer padlock in Swagger when the endpoint
// is actually protected by [Authorize] (and not opted back out via [AllowAnonymous]).
public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize =
            context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true ||
            context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

        var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();

        if (!hasAuthorize || hasAllowAnonymous)
            return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document, null)] = []
            }
        ];
    }
}

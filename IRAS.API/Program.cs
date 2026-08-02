// IRAS.API/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
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
using IRAS.Application.Modules.SkillGaps;
using IRAS.Application.Modules.SkillTaxonomy;
using IRAS.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

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
});

builder.Services.AddDbContext<IrasDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Audit logging (Module 11) — registered early since several admin-only services below
// depend on it. Needs HttpContext to capture the caller's IP address.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<ISkillTaxonomyService, SkillTaxonomyService>();
builder.Services.AddScoped<IJobService, JobService>();

// JD generation — real Google Gemini API call (Module 5), chosen for its genuinely free
// tier (no billing card required). TemplateJdGenerator, ClaudeJdGenerator, and
// GptJdGenerator remain in the codebase as alternative IJdGenerator implementations
// (Template is the deterministic baseline for the thesis's evaluation chapter; Claude and
// GPT are working alternative providers), but GeminiJdGenerator is what actually serves
// requests. Same IJdGenerator contract across all four — swapping which one is active is
// a one-line change here, nothing else in the app depends on which provider is behind it.
// No official Google-maintained C# SDK exists for this endpoint, so GeminiJdGenerator
// calls the documented REST API directly — same typed-HttpClient pattern as the Python
// AI service below.
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

// Skill gap explanation — real Google Gemini API call, same swappable pattern as
// IJdGenerator. TemplateSkillGapExplainer remains as the deterministic baseline (thesis
// evaluation chapter); GeminiSkillGapExplainer is what actually serves requests. Reuses the
// GeminiOptions binding configured above. Registered before IApplicationService, which
// depends on it directly (ApplyAsync calls it while building an application's skill gaps).
builder.Services.AddHttpClient<ISkillGapExplainer, GeminiSkillGapExplainer>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// Feedback (Module 9) — real Google Gemini API call, same swappable pattern as
// IJdGenerator. TemplateFeedbackGenerator remains as the deterministic baseline (thesis
// evaluation chapter); GeminiFeedbackGenerator is what actually serves requests. Registered
// before IApplicationService since ApplicationService depends on IFeedbackService.
builder.Services.AddHttpClient<IFeedbackGenerator, GeminiFeedbackGenerator>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

// Interview Scheduling — depends on IApplicationService (advances an application to
// ApplicationStatus.Interview on first booking) and INotificationService (in-app +
// email notification to the candidate on schedule/reschedule/cancel).
builder.Services.AddScoped<IInterviewService, InterviewService>();

// Chatbot (Module 10) — real Google Gemini API call. RuleBasedChatResponder remains in
// the codebase as the deterministic, zero-cost baseline (same swappable pattern as
// IJdGenerator), but GeminiChatResponder is what actually serves requests. Both share
// ChatScopeGate for off-topic refusal, so that guarantee holds regardless of which one is
// active. ChatService reuses ISkillGapService/IApplicationService/IJobMatchingService/
// INotificationService rather than re-querying the database. Reuses the same GeminiOptions
// binding as GeminiJdGenerator (configured above).
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddHttpClient<IChatResponder, GeminiChatResponder>((sp, client) =>
{
    var opts = builder.Configuration.GetSection(GeminiOptions.SectionName).Get<GeminiOptions>()
        ?? new GeminiOptions();
    client.BaseAddress = new Uri(opts.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<IChatService, ChatService>();

// Admin (Module 11) — user management, cross-employer job moderation, and reporting.
// IAuditLogService itself is registered above; KnowledgeBaseService and
// SkillTaxonomyService also write to it directly for their own admin-only mutations.
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
        policy.WithOrigins("http://localhost:5173")   // your Vite dev server
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

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

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

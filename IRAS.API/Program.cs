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
using IRAS.Application.Common.Email;
using IRAS.Application.Common.Notifications;
using IRAS.Application.Common.Options;
using IRAS.Application.Common.Scoring;
using IRAS.Application.Data;
using IRAS.Application.Modules.Applications;
using IRAS.Application.Modules.Auth;
using IRAS.Application.Modules.Candidates;
using IRAS.Application.Modules.Jobs;
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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICandidateProfileService, CandidateProfileService>();
builder.Services.AddScoped<ISkillTaxonomyService, SkillTaxonomyService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IJdGenerator, TemplateJdGenerator>();

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

// Storage + resume module
builder.Services.AddSingleton<IRAS.Application.Common.Storage.IFileStorage,
                              IRAS.Application.Common.Storage.LocalDiskFileStorage>();
builder.Services.AddScoped<IResumeService, ResumeService>();

// Scoring — shared by Module 6 (application ranking) and Module 8 (proactive matching)
builder.Services.AddSingleton<IValidateOptions<ScoringOptions>, ScoringOptionsValidator>();
builder.Services.AddOptions<ScoringOptions>()
    .Bind(builder.Configuration.GetSection(ScoringOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddScoped<IScoringService, ScoringService>();

builder.Services.AddScoped<IApplicationService, ApplicationService>();

// Notifications — LogEmailSender is the dev-safe default (no SMTP credentials needed);
// swap in a real SmtpEmailSender/SendGridEmailSender behind the same IEmailSender later.
builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IJobMatchingService, JobMatchingService>();

builder.Services.AddScoped<ISkillGapService, SkillGapService>();

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
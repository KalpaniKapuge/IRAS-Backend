using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IRAS.Application.Common.Ai;
using IRAS.Application.Modules.Feedback;
using IRAS.Application.Modules.Jobs;
using IRAS.Application.Modules.SkillGaps;
using IRAS.Application.Modules.SkillImprovementPlans;
using IRAS.Infrastructure.Data;
using IRAS.Tests.Support;

namespace IRAS.Tests.Integration;

public class IrasApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "0123456789abcdef0123456789abcdef0123456789abcdef");
        builder.UseSetting("Jwt:Issuer", "IRAS.Tests");
        builder.UseSetting("Jwt:Audience", "IRAS.Tests");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
        builder.UseSetting("AiService:BaseUrl", "http://localhost:9999");
        builder.UseSetting("Gemini:ApiKey", "");
        builder.UseSetting("Seed:AdminEmail", "admin@iras.local");
        builder.UseSetting("Seed:AdminPassword", "ChangeMe@123");
        builder.UseSetting("FileStorage:Provider", "Local");
        builder.UseSetting("FileStorage:ResumeRootPath", Path.Combine(Path.GetTempPath(), "iras-tests", _databaseName));

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<IrasDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAiServiceClient>();
            services.AddSingleton<IAiServiceClient, FakeAiServiceClient>();

            services.RemoveAll<IJdGenerator>();
            services.AddSingleton<IJdGenerator, TemplateJdGenerator>();

            services.RemoveAll<IFeedbackGenerator>();
            services.AddSingleton<IFeedbackGenerator, TemplateFeedbackGenerator>();

            services.RemoveAll<ISkillGapExplainer>();
            services.AddSingleton<ISkillGapExplainer, FakeSkillGapExplainer>();

            services.RemoveAll<ISkillPlanGenerator>();
            services.AddSingleton<ISkillPlanGenerator, TemplateSkillPlanGenerator>();
        });
    }
}

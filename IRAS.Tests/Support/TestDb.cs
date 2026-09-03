using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using IRAS.Infrastructure.Data;

namespace IRAS.Tests.Support;

internal static class TestDb
{
    public static IrasDbContext Create()
    {
        var options = new DbContextOptionsBuilder<IrasDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new IrasDbContext(options);
    }
}

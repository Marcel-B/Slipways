using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public class SqliteTests
{
    [Fact]
    public async Task Lower_folds_umlauts_on_connections_opened_by_ef()
    {
        var path = Path.Combine(Path.GetTempPath(), $"slipways-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<SlipwaysDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False")
                .AddInterceptors(new SqliteUnicodeInterceptor())
                .Options;
            await using var db = new SlipwaysDbContext(options);

            var lowered = await db.Database
                .SqlQuery<string>($"SELECT lower('ÜBERLINGEN AM BODENSEE') AS \"Value\"")
                .SingleAsync(TestContext.Current.CancellationToken);

            Assert.Equal("überlingen am bodensee", lowered);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Timestamps_are_read_back_as_utc()
    {
        await using var factory = new SlipwaysApiFactory();
        await factory.InitializeAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SlipwaysDbContext>();

        var created = await db.Slipways.Select(s => s.Created).FirstAsync(TestContext.Current.CancellationToken);

        Assert.Equal(DateTimeKind.Utc, created.Kind);
    }
}

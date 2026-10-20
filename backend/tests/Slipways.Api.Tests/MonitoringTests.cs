using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Persistence;

namespace Slipways.Api.Tests;

public sealed class MetricsEndpointTests : IAsyncLifetime
{
    private readonly SlipwaysApiFactory _factory = new();

    public ValueTask InitializeAsync() => _factory.InitializeAsync();

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task The_public_port_does_not_answer_metrics()
    {
        var response = await _factory.CreateClient().GetAsync("/metrics", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_metrics_port_answers_metrics_and_nothing_else()
    {
        // The test server reports local port 0, so that is the metrics port here.
        var client = _factory.WithWebHostBuilder(b => b.UseSetting("Monitoring:MetricsPort", "0")).CreateClient();

        var metrics = await client.GetStringAsync("/metrics", TestContext.Current.CancellationToken);
        var api = await client.GetAsync("/api/slipways", TestContext.Current.CancellationToken);

        Assert.Contains("slipways_job_runs_total{task=\"osm\",result=\"failure\"} 0", metrics);
        Assert.Contains("slipways_places", metrics);
        Assert.Contains("process_cpu_seconds_total", metrics);
        Assert.Contains("process_resident_memory_bytes", metrics);
        Assert.DoesNotMatch("(?m)^slipways_osm_missing_elements ", metrics);
        Assert.Equal(HttpStatusCode.NotFound, api.StatusCode);
    }
}

public sealed class CatalogMetricsTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private SlipwaysDbContext _db = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new SlipwaysDbContext(new DbContextOptionsBuilder<SlipwaysDbContext>().UseSqlite(_connection).Options);
        await _db.Database.MigrateAsync();
        await SeedData.SeedAsync(_db);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Counts_places_by_kind_and_source()
    {
        await CatalogMetricsWorker.UpdateAsync(_db, TestContext.Current.CancellationToken);

        Assert.Equal(await _db.Slipways.CountAsync(TestContext.Current.CancellationToken), SlipwaysMetrics.Places.WithLabels("slipway", "manual").Value);
        Assert.Equal(0, SlipwaysMetrics.Places.WithLabels("slipway", "osm").Value);
        Assert.Equal(await _db.Marinas.CountAsync(TestContext.Current.CancellationToken), SlipwaysMetrics.Places.WithLabels("marina", "manual").Value);
        Assert.Equal(await _db.Stations.CountAsync(TestContext.Current.CancellationToken), SlipwaysMetrics.Places.WithLabels("station", "pegelonline").Value);
    }
}

public class JobMetricsTests
{
    private static readonly TimeProvider Time = TimeProvider.System;

    [Fact]
    public async Task Records_successful_runs()
    {
        var before = SlipwaysMetrics.JobRuns.WithLabels("test-success", "success").Value;

        var result = await SlipwaysMetrics.MeasureJobAsync("test-success", Time, () => Task.FromResult(42), TestContext.Current.CancellationToken);

        Assert.Equal(42, result);
        Assert.Equal(before + 1, SlipwaysMetrics.JobRuns.WithLabels("test-success", "success").Value);
        Assert.True(SlipwaysMetrics.JobLastSuccess.WithLabels("test-success").Value > 0);
    }

    [Fact]
    public async Task Records_failed_runs_and_passes_the_error_on()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => SlipwaysMetrics.MeasureJobAsync<int>(
            "test-failure", Time, () => throw new HttpRequestException("down"), TestContext.Current.CancellationToken));

        Assert.Equal(1, SlipwaysMetrics.JobRuns.WithLabels("test-failure", "failure").Value);
        Assert.Equal(0, SlipwaysMetrics.JobRuns.WithLabels("test-failure", "success").Value);
    }

    [Fact]
    public async Task Does_not_count_runs_cut_short_by_shutdown()
    {
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => SlipwaysMetrics.MeasureJobAsync(
            "test-shutdown", Time, () => Task.FromCanceled<int>(stopping.Token), stopping.Token));

        Assert.Equal(0, SlipwaysMetrics.JobRuns.WithLabels("test-shutdown", "failure").Value);
    }
}

public class LokiShipperTests
{
    private readonly StubHandler _loki = new();

    [Fact]
    public async Task Pushes_lines_grouped_into_streams_with_fixed_labels()
    {
        var (queue, shipper, provider) = Create();
        provider.CreateLogger("Slipways.Infrastructure.Osm.OsmImportWorker").LogInformation("Import finished: {Added} added", 3);
        provider.CreateLogger("Slipways.Infrastructure.Osm.OverpassClient").LogInformation("second line");
        provider.CreateLogger("Microsoft.AspNetCore.Server.Kestrel").LogWarning(new InvalidOperationException("boom"), "Request failed");

        await shipper.PushAsync(TestContext.Current.CancellationToken);

        var request = Assert.Single(_loki.Bodies);
        Assert.Equal("/loki/api/v1/push", _loki.Paths.Single());
        var streams = JsonDocument.Parse(request).RootElement.GetProperty("streams").EnumerateArray().ToList();
        Assert.Equal(2, streams.Count);

        var osm = streams.Single(s => s.GetProperty("stream").GetProperty("source").GetString() == "osm");
        Assert.Equal(
            new Dictionary<string, string> { ["app"] = "slipways", ["host"] = "test-host", ["level"] = "info", ["source"] = "osm" },
            osm.GetProperty("stream").Deserialize<Dictionary<string, string>>());
        var values = osm.GetProperty("values").EnumerateArray().ToList();
        Assert.Equal("1792490400000000000", values[0][0].GetString());
        Assert.Equal("Slipways.Infrastructure.Osm.OsmImportWorker: Import finished: 3 added", values[0][1].GetString());
        Assert.Equal(2, values.Count);

        var http = streams.Single(s => s.GetProperty("stream").GetProperty("source").GetString() == "http");
        Assert.Equal("warning", http.GetProperty("stream").GetProperty("level").GetString());
        Assert.Contains("System.InvalidOperationException: boom", http.GetProperty("values")[0][1].GetString());
        Assert.False(queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Drops_lines_when_loki_is_down_without_throwing()
    {
        _loki.Status = HttpStatusCode.ServiceUnavailable;
        var (queue, shipper, provider) = Create();
        provider.CreateLogger("Slipways.Api").LogError("first");
        var dropped = SlipwaysMetrics.LokiLinesDropped.WithLabels("push_failed").Value;

        await shipper.PushAsync(TestContext.Current.CancellationToken);

        Assert.Equal(dropped + 1, SlipwaysMetrics.LokiLinesDropped.WithLabels("push_failed").Value);
        Assert.False(queue.Reader.TryRead(out _));
    }

    [Fact]
    public void Never_ships_its_own_lines_and_drops_lines_beyond_the_queue_limit()
    {
        var (queue, _, provider) = Create(queueLimit: 2);

        provider.CreateLogger(typeof(LokiShipper).FullName!).LogWarning("Loki is unreachable");
        var logger = provider.CreateLogger("Slipways.Api");
        logger.LogInformation("one");
        logger.LogInformation("two");
        logger.LogInformation("three");

        Assert.Equal(["Slipways.Api: one", "Slipways.Api: two"], Drain(queue).Select(e => e.Line));
    }

    private static List<LokiEntry> Drain(LokiLogQueue queue)
    {
        var entries = new List<LokiEntry>();
        while (queue.Reader.TryRead(out var entry))
            entries.Add(entry);
        return entries;
    }

    private (LokiLogQueue, LokiShipper, LokiLoggerProvider) Create(int queueLimit = 100)
    {
        var options = Options.Create(new MonitoringOptions { Loki = new LokiOptions { Url = "https://loki.test", Host = "test-host", QueueLimit = queueLimit } });
        var time = new FixedTime(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));
        var queue = new LokiLogQueue(options);
        var shipper = new LokiShipper(queue, new StubClientFactory(_loki), options, time, NullLogger<LokiShipper>.Instance);
        return (queue, shipper, new LokiLoggerProvider(queue, time));
    }

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://loki.test") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.NoContent;
        public List<string> Bodies { get; } = [];
        public List<string> Paths { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(Status) { Content = new StringContent("", Encoding.UTF8) };
        }
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

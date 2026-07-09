using System.Net;
using System.Net.Http.Json;
using Adle.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Adle.Api.Test;

/// <summary>
/// End-to-end HTTP tests over the real API. Requires the local PostgreSQL infra
/// (docker-compose) to be up; ml_service is stubbed by <see cref="AdleApiFactory"/>.
/// </summary>
[TestClass]
public class ApiEndpointTests
{
    private static AdleApiFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void Init(TestContext _)
    {
        _factory = new AdleApiFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    private static string UniqueName() => $"__it_{Guid.NewGuid():N}__";

    [TestMethod]
    public async Task Predict_ReturnsThesisResult()
    {
        var req = new PredictRequest(
            new[]
            {
                new TrainingSequenceDto(new[] { "1P", "1I", "2P", "2I" }, "Baba"),
                new TrainingSequenceDto(new[] { "1P", "1I", "2P", "3P" }, "Baba"),
                new TrainingSequenceDto(new[] { "1P", "1I", "2P", "2I" }, "Anne"),
            },
            new[] { "1P", "1I" },
            0);

        var res = await _client.PostAsJsonAsync("/api/analysis/predict", req);

        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<PredictResponse>();
        Assert.IsNotNull(body);
        Assert.AreEqual("2P", body!.PredictedNextNode);
        Assert.AreEqual("Baba", body.PredictedPerson);
    }

    [TestMethod]
    public async Task GetAreas_ReturnsSeededRows()
    {
        var areas = await _client.GetFromJsonAsync<List<AreaDto>>("/api/areas");
        Assert.IsNotNull(areas);
        Assert.IsTrue(areas!.Count >= 6, $"expected >= 6 seeded areas, got {areas.Count}");
    }

    [TestMethod]
    public async Task Area_CreateUpdateDelete_RoundTrip()
    {
        var name = UniqueName();

        var create = await _client.PostAsJsonAsync("/api/areas", new AreaWriteDto(name, 3, 4, 1, 1));
        Assert.AreEqual(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<AreaDto>();
        Assert.IsNotNull(created);
        Assert.AreEqual("LivingRoom", created!.AreaTypeName, "create response should carry the joined type name");

        var put = await _client.PutAsJsonAsync($"/api/areas/{created.Id}", new AreaWriteDto(name, 9, 9, 1, 2));
        Assert.AreEqual(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<AreaDto>();
        Assert.AreEqual(9, updated!.Width);
        Assert.AreEqual("Bedroom", updated.AreaTypeName);

        Assert.AreEqual(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/areas/{created.Id}")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/areas/{created.Id}")).StatusCode);
    }

    [TestMethod]
    public async Task Area_BadForeignKey_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/areas", new AreaWriteDto(UniqueName(), 1, 1, 999999, null));
        Assert.AreEqual(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [TestMethod]
    public async Task Graph_Create_Run_Delete()
    {
        var matrix = string.Join("\r\n", new[]
        {
            "\tA\tB\tC\tD", "A\t0\t1\t0\t0", "B\t0\t0\t1\t0", "C\t0\t0\t0\t1", "D\t0\t0\t0\t0",
        });

        var create = await _client.PostAsJsonAsync("/api/graphs", new GraphWriteDto(UniqueName(), matrix));
        Assert.AreEqual(HttpStatusCode.Created, create.StatusCode);
        var g = await create.Content.ReadFromJsonAsync<GraphDto>();
        Assert.IsNotNull(g);

        try
        {
            var run = await _client.PostAsJsonAsync($"/api/graphs/{g!.Id}/run", new RunGraphRequest("A", "D", 3, false, false));
            Assert.AreEqual(HttpStatusCode.OK, run.StatusCode);
            var result = await run.Content.ReadFromJsonAsync<RunGraphResponse>();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result!.WalkCount);
            CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, result.Walks[0]);
        }
        finally
        {
            await _client.DeleteAsync($"/api/graphs/{g!.Id}");
        }
    }

    [TestMethod]
    public async Task Graph_Run_UnknownNode_Returns400()
    {
        var matrix = string.Join("\r\n", new[] { "\tA\tB", "A\t0\t1", "B\t0\t0" });
        var create = await _client.PostAsJsonAsync("/api/graphs", new GraphWriteDto(UniqueName(), matrix));
        var g = await create.Content.ReadFromJsonAsync<GraphDto>();
        try
        {
            var run = await _client.PostAsJsonAsync($"/api/graphs/{g!.Id}/run", new RunGraphRequest("Z", "B", 1, false, false));
            Assert.AreEqual(HttpStatusCode.BadRequest, run.StatusCode);
        }
        finally
        {
            await _client.DeleteAsync($"/api/graphs/{g!.Id}");
        }
    }

    [TestMethod]
    public async Task GetGraph_Missing_Returns404()
    {
        Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync("/api/graphs/999999")).StatusCode);
    }

    [TestMethod]
    public async Task Anomaly_Tsne_ProxiesStubbedMlService()
    {
        var req = new TsneRequestDto(new[] { new[] { 0.0, 0.0 }, new[] { 1.0, 1.0 } }, 2, 2);
        var res = await _client.PostAsJsonAsync("/api/anomaly/tsne", req);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<TsneResponseDto>();
        Assert.IsNotNull(body);
        Assert.AreEqual(2, body!.Output.Length);
        Assert.AreEqual(0.1, body.Output[0][0], 1e-9);
    }

    [TestMethod]
    public async Task Anomaly_RandomForest_ProxiesStubbedMlService()
    {
        var req = new RandomForestRequestDto(
            new[] { new[] { 0.0, 0.0 } },
            new[] { new[] { 1.0, 1.0 } },
            new[] { new[] { 0.0, 0.0 }, new[] { 1.0, 1.0 } },
            10);
        var res = await _client.PostAsJsonAsync("/api/anomaly/random-forest", req);
        Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<RandomForestResponseDto>();
        Assert.IsNotNull(body);
        Assert.AreEqual(2, body!.ProximityMatrix.Length);
        Assert.AreEqual(1, body.TreeTexts.Length);
    }
}

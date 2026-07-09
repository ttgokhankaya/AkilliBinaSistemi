using System.Net;
using System.Text;
using Adle.Api.MlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Adle.Api.Test;

/// <summary>
/// Hosts the real API for integration tests. The database is the real local
/// PostgreSQL (the running dev infra); only the ml_service HTTP calls are
/// stubbed, so /api/anomaly/* tests are deterministic and don't need Python.
/// </summary>
public class AdleApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Re-point the typed ml client at a canned in-memory handler.
            services.AddHttpClient<MlServiceClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new StubMlHandler());
        });
    }
}

/// <summary>Returns canned snake_case ml_service responses without any network.</summary>
internal sealed class StubMlHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var json = path.Contains("random-forest")
            ? "{\"proximity_matrix\":[[1.0,0.0],[0.0,1.0]],\"tree_texts\":[\"|--- feature_0\"]}"
            : "{\"output\":[[0.1,0.2],[0.3,0.4]]}";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }
}

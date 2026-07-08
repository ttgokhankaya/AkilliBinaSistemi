using System.Net.Http.Json;
using System.Text.Json;
using Adle.Contracts;

namespace Adle.Api.MlClient;

/// <summary>
/// Typed client for the Python ml_service. Reuses the Adle.Contracts DTOs but
/// serializes them as snake_case (n_components, all_observations, …) to match
/// the FastAPI Pydantic models. Registered as a typed HttpClient in Program.cs.
/// </summary>
public class MlServiceClient
{
    private static readonly JsonSerializerOptions SnakeCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;

    public MlServiceClient(HttpClient http) => _http = http;

    public async Task<TsneResponseDto> ComputeTsneAsync(TsneRequestDto request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("tsne", request, SnakeCase, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TsneResponseDto>(SnakeCase, ct))!;
    }

    public async Task<RandomForestResponseDto> ComputeRandomForestAsync(RandomForestRequestDto request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("random-forest", request, SnakeCase, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RandomForestResponseDto>(SnakeCase, ct))!;
    }
}

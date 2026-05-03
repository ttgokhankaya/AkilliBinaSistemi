using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GUI_Simulation
{
    public static class MlServiceClient
    {
        private static readonly HttpClient _http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8000/"),
            Timeout = TimeSpan.FromSeconds(120)
        };

        public static async Task<double[][]> ComputeTsneAsync(double[][] observations, double perplexity, int nComponents = 2)
        {
            var response = await _http.PostAsJsonAsync("tsne", new
            {
                observations,
                perplexity,
                n_components = nComponents
            });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TsneResponse>();
            return result.output;
        }

        public static async Task<RandomForestResponse> ComputeRandomForestAsync(
            double[][] s0, double[][] s1, double[][] allObservations, int nTrees = 100)
        {
            var response = await _http.PostAsJsonAsync("random-forest", new
            {
                s0,
                s1,
                all_observations = allObservations,
                n_trees = nTrees
            });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RandomForestResponse>();
        }

        private record TsneResponse(double[][] output);
    }

    public record RandomForestResponse(double[][] proximity_matrix, string[] tree_texts);
}

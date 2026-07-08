using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adle.Benchmark.Predictors
{
    /// <summary>
    /// Runs a Python-side sequence model (HMM, later LSTM) over the whole
    /// train/test split in one call to the ML service, returning per-prefix
    /// ranked next tokens + predicted label so the C# harness scores it
    /// identically to the in-process predictors.
    /// </summary>
    public static class RemoteBenchmark
    {
        public static string ServiceUrl =>
            Environment.GetEnvironmentVariable("ADLE_ML_URL") ?? "http://localhost:8000";

        public sealed class StepPrediction
        {
            [JsonPropertyName("ranked")] public List<string> Ranked { get; set; } = new();
            [JsonPropertyName("label")] public string Label { get; set; } = "";
        }

        public sealed class TestResult
        {
            [JsonPropertyName("steps")] public List<StepPrediction> Steps { get; set; } = new();
        }

        private sealed class Response
        {
            [JsonPropertyName("test")] public List<TestResult> Test { get; set; } = new();
        }

        /// <summary>Returns per-test-sequence step predictions, aligned with <paramref name="test"/> order.</summary>
        public static List<TestResult> Run(
            string model,
            List<(string label, List<string> tokens)> train,
            List<(string label, List<string> tokens)> test,
            int timeoutSeconds = 300)
        {
            var payload = new
            {
                model,
                train = SequencesToDto(train),
                test = SequencesToDto(test),
            };
            string json = JsonSerializer.Serialize(payload);

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = client.PostAsync($"{ServiceUrl}/sequence-benchmark", content).GetAwaiter().GetResult();
            resp.EnsureSuccessStatusCode();
            string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonSerializer.Deserialize<Response>(body).Test;
        }

        public static bool IsServiceUp()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var r = client.GetAsync($"{ServiceUrl}/health").GetAwaiter().GetResult();
                return r.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private static List<object> SequencesToDto(List<(string label, List<string> tokens)> seqs)
        {
            var list = new List<object>(seqs.Count);
            foreach (var (label, tokens) in seqs)
                list.Add(new { label, tokens });
            return list;
        }
    }
}

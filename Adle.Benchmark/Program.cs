using System;
using System.Collections.Generic;
using System.Linq;
using Adle.Analysis;
using Adle.Analysis.Prediction;
using AdleGraph;
using AdleGraph.Interfaces;

namespace Adle.Benchmark
{
    // Next-step prediction benchmark for the thesis LCS+softmax predictor.
    //   dotnet run --project Adle.Benchmark                 -> synthetic (AdleGraph)
    //   dotnet run --project Adle.Benchmark -- casas [path] -> real CASAS data
    // Default CASAS file: C:\Gokhan\CASAS\labeled\hh101.csv (override via arg or
    // the ADLE_CASAS_FILE env var). This is the harness the Markov/HMM/LSTM
    // baseline comparison plugs into.
    internal static class Program
    {
        private const double TrainRatio = 0.7;

        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("casas", StringComparison.OrdinalIgnoreCase))
            {
                string path = args.Length > 1
                    ? args[1]
                    : Environment.GetEnvironmentVariable("ADLE_CASAS_FILE")
                      ?? @"C:\Gokhan\CASAS\labeled\hh101.csv";
                RunCasas(path);
            }
            else
            {
                RunSynthetic();
            }
        }

        // ---- shared evaluation -------------------------------------------------

        // Each labelled sequence: (label, token list). Trains on a prefix of the
        // set, then for every test sequence predicts each next token from its
        // growing prefix and scores next-step + label-identification accuracy.
        private static void Evaluate(
            List<(string label, List<string> tokens)> data,
            string labelKind)
        {
            var train = new List<SequenceBarDTO>();
            var test = new List<(string label, List<string> tokens)>();
            int order = 0;

            // Stratified split per label so both sides see every label.
            foreach (var grp in data.GroupBy(d => d.label))
            {
                var items = grp.ToList();
                int split = Math.Max(1, (int)(items.Count * TrainRatio));
                for (int i = 0; i < items.Count; i++)
                {
                    if (i < split)
                        train.Add(SequenceFactory.ToTrainingBar(items[i].tokens, items[i].label, order++));
                    else
                        test.Add(items[i]);
                }
            }

            Console.WriteLine($"labels: {data.Select(d => d.label).Distinct().Count()}   " +
                              $"train: {train.Count}   test: {test.Count}\n");

            var service = new PredictionService();
            var scorer = new PredictionScorer();
            int labelPredicted = 0, labelCorrect = 0;

            foreach (var (label, tokens) in test)
            {
                for (int k = 1; k < tokens.Count; k++)
                {
                    var prefix = SequenceFactory.FromNodeNames(tokens.Take(k));
                    var result = service.PredictNextStep(prefix, train);

                    scorer.Record(result, tokens[k]);

                    if (!string.IsNullOrEmpty(result.PredictedPerson))
                    {
                        labelPredicted++;
                        if (result.PredictedPerson == label) labelCorrect++;
                    }
                }
            }

            Console.WriteLine("Next-step prediction");
            Console.WriteLine($"  predictions (N)     : {scorer.Total}");
            Console.WriteLine($"  TP / FP / FN        : {scorer.TruePositive} / {scorer.FalsePositive} / {scorer.FalseNegative}");
            Console.WriteLine($"  accuracy (thesis)   : {scorer.Accuracy:P1}   [(TP+FP)/N]");
            Console.WriteLine($"  top-1 accuracy      : {scorer.Top1Accuracy:P1}");
            Console.WriteLine($"  precision           : {scorer.Precision:P1}");
            Console.WriteLine($"  recall              : {scorer.Recall:P1}");
            Console.WriteLine($"  F1                  : {scorer.F1:P1}");

            double labelAcc = labelPredicted == 0 ? 0 : (double)labelCorrect / labelPredicted;
            Console.WriteLine($"\n{labelKind} identification");
            Console.WriteLine($"  predicted / correct : {labelPredicted} / {labelCorrect}");
            Console.WriteLine($"  accuracy            : {labelAcc:P1}");
        }

        // ---- CASAS (real data) -------------------------------------------------

        private static void RunCasas(string path)
        {
            Console.WriteLine("ADLE next-step prediction benchmark (CASAS real data)");
            Console.WriteLine("=====================================================\n");
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine($"CASAS file not found: {path}");
                Console.WriteLine(@"Pass a path or set ADLE_CASAS_FILE (see C:\Gokhan\CASAS\README.md).");
                return;
            }

            Console.WriteLine($"file: {path}");
            var sessions = CasasReader.ReadSessions(path, minLength: 3, maxLength: 40, maxSessions: 600);
            Console.WriteLine($"labeled activity sessions: {sessions.Count}");

            var byActivity = sessions.GroupBy(s => s.Activity)
                                     .OrderByDescending(g => g.Count())
                                     .Take(12);
            Console.WriteLine("top activities: " +
                string.Join(", ", byActivity.Select(g => $"{g.Key}({g.Count()})")) + "\n");

            var data = sessions.Select(s => (s.Activity, s.Tokens)).ToList();
            Evaluate(data, "Activity");
        }

        // ---- synthetic (AdleGraph) --------------------------------------------

        private static void RunSynthetic()
        {
            Console.WriteLine("ADLE next-step prediction benchmark (synthetic)");
            Console.WriteLine("================================================\n");

            var people = new[]
            {
                ("Baba", BabaGraph()),
                ("Anne", AnneGraph()),
            };

            var data = new List<(string, List<string>)>();
            foreach (var (name, graph) in people)
            {
                var walks = Generate(graph, 150);
                foreach (var w in walks) data.Add((name, w));
                Console.WriteLine($"{name,-6}: {walks.Count} usable walks");
            }
            Console.WriteLine();
            Evaluate(data, "Person");
        }

        private const string Start = "G";
        private const string End = "C";

        private static List<List<string>> Generate(IGraph graph, int count)
        {
            var start = graph.GetNodeWithName(Start);
            var end = graph.GetNodeWithName(End);
            var walks = graph.Run(start, end, useEgdeWeights: true, maxIteration: count, allowLoops: false);
            return walks
                .Where(w => w.Count >= 3 && w[w.Count - 1].Name == End)
                .Select(w => w.Select(n => n.Name).ToList())
                .ToList();
        }

        private static IGraph Build(params (string from, string to, double w)[] edges)
        {
            var g = new Graph();
            foreach (var e in edges)
            {
                g.GetNodeWithName(e.from, addNodeIfNotExist: true);
                g.GetNodeWithName(e.to, addNodeIfNotExist: true);
            }
            foreach (var e in edges)
                g.AddEdge(e.from, e.to, e.w, isDirected: true);
            return g;
        }

        private static IGraph BabaGraph() => Build(
            ("G", "1P", 0.9), ("G", "2P", 0.1),
            ("1P", "1I", 1.0),
            ("1I", "2P", 0.7), ("1I", "C", 0.3),
            ("2P", "2I", 1.0),
            ("2I", "C", 0.8), ("2I", "5P", 0.2),
            ("5P", "5I", 1.0),
            ("5I", "C", 1.0));

        private static IGraph AnneGraph() => Build(
            ("G", "2P", 0.9), ("G", "1P", 0.1),
            ("2P", "2I", 1.0),
            ("2I", "1P", 0.7), ("2I", "C", 0.3),
            ("1P", "1I", 1.0),
            ("1I", "C", 0.8), ("1I", "5P", 0.2),
            ("5P", "5I", 1.0),
            ("5I", "C", 1.0));
    }
}

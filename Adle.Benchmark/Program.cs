using System;
using System.Collections.Generic;
using System.Linq;
using Adle.Analysis.Prediction;
using Adle.Benchmark.Predictors;
using AdleGraph;
using AdleGraph.Interfaces;

namespace Adle.Benchmark
{
    // Next-step prediction + label-identification benchmark comparing the thesis
    // LCS+softmax predictor against baselines on the same train/test split.
    //   dotnet run --project Adle.Benchmark                 -> synthetic (AdleGraph)
    //   dotnet run --project Adle.Benchmark -- casas [path] -> real CASAS data
    // Default CASAS file: C:\Gokhan\CASAS\labeled\hh101.csv (or ADLE_CASAS_FILE).
    internal static class Program
    {
        private const double TrainRatio = 0.7;

        private static void Main(string[] args)
        {
            bool casas = args.Length > 0 && args[0].Equals("casas", StringComparison.OrdinalIgnoreCase);

            List<(string label, List<string> tokens)> data;
            string labelKind;

            if (casas)
            {
                string path = args.Length > 1
                    ? args[1]
                    : Environment.GetEnvironmentVariable("ADLE_CASAS_FILE")
                      ?? @"C:\Gokhan\CASAS\labeled\hh101.csv";
                Console.WriteLine("ADLE benchmark — CASAS real data");
                Console.WriteLine("================================\n");
                if (!System.IO.File.Exists(path))
                {
                    Console.WriteLine($"CASAS file not found: {path}");
                    Console.WriteLine(@"Pass a path or set ADLE_CASAS_FILE (see C:\Gokhan\CASAS\README.md).");
                    return;
                }
                Console.WriteLine($"file: {path}");
                var sessions = CasasReader.ReadSessions(path, minLength: 3, maxLength: 40, maxSessions: 600);
                Console.WriteLine($"labeled activity sessions: {sessions.Count}");
                Console.WriteLine("top activities: " + string.Join(", ",
                    sessions.GroupBy(s => s.Activity).OrderByDescending(g => g.Count()).Take(10)
                            .Select(g => $"{g.Key}({g.Count()})")));
                data = sessions.Select(s => (s.Activity, s.Tokens)).ToList();
                labelKind = "Activity";
            }
            else
            {
                Console.WriteLine("ADLE benchmark — synthetic (AdleGraph)");
                Console.WriteLine("======================================\n");
                data = BuildSynthetic();
                labelKind = "Person";
            }

            var (train, test) = Split(data);
            Console.WriteLine($"\nlabels: {data.Select(d => d.label).Distinct().Count()}   " +
                              $"train: {train.Count}   test: {test.Count}\n");

            var predictors = new ISequencePredictor[]
            {
                new LcsSoftmaxPredictor(),
                new MarkovPredictor(),
            };

            var rows = new List<Metrics>();
            foreach (var p in predictors)
            {
                p.Train(train);
                rows.Add(EvaluatePredictor(p, test, labelKind));
            }

            // Python-backed baselines (HMM now, LSTM once added) via the ML service.
            if (RemoteBenchmark.IsServiceUp())
            {
                foreach (var (model, display) in new[] { ("hmm", "HMM (hmmlearn)") })
                {
                    try { rows.Add(EvaluateRemote(model, display, train, test)); }
                    catch (Exception ex) { Console.WriteLine($"[{display}] skipped: {ex.Message}"); }
                }
            }
            else
            {
                Console.WriteLine("(ML service down — skipping HMM/LSTM; `docker-compose up -d ml_service`)\n");
            }

            PrintTable(rows, labelKind);
        }

        private struct Metrics
        {
            public string Model;
            public double Top1, ThesisAcc, Precision, Recall, F1, LabelAcc;
            public int N;
        }

        private static Metrics EvaluatePredictor(
            ISequencePredictor predictor,
            List<(string label, List<string> tokens)> test,
            string labelKind)
        {
            var scorer = new PredictionScorer();
            int labelPredicted = 0, labelCorrect = 0;

            foreach (var (label, tokens) in test)
            {
                for (int k = 1; k < tokens.Count; k++)
                {
                    var prefix = tokens.Take(k).ToList();
                    scorer.Record(predictor.RankNext(prefix), tokens[k]);

                    var predLabel = predictor.PredictLabel(prefix);
                    if (!string.IsNullOrEmpty(predLabel))
                    {
                        labelPredicted++;
                        if (predLabel == label) labelCorrect++;
                    }
                }
            }

            return new Metrics
            {
                Model = predictor.Name,
                N = scorer.Total,
                Top1 = scorer.Top1Accuracy,
                ThesisAcc = scorer.Accuracy,
                Precision = scorer.Precision,
                Recall = scorer.Recall,
                F1 = scorer.F1,
                LabelAcc = labelPredicted == 0 ? 0 : (double)labelCorrect / labelPredicted,
            };
        }

        private static Metrics EvaluateRemote(
            string model, string display,
            List<(string label, List<string> tokens)> train,
            List<(string label, List<string> tokens)> test,
            string labelKind = null)
        {
            var results = RemoteBenchmark.Run(model, train, test);
            var scorer = new PredictionScorer();
            int labelPredicted = 0, labelCorrect = 0;

            for (int i = 0; i < test.Count && i < results.Count; i++)
            {
                var tokens = test[i].tokens;
                var steps = results[i].Steps;
                for (int k = 1; k < tokens.Count && (k - 1) < steps.Count; k++)
                {
                    var step = steps[k - 1];
                    scorer.Record(step.Ranked, tokens[k]);
                    if (!string.IsNullOrEmpty(step.Label))
                    {
                        labelPredicted++;
                        if (step.Label == test[i].label) labelCorrect++;
                    }
                }
            }

            return new Metrics
            {
                Model = display,
                N = scorer.Total,
                Top1 = scorer.Top1Accuracy,
                ThesisAcc = scorer.Accuracy,
                Precision = scorer.Precision,
                Recall = scorer.Recall,
                F1 = scorer.F1,
                LabelAcc = labelPredicted == 0 ? 0 : (double)labelCorrect / labelPredicted,
            };
        }

        private static void PrintTable(List<Metrics> rows, string labelKind)
        {
            Console.WriteLine($"{"Model",-22} {"top-1",7} {"thesisA",7} {"prec",7} {"recall",7} {"F1",7} {labelKind + "Id",8}");
            Console.WriteLine(new string('-', 70));
            foreach (var r in rows)
                Console.WriteLine($"{r.Model,-22} {r.Top1,7:P1} {r.ThesisAcc,7:P1} {r.Precision,7:P1} {r.Recall,7:P1} {r.F1,7:P1} {r.LabelAcc,8:P1}");
            Console.WriteLine($"\n(N = {rows.FirstOrDefault().N} next-step predictions on the test split)");
        }

        private static (List<(string, List<string>)> train, List<(string, List<string>)> test)
            Split(List<(string label, List<string> tokens)> data)
        {
            var train = new List<(string, List<string>)>();
            var test = new List<(string, List<string>)>();
            foreach (var grp in data.GroupBy(d => d.label))
            {
                var items = grp.ToList();
                int split = Math.Max(1, (int)(items.Count * TrainRatio));
                for (int i = 0; i < items.Count; i++)
                    (i < split ? train : test).Add((items[i].label, items[i].tokens));
            }
            return (train, test);
        }

        // ---- synthetic data (AdleGraph weighted random walks) ------------------

        private static List<(string, List<string>)> BuildSynthetic()
        {
            var data = new List<(string, List<string>)>();
            foreach (var (name, graph) in new[] { ("Baba", BabaGraph()), ("Anne", AnneGraph()) })
            {
                var start = graph.GetNodeWithName("G");
                var end = graph.GetNodeWithName("C");
                var walks = graph.Run(start, end, useEgdeWeights: true, maxIteration: 150, allowLoops: false);
                foreach (var w in walks.Where(w => w.Count >= 3 && w[w.Count - 1].Name == "C"))
                    data.Add((name, w.Select(n => n.Name).ToList()));
            }
            return data;
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

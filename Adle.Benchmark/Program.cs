using System;
using System.Collections.Generic;
using System.Linq;
using Adle.Analysis;
using Adle.Analysis.Prediction;
using AdleGraph;
using AdleGraph.Interfaces;

namespace Adle.Benchmark
{
    // Reproduces the thesis experiment shape on synthetic data: per-person weighted
    // graphs generate habit sequences (AdleGraph random walks), the LCS+softmax
    // PredictionService predicts each next step, and PredictionScorer reports
    // accuracy / precision / recall / F1 plus person-identification accuracy.
    //
    // This is the harness the real-dataset (CASAS) + baseline (Markov/HMM/LSTM)
    // comparison will plug into.
    internal static class Program
    {
        private const string Start = "G";   // Giris (entrance)
        private const string End = "C";     // Cikis (exit)
        private const int WalksPerPerson = 150;
        private const double TrainRatio = 0.7;

        private static void Main()
        {
            Console.WriteLine("ADLE next-step prediction benchmark (synthetic)");
            Console.WriteLine("================================================\n");

            var people = new[]
            {
                new Person("Baba", BabaGraph()),
                new Person("Anne", AnneGraph()),
            };

            var train = new List<SequenceBarDTO>();
            var test = new List<(string person, List<string> seq)>();
            int order = 0;

            foreach (var p in people)
            {
                var walks = Generate(p.Graph, WalksPerPerson);
                int split = (int)(walks.Count * TrainRatio);
                for (int i = 0; i < walks.Count; i++)
                {
                    if (i < split)
                        train.Add(SequenceFactory.ToTrainingBar(walks[i], p.Name, order++));
                    else
                        test.Add((p.Name, walks[i]));
                }
                Console.WriteLine($"{p.Name,-6}: {walks.Count} usable walks (train {split}, test {walks.Count - split})");
            }

            Console.WriteLine($"\nTraining sequences: {train.Count}   Test sequences: {test.Count}\n");

            var service = new PredictionService();
            var scorer = new PredictionScorer();
            int personPredicted = 0, personCorrect = 0;

            foreach (var (person, seq) in test)
            {
                for (int k = 1; k < seq.Count; k++)
                {
                    var prefix = SequenceFactory.FromNodeNames(seq.Take(k));
                    var result = service.PredictNextStep(prefix, train);

                    scorer.Record(result, seq[k]);

                    if (!string.IsNullOrEmpty(result.PredictedPerson))
                    {
                        personPredicted++;
                        if (result.PredictedPerson == person) personCorrect++;
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

            double personAcc = personPredicted == 0 ? 0 : (double)personCorrect / personPredicted;
            Console.WriteLine("\nPerson identification");
            Console.WriteLine($"  predicted / correct : {personPredicted} / {personCorrect}");
            Console.WriteLine($"  accuracy            : {personAcc:P1}");
        }

        private sealed class Person
        {
            public string Name { get; }
            public IGraph Graph { get; }
            public Person(string name, IGraph graph) { Name = name; Graph = graph; }
        }

        // Generate walks from Start to End and keep the ones that actually reach End.
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

        // Baba: entrance -> living room -> kitchen -> exit, occasionally bedroom.
        private static IGraph BabaGraph() => Build(
            ("G", "1P", 0.9), ("G", "2P", 0.1),
            ("1P", "1I", 1.0),
            ("1I", "2P", 0.7), ("1I", "C", 0.3),
            ("2P", "2I", 1.0),
            ("2I", "C", 0.8), ("2I", "5P", 0.2),
            ("5P", "5I", 1.0),
            ("5I", "C", 1.0));

        // Anne: entrance -> kitchen -> living room -> exit, occasionally bedroom.
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

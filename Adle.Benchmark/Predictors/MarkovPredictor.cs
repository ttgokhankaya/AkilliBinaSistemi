using System;
using System.Collections.Generic;
using System.Linq;

namespace Adle.Benchmark.Predictors
{
    /// <summary>
    /// Baselines in one: a first-order Markov chain for next-token prediction
    /// (rank successors of the last token by transition frequency) and a
    /// multinomial Naive Bayes over the token bag for label identification.
    /// This is the natural lower bound — the synthetic generator is itself a
    /// first-order Markov process, so Markov should be near-optimal there.
    /// </summary>
    public sealed class MarkovPredictor : ISequencePredictor
    {
        public string Name => "1st-order Markov + NB";

        private readonly Dictionary<string, Dictionary<string, int>> _transitions = new();
        private readonly Dictionary<string, int> _labelCounts = new();
        private readonly Dictionary<string, Dictionary<string, int>> _tokenGivenLabel = new();
        private readonly HashSet<string> _vocab = new();
        private int _totalSequences;

        public void Train(List<(string label, List<string> tokens)> train)
        {
            foreach (var (label, tokens) in train)
            {
                _totalSequences++;
                _labelCounts.TryGetValue(label, out int lc);
                _labelCounts[label] = lc + 1;

                if (!_tokenGivenLabel.TryGetValue(label, out var tgl))
                    _tokenGivenLabel[label] = tgl = new Dictionary<string, int>();

                for (int i = 0; i < tokens.Count; i++)
                {
                    _vocab.Add(tokens[i]);
                    tgl.TryGetValue(tokens[i], out int tc);
                    tgl[tokens[i]] = tc + 1;

                    if (i + 1 < tokens.Count)
                    {
                        if (!_transitions.TryGetValue(tokens[i], out var succ))
                            _transitions[tokens[i]] = succ = new Dictionary<string, int>();
                        succ.TryGetValue(tokens[i + 1], out int sc);
                        succ[tokens[i + 1]] = sc + 1;
                    }
                }
            }
        }

        public List<string> RankNext(List<string> prefix)
        {
            if (prefix == null || prefix.Count == 0) return new List<string>();
            string last = prefix[prefix.Count - 1];
            if (!_transitions.TryGetValue(last, out var succ)) return new List<string>();
            return succ.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();
        }

        public string PredictLabel(List<string> prefix)
        {
            if (prefix == null || prefix.Count == 0 || _labelCounts.Count == 0) return null;
            int vocab = Math.Max(1, _vocab.Count);
            string best = null;
            double bestScore = double.NegativeInfinity;

            foreach (var label in _labelCounts.Keys)
            {
                var tgl = _tokenGivenLabel[label];
                int labelTokenTotal = tgl.Values.Sum();
                double score = Math.Log((double)_labelCounts[label] / _totalSequences);
                foreach (var token in prefix)
                {
                    tgl.TryGetValue(token, out int tc);
                    score += Math.Log((tc + 1.0) / (labelTokenTotal + vocab)); // Laplace smoothing
                }
                if (score > bestScore) { bestScore = score; best = label; }
            }
            return best;
        }
    }
}

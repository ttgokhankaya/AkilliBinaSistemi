using System.Collections.Generic;

namespace Adle.Benchmark.Predictors
{
    /// <summary>
    /// Common contract so the thesis LCS+softmax predictor and every baseline
    /// (Markov, HMM, LSTM) are trained and scored through the identical harness.
    /// </summary>
    public interface ISequencePredictor
    {
        string Name { get; }

        void Train(List<(string label, List<string> tokens)> train);

        /// <summary>Candidate next tokens, best first (empty if no prediction).</summary>
        List<string> RankNext(List<string> prefix);

        /// <summary>Predicted label (activity/person) for the prefix, or null.</summary>
        string PredictLabel(List<string> prefix);
    }
}

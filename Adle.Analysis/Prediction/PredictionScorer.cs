namespace Adle.Analysis.Prediction
{
    /// <summary>
    /// Accumulates next-step prediction outcomes and exposes both the thesis's
    /// custom confusion metrics and standard top-1 accuracy.
    ///
    /// Per prediction, the actual next node is classified as:
    ///   - TruePositive  : it is the top-1 prediction
    ///   - FalsePositive : it appears in the predicted distribution but not top-1
    ///   - FalseNegative : it is absent from the distribution (or no prediction)
    ///
    /// Framework-free port of GUI_Simulation ... Scoring/AccuracyInformation.
    /// </summary>
    public class PredictionScorer
    {
        public int TruePositive { get; private set; }
        public int FalsePositive { get; private set; }
        public int FalseNegative { get; private set; }

        public int Total => TruePositive + FalsePositive + FalseNegative;

        public void Record(PredictionResult result, string actualNextNode)
        {
            if (result == null || !result.HasPrediction || !result.IsInDistribution(actualNextNode))
                FalseNegative++;
            else if (result.IsTop1(actualNextNode))
                TruePositive++;
            else
                FalsePositive++;
        }

        /// <summary>Thesis accuracy: (TP + FP) / N — credit for any in-distribution hit.</summary>
        public double Accuracy => Safe(TruePositive + FalsePositive, Total);

        /// <summary>Standard top-1 accuracy: TP / N.</summary>
        public double Top1Accuracy => Safe(TruePositive, Total);

        public double Precision => Safe(TruePositive, TruePositive + FalsePositive);

        public double Recall => Safe(TruePositive, TruePositive + FalseNegative);

        public double F1
        {
            get
            {
                double p = Precision, r = Recall;
                return (p + r) == 0 ? 0 : 2 * p * r / (p + r);
            }
        }

        private static double Safe(int numerator, int denominator) =>
            denominator == 0 ? 0.0 : (double)numerator / denominator;
    }
}

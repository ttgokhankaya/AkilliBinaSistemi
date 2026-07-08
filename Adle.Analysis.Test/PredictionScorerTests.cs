using System.Collections.Generic;
using Adle.Analysis;
using Adle.Analysis.Prediction;
using AdleGraph;
using AdleGraph.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Adle.Analysis.Test
{
    [TestClass]
    public class PredictionScorerTests
    {
        private static PredictionResult ResultWith(string top1, params string[] others)
        {
            var dist = new List<AnalyzeResult>();
            INode topNode = new Node(top1);
            dist.Add(new AnalyzeResult { value = topNode, probability = 1.0 });
            foreach (var o in others)
                dist.Add(new AnalyzeResult { value = new Node(o), probability = 0.1 });
            return new PredictionResult { PredictedNextNode = topNode, NextStepDistribution = dist };
        }

        [TestMethod]
        public void Record_Top1Hit_CountsTruePositive()
        {
            var scorer = new PredictionScorer();
            scorer.Record(ResultWith("2P", "3P"), "2P");
            Assert.AreEqual(1, scorer.TruePositive);
            Assert.AreEqual(0, scorer.FalsePositive);
            Assert.AreEqual(0, scorer.FalseNegative);
        }

        [TestMethod]
        public void Record_InDistributionNotTop1_CountsFalsePositive()
        {
            var scorer = new PredictionScorer();
            scorer.Record(ResultWith("2P", "3P"), "3P");
            Assert.AreEqual(0, scorer.TruePositive);
            Assert.AreEqual(1, scorer.FalsePositive);
            Assert.AreEqual(0, scorer.FalseNegative);
        }

        [TestMethod]
        public void Record_NotInDistribution_CountsFalseNegative()
        {
            var scorer = new PredictionScorer();
            scorer.Record(ResultWith("2P", "3P"), "9X");
            Assert.AreEqual(0, scorer.TruePositive);
            Assert.AreEqual(0, scorer.FalsePositive);
            Assert.AreEqual(1, scorer.FalseNegative);
        }

        [TestMethod]
        public void Metrics_MatchThesisFormulas()
        {
            var scorer = new PredictionScorer();
            // 3 TP, 1 FP, 1 FN  -> N=5
            scorer.Record(ResultWith("A", "B"), "A");
            scorer.Record(ResultWith("A", "B"), "A");
            scorer.Record(ResultWith("A", "B"), "A");
            scorer.Record(ResultWith("A", "B"), "B");     // FP
            scorer.Record(ResultWith("A", "B"), "Z");     // FN

            Assert.AreEqual(5, scorer.Total);
            Assert.AreEqual(0.80, scorer.Accuracy, 1e-9);      // (3+1)/5
            Assert.AreEqual(0.60, scorer.Top1Accuracy, 1e-9);  // 3/5
            Assert.AreEqual(0.75, scorer.Precision, 1e-9);     // 3/(3+1)
            Assert.AreEqual(0.75, scorer.Recall, 1e-9);        // 3/(3+1)
            Assert.AreEqual(0.75, scorer.F1, 1e-9);
        }
    }
}

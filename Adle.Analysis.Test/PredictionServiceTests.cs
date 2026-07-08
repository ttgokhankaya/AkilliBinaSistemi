using System.Collections.Generic;
using Adle.Analysis.Prediction;
using Adle.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Adle.Analysis.Test
{
    [TestClass]
    public class PredictionServiceTests
    {
        // A deterministic habit: entrance -> living-room -> light. If the training
        // set only ever continues "1P -> 1I" with "2P", the predictor must return 2P.
        private static List<SequenceBarDTO> TrainingSet()
        {
            return new List<SequenceBarDTO>
            {
                SequenceFactory.ToTrainingBar(new[] { "1P", "1I", "2P", "2I" }, person: "Baba"),
                SequenceFactory.ToTrainingBar(new[] { "1P", "1I", "2P", "3P" }, person: "Baba"),
                SequenceFactory.ToTrainingBar(new[] { "1P", "1I", "2P", "2I" }, person: "Anne"),
            };
        }

        [TestMethod]
        public void PredictNextStep_ReturnsTheConsistentNextNode()
        {
            var service = new PredictionService();
            var prefix = SequenceFactory.FromNodeNames(new[] { "1P", "1I" });

            var result = service.PredictNextStep(prefix, TrainingSet());

            Assert.IsTrue(result.HasPrediction);
            Assert.AreEqual("2P", result.PredictedNextNode.Name);
            Assert.IsTrue(result.IsTop1("2P"));
        }

        [TestMethod]
        public void PredictNextStep_ProbabilitiesAreRankedDescending()
        {
            var service = new PredictionService();
            var prefix = SequenceFactory.FromNodeNames(new[] { "1P", "1I", "2P" });

            var result = service.PredictNextStep(prefix, TrainingSet());

            // "2I" appears twice after the prefix, "3P" once -> 2I must rank first.
            Assert.IsTrue(result.HasPrediction);
            Assert.AreEqual("2I", result.PredictedNextNode.Name);
            for (int i = 1; i < result.NextStepDistribution.Count; i++)
                Assert.IsTrue(result.NextStepDistribution[i - 1].probability >= result.NextStepDistribution[i].probability);
        }

        [TestMethod]
        public void PredictNextStep_PredictsMostFrequentPerson()
        {
            var service = new PredictionService();
            var prefix = SequenceFactory.FromNodeNames(new[] { "1P", "1I" });

            var result = service.PredictNextStep(prefix, TrainingSet());

            // Two of three matching training rows are "Baba".
            Assert.AreEqual("Baba", result.PredictedPerson);
        }
    }
}

using Adle.Analysis;
using Adle.Analysis.Rules;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Adle.Analysis.Test
{
    [TestClass]
    public class MinMaxNormalizerTests
    {
        [TestMethod]
        public void Calculate_ScalesValuesBetweenZeroAndOne()
        {
            var data = new List<AnalyzeResult>
            {
                new AnalyzeResult() { countOfNode = 0 },
                new AnalyzeResult() { countOfNode = 5 },
                new AnalyzeResult() { countOfNode = 10 }
            };

            var output = MinMaxNormalizer.Calculate(data);

            Assert.AreEqual(0.0, output[0].normalizedValue);
            Assert.AreEqual(0.5, output[1].normalizedValue);
            Assert.AreEqual(1.0, output[2].normalizedValue);
        }

        [TestMethod]
        public void Calculate_SingleElement_NormalizesToOne()
        {
            var data = new List<AnalyzeResult> { new AnalyzeResult() { countOfNode = 7 } };

            var output = MinMaxNormalizer.Calculate(data);

            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(1.0, output[0].normalizedValue);
        }

        [TestMethod]
        public void Calculate_AllValuesEqual_NormalizesToOne()
        {
            var data = new List<AnalyzeResult>
            {
                new AnalyzeResult() { countOfNode = 3 },
                new AnalyzeResult() { countOfNode = 3 }
            };

            var output = MinMaxNormalizer.Calculate(data);

            Assert.IsTrue(output.All(x => x.normalizedValue == 1.0));
        }

        [TestMethod]
        public void Calculate_EmptyOrNull_ReturnsNull()
        {
            Assert.IsNull(MinMaxNormalizer.Calculate((List<AnalyzeResult>)null));
            Assert.IsNull(MinMaxNormalizer.Calculate(new List<AnalyzeResult>()));
        }
    }

    [TestClass]
    public class SoftmaxNormalizerTests
    {
        [TestMethod]
        public void Calculate_ProbabilitiesSumToOne()
        {
            var data = new List<AnalyzeResult>
            {
                new AnalyzeResult() { countOfNode = 10 },
                new AnalyzeResult() { countOfNode = 9 },
                new AnalyzeResult() { countOfNode = 0 }
            };

            var output = SoftmaxNormalizer.Calculate(data);

            Assert.AreEqual(1.0, output.Sum(x => x.probability), 1e-9);
        }

        [TestMethod]
        public void Calculate_PreservesOrdering()
        {
            var data = new List<AnalyzeResult>
            {
                new AnalyzeResult() { countOfNode = 15 },
                new AnalyzeResult() { countOfNode = 5 },
                new AnalyzeResult() { countOfNode = 0 }
            };

            var output = SoftmaxNormalizer.Calculate(data);

            Assert.IsTrue(output[0].probability > output[1].probability);
            Assert.IsTrue(output[1].probability > output[2].probability);
        }

        [TestMethod]
        public void Calculate_PlainDoubles_MatchesSoftmaxDefinition()
        {
            var softmax = new SoftmaxNormalizer();
            var data = new List<double> { 1.0, 2.0, 3.0 };

            var output = softmax.Calculate(data);

            double total = data.Sum(x => Math.Exp(x));
            for (int i = 0; i < data.Count; i++)
            {
                Assert.AreEqual(Math.Exp(data[i]) / total, output[i], 1e-9);
            }
        }
    }
}

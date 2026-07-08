using System.Collections.Generic;
using System.Linq;
using Adle.Analysis.Rules;
using AdleGraph.Interfaces;
using SequentialPattern;

namespace Adle.Analysis.Prediction
{
    /// <summary>
    /// Reusable, UI-free port of the thesis prediction pipeline that used to live
    /// in MainWindowSequentialPattern.xaml.cs. Given a training set of sequences
    /// and an observed prefix, it predicts the next node (and the acting person)
    /// using the SimilarityRule -> LCSRule pipeline with min-max + softmax
    /// probability distribution. Shared by the benchmark console and the web API.
    /// </summary>
    public class PredictionService
    {
        private readonly int _lcsThreshold;

        public PredictionService(int lcsThreshold = 0)
        {
            _lcsThreshold = lcsThreshold;
        }

        public PredictionResult PredictNextStep(Sequence<INode> prefix, List<SequenceBarDTO> trainingData)
        {
            var analyzer = new SequenceAnalyzer { Data = trainingData };

            var similarityRule = new SimilarityRule(1);
            var lcsRule = new LCSRule(2);
            similarityRule.setParams(new SoftmaxNormalizer(), new MinMaxNormalizer());
            lcsRule.setParams(_lcsThreshold, new SoftmaxNormalizer(), new MinMaxNormalizer());
            analyzer.Rules.Add(similarityRule);
            analyzer.Rules.Add(lcsRule);

            analyzer.Analyze(prefix);

            var distribution = analyzer.probabilityDistributionOfNodesInTheNextStep ?? new List<AnalyzeResult>();
            var ranked = distribution.OrderByDescending(x => x.probability).ToList();
            var matches = analyzer.currentSequenceAnalysisResult ?? new List<SequenceBarDTO>();

            return new PredictionResult
            {
                NextStepDistribution = ranked,
                PredictedNextNode = ranked.FirstOrDefault()?.value,
                PredictedPerson = PredictPerson(matches),
                RuleUsed = analyzer.ResultsFromWhichRule,
                MatchCount = matches.Count
            };
        }

        /// <summary>Most frequent person label among the matched training sequences.</summary>
        private static string PredictPerson(List<SequenceBarDTO> matches)
        {
            return matches
                .Where(m => !string.IsNullOrEmpty(m.Person))
                .GroupBy(m => m.Person)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
        }
    }

    public class PredictionResult
    {
        public INode PredictedNextNode { get; set; }
        public string PredictedPerson { get; set; }
        public List<AnalyzeResult> NextStepDistribution { get; set; } = new List<AnalyzeResult>();
        public string RuleUsed { get; set; }
        public int MatchCount { get; set; }

        public bool HasPrediction => PredictedNextNode != null;

        /// <summary>True when <paramref name="actualNodeName"/> is the top-1 prediction.</summary>
        public bool IsTop1(string actualNodeName) =>
            PredictedNextNode != null && PredictedNextNode.Name == actualNodeName;

        /// <summary>True when <paramref name="actualNodeName"/> appears anywhere in the predicted distribution.</summary>
        public bool IsInDistribution(string actualNodeName) =>
            NextStepDistribution.Any(x => x.value != null && x.value.Name == actualNodeName);
    }
}

using System.Collections.Generic;
using System.Linq;
using Adle.Analysis;
using Adle.Analysis.Prediction;

namespace Adle.Benchmark.Predictors
{
    /// <summary>The thesis method: SimilarityRule -> LCSRule with min-max + softmax.</summary>
    public sealed class LcsSoftmaxPredictor : ISequencePredictor
    {
        public string Name => "LCS+softmax (thesis)";

        private readonly PredictionService _service = new PredictionService();
        private List<SequenceBarDTO> _train = new List<SequenceBarDTO>();

        public void Train(List<(string label, List<string> tokens)> train)
        {
            int order = 0;
            _train = train.Select(t => SequenceFactory.ToTrainingBar(t.tokens, t.label, order++)).ToList();
        }

        public List<string> RankNext(List<string> prefix)
        {
            var result = _service.PredictNextStep(SequenceFactory.FromNodeNames(prefix), _train);
            return result.NextStepDistribution
                .Where(x => x.value != null)
                .Select(x => x.value.Name)
                .ToList();
        }

        public string PredictLabel(List<string> prefix)
        {
            var result = _service.PredictNextStep(SequenceFactory.FromNodeNames(prefix), _train);
            return result.PredictedPerson;
        }
    }
}

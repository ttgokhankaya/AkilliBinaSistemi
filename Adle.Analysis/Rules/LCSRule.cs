using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdleGraph.Interfaces;
using SequentialPattern;

namespace Adle.Analysis.Rules
{
    public class LCSRule : IAnalysisRule
    {
        #region Fields
        private int _threshold = 0;
        private ICalculator _calculatorFuncationOfNormalization;
        private ICalculator _calculatorFunctionOfProbabilityDistribution;
        private readonly int _order;
        #endregion Fields

        public LCSRule(int order)
        {
            _order = order;
        }

        #region Properties
        public string name
        {
            get
            {
                return "LCS";
            }
        }

        public int order
        {
            get
            {
                return _order;
            }
        }

        public List<AnalyzeResult> probabilityDistributionOfNodesInTheNextStep { get; private set; }

        public List<SequenceBarDTO> currentSequenceAnalysisResultOfSequenceBars { get; private set; }

        public List<AnalyzeResult> lastProbabilityDistributionOfNodes { get; private set; }

        #endregion Properties 

        #region Public Methods
        public List<SequenceBarDTO> Analyze(Sequence<INode> sequence, List<SequenceBarDTO> data)
        {
            if (order <= 0)
                return new List<SequenceBarDTO>();

            currentSequenceAnalysisResultOfSequenceBars = new List<SequenceBarDTO>();
            lastProbabilityDistributionOfNodes = probabilityDistributionOfNodesInTheNextStep;
            probabilityDistributionOfNodesInTheNextStep = new List<AnalyzeResult>();

            INode lastNodeOfSequence = sequence[0]?.Last()?.Value;

            foreach (var item in data)
            {

                item.LCS = item.Sequence.CalculateLCS(sequence);

                if (item.LCS < _threshold)
                    continue;

                currentSequenceAnalysisResultOfSequenceBars.Add(item);

                var node = item.Sequence.GetNextItemValue(lastNodeOfSequence);

                if (node == null)
                    continue;

                var foundNode = probabilityDistributionOfNodesInTheNextStep.Find(x => x.value.Name == node.Name);

                if (foundNode == null)
                    probabilityDistributionOfNodesInTheNextStep.Add(new AnalyzeResult() { value = node, countOfNode = 1, probability = 0.0 });
                else
                    foundNode.countOfNode = foundNode.countOfNode + 1;
            }



            var NextStepNormalizationFunctionResult = _calculatorFuncationOfNormalization?.Calculate(probabilityDistributionOfNodesInTheNextStep.Select(x => (double)x.countOfNode).ToList(), null).ToList();

            if (NextStepNormalizationFunctionResult != null)
            {
                for (int i = 0; i < NextStepNormalizationFunctionResult?.Count; i++)
                {
                    probabilityDistributionOfNodesInTheNextStep[i].normalizedValue = NextStepNormalizationFunctionResult[i];
                }
            }

            var nextStepDataOfUsage = NextStepNormalizationFunctionResult == null ? probabilityDistributionOfNodesInTheNextStep.Select(x => (double)x.countOfNode).ToList() : probabilityDistributionOfNodesInTheNextStep.Select(x => (double)x.normalizedValue).ToList();

            var nextStepProbabilityResult = _calculatorFunctionOfProbabilityDistribution?.Calculate(nextStepDataOfUsage, null).ToList();

            for (int i = 0; i < nextStepProbabilityResult.Count; i++)
            {
                probabilityDistributionOfNodesInTheNextStep[i].probability = nextStepProbabilityResult[i];
            }

            var currentStepNormalizationFunctionResult = _calculatorFuncationOfNormalization?.Calculate(currentSequenceAnalysisResultOfSequenceBars.Select(x => (double)x.LCS).ToList(), null).ToList();

            if (currentStepNormalizationFunctionResult != null)
            {
                for (int i = 0; i < currentStepNormalizationFunctionResult?.Count; i++)
                {
                    currentSequenceAnalysisResultOfSequenceBars[i].normalizedValue = currentStepNormalizationFunctionResult[i];
                }
            }

            var currentStepDataOfUsage = currentStepNormalizationFunctionResult == null ? currentSequenceAnalysisResultOfSequenceBars.Select(x => (double)x.LCS).ToList() : currentSequenceAnalysisResultOfSequenceBars.Select(x => (double)x.normalizedValue).ToList();

            var currentStepProbabilityResult = _calculatorFunctionOfProbabilityDistribution?.Calculate(currentStepDataOfUsage, null).ToList();

            for (int i = 0; i < currentStepProbabilityResult.Count; i++)
            {
                currentSequenceAnalysisResultOfSequenceBars[i].SimilarityRatio = $"{currentStepProbabilityResult[i] * 100}% (LCS:{currentSequenceAnalysisResultOfSequenceBars[i].LCS})";
            }
            return currentSequenceAnalysisResultOfSequenceBars;
        }

        /// <summary>
        /// Parametreleri set eder. :)
        /// </summary>
        /// <param name="param">ilk parametre Eşik değeridir. 
        /// ikinci parametre olasılık dağılımının hesaplanması için kullanılan yöntemdir. (softmax)
        /// üçüncü parametre birlikte kullanılacak alt yöntemdir. (min-maz/zscore) </param>
        public void setParams(params object[] param)
        {
            if (param == null || param.Length <= 0)
                return;

            if (param[0] is int)
                _threshold = (int)param[0];

            if (param[1] is ICalculator)
                _calculatorFunctionOfProbabilityDistribution = (ICalculator)param[1];

            if (param[2] is ICalculator)
                _calculatorFuncationOfNormalization = (ICalculator)param[2];
        }

        #endregion Public Methods
    }
}

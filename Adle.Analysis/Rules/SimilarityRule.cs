using AdleGraph.Interfaces;
using SequentialPattern;
using System.Collections.Generic;

namespace Adle.Analysis.Rules
{
    public class SimilarityRule : IAnalysisRule
    {
        #region Fields
        private readonly int _order;

        #endregion Fields

        public SimilarityRule(int order = -1)
        {
            _order = order;
        }

        #region Properties
        public string name
        {
            get
            {
                return "Similarity Rule";
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

            foreach (var item in data)
            {

                if (!compare(sequence, item.Sequence))
                    continue;

                item.LCS = item.Sequence.CalculateLCS(sequence);
                currentSequenceAnalysisResultOfSequenceBars.Add(item);
                
                if (item.Sequence[0].Count <= sequence[0].Count)
                    continue;

                INode node = item.Sequence[0][sequence[0].Count].Value;

                var foundNode = probabilityDistributionOfNodesInTheNextStep.Find(x => x.value.Name == node.Name);

                if (foundNode == null)
                    probabilityDistributionOfNodesInTheNextStep.Add(new AnalyzeResult() { value = node, countOfNode = 1, probability = 0.0 });
                else
                    foundNode.countOfNode = foundNode.countOfNode + 1;
            }

            probabilityDistributionOfNodesInTheNextStep = SoftmaxNormalizer.Calculate(probabilityDistributionOfNodesInTheNextStep);
            currentSequenceAnalysisResultOfSequenceBars = SoftmaxNormalizer.Calculate(currentSequenceAnalysisResultOfSequenceBars);

            return currentSequenceAnalysisResultOfSequenceBars;
        }

        private bool compare(Sequence<INode> node1, Sequence<INode> node2)
        {
            if (node2 == null || node2.Count <= 0)
                return false;

            if (node1 == null || node1.Count <= 0)
                return false;

            if (node1[0].Count >= node2[0].Count)
                return false;

            for (int i = 0; i < node1[0].Count; i++)
            {
                if (!(node1[0][i].Value.ToString() == node2[0][i].Value.ToString()))
                    return false;
            }

            return true;
        }

        public void setParams(params object[] param)
        {

        }
        #endregion Public Methods
    }
}

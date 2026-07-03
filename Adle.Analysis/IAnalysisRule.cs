using AdleGraph.Interfaces;
using SequentialPattern;
using System.Collections.Generic;

namespace Adle.Analysis
{
    public interface IAnalysisRule
    {
        string name { get; }

        void setParams(params object[] param);

        List<SequenceBarDTO> Analyze(Sequence<INode> param, List<SequenceBarDTO> data);

        List<AnalyzeResult> probabilityDistributionOfNodesInTheNextStep { get; }

        List<AnalyzeResult> lastProbabilityDistributionOfNodes { get; }

        List<SequenceBarDTO> currentSequenceAnalysisResultOfSequenceBars { get; }

        int order { get; }
    }
}

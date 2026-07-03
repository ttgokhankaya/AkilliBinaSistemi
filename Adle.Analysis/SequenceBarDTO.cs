using AdleGraph.Interfaces;
using SequentialPattern;
using System;

namespace Adle.Analysis
{
    public class SequenceBarDTO
    {
        public int Order { get; set; }
        public string Person { get; set; }
        public Scenario Scenario { get; set; }
        public Sequence<INode> Sequence { get; set; }
        public string SimilarityRatio { get; set; }

        public double normalizedValue { get; set; }
        public int LCS { get; set; }

        public Guid ID { get; set; }

        public bool TrainingData { get; set; }
    }
}
using AdleGraph.Interfaces;
using System;

namespace Adle.Analysis
{
    public class AnalyzeResult
    {
        public INode value { get; set; }

        public double probability { get; set; }

        public int countOfNode { get; set; }

        public double normalizedValue { get; set; }

        public override string ToString()
        {
            return $"{value.Name} ({countOfNode}) {Math.Round(probability, 2) * 100}%";
        }
    }
}

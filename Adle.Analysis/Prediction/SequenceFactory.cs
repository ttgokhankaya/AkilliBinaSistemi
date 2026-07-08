using System.Collections.Generic;
using AdleGraph;
using AdleGraph.Interfaces;
using SequentialPattern;

namespace Adle.Analysis.Prediction
{
    /// <summary>
    /// Builds the single-ItemSet <see cref="Sequence{INode}"/> shape the analysis
    /// rules expect: all nodes live in ItemSet 0 in order, so SimilarityRule/LCSRule
    /// can index the next node as <c>sequence[0][prefixLength]</c>.
    /// </summary>
    public static class SequenceFactory
    {
        public static Sequence<INode> FromNodeNames(IEnumerable<string> nodeNames)
        {
            var sequence = new Sequence<INode>();
            int index = 0;
            foreach (var name in nodeNames)
            {
                INode node = new Node(name);
                if (index == 0)
                    sequence.AddItemAsItemSet(node);
                else
                    sequence.AddItemToItemSet(node, 0);
                index++;
            }
            return sequence;
        }

        /// <summary>Wraps sequences as training rows the analyzer consumes.</summary>
        public static SequenceBarDTO ToTrainingBar(IEnumerable<string> nodeNames, string person = null, int order = 0)
        {
            return new SequenceBarDTO
            {
                Order = order,
                Person = person,
                Sequence = FromNodeNames(nodeNames),
                TrainingData = true
            };
        }
    }
}

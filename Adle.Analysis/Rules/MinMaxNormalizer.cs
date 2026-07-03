using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adle.Analysis.Rules
{
    public class MinMaxNormalizer : ICalculator
    {
        public IEnumerable<double> Calculate(IEnumerable<double> data, ICalculator subCalculator)
        {
            List<double> output = new List<double>();

            if (data == null || data.Count() <= 0)
                return output;

            if (data.Count() == 1)
            {
                output.Add(1.0d);
                return output;
            }

            double min = data.Min();
            double max = data.Max();

            foreach (var input in data)
            {
                double result = (input - min) / (max - min);
                output.Add(result.Equals(double.NaN) ? 1 : result);
            }

            return output;
        }

        public static List<AnalyzeResult> Calculate(List<AnalyzeResult> data)
        {
            List<AnalyzeResult> output = new List<AnalyzeResult>();

            if (data == null || data.Count <= 0)
                return null;


            if (data.Count == 1)
            {
                output.Add(new AnalyzeResult()
                {
                    value = data[0].value,
                    countOfNode = data[0].countOfNode,
                    normalizedValue = 1
                });
                return output;
            }

            double min = data.Min(x => x.countOfNode);
            double max = data.Max(x => x.countOfNode);

            foreach (var input in data)
            {
                double result = (input.countOfNode - min) / (max - min);
                output.Add(new AnalyzeResult()
                {
                    value = input.value,
                    countOfNode = input.countOfNode,
                    normalizedValue = result.Equals(double.NaN) ? 1 : result
                });
            }

            return output;
        }

        public static List<SequenceBarDTO> Calculate(List<SequenceBarDTO> data)
        {
            List<SequenceBarDTO> output = new List<SequenceBarDTO>();

            if (data == null || data.Count <= 0)
                return null;

            if (data.Count == 1)
            {
                output.Add(new SequenceBarDTO()
                {
                    Order = data[0].Order,
                    Person = data[0].Person,
                    Scenario = data[0].Scenario,
                    Sequence = data[0].Sequence,
                    SimilarityRatio = data[0].SimilarityRatio,
                    LCS = data[0].LCS,
                    normalizedValue = 1,
                    TrainingData = data[0].TrainingData
                });
                return output;
            }

            double min = data.Min(x => x.LCS);
            double max = data.Max(x => x.LCS);

            foreach (var input in data)
            {
                double result = (input.LCS - min) / (max - min);
                output.Add(new SequenceBarDTO()
                {
                    Order = input.Order,
                    Person = input.Person,
                    Scenario = input.Scenario,
                    Sequence = input.Sequence,
                    SimilarityRatio = input.SimilarityRatio,
                    LCS = input.LCS,
                    normalizedValue = (result.Equals(double.NaN)) ? 1 : result,
                    TrainingData = input.TrainingData
                });
            }

            return output;
        }

    }
}

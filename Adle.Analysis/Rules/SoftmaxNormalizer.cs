using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adle.Analysis.Rules
{
    public class SoftmaxNormalizer : ICalculator
    {
        public IEnumerable<double> Calculate(IEnumerable<double> data, ICalculator subCalculator)
        {
            double total = 0.0;
            List<double> output = new List<double>();
            if (data == null || data.Count() <= 0)
                return output;


            List<double> normalizedData = new List<double>();

            if (subCalculator != null)
                normalizedData = subCalculator?.Calculate(data, null)?.ToList();

            var usageData = subCalculator != null ? normalizedData : data;
            total = usageData.Select(x => Math.Exp(x)).Sum();

            output.AddRange(usageData.Select(x => Math.Round((Math.Exp(x) / total), 2)));

            return output;
        }

        public List<double> Calculate(List<double> data)
        {
            double total = 0.0;
            List<double> output = new List<double>();

            foreach (var input in data)
            {
                total += Math.Exp(input);
            }

            foreach (var input in data)
            {
                double result = Math.Exp(input) / total;
                output.Add(result);
            }

            return output;
        }

        public static List<AnalyzeResult> Calculate(List<AnalyzeResult> data)
        {
            double total = 0.0;
            List<AnalyzeResult> output = new List<AnalyzeResult>();
            var normalizedData = MinMaxNormalizer.Calculate(data);

            if (normalizedData == null)
                return output;

            foreach (var input in normalizedData)
            {
                total += Math.Exp(input.normalizedValue);
            }

            foreach (var input in normalizedData)
            {
                double result = Math.Exp(input.normalizedValue) / total;
                output.Add(new AnalyzeResult() { value = input.value, countOfNode = input.countOfNode, probability = result, normalizedValue = input.normalizedValue });
            }

            return output;
        }

        public static List<SequenceBarDTO> Calculate(List<SequenceBarDTO> data)
        {
            double total = 0.0;
            List<SequenceBarDTO> output = new List<SequenceBarDTO>();
            var normalizedData = MinMaxNormalizer.Calculate(data);

            if (normalizedData == null)
                return output;

            foreach (var input in normalizedData)
            {
                total += Math.Exp(input.normalizedValue);
            }

            foreach (var input in normalizedData)
            {
                double result = Math.Exp(input.normalizedValue) / total;
                output.Add(new SequenceBarDTO()
                {
                    Order = input.Order,
                    Person = input.Person,
                    Scenario = input.Scenario,
                    Sequence = input.Sequence,
                    SimilarityRatio = $"{Math.Round(result, 2) * 100}%",
                    LCS = input.LCS,
                    normalizedValue = input.normalizedValue,
                    TrainingData = input.TrainingData
                });
            }

            return output;
        }

    }
}

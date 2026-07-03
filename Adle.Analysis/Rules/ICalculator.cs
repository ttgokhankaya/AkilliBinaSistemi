using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adle.Analysis.Rules
{
    public interface ICalculator
    {
       IEnumerable<double> Calculate(IEnumerable<double> data, ICalculator subCalculator);
    }
}

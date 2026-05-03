using System;

namespace AdleGraph
{
    public class Utility
    {
        public static int Between(int minimumValue, int maximumValue, bool includeMaxValue = true)
        {
            if (!includeMaxValue)
                maximumValue = maximumValue - 1;
            return Random.Shared.Next(minimumValue, maximumValue + 1);
        }

        public static int Between2(int minimumValue, int maximumValue, bool includeMaxValue = true)
        {
            if (!includeMaxValue)
                maximumValue = maximumValue - 1;
            return Random.Shared.Next(minimumValue, maximumValue + 1);
        }

        /// <summary>
        /// weights bir çizelgedeki bir düğümden diğer düğüme olan kenarların ağırlıklarını tutan dizidir.
        /// Dizi içindeki değerler int olarak tutulur. Yani kenar değeri 0.1, 0.9 ise sırası ile 10 ve 90 olarak verilir.
        /// weights toplamı 100 olmalıdır. 0 değeri verilirse işleme alınmaz.
        /// </summary>
        public static int RandomWeighted(int[] weights)
        {
            int total = 0;
            foreach (var w in weights) total += w;
            int randVal = Random.Shared.Next(1, total + 1);
            int cumulative = 0;
            for (int result = 0; result < weights.Length; result++)
            {
                cumulative += weights[result];
                if (cumulative >= randVal) return result;
            }
            return weights.Length - 1;
        }
    }
}

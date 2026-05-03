using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SequentialPattern
{
    public class NodeItem<T>
    {
        public T Value { get; }
        public bool Flag { get; }

        public NodeItem(T value, bool flag)
        {
            Value = value;
            Flag = flag;
        }

        public override string ToString() => Value?.ToString() ?? "";
    }

    public class ItemSet<T> : List<NodeItem<T>>
    {
        public void Add(T value, bool flag) => Add(new NodeItem<T>(value, flag));
    }

    public class Sequence<T> : List<ItemSet<T>>
    {
        public int Length => this.Sum(s => s.Count);

        public void AddItemAsItemSet(T item)
        {
            var set = new ItemSet<T>();
            set.Add(item, true);
            Add(set);
        }

        public void AddItemToItemSet(T item, int itemSetIndex)
        {
            if (itemSetIndex >= 0 && itemSetIndex < Count)
                this[itemSetIndex].Add(item, true);
        }

        public int CalculateLCS(Sequence<T> other)
        {
            var a = FlatValues();
            var b = other.FlatValues();
            int m = a.Count, n = b.Count;
            int[,] dp = new int[m + 1, n + 1];
            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    dp[i, j] = string.Equals(a[i - 1]?.ToString(), b[j - 1]?.ToString())
                        ? dp[i - 1, j - 1] + 1
                        : Math.Max(dp[i - 1, j], dp[i, j - 1]);
            return dp[m, n];
        }

        public List<T> FindLCS(Sequence<T> other)
        {
            var a = FlatValues();
            var b = other.FlatValues();
            int m = a.Count, n = b.Count;
            int[,] dp = new int[m + 1, n + 1];
            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    dp[i, j] = string.Equals(a[i - 1]?.ToString(), b[j - 1]?.ToString())
                        ? dp[i - 1, j - 1] + 1
                        : Math.Max(dp[i - 1, j], dp[i, j - 1]);

            var result = new List<T>();
            int x = m, y = n;
            while (x > 0 && y > 0)
            {
                if (string.Equals(a[x - 1]?.ToString(), b[y - 1]?.ToString()))
                {
                    result.Insert(0, a[x - 1]);
                    x--; y--;
                }
                else if (dp[x - 1, y] >= dp[x, y - 1])
                    x--;
                else
                    y--;
            }
            return result;
        }

        public T GetNextItemValue(T lastNode)
        {
            var values = FlatValues();
            for (int i = 0; i < values.Count - 1; i++)
                if (string.Equals(values[i]?.ToString(), lastNode?.ToString()))
                    return values[i + 1];
            return default;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var itemSet in this)
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append(string.Join(" → ", itemSet.Select(n => n.Value?.ToString() ?? "")));
            }
            return sb.ToString();
        }

        private List<T> FlatValues()
        {
            var result = new List<T>();
            foreach (var itemSet in this)
                foreach (var item in itemSet)
                    result.Add(item.Value);
            return result;
        }
    }
}

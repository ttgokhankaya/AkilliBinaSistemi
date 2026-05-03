namespace GUI_Simulation
{
    public static class extensions
    {
        public static string ToStringValue(this double[] ob)
        {
            string value = "";
            for (int i = 0; i < ob.Length; i++)
            {
                value += $" {ob[i]},".TrimEnd(new char[] { ',' });
            }
            return value.TrimStart(new char[] { ' ' });
        }
    }
}

namespace GUI_Simulation.AnomalyExploration
{
    public class InputWindow
    {
        private static readonly double[] ConvolutionKernel = { 1.0, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1, 0.0 };

        public InputWindow(int numberOfSensors, int lenghtOfWindow)
        {
            states = new double[numberOfSensors][];
            for (int i = 0; i < numberOfSensors; i++)
                states[i] = new double[lenghtOfWindow];
        }

        public int order { get; set; }
        public double[][] states { get; set; }
        public double[][] convolvedStates { get; set; }

        public void ConvolveWindow()
        {
            convolvedStates = new double[states.Length][];
            for (int i = 0; i < states.Length; i++)
                convolvedStates[i] = Convolve(states[i], ConvolutionKernel);
        }

        public override string ToString() => order.ToString();

        private static double[] Convolve(double[] signal, double[] kernel)
        {
            int n = signal.Length;
            int m = kernel.Length;
            double[] result = new double[n];
            for (int i = 0; i < n; i++)
            {
                double sum = 0;
                for (int k = 0; k < m; k++)
                {
                    int j = i - k;
                    if (j >= 0 && j < n)
                        sum += signal[j] * kernel[k];
                }
                result[i] = sum;
            }
            return result;
        }
    }
}

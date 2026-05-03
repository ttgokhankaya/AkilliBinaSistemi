using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace GUI_Simulation.AnomalyExploration
{
    public partial class GraficView : Window, INotifyPropertyChanged
    {
        private double _perplexityValue = 0;
        private PlotModel _plotModel;

        public event PropertyChangedEventHandler PropertyChanged;
        private void onPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<ObservationSet> observations { get; set; }

        public string PerplexityValue
        {
            get { return _perplexityValue.ToString(); }
            set
            {
                if (double.TryParse(value, out _perplexityValue))
                    onPropertyChanged();
            }
        }

        public GraficView(List<ObservationSet> observations)
        {
            InitializeComponent();
            PerplexityValue = "0.9";
            txtPerplexityValue.Text = PerplexityValue;
            this.observations = observations;
            this.Loaded += GraficView_Loaded;
        }

        private void GraficView_Loaded(object sender, RoutedEventArgs e)
        {
            generateGraficView();
        }

        private void generateGraficView()
        {
            if (!double.TryParse(txtPerplexityValue.Text, out _perplexityValue))
            {
                MessageBox.Show("Perplexity değeri ondalık bir sayı olmalıdır.");
                return;
            }

            _plotModel = new PlotModel { Title = "" };
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "X",
                Minimum = -5,
                Maximum = 30
            });
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Y",
                Minimum = -5,
                Maximum = 30
            });

            var oxyColors = new[]
            {
                OxyColors.SteelBlue, OxyColors.OrangeRed, OxyColors.SeaGreen,
                OxyColors.Purple, OxyColors.DarkGoldenrod, OxyColors.Crimson,
                OxyColors.Teal, OxyColors.SlateBlue, OxyColors.Coral
            };
            int colorIndex = 0;

            foreach (var observation in observations)
            {
                double[][] output;
                try
                {
                    output = Task.Run(() =>
                        MlServiceClient.ComputeTsneAsync(observation.Observations, _perplexityValue)
                    ).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"ML servisi hatası: {ex.Message}\nDocker'ın çalıştığından emin olun.");
                    return;
                }

                var series = new ScatterSeries
                {
                    Title = observation.Name,
                    MarkerType = MarkerType.Diamond,
                    MarkerSize = 5,
                    MarkerFill = oxyColors[colorIndex % oxyColors.Length]
                };
                for (int i = 0; i < output.Length; i++)
                    series.Points.Add(new ScatterPoint(output[i][0], output[i][1]));
                _plotModel.Series.Add(series);
                colorIndex++;
            }

            plotView.Model = _plotModel;
        }

        private void btnReflesh_Click(object sender, RoutedEventArgs e)
        {
            generateGraficView();
        }
    }
}

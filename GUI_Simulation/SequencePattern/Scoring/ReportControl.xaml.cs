using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace GUI_Simulation.SequencePattern.Scoring
{
    public partial class ReportControl : UserControl, INotifyPropertyChanged
    {
        private PlotModel _plotModel;

        public ReportControl(AccuracyInformation information)
        {
            InitializeComponent();
            DataContext = this;
            this.information = information;
            this.information.dataChanged += Information_dataChanged;
            ReportStartingTime = DateTime.Now;
            setSummary();

            _accuracyObservations = new Dictionary<int, double>();
            _recallObservations = new Dictionary<int, double>();
            _precisionObservations = new Dictionary<int, double>();
            _fscoreObservations = new Dictionary<int, double>();

            InitGraph();
        }

        #region Fields
        private DateTime _reportStartingTime;
        private int _trainingSetCount;
        private int _newAddedDataCount;
        private int _testCount;
        private string _summaryValue = "";
        private Dictionary<int, double> _accuracyObservations;
        private Dictionary<int, double> _recallObservations;
        private Dictionary<int, double> _precisionObservations;
        private Dictionary<int, double> _fscoreObservations;
        #endregion

        #region Events
        private void Information_dataChanged(object sender, DataEventArgs e)
        {
            setSummary();
            int index = _accuracyObservations.Values.Count;
            _accuracyObservations.Add(index, e.Accuracy);
            _recallObservations.Add(index, e.Recall);
            _precisionObservations.Add(index, e.Precision);
            _fscoreObservations.Add(index, e.Fscore);
            RefleshGraphic();
        }
        #endregion

        #region Properties
        public AccuracyInformation information { get; set; }

        public DateTime ReportStartingTime
        {
            get { return _reportStartingTime; }
            set { _reportStartingTime = value; onPropertyCahnged(); onPropertyCahnged("TimeValue"); }
        }

        public string TimeValue => $"{ReportStartingTime.ToShortDateString()} {ReportStartingTime.ToShortTimeString()}";

        public string Summary
        {
            get
            {
                if (information == null) return "Özet bilgisi bulunamadı";
                return _summaryValue;
            }
        }

        public int TrainingSetCount
        {
            get { return _trainingSetCount; }
            set { _trainingSetCount = value; onPropertyCahnged(); onPropertyCahnged("TotalCount"); }
        }

        public int NewAddedDataCount
        {
            get { return _newAddedDataCount; }
            set { _newAddedDataCount = value; onPropertyCahnged(); onPropertyCahnged("TotalCount"); setSummary(); }
        }

        public int TestCount
        {
            get { return _testCount; }
            set { _testCount = value; onPropertyCahnged(); }
        }

        public int TotalCount => TrainingSetCount + NewAddedDataCount;
        #endregion

        #region Public Methods
        public void RemoveLastPointsFromGraphic()
        {
            int index = _accuracyObservations.Count;
            _accuracyObservations.Remove(index - 1);
            _recallObservations.Remove(index - 1);
            _precisionObservations.Remove(index - 1);
            _fscoreObservations.Remove(index - 1);

            index--;
            if (index >= 1)
            {
                information.Accuracy = _accuracyObservations[index - 1];
                information.Precision = _precisionObservations[index - 1];
                information.Recall = _recallObservations[index - 1];
                information.Fscore = _fscoreObservations[index - 1];
                information.CalculateAccuracyPrecisionRecallAndFscore();
            }

            RefleshGraphic();
            setSummary();
        }
        #endregion

        #region Private Methods
        private void setSummary()
        {
            string accuracy = string.Format("{0:0.00}", information.Accuracy);
            string fscore = string.Format("{0:0.00}", information.Fscore);
            _summaryValue = $"{TimeValue}' de başlayan simülasyonda, {TrainingSetCount} adet eğitim verisine {NewAddedDataCount} adet yeni veri eklenerek {TestCount} adet deneme yapılmıştır.\n{accuracy} doğruluk oranıyla ve {fscore} doğruluk ortalamasıyla tahminler yapılmıştır.";
            onPropertyCahnged("Summary");
        }

        private void RefleshGraphic()
        {
            _plotModel.Series.Clear();
            AddLine(_accuracyObservations, OxyColors.Green, "Doğruluk Oranı");
            AddLine(_recallObservations, OxyColors.Blue, "Hassasiyet");
            AddLine(_precisionObservations, OxyColors.BlueViolet, "Kesinlik");
            AddLine(_fscoreObservations, OxyColors.Red, "Ortalama");
            _plotModel.InvalidatePlot(true);
        }

        private void AddLine(Dictionary<int, double> observations, OxyColor color, string name)
        {
            var series = new LineSeries
            {
                Title = name,
                Color = color,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = color
            };
            foreach (var obs in observations)
                series.Points.Add(new DataPoint(obs.Key, obs.Value));
            _plotModel.Series.Add(series);
        }

        private void InitGraph()
        {
            _plotModel = new PlotModel { Title = "" };
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Title = "Deneme Sayısı",
                Minimum = -1,
                Maximum = 45
            });
            _plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Title = "Sonuç",
                Minimum = -0.3,
                Maximum = 1.3
            });
            plotView.Model = _plotModel;
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void onPropertyCahnged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var builder = new StringBuilder();
            builder.Append("Accuracy\n");
            foreach (var item in _accuracyObservations)
                builder.Append($"{item.Key}\t{item.Value}\n");
            builder.Append("\nRecall\n");
            foreach (var item in _recallObservations)
                builder.Append($"{item.Key}\t{item.Value}\n");
            builder.Append("\nPrecision\n");
            foreach (var item in _precisionObservations)
                builder.Append($"{item.Key}\t{item.Value}\n");
            builder.Append("\nF1Score\n");
            foreach (var item in _fscoreObservations)
                builder.Append($"{item.Key}\t{item.Value}\n");

            string path = $@"C:\ADLE_Observations\{Name}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.txt";
            System.IO.File.WriteAllText(path, builder.ToString());
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }
}

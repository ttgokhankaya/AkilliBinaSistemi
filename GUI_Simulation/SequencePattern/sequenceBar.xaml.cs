using Adle.Analysis;
using Adle.Analysis.Rules;
using AdleGraph.Interfaces;
using GUI_Simulation.AnomalyExploration;
using SequentialPattern;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GUI_Simulation.SequencePattern
{
    /// <summary>
    /// Interaction logic for sequenceBar.xaml
    /// </summary>
    public partial class sequenceBar : UserControl, INotifyPropertyChanged
    {
        #region Fields

        private string _person;
        private string _sequenceString;
        private int _sequenceLength;
        private string _similarityRatio;

        private bool _showPerson;
        private bool _showSimilarityRatio;

        private Sequence<INode> _sequence;
        private Scenario _scenario;
        private int _order;

        private bool _trainingData;
        private bool _showData;
        #endregion Fields

        #region Properties

        public SequenceBarDTO DTO
        {
            get
            {
                SequenceBarDTO _dto = new SequenceBarDTO();
                _dto.Order = _order;
                _dto.Person = _person;
                _dto.Scenario = _scenario;
                _dto.Sequence = _sequence;
                _dto.SimilarityRatio = _similarityRatio;
                _dto.ID = Id;
                _dto.TrainingData = TrainingData;
                return _dto;
            }
            set
            {
                SequenceBarDTO _dto = value;
                Order = _dto.Order.ToString();
                Person = _dto.Person;
                Scenario = _dto.Scenario;
                _sequence = _dto.Sequence;
                SimilarityRatio = _dto.SimilarityRatio;
                SequenceString = _dto.Sequence.ToString();
                SequenceLength = _dto.Sequence.Length.ToString();
                TrainingData = _dto.TrainingData;
                Id = _dto.ID;
            }
        }


        public string SequenceString
        {
            get
            {
                return $" {_sequenceString} ";
            }

            set
            {
                _sequenceString = value;
                onPropertyChanged();
            }
        }

        public String SequenceLength
        {
            get
            {
                return $" ({_sequenceLength})  ";
            }

            set
            {
                if (int.TryParse(value, out _sequenceLength))
                    onPropertyChanged();
            }
        }

        public string SimilarityRatio
        {
            get
            {
                return _showSimilarityRatio ? $"{_similarityRatio}" : "";
            }
            set
            {
                _similarityRatio = value;
                onPropertyChanged();
            }
        }

        public string Person
        {
            get
            {
                return _showPerson ? $" {_person}  " : "";
            }
            set
            {
                _person = value;
                onPropertyChanged();
            }
        }

        public String Order
        {
            get
            {
                return $"{_order.ToString()})  ";
            }
            set
            {
                if (int.TryParse(value, out _order))
                    onPropertyChanged();

            }
        }

        public Sequence<INode> Sequence
        {
            get
            {
                return _sequence;
            }

            set
            {
                _sequence = value;
                onPropertyChanged();
            }
        }

        public Scenario Scenario
        {
            get
            {
                return _scenario;
            }

            set
            {
                _scenario = value;
            }
        }

        public string GetPersonValue
        {
            get
            {
                return _person;
            }
        }

        public Guid Id { get; set; }

        public bool TrainingData
        {
            get
            {
                return _trainingData;
            }
            set
            {
                _trainingData = value;
                onPropertyChanged();
                lblTrainingSet.Visibility = _trainingData ? Visibility.Visible : Visibility.Collapsed;
                border.BorderBrush = new SolidColorBrush(_trainingData ? Colors.IndianRed : Colors.WhiteSmoke);
            }
        }
        public bool ShowData
        {
            get { return _showData; }
            set
            {
                _showData = value;
                onPropertyChanged();
            }
        }

        #endregion Properties

        #region Constactor

        public sequenceBar(SequenceBarDTO data, bool showPerson = false, bool showSimilarityRatio = false)
        {
            InitializeComponent();
            DataContext = this;

            DTO = data;
            _showPerson = showPerson;
            _showSimilarityRatio = showSimilarityRatio;
            Id = Guid.NewGuid();
            TrainingData = false;
            ShowData = true;
        }

        public sequenceBar(int order, Sequence<INode> sequence, Scenario scenario, bool showPerson = false, bool showSimilarityRatio = false, string similarityRatio = "0", string person = "Belirsiz")
        {
            InitializeComponent();
            DataContext = this;

            Order = order.ToString();
            Sequence = sequence;
            Scenario = scenario;
            _showPerson = showPerson;
            _showSimilarityRatio = showSimilarityRatio;

            SimilarityRatio = similarityRatio;
            SequenceString = sequence.ToString();
            SequenceLength = sequence.Length.ToString();
            Person = person;
            Id = Guid.NewGuid();
            TrainingData = false;
            ShowData = true;
        }

        #endregion Constactor

        #region Events
        private void btnScenarios_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Scenario.ToString());
        }

        #endregion Events

        #region Methods
        public void ChangeBackgroud(SolidColorBrush brush = null)
        {
            SolidColorBrush color = (brush == null) ? new SolidColorBrush(Colors.WhiteSmoke) : brush;
            this.Background = color;
        }

        #endregion Methods

        #region INotifyProperyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged([CallerMemberName] string propertyChanged = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyChanged));
        }

        #endregion
    }
}

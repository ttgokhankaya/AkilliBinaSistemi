using Adle.Analysis;
using Adle.Analysis.Rules;
using AdleGraph.Interfaces;
using GUI_Simulation.AnomalyExploration;
using SequentialPattern;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GUI_Simulation.SequencePattern
{
    /// <summary>
    /// Interaction logic for ResultBarControl.xaml
    /// </summary>
    public partial class ResultBarControl : UserControl, INotifyPropertyChanged
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

        private string _expectedNodeValue;
        private string _nodeValue;

        private string _expectedNodeProbability;
        private string _incomingNodeProbability;

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

        public string ExpectedNodeValue
        {
            get
            {
                return _expectedNodeValue;
            }

            set
            {
                _expectedNodeValue = value;
                onPropertyChanged();
            }
        }

        public string NodeValue
        {
            get
            {
                return _nodeValue;
            }

            set
            {
                _nodeValue = value;
                onPropertyChanged();
            }
        }

        public string ExpectedNodeProbability
        {
            get
            {
                return _expectedNodeProbability;
            }

            set
            {
                _expectedNodeProbability = value;
                onPropertyChanged();
            }
        }

        public string IncomingNodeProbability
        {
            get
            {
                return _incomingNodeProbability;
            }

            set
            {
                _incomingNodeProbability = value;
                onPropertyChanged();
            }
        }
        #endregion Properties

        #region Constactor

        public ResultBarControl(SequenceBarDTO data, string expectedNodeValue, string nodeValue)
        {
            InitializeComponent();
            DataContext = this;
            ExpectedNodeValue = expectedNodeValue;
            NodeValue = nodeValue;
            DTO = data;
            Id = Guid.NewGuid();
        }

        public ResultBarControl(int order, Sequence<INode> sequence, string expectedNodeValue, string nodeValue)
        {
            InitializeComponent();
            DataContext = this;

            Order = order.ToString();
            Sequence = sequence;

            SequenceString = sequence.ToString();
            SequenceLength = sequence.Length.ToString();
            ExpectedNodeValue = expectedNodeValue;
            NodeValue = nodeValue;
            Id = Guid.NewGuid();


        }

        public ResultBarControl(int order, Sequence<INode> sequence, string expectedNodeValue, string nodeValue, SolidColorBrush brush) : this(order, sequence, expectedNodeValue, nodeValue)
        {
            SolidColorBrush _brush = brush == null ? (new SolidColorBrush(expectedNodeValue == nodeValue ? Colors.LightGreen : Colors.LightPink)) : brush;
            ChangeBackgroud(_brush);
        }

        #endregion Constactor

        #region Events

        #endregion   Events

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

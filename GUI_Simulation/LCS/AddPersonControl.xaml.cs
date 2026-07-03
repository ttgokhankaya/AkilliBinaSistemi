using Adle.Analysis;
using Adle.Analysis.Rules;
using AdleGraph.Interfaces;
using GUI_Simulation.AnomalyExploration;
using GUI_Simulation.GraphSimulation;
using SequentialPattern;
using SimulationObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace GUI_Simulation.LCS
{
    /// <summary>
    /// Interaction logic for AddPersonControl.xaml
    /// </summary>
    public partial class AddPersonControl : UserControl, INotifyPropertyChanged
    {
        #region Ctor
        public AddPersonControl(int order, string person = "")
        {
            InitializeComponent();
            DataContext = this;
            this.order = order;
            ID = Guid.NewGuid();
            familyMember = !string.IsNullOrEmpty(person);
            this.person = person;
            Loaded += AddPersonControl_Loaded;
            GraphListRenewed();
        }

        #endregion Ctor

        #region Fields

        private List<IGraph> _graphList;

        private int _order;

        private List<string> _actionLog = new List<string>();

        private string _person;

        private IGraph _selectedGraph;

        private int _deviceCount;

        private bool _familyMember;

        private int _scenarioCount;

        private string _personString;

        private Sequence<INode> _sequence;

        #endregion Fields

        #region Properties
        public Guid ID { get; set; }

        public List<IGraph> GraphList
        {
            get
            {
                if (_graphList == null)
                    _graphList = new List<IGraph>();

                return _graphList;
            }

            private set
            {
                _graphList = value;

                GraphListRenewed();
            }
        }

        public List<DeviceBase> Devices { get; set; } = new List<DeviceBase>();

        public List<Sensor> sensors { get; set; } = new List<Sensor>();

        public List<Adle.Analysis.Scenario> scenarios { get; set; } = new List<Adle.Analysis.Scenario>();

        public int DeviceCount
        {
            get
            {
                return _deviceCount;
            }

            set
            {
                _deviceCount = value;
                onPropertyChanged();
            }
        }

        public int order
        {
            get
            {
                return _order;
            }

            set
            {
                _order = value;
                onPropertyChanged();
            }
        }

        public string person
        {
            get
            {
                if (string.IsNullOrEmpty(_person))
                {
                    return "belirsiz";
                }
                return _person;
            }

            set
            {
                AddToActionLogs($"{person} kişisinin adı {value} olarak değiştirildi.");
                _person = value;
                onPropertyChanged();
            }
        }

        public bool familyMember
        {
            get { return _familyMember; }
            set
            {
                btnTest.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                _familyMember = value;
                onPropertyChanged();
            }
        }

        public string PersonSequenceString
        {
            get
            {
                return _personString;
            }

            set
            {
                _personString = value;
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
            }
        }

        #endregion Properties

        #region Events

        public delegate void addedToActionLogHandler(object sender, LogAddedEventArgs e);
        public event addedToActionLogHandler OnMessageAdding;

        public delegate void closeControlHadler(object sender, RoutedEventArgs e);
        public event closeControlHadler OnClose;

        public delegate void showContentHadler(object sender, ShowContentEventArgs e);
        public event showContentHadler showContentEvent;

        public delegate void testHandler(object sender, testEventArgs<INode> e);
        public event testHandler test;

        private void AddPersonControl_Loaded(object sender, RoutedEventArgs e)
        {
            AddToActionLogs("Eklendi");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            AddToActionLogs("Çıkarıldı.");
            OnClose?.Invoke(this, new RoutedEventArgs());
        }

        private void btnLoadGraph_Click(object sender, RoutedEventArgs e)
        {
            MainWindowForGraph graphWindow = new MainWindowForGraph(true);


            if (graphWindow.returnGraphValue == null)
                return;

            if (GraphList.Exists(x => x.Name == graphWindow.returnGraphValue.Name))
                return;

            GraphList.Add(graphWindow.returnGraphValue);
            GraphListRenewed();
        }

        private void btnSelectGraph_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGraphs.SelectedItem == null)
            {
                MessageBox.Show("Lütfen Çizelge Seçiniz.");
                return;
            }

            if (((ListBoxItem)cmbGraphs.SelectedItem).Tag == null)
            {
                MessageBox.Show("Lütfen Çizelge Seçiniz.");
                return;
            }

            _selectedGraph = (IGraph)(((ListBoxItem)cmbGraphs.SelectedItem).Tag);
            LoadDevicesAndSensors();
            LoadNodes();

            AddToActionLogs($"{_selectedGraph.Name} seçildi.");
        }

        private void OnShowContentMethod(List<InputWindow> data = null, IList list = null, string contentToShow = "")
        {
            showContentEvent?.Invoke(this, new ShowContentEventArgs()
            {
                Data = data,
                ListToShow = list,
                ContentToShow = contentToShow
            });
        }

        private void btnsensors_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(list: sensors);
        }

        private void btnScenarios_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(list: scenarios);
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            Run();
            test?.Invoke(this, new testEventArgs<INode>() { sequence = Sequence, name = person });
        }

        private void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGraph == null)
            {
                MessageBox.Show("Lütfen ilk önce çizelge seçiniz.");
                return;
            }

            graphShow window = new graphShow();
            window.UpdateBoad(_selectedGraph);
            window.ShowDialog();
        }

        #endregion Events

        #region Private Methods

        private void GraphListRenewed()
        {
            cmbGraphs.Items.Clear();

            ListBoxItem firstItem = new ListBoxItem();
            firstItem.Content = GraphList.Count > 0 ? "Lütfen işlem yapmak istediğiniz çizelgeyi seçiniz."
                    : "Lütfen Çizelgeler butonu ile çizelge yükleyiniz.";
            firstItem.Tag = null;

            cmbGraphs.Items.Add(firstItem);
            cmbGraphs.SelectedIndex = 0;

            if (GraphList == null)
                return;

            foreach (var graph in GraphList)
            {
                ListBoxItem item = new ListBoxItem();
                item.Tag = graph;
                item.Content = graph.Name;
                cmbGraphs.Items.Add(item);
            }
        }

        private void AddToActionLogs(string message = "")
        {
            if (string.IsNullOrEmpty(message))
                return;

            string messsage = $"{DateTime.Now}: {person} {message}\n{ID}\n";
            _actionLog.Add(messsage);
            OnMessageAdding?.Invoke(this, new LogAddedEventArgs() { Message = messsage });
        }

        private void LoadDevicesAndSensors()
        {
            if (!checkSelectedGraph())
                return;

            Devices = new List<DeviceBase>();
            sensors = new List<Sensor>();

            for (int i = 0; i < _selectedGraph.NodeList.Count; i++)
            {
                var _device = (DeviceBase)_selectedGraph.NodeList[i].Tag;
                var _foundDevice = Devices.Where(x => x.Name == _device.Name).FirstOrDefault();
                if (_foundDevice != null)
                    continue;

                Devices.Add(_device);

                sensors.Add(new Sensor()
                {
                    ID = i + 1,
                    Name = _device.Name,
                    Type = _device.Type,
                    IP = _device.ip
                });
            }

            DeviceCount = Devices.Count;
        }

        private bool checkSelectedGraph()
        {
            if (_selectedGraph.NodeList == null)
            {
                MessageBox.Show($"Çizelgesi bulunamadı. Lüffen tekrar deneyiniz.");
                return false;
            }

            if (_selectedGraph.NodeList.Count <= 0)
            {
                MessageBox.Show($"{_selectedGraph.Name} çizelgesine eklenmiş düğüm bulunamadı. Lütfen uygun bir çizelge seçiniz.");
                return false;
            }

            if (_selectedGraph.EdgeList.Count <= 0)
            {
                MessageBox.Show($"{_selectedGraph.Name} çizelgesine eklenmiş kenar bulunamadı. Lütfen uygun bir çizelge seçiniz.");
                return false;
            }

            return true;
        }

        private void LoadNodes()
        {
            if (!checkSelectedGraph())
                return;

            cmbFirstNode.Items.Clear();
            cmbLastNode.Items.Clear();

            foreach (var node in _selectedGraph.NodeList)
            {
                ListBoxItem item = new ListBoxItem();
                item.Tag = node;
                item.Content = node.Name;
                cmbFirstNode.Items.Add(item);

                item = new ListBoxItem();
                item.Tag = node;
                item.Content = node.Name;
                cmbLastNode.Items.Add(item);
            }

            cmbFirstNode.SelectedIndex = 0;
            cmbLastNode.SelectedIndex = _selectedGraph.NodeList.Count - 1;
        }

        private bool convertSequenceToScenarios(List<List<INode>> sequences)
        {
            if (sequences == null || sequences?.Count <= 0)
            {
                AddToActionLogs("Sequence Oluşmamış. Lütfen Öncelikle çizelge seçerek başlat tuşuna basın");
                return false;
            }

            scenarios = new List<Adle.Analysis.Scenario>();
            for (int i = 0; i < _scenarioCount; i++)
            {
                Adle.Analysis.Scenario newScenario = new Adle.Analysis.Scenario()
                { name = $"{i + 1}. Senaryo" };

                for (int j = 0; j < sequences[i].Count; j++)
                {
                    var foundSensor = sensors.Where(x => x.Name == ((DeviceBase)sequences[i][j].Tag).Name).FirstOrDefault();
                    if (foundSensor != null)
                        newScenario.sensors.Add(foundSensor);
                }

                scenarios.Add(newScenario);
            }
            return true;
        }

        private bool convertGraphSequencesToSequencePattern(List<List<INode>> sequences)
        {
            if (sequences == null || sequences.Count <= 0)
                return false;

            Sequence = new Sequence<INode>();
            foreach (var itemSet in sequences)
            {
                ItemSet<INode> set = new ItemSet<INode>();
                foreach (var item in itemSet)
                {
                    set.Add(item, true);
                }
                Sequence.Add(set);
            }

            PersonSequenceString = Sequence.ToString();
            return true;
        }

        #endregion Private Methods

        #region Public Methods

        public bool Run(int scenarioCount = 25)
        {
            if (cmbFirstNode.SelectedItem == null)
                return false;
            if (cmbLastNode.SelectedItem == null)
                return false;

            if (_selectedGraph == null || _selectedGraph?.NodeList?.Count <= 0)
            {
                AddToActionLogs($"Lütfen geçerli bir çizelge seçiniz.");
                return false;
            }

            _scenarioCount = scenarioCount;

            AddLogicalChild("Koşum başladı.");

            INode startNode = (INode)((ListBoxItem)cmbFirstNode.SelectedItem).Tag;
            INode stopNode = (INode)((ListBoxItem)cmbLastNode.SelectedItem).Tag;

            var sequences = MainWindowForGraph.RunGraph(_selectedGraph, scenarioCount, startNode, stopNode);

            if (!convertSequenceToScenarios(sequences)) return false;
            if (!convertGraphSequencesToSequencePattern(sequences)) return false;

            AddToActionLogs("Koşum tamamlandı.");
            return true;
        }

        public int testLCS(Sequence<INode> sequence)
        {
            if (sequence == null || sequence.Count <= 0)
                return -1;

            if (this.Sequence == null || this.Sequence.Count <= 0)
                Run();

            int LCSValue = Sequence.CalculateLCS(sequence);
            return LCSValue;
        }

        public string FindLCS(Sequence<INode> sequence)
        {
            if (sequence == null || sequence.Count <= 0)
                return "";

            if (this.Sequence == null || this.Sequence.Count <= 0)
                Run();

            var LCSList = Sequence.FindLCS(sequence);

            string builder = string.Join(",", LCSList);

            return builder;
        }


        #endregion Public Methods       

        #region INotifyProperyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged([CallerMemberName] string propertyChanged = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyChanged));
        }

        #endregion
    }
}
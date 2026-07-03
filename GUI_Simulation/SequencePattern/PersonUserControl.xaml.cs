using Adle.Analysis;
using Adle.Analysis.Rules;
using AdleGraph.Interfaces;
using GUI_Simulation.AnomalyExploration;
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

namespace GUI_Simulation.SequencePattern
{
    /// <summary>
    /// Interaction logic for PersonUserControl.xaml
    /// </summary>
    public partial class PersonUserControl : UserControl, INotifyPropertyChanged
    {
        #region Constractor
        public PersonUserControl(int order, string person = "")
        {
            InitializeComponent();
            DataContext = this;
            Order = order;
            ID = Guid.NewGuid();
            FamilyMember = !string.IsNullOrEmpty(person);
            Person = person;
            this.Loaded += PersonUserControl_Loaded;
            GraphListRenewed();
            _currentNode = null;
        }

        #endregion Constractor

        #region Fields

        private int _order;
        private bool _familyMember;
        private string _person;
        private List<IGraph> _graphList;
        private IGraph _selectedGraph;
        private int _deviceCount;
        private List<string> _actionLog = new List<string>();
        private int _scenarioCount;
        private Sequence<INode> _sequence;
        private List<INode> _graphSequence;

        private INode _currentNode;
        private INode _startNode;
        private INode _endNode;
        private string _lastGraphName;


        #endregion Fields

        #region Properties

        public Guid ID { get; set; }

        public int Order
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

        public bool FamilyMember
        {
            get
            {
                return _familyMember;
            }
            set
            {
                _familyMember = value;
                btnRunTest.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                btnNext.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                btnBack.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                onPropertyChanged();
            }
        }

        public string Person
        {
            get
            {
                return string.IsNullOrEmpty(_person) ? "belirsiz" : _person;
            }
            set
            {
                _person = value;
                onPropertyChanged();
            }
        }

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

        public List<DeviceBase> Devices { get; set; }

        public List<Sensor> Sensors { get; set; }
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

        public List<Sequence<INode>> SequenceList { get; set; } = new List<Sequence<INode>>();

        public List<Adle.Analysis.Scenario> Scenarios { get; set; } = new List<Adle.Analysis.Scenario>();

        public IGraph SelectedGraph
        {
            get
            {
                return _selectedGraph;
            }

            set
            {
                _selectedGraph = value;
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
        private void PersonUserControl_Loaded(object sender, RoutedEventArgs e)
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

            SelectedGraph = (IGraph)(((ListBoxItem)cmbGraphs.SelectedItem).Tag);
            LoadDevicesAndSensors();
            LoadNodes();

            AddToActionLogs($"{SelectedGraph.Name} seçildi.");
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGraph == null)
            {
                MessageBox.Show("Lütfen ilk önce çizelge seçiniz.");
                return;
            }

            if (_currentNode == null)
            {
                MessageBox.Show("Eklenmiş düğüm yok.");
                return;
            }

            if (Sequence[0].Count <= 1)
            {
                MessageBox.Show("Geri gidiş alanı bulunmamaktadır.");
                return;
            }
            if (!checkStartAndEndNodes()) return;

            var lastSequenceItem = Sequence[0][Sequence[0].Count - 1].Value;

            if (lastSequenceItem == _startNode)
            {
                MessageBox.Show("Geri gidiş alanı bulunmamaktadır.");
                return;
            }


            Sequence[0].RemoveAt(Sequence[0].Count - 1);
            _graphSequence.RemoveAt(_graphSequence.Count - 1);

            _currentNode = Sequence[0][Sequence[0].Count - 1].Value;



            MoveBack?.Invoke(this, new MoveTestEventArgs<INode>() { name = Person, Node = _currentNode, sequence = Sequence, beginNewSequence = false, scenario = null, GraphSequence = _graphSequence, graph = SelectedGraph, startNode = _startNode, EndNode = _endNode });
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGraph == null)
            {
                MessageBox.Show("Lütfen ilk önce çizelge seçiniz.");
                return;
            }

            if (!checkStartAndEndNodes()) return;

            if (!string.IsNullOrEmpty(_lastGraphName) && SelectedGraph.Name != _lastGraphName)
            {
                if (MessageBox.Show($"{_lastGraphName} Çizelgesini {SelectedGraph.Name} çizelgesi ile değiştirmek istediğinize emin misiniz?\nDikkat! Mevcut {Sequence.ToString()} dizisi kaybedersiniz.", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    return;

                Sequence = new Sequence<INode>();
                _currentNode = null;
            }

            if (_currentNode == null)
                _currentNode = _startNode;

            _lastGraphName = SelectedGraph.Name;

            if (Sequence == null)
                Sequence = new Sequence<INode>();

            INode newNode = _startNode;
            bool isNewSequence = this.isNewSequence();

            if (!isNewSequence)
                newNode = SelectedGraph.MoveNext(_currentNode, true);

            if (newNode == null)
                return;

            var foundSensor = Sensors.Where(x => x.Name == ((DeviceBase)newNode.Tag).Name).FirstOrDefault();
            Adle.Analysis.Scenario scenarioToSent = null;

            if (isNewSequence)
            {
                Adle.Analysis.Scenario newScenario = new Adle.Analysis.Scenario()
                { name = $"{Scenarios.Count + 1 }. Senaryo" };
                newScenario.sensors.Add(foundSensor);
                Scenarios.Add(newScenario);

                scenarioToSent = newScenario;
                Sequence = new Sequence<INode>();
                Sequence.AddItemAsItemSet(newNode);
                SequenceList.Add(Sequence);


                _graphSequence = new List<INode>();
            }
            else
            {
                if (foundSensor != null)
                    Scenarios[Scenarios.Count - 1].sensors.Add(foundSensor);

                scenarioToSent = Scenarios[Scenarios.Count - 1];
                Sequence.AddItemToItemSet(newNode, 0);
                SequenceList[SequenceList.Count - 1] = Sequence;
            }

            _graphSequence.Add(newNode);

            MoveNext?.Invoke(this, new MoveTestEventArgs<INode>() { name = Person, Node = newNode, sequence = Sequence, beginNewSequence = isNewSequence, scenario = scenarioToSent, GraphSequence = _graphSequence, graph = SelectedGraph, startNode = _startNode, EndNode = _endNode });

            _currentNode = newNode;
        }

        private bool isNewSequence()
        {
            return _currentNode.Name == _endNode.Name || Sequence.Count == 0;
        }

        private void btnRunTest_Click(object sender, RoutedEventArgs e)
        {
            Run(1);

            if (SequenceList?.Count == 0 || Scenarios?.Count == 0) return;

            if (!checkStartAndEndNodes()) return;

            Sequence = SequenceList[SequenceList.Count - 1];
            var scenario = Scenarios[Scenarios.Count - 1];

            RunTest?.Invoke(this, new testEventArgs<INode>() { sequence = Sequence, name = Person, graph = SelectedGraph, startNode = _startNode, EndNode = _endNode, GraphSequence = _graphSequence, scenario = scenario });

            _currentNode = _graphSequence.Last();
        }

        private void btnShowSensors_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(list: Sensors);
        }

        private void btnScnarios_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(list: Scenarios);
        }

        private void btnShowGraph_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGraph == null)
            {
                MessageBox.Show("Lütfen ilk önce çizelge seçiniz.");
                return;
            }

            GraphSimulation.graphShow window = new GraphSimulation.graphShow();
            window.UpdateBoad(_selectedGraph);
            window.ShowDialog();
        }

        private void brnShowSequence_Click(object sender, RoutedEventArgs e)
        {
            if (SequenceList == null || SequenceList.Count >= 0)
            {
                MessageBox.Show("Kayıtlı dizi yok.");
                return;
            }

            OnShowContentMethod(list: SequenceList);
        }

        #endregion Events

        #region Event Declerations
        public delegate void closeControlHadler(object sender, RoutedEventArgs e);
        public event closeControlHadler OnClose;

        public delegate void showContentHadler(object sender, ShowContentEventArgs e);
        public event showContentHadler showContentEvent;

        public delegate void MoveNextHanler(object sender, MoveTestEventArgs<INode> e);
        public event MoveNextHanler MoveNext;

        public delegate void MoveBackHandler(object sender, MoveTestEventArgs<INode> e);
        public event MoveBackHandler MoveBack;

        public delegate void testHandler(object sender, testEventArgs<INode> e);
        public event testHandler RunTest;

        public delegate void addedToActionLogHandler(object sender, LogAddedEventArgs e);
        public event addedToActionLogHandler OnMessageAdding;

        #endregion Event Declerations

        #region Public Methods
        public bool Run(int scenarioCount = 25)
        {
            if (!checkStartAndEndNodes()) return false;

            if (SelectedGraph == null || SelectedGraph?.NodeList?.Count <= 0)
            {
                AddToActionLogs($"Lütfen geçerli bir çizelge seçiniz.");
                return false;
            }

            _scenarioCount = scenarioCount;
            SequenceList = new List<Sequence<INode>>();

            AddLogicalChild("Koşum başladı.");


            var sequences = MainWindowForGraph.RunGraph(SelectedGraph, scenarioCount, _startNode, _endNode);
            _graphSequence = sequences[0];

            if (!convertSequenceToScenarios(sequences)) return false;
            if (!convertGraphSequencesToSequencePattern(sequences)) return false;

            AddToActionLogs("Koşum tamamlandı.");
            return true;
        }

        public bool Reset()
        {
            Sequence = new Sequence<INode>();
            Scenarios = new List<Adle.Analysis.Scenario>();
            SequenceList = new List<Sequence<INode>>();
            _currentNode = null;
            return true;
        }


        #endregion Public Methods

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

            string messsage = $"{DateTime.Now}: {Person} {message} - ({ID})\n";
            _actionLog.Add(messsage);
            OnMessageAdding?.Invoke(this, new LogAddedEventArgs() { Message = messsage });
        }

        private void LoadNodes()
        {
            if (!checkSelectedGraph())
                return;

            cmbFirstNode.Items.Clear();
            cmbLastNode.Items.Clear();

            var nodeList = SelectedGraph.NodeList.OrderByDescending(x => x.Name).ToList();

            foreach (var node in nodeList)
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
            cmbLastNode.SelectedIndex = SelectedGraph.NodeList.Count - 1;
        }

        private void LoadDevicesAndSensors()
        {
            string notFoundDevices = "";

            if (!checkSelectedGraph())
                return;

            Devices = new List<DeviceBase>();
            Sensors = new List<Sensor>();

            for (int i = 0; i < SelectedGraph.NodeList.Count; i++)
            {
                var _device = (DeviceBase)SelectedGraph.NodeList[i].Tag;
                if (_device == null)
                {
                    notFoundDevices += $" {SelectedGraph.NodeList[i].ToString()} ";
                    continue;
                }

                var _foundDevice = Devices.Where(x => x.Name == _device.Name).FirstOrDefault();

                if (_foundDevice != null) continue;

                Devices.Add(_device);

                Sensors.Add(new Sensor()
                {
                    ID = i + 1,
                    Name = _device.Name,
                    Type = _device.Type,
                    IP = _device.ip
                });
            }

            if (!string.IsNullOrEmpty(notFoundDevices)) MessageBox.Show(notFoundDevices);

            DeviceCount = Devices.Count;
        }

        private bool checkSelectedGraph()
        {
            if (SelectedGraph.NodeList == null)
            {
                MessageBox.Show($"Çizelgesi bulunamadı. Lüffen tekrar deneyiniz.");
                return false;
            }

            if (SelectedGraph.NodeList.Count <= 0)
            {
                MessageBox.Show($"{SelectedGraph.Name} çizelgesine eklenmiş düğüm bulunamadı. Lütfen uygun bir çizelge seçiniz.");
                return false;
            }

            if (SelectedGraph.EdgeList.Count <= 0)
            {
                MessageBox.Show($"{SelectedGraph.Name} çizelgesine eklenmiş kenar bulunamadı. Lütfen uygun bir çizelge seçiniz.");
                return false;
            }

            return true;
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

        private bool convertSequenceToScenarios(List<List<INode>> sequences)
        {
            if (sequences == null || sequences?.Count <= 0)
            {
                AddToActionLogs("Sequence Oluşmamış. Lütfen Öncelikle çizelge seçerek başlat tuşuna basın");
                return false;
            }

            Scenarios = new List<Adle.Analysis.Scenario>();
            for (int i = 0; i < _scenarioCount; i++)
            {
                Adle.Analysis.Scenario newScenario = new Adle.Analysis.Scenario()
                { name = $"{i + 1}. Senaryo" };

                for (int j = 0; j < sequences[i].Count; j++)
                {
                    var foundSensor = Sensors.Where(x => x.Name == ((DeviceBase)sequences[i][j].Tag).Name).FirstOrDefault();
                    if (foundSensor != null)
                        newScenario.sensors.Add(foundSensor);
                }

                Scenarios.Add(newScenario);
            }
            return true;
        }

        private bool convertGraphSequencesToSequencePattern(List<List<INode>> sequences)
        {
            if (sequences == null || sequences.Count <= 0)
                return false;

            foreach (var itemSet in sequences)
            {
                Sequence<INode> Sequence = new Sequence<INode>();
                ItemSet<INode> set = new ItemSet<INode>();
                foreach (var item in itemSet)
                {
                    set.Add(item, true);
                }
                Sequence.Add(set);
                SequenceList.Add(Sequence);
            }

            return true;
        }

        private bool checkStartAndEndNodes()
        {
            bool result = (cmbLastNode.SelectedItem == null || !(cmbLastNode.SelectedItem is ListBoxItem) || ((ListBoxItem)cmbLastNode.SelectedItem)?.Tag == null || !(((ListBoxItem)cmbLastNode.SelectedItem).Tag is INode));

            if (result)
            {
                MessageBox.Show("Başlangıç ve bitiş düğümlerinin seçilmesi gerekli.");
                return false;
            }


            _startNode = (INode)((ListBoxItem)cmbFirstNode.SelectedItem).Tag;
            _endNode = (INode)((ListBoxItem)cmbLastNode.SelectedItem).Tag;
            result = _startNode.Name == _endNode.Name;

            if (result)
            {
                MessageBox.Show("Başlangıç ve bitiş düğümleri aynı olamaz.");
                return false;
            }

            return true;
        }

        #endregion Private Methods

        #region INotifyProperyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged([CallerMemberName] string propertyChanged = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyChanged));
        }

        #endregion
    }
}

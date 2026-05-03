using AdleGraph.Interfaces;
using SimulationObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GUI_Simulation.AnomalyExploration
{
    public partial class graphSelectionControl : UserControl, INotifyPropertyChanged
    {
        #region Ctor
        public graphSelectionControl(int order)
        {
            InitializeComponent();
            ID = Guid.NewGuid();
            Order = order;
            this.DataContext = this;
            this.Loaded += GraphSelectionControl_Loaded;
            IsMainGraph = false;
            GraphListRenewed();
        }
        #endregion Ctor

        #region Field
        private List<IGraph> _graphList;
        private IGraph _selectedGraph;
        private int _deviceCount;
        private int _order;
        private int _scenarioCount = 20;
        private string _randomForestDetails = "";
        private int _countOfDecisionTreesInForest = 5;
        private int _inputWindowLength = 10;
        private List<string> _actionLog = new List<string>();
        private bool isMainGraph;
        #endregion Field

        #region Properties
        public Guid ID { get; set; }
        public int Order
        {
            get { return _order; }
            set { _order = value; propertyChanged(); }
        }

        public string GraphName { get { return _selectedGraph != null ? _selectedGraph.Name : ""; } }
        #endregion Properties

        #region Algorithm Properties
        public List<Sensor> sensors { get; set; } = new List<Sensor>();
        public List<Scenario> scenarios { get; set; } = new List<Scenario>();
        public List<InputWindow> s0 { get; set; } = new List<InputWindow>();
        public List<InputWindow> s1 { get; set; } = new List<InputWindow>();
        public double[][] proximityMatrix { get; set; }
        public int ScenarioCount
        {
            get { return _scenarioCount; }
            set { _scenarioCount = value; }
        }
        public List<DeviceBase> Devices { get; set; } = new List<DeviceBase>();
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
        public int DeviceCount
        {
            get { return _deviceCount; }
            set { _deviceCount = value; propertyChanged(); }
        }
        public int CountOfDecisionTreesInForest
        {
            get { return _countOfDecisionTreesInForest; }
            set { _countOfDecisionTreesInForest = value; propertyChanged(); }
        }
        public int InputWindowLength
        {
            get { return _inputWindowLength; }
            set { _inputWindowLength = value; propertyChanged(); }
        }
        public bool IsMainGraph
        {
            get { return isMainGraph; }
            set
            {
                if (IsMainGraph == value) return;
                isMainGraph = value;
                propertyChanged();
                onMainConfigChanged?.Invoke(this, new RoutedEventArgs());
            }
        }
        #endregion Algorithm Properties

        #region Events
        public delegate void closeControlHadler(object sender, RoutedEventArgs e);
        public event closeControlHadler OnClose;
        public delegate void showContentHadler(object sender, ShowContentEventArgs e);
        public event showContentHadler showContentEvent;
        public delegate void startRuningHandler(object sender, RoutedEventArgs e);
        public event startRuningHandler onStartRuning;
        public delegate void endRuningHandler(object sender, ResultEventArgs e);
        public event endRuningHandler onEndRuning;
        public delegate void addedToActionLogHandler(object sender, LogAddedEventArgs e);
        public event addedToActionLogHandler OnMessageAdding;
        public delegate void isConfigChangedHandler(object sender, RoutedEventArgs e);
        public event isConfigChangedHandler onMainConfigChanged;

        private void GraphSelectionControl_Loaded(object sender, RoutedEventArgs e)
        {
            AddToActionLogs("Eklendi.");
        }

        private void btnCloseControl_Click(object sender, RoutedEventArgs e)
        {
            AddToActionLogs("Çıkarıldı.");
            OnClose?.Invoke(this, new RoutedEventArgs());
        }

        private void btnGraphManagement_Click(object sender, RoutedEventArgs e)
        {
            MainWindowForGraph graphWindow = new MainWindowForGraph();
            graphWindow.ShowDialog();
            GraphList = graphWindow.GraphList;
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

        private void btnSeonsors_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(list: sensors);
        }

        private void btnScenarios_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(list: scenarios);
        }

        private void btnS0_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(data: s0);
        }

        private void btnS1_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(data: s1);
        }

        private void btnRF_Click(object sender, RoutedEventArgs e)
        {
            OnShowContentMethod(contentToShow: string.IsNullOrEmpty(_randomForestDetails) ? "Gösterecek eleman yok." : _randomForestDetails);
        }
        #endregion Events

        #region Public Methods
        public bool Run(int scenarioCount = 20, int InputWindowLength = 10, int CountOfDecisionTreesInForest = 5, List<InputWindow> S1 = null)
        {
            if (cmbFirstNode.SelectedItem == null) return false;
            if (cmbLastNode.SelectedItem == null) return false;

            s1 = null;

            if (S1 != null)
                s1 = S1;

            _scenarioCount = scenarioCount;
            _inputWindowLength = InputWindowLength;
            _countOfDecisionTreesInForest = CountOfDecisionTreesInForest;

            if (_selectedGraph == null || _selectedGraph?.NodeList?.Count <= 0)
            {
                AddToActionLogs("Lütfen geçerli bir çizelge seçiniz.");
                return false;
            }

            AddLogicalChild("Koşum başladı.");
            INode startNode = (INode)((ListBoxItem)cmbFirstNode.SelectedItem).Tag;
            INode stopNode = (INode)((ListBoxItem)cmbLastNode.SelectedItem).Tag;
            var sequences = MainWindowForGraph.RunGraph(_selectedGraph, scenarioCount, startNode, stopNode);

            if (!convertSequenceToScenarios(sequences)) return false;
            if (!createS0()) return false;
            if (!createS1()) return false;
            if (!createRandomForestAndProximity()) return false;

            AddToActionLogs("Koşum tamamlandı.");
            return true;
        }

        private void AddToActionLogs(string message = "")
        {
            if (string.IsNullOrEmpty(message)) return;
            string messsage = $"{DateTime.Now}:\t{Order}\t{message} - id:({ID})";
            _actionLog.Add(messsage);
            OnMessageAdding?.Invoke(this, new LogAddedEventArgs() { Message = messsage });
        }
        #endregion Public Methods

        #region Private Methods
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

            scenarios = new List<Scenario>();
            for (int i = 0; i < ScenarioCount; i++)
            {
                Scenario newScenario = new Scenario() { name = $"{i + 1}. Senaryo" };
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

        private void GraphListRenewed()
        {
            cmbGraphs.Items.Clear();
            ListBoxItem firstItem = new ListBoxItem();
            firstItem.Content = GraphList.Count > 0 ? "Lütfen işlem yapmak istediğiniz çizelgeyi seçiniz."
                    : "Lütfen Çizelgeler butonu ile çizelge yükleyiniz.";
            firstItem.Tag = null;
            cmbGraphs.Items.Add(firstItem);
            cmbGraphs.SelectedIndex = 0;

            if (GraphList == null) return;
            foreach (var graph in GraphList)
            {
                ListBoxItem item = new ListBoxItem();
                item.Tag = graph;
                item.Content = graph.Name;
                cmbGraphs.Items.Add(item);
            }
        }

        private void LoadNodes()
        {
            if (!checkSelectedGraph()) return;
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

        private void LoadDevicesAndSensors()
        {
            if (!checkSelectedGraph()) return;
            Devices = new List<DeviceBase>();
            sensors = new List<Sensor>();
            for (int i = 0; i < _selectedGraph.NodeList.Count; i++)
            {
                var _device = (DeviceBase)_selectedGraph.NodeList[i].Tag;
                var _foundDevice = Devices.Where(x => x.Name == _device.Name).FirstOrDefault();
                if (_foundDevice != null) continue;
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
                MessageBox.Show("Çizelgesi bulunamadı. Lüffen tekrar deneyiniz.");
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
        #endregion Private Methods

        #region Algorithm Methods
        private bool createS0()
        {
            if (scenarios == null || scenarios.Count <= 0)
            {
                AddToActionLogs("Senaryo oluşmamış. Lütfen seçmediyseniz çizelge seçerek başlat tuşuna basın.");
                return false;
            }

            InputWindow window = new InputWindow(sensors.Count, _inputWindowLength);
            window.order = 1;
            int count = 0;
            int step = 0;
            s0 = new List<InputWindow>();
            foreach (var scenario in scenarios)
            {
                foreach (var sensor in scenario.sensors)
                {
                    if (step == _inputWindowLength)
                    {
                        s0.Add(window);
                        int lastOrder = window.order;
                        window = new InputWindow(sensors.Count, _inputWindowLength);
                        window.order = lastOrder + 1;
                        step = 0;
                    }
                    window.states[sensor.ID - 1][step] = 1.0;
                    step++;
                    count++;
                    if (count == scenarios.Count)
                        s0.Add(window);
                }
            }
            foreach (var data in s0)
                data.ConvolveWindow();

            AddToActionLogs("S0 oluştu.");
            return true;
        }

        private bool createS1()
        {
            if (s1 != null) return true;
            if (s0 == null || s0.Count <= 0)
            {
                AddToActionLogs("S0 oluşmamış lütfen öncelikle çizelce seçerek başlat tuşuna basınız");
                return false;
            }

            var copyOfS0 = new List<InputWindow>(s0);
            s1 = new List<InputWindow>();
            while (copyOfS0.Count > 0)
            {
                int selected = Random.Shared.Next(0, copyOfS0.Count);
                s1.Add(copyOfS0[selected]);
                copyOfS0.RemoveAt(selected);
            }
            foreach (var data in s1)
                data.ConvolveWindow();

            AddToActionLogs("S1 oluştu");
            return true;
        }

        private bool createRandomForestAndProximity()
        {
            try
            {
                var s0Flat = FlattenWindows(s0);
                var s1Flat = FlattenWindows(s1);
                var allFlat = s0Flat.Concat(s1Flat).ToArray();

                RandomForestResponse result = Task.Run(() =>
                    MlServiceClient.ComputeRandomForestAsync(s0Flat, s1Flat, allFlat, _countOfDecisionTreesInForest)
                ).GetAwaiter().GetResult();

                proximityMatrix = result.proximity_matrix;

                _randomForestDetails = $"{Order} ({ID})\n\t1 for S0, 0 for S1\n\t-----------------------\n\t";
                if (result.tree_texts != null)
                    for (int i = 0; i < result.tree_texts.Length; i++)
                        _randomForestDetails += $"{i + 1}. tree in forest\n{result.tree_texts[i]}\n";

                AddToActionLogs("Random forest ve yakınlık matrisi oluştu.");
                return true;
            }
            catch (Exception ex)
            {
                AddToActionLogs($"ML servisi hatası: {ex.Message}");
                return false;
            }
        }

        private double[][] FlattenWindows(List<InputWindow> windows)
        {
            var rows = new List<double[]>();
            foreach (var w in windows)
                foreach (var row in w.convolvedStates)
                    rows.Add(row);
            return rows.ToArray();
        }

        private bool showInGraph()
        {
            if (proximityMatrix == null || proximityMatrix.Length <= 0)
            {
                AddToActionLogs("Yakınlık matrisi hesaplanmamış.\nLütfen cizelge seçip başka tuşuna basınız.");
                return false;
            }
            GraficView form = new GraficView(new List<ObservationSet>()
            {
                new ObservationSet() { Observations = proximityMatrix, Name = _selectedGraph.Name }
            });
            form.ShowDialog();
            return true;
        }
        #endregion

        #region PropertyChagedInterfaceImplementaion
        public event PropertyChangedEventHandler PropertyChanged;

        private void propertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion PropertyChagedInterfaceImplementaion
    }
}

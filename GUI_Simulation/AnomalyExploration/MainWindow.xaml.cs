using Adle.Analysis;
using Scenario = Adle.Analysis.Scenario;
using Sensor = Adle.Analysis.Sensor;
using Adle.Analysis.Rules;
using AdleGraph.Interfaces;
using SimulationObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GUI_Simulation.AnomalyExploration
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Ctor
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            GraphList = new List<IGraph>();
        }
        #endregion Ctor

        #region Fields
        private List<IGraph> _graphList;
        private IGraph _selectedGRaph;
        private int _deviceCount;
        private int _inputWindowLength = 10;
        private int _countOfDecisionTreesInForest = 5;
        private int _scenarioCount = 20;
        private string _randomForestDetails = "";
        #endregion

        #region Properties
        public List<DeviceBase> Devices { get; set; }
        public List<IGraph> GraphList
        {
            get { return _graphList; }
            private set { _graphList = value; GraphListRenewed(); }
        }

        public int DeviceCount
        {
            get { return _deviceCount; }
            set { _deviceCount = value; propertyChanged(); }
        }

        public int InputWindowLength
        {
            get { return _inputWindowLength; }
            set { _inputWindowLength = value; propertyChanged(); }
        }

        public int CountOfDecisionTreesInForest
        {
            get { return _countOfDecisionTreesInForest; }
            set { _countOfDecisionTreesInForest = value; propertyChanged(); }
        }

        public int SenaryoCount
        {
            get { return _scenarioCount; }
            set { _scenarioCount = value; propertyChanged(); }
        }

        #region Algorithm Properties
        public List<Sensor> sensors { get; set; } = new List<Sensor>();
        public List<Scenario> scenarios { get; set; } = new List<Scenario>();
        public List<InputWindow> s0 { get; set; } = new List<InputWindow>();
        public List<InputWindow> s1 { get; set; } = new List<InputWindow>();
        public double[][] proximityMatrix { get; set; }
        #endregion Algorithm Properties
        #endregion Properties

        #region Events
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

            _selectedGRaph = (IGraph)(((ListBoxItem)cmbGraphs.SelectedItem).Tag);
            Devices = new List<DeviceBase>();
            sensors = new List<Sensor>();

            for (int i = 0; i < _selectedGRaph.NodeList.Count; i++)
            {
                var _device = (DeviceBase)_selectedGRaph.NodeList[i].Tag;
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
            btnShowSensors_Click(null, null);
        }

        private void btnManageGraphs_Click(object sender, RoutedEventArgs e)
        {
            MainWindowForGraph graphWindow = new MainWindowForGraph();
            graphWindow.ShowDialog();
            GraphList = graphWindow.GraphList;
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGRaph == null || _selectedGRaph?.NodeList?.Count <= 0)
            {
                MessageBox.Show("Lütfen geçerli bir çizelge seçiniz.");
                return;
            }

            INode startNode = _selectedGRaph.NodeList.Find(x => x.Name.StartsWith("8P"));
            INode stopNode = _selectedGRaph.NodeList.Find(x => x.Name.StartsWith("10P"));
            var sequences = MainWindowForGraph.RunGraph(_selectedGRaph, SenaryoCount, startNode, stopNode);

            if (!convertSequenceToScenarios(sequences)) return;
            if (!createS0()) return;
            if (!createS1()) return;
            if (!createRandomForestAndProximity()) return;

            btnShowSenaryo_Click(null, null);
        }

        private void btnShowSensors_Click(object sender, RoutedEventArgs e)
        {
            FillList(sensors);
        }

        private void btnShowSenaryo_Click(object sender, RoutedEventArgs e)
        {
            FillList(scenarios);
        }

        private void btnS0_Click(object sender, RoutedEventArgs e)
        {
            FillListWithData(s0);
        }

        private void btnS1_Click(object sender, RoutedEventArgs e)
        {
            FillListWithData(s1);
        }

        private void btnRF_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_randomForestDetails))
            {
                MessageBox.Show("Karar ağaçları oluşmamış. Lütfen öncelikle cizelge seçerek başla tuşuna basınız.");
                return;
            }
            new DecisionTreeShown(_randomForestDetails).ShowDialog();
        }

        private void btnGrafic_Click(object sender, RoutedEventArgs e)
        {
            if (!showInGraph()) return;
        }
        #endregion Events

        #region Methods
        private void GraphListRenewed()
        {
            cmbGraphs.Items.Clear();
            ListBoxItem firstItem = new ListBoxItem();
            firstItem.Content = _graphList.Count > 0 ? "Lütfen işlem yapmak istediğiniz çizelgeyi seçiniz."
                    : "Lütfen Çizelgeler butonu ile çizelge yükleyiniz.";
            firstItem.Tag = null;
            cmbGraphs.Items.Add(firstItem);
            cmbGraphs.SelectedIndex = 0;

            foreach (var graph in _graphList)
            {
                ListBoxItem item = new ListBoxItem();
                item.Tag = graph;
                item.Content = graph.Name;
                cmbGraphs.Items.Add(item);
            }
        }

        private void FillList(IList list)
        {
            ArrayList itemsList = new ArrayList();
            if (list?.Count <= 0)
            {
                itemsList.Add("Gösterilecek eleman yok.");
                lvList.ItemsSource = itemsList;
                return;
            }
            foreach (var item in list)
                itemsList.Add(item.ToString());
            lvList.ItemsSource = itemsList;
        }

        private void FillListWithData(List<InputWindow> data)
        {
            ArrayList itemsList = new ArrayList();
            if (data == null || data?.Count <= 0)
            {
                itemsList.Add("Gösterilecek eleman yok.");
                lvList.ItemsSource = itemsList;
                return;
            }
            itemsList.Add("|----------- Time ----------->");
            for (int i = 0; i < sensors.Count; i++)
            {
                StringBuilder line = new StringBuilder();
                line.Append($"{i + 1}. sensor:\t");
                foreach (var windowInData in data)
                    for (int j = 0; j < windowInData.states[i].Length; j++)
                        line.Append(windowInData.states[i][j]);
                itemsList.Add(line.ToString());
            }
            lvList.ItemsSource = itemsList;
        }

        private bool convertSequenceToScenarios(List<List<INode>> sequences)
        {
            if (sequences?.Count <= 0)
            {
                MessageBox.Show("Sequence Oluşmamış. Lütfen Öncelikle çizelge seçerek başlat tuşuna basın");
                return false;
            }
            scenarios = new List<Scenario>();
            for (int i = 0; i < _scenarioCount; i++)
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

        private bool createS0()
        {
            if (scenarios == null || scenarios.Count <= 0)
            {
                MessageBox.Show("Senaryo oluşmamış.\nLütfen seçmediyseniz çizelge seçerek başlat tuşuna basın.");
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
            return true;
        }

        private bool createS1()
        {
            if (s0 == null || s0.Count <= 0) return false;
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

                _randomForestDetails = "";
                if (result.tree_texts != null)
                    for (int i = 0; i < result.tree_texts.Length; i++)
                        _randomForestDetails += $"{i + 1}. tree in forest\n{result.tree_texts[i]}\n";

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ML servisi hatası: {ex.Message}\nDocker'ın çalıştığından emin olun.");
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
                MessageBox.Show("Yakınlık matrisi hesaplanmamış.\nLütfen cizelge seçip başka tuşuna basınız.");
                return false;
            }
            GraficView form = new GraficView(new List<ObservationSet>()
            {
                new ObservationSet() { Observations = proximityMatrix, Name = "test" }
            });
            form.ShowDialog();
            return true;
        }
        #endregion Methods

        #region PropertyChagedInterfaceImplementaion
        public event PropertyChangedEventHandler PropertyChanged;

        private void propertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion PropertyChagedInterfaceImplementaion
    }
}

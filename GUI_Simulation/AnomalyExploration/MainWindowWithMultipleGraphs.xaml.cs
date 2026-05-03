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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GUI_Simulation.AnomalyExploration
{
    /// <summary>
    /// Interaction logic for MainWindowWithMultipleGraphs.xaml
    /// </summary>
    public partial class MainWindowWithMultipleGraphs : Window, INotifyPropertyChanged
    {
        #region Fields

        private List<DeviceBase> _sensors = new List<DeviceBase>();

        private int _inputWindowLength = 10;
        private int _countOfDecisionTreesInForest = 5;
        private int _scenarioCount = 20;

        #endregion Fields

        #region Properties

        public List<graphSelectionControl> ControlList { get; set; }

        public int InputWindowLength
        {
            get
            {
                return _inputWindowLength;
            }

            set
            {
                _inputWindowLength = value;
                propertyChanged();
            }
        }

        public int CountOfDecisionTreesInForest
        {
            get
            {
                return _countOfDecisionTreesInForest;
            }

            set
            {
                _countOfDecisionTreesInForest = value;
                propertyChanged();
            }
        }

        public int ScenarioCount
        {
            get
            {
                return _scenarioCount;
            }

            set
            {
                _scenarioCount = value;
                propertyChanged();
            }
        }


        #endregion Properties

        #region Ctor
        public MainWindowWithMultipleGraphs()
        {
            InitializeComponent();

            this.DataContext = this;
            ControlList = new List<graphSelectionControl>();
        }

        #endregion Ctor

        #region Events
        private void btnAddNewControl_Click(object sender, RoutedEventArgs e)
        {
            graphSelectionControl control = new graphSelectionControl(ControlList.Count + 1);
            control.Margin = new Thickness(0, 2, 0, 2);
            control.OnClose += Control_OnClose;
            control.OnMessageAdding += Control_OnMessageAdding;
            control.showContentEvent += Control_showContentEvent;
            control.onMainConfigChanged += Control_onMainConfigChanged;
            ControlList.Add(control);
            pnlControlContainer.Children.Add(control);
        }

        private void Control_onMainConfigChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is graphSelectionControl))
                return;

            if (!((graphSelectionControl)sender).IsMainGraph)
                return;

            foreach (var control in pnlControlContainer.Children)
            {
                if (!(control is graphSelectionControl))
                    continue;

                if (((graphSelectionControl)control).ID == ((graphSelectionControl)sender).ID)
                    continue;

                ((graphSelectionControl)control).IsMainGraph = false;
            }

            foreach (var control in ControlList)
            {
                if (control.ID == ((graphSelectionControl)sender).ID)
                    continue;

                control.IsMainGraph = false;
            }
        }

        private void Control_showContentEvent(object sender, ShowContentEventArgs e)
        {
            _sensors = ((graphSelectionControl)sender).Devices;

            FillList(e.ListToShow);

            FillListWithData(e.Data);

            if (!string.IsNullOrEmpty(e.ContentToShow))
                ShowRF(e.ContentToShow);
        }

        private void Control_OnMessageAdding(object sender, LogAddedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                TextBlock log = new TextBlock();
                log.Text = e.Message;
                log.TextWrapping = TextWrapping.Wrap;
                log.Margin = new Thickness(1);
                pnlLogs.Children.Insert(0, log);

                if (pnlLogs.Children.Count > 100)
                    pnlLogs.Children.RemoveAt(100);
            });
        }

        private void Control_OnClose(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int index = pnlControlContainer.Children.IndexOf((UIElement)sender);
                pnlControlContainer.Children.RemoveAt(index);

                var control = ControlList.Where(x => x.ID == ((graphSelectionControl)sender).ID).FirstOrDefault();
                if (control != null)
                {
                    ControlList.Remove(control);
                }

                for (int i = 0; i < pnlControlContainer.Children.Count; i++)
                {
                    if (!(pnlControlContainer.Children[i] is graphSelectionControl))
                        continue;

                    ((graphSelectionControl)pnlControlContainer.Children[i]).Order = i + 1;
                }
            });
        }

        private void btnRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            pnlControlContainer.Children.Clear();
            ControlList = new List<graphSelectionControl>();
        }

        private void btnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            pnlLogs.Children.Clear();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var mainGraphList = ControlList.Where(x => x.IsMainGraph).ToList();

            if (mainGraphList == null || mainGraphList.Count != 1)
            {
                MessageBox.Show("Ana çizelge seçiminde hata");
                return;
            }

            var mainGraph = mainGraphList[0];

            mainGraph.Run(ScenarioCount, InputWindowLength, CountOfDecisionTreesInForest);

            foreach (var control in ControlList)
            {
                if (control.ID == mainGraph.ID)
                    continue;

                control.Run(ScenarioCount, InputWindowLength, CountOfDecisionTreesInForest, mainGraph.s1);
            }
        }

        private void btnGraph_Click(object sender, RoutedEventArgs e)
        {
            List<ObservationSet> list = new List<ObservationSet>();
            foreach (var control in ControlList)
            {
                if (string.IsNullOrEmpty(control.GraphName))
                    continue;
                list.Add(new ObservationSet() { Observations = control.proximityMatrix, Name = control.GraphName });
            }

            GraficView form = new GraficView(list);
            form.ShowDialog();
        }

        #endregion Events

        #region Methods

        private void FillList(IList list)
        {
            if (list == null)
                return;

            ArrayList itemsList = new ArrayList();
            if (list?.Count <= 0)
            {
                itemsList.Add("Gösterilecek eleman yok.");
                lvList.ItemsSource = itemsList;
                return;
            }

            foreach (var item in list)
            {
                itemsList.Add(item.ToString());
            }

            lvList.ItemsSource = itemsList;
        }

        private void FillListWithData(List<InputWindow> data)
        {
            if (data == null)
                return;

            if (_sensors == null)
            {
                return;
            }

            ArrayList itemsList = new ArrayList();

            if (data == null || data?.Count <= 0)
            {
                itemsList.Add("Gösterilecek eleman yok.");
                lvList.ItemsSource = itemsList;
                return;
            }

            itemsList.Add("|----------- Time ----------->");

            for (int i = 0; i < _sensors.Count; i++)
            {
                StringBuilder line = new StringBuilder();
                line.Append($"{i + 1}. sensor:\t");
                foreach (var windowInData in data)
                {
                    for (int j = 0; j < windowInData.states[i].Length; j++)
                    {
                        double result = windowInData.states[i][j];
                        line.Append(result); //Console.Write(result);
                    }
                    line.Append(" "); //Console.Write(" ");
                }
                itemsList.Add(line.ToString());//Console.WriteLine();
            }

            lvList.ItemsSource = itemsList;
        }

        private void ShowRF(string randomForestDetails)
        {
            if (string.IsNullOrEmpty(randomForestDetails))
            {
                MessageBox.Show("Karar ağaçları oluşmamış. Lütfen öncelikle çizelge seçerek başla tuşuna basınız.");
                return;
            }
            new DecisionTreeShown(randomForestDetails).ShowDialog();
        }


        #endregion Methods

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        private void propertyChanged([CallerMemberName] string propertyChanged = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyChanged));
        }
        #endregion INotifyPropertyChanged Implementation
    }
}

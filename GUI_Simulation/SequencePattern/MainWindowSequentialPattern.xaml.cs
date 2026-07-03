using AdleGraph;
using AdleGraph.Interfaces;
using AdleGraph.Wpf;
using GUI_Simulation.AnomalyExploration;
using Adle.Analysis;
using Adle.Analysis.Rules;
using GUI_Simulation.SequencePattern.Scoring;
using SequentialPattern;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GUI_Simulation.SequencePattern
{
    /// <summary>
    /// Interaction logic for MainWindowSequentialPattern.xaml
    /// </summary>
    public partial class MainWindowSequentialPattern : Window, INotifyPropertyChanged
    {
        #region Constractor
        public MainWindowSequentialPattern()
        {
            InitializeComponent();
            this.DataContext = this;

            Individuals = new List<string>() { "Baba", "Anne", "Kız Çocuk", "Erkek Çocuk" };
            board.coordinates += Board_coordinates;
            _coordinates = null;
            LCSThreshold = 5;
            UseTrainingSet = true;

            this.Loaded += MainWindowSequentialPattern_Loaded;
        }


        private void Board_coordinates(object sender, CoordinatesEventArgs e)
        {
            _coordinates = e.Coordinates;
        }

        #endregion Constaractor

        #region Fields

        private int _scenarioCount = 25;
        private Sequence<INode> _sequence;
        private string _drawedGraphName = "";

        private string _previousExpectedNode;
        private string _incomingNode;
        private string _nextExpectedNode;
        private string _personPrediction;
        private int _LCSThreshold;

        private List<string> _previousExpectedStringCache = new List<string>();
        private List<INode> _expectedNodeCache = new List<INode>();
        private string _stepSummaryOfSequentialPredictionList;
        private string _personSummaryOfSequentialPredictionList;

        private Dictionary<string, X_Y_values> _coordinates;
        private bool _useTrainingSet;
        private INode _incomingNodecache;

        private bool _showPersonAbstract = false;
        private SequenceAnalyzer analyzer;
        #endregion Fields

        #region Properties

        public List<string> Individuals { get; set; }

        public string ScenarioCount
        {
            get
            {
                return _scenarioCount.ToString();
            }
            set
            {
                int _cache = _scenarioCount;
                if (!int.TryParse(value, out _scenarioCount))
                    _scenarioCount = _cache;
                onPropertyChanged();
            }
        }

        public Sequence<INode> TestSequence
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

        public string PreviousExpectedNode
        {
            get
            {
                return string.IsNullOrEmpty(_previousExpectedNode) ? " - " : _previousExpectedNode;
            }

            set
            {
                _previousExpectedNode = value;
                onPropertyChanged();
            }
        }

        public string IncomingNode
        {
            get
            {
                return string.IsNullOrEmpty(_incomingNode) ? " - " : _incomingNode;
            }

            set
            {
                _incomingNode = value;
                onPropertyChanged();
            }
        }

        public string NextExpectedNode
        {
            get
            {
                return string.IsNullOrEmpty(_nextExpectedNode) ? " - " : _nextExpectedNode;
            }

            set
            {
                _nextExpectedNode = value;
                onPropertyChanged();
            }
        }

        public string PersonPrediction
        {
            get
            {
                return string.IsNullOrEmpty(_personPrediction) ? " - " : _personPrediction;
            }

            set
            {
                _personPrediction = value;
                onPropertyChanged();
            }
        }

        public int LCSThreshold
        {
            get
            {
                return _LCSThreshold;
            }

            set
            {
                _LCSThreshold = value;
                onPropertyChanged();
            }
        }

        public bool UseTrainingSet
        {
            get
            {
                return _useTrainingSet;
            }

            set
            {
                if (_useTrainingSet == value)
                    return;

                _useTrainingSet = value;
                onPropertyChanged();

                AnalyzeIncomingSequence(_sequence, _incomingNodecache);
                //ReloadSuitableSequences();
            }
        }

        public string StepSummaryOfSequentialPredictionList
        {
            get
            {
                return _stepSummaryOfSequentialPredictionList;
            }

            set
            {
                _stepSummaryOfSequentialPredictionList = value;
                onPropertyChanged();
            }
        }

        public string PersonSummaryOfSequentialPredictionList
        {
            get
            {
                return _personSummaryOfSequentialPredictionList;
            }

            set
            {
                _personSummaryOfSequentialPredictionList = value;
                onPropertyChanged();
            }
        }

        #endregion Properties

        #region Events

        #region Control Events
        private void Control_MoveBack(object sender, MoveTestEventArgs<INode> e)
        {
            TestSequence = e.sequence;

            if (_previousExpectedStringCache.Count > 0 && _previousExpectedStringCache.Count + 1 == TestSequence[0].Count)
            {
                NextExpectedNode = _previousExpectedStringCache[_previousExpectedStringCache.Count - 1]; ;
                _previousExpectedStringCache.RemoveAt(_previousExpectedStringCache.Count - 1);
            }

            PersonPrediction = string.Empty;
            PreviousExpectedNode = string.Empty;
            IncomingNode = string.Empty;
            pnlNodeComparer.Background = new SolidColorBrush(Colors.WhiteSmoke);
            drawGraph(e.graph, e.startNode, e.EndNode, e.GraphSequence);
            GetReportControl().TestCount--;
            GetReportControl().RemoveLastPointsFromGraphic();
        }

        private void Control_MoveNext(object sender, MoveTestEventArgs<INode> e)
        {
            ListBoxItem li = null;
            Guid id = Guid.Empty;
            bool newSequenceBarAdded = false;
            var _selectedControl = GetReportControl();
            var _selectedPersonReportControl = GetReportControl(1);

            //Yeni gelen sequence için yeni sequencebar oluştur.
            if ((TestSequence != null || TestSequence?.Count >= 0) && e.beginNewSequence)
            {
                _selectedControl.NewAddedDataCount++;
                _selectedPersonReportControl.NewAddedDataCount++;

                var content = new sequenceBar(lsbAllSequemces.Items.Count, TestSequence, e.scenario, person: e.name, showPerson: true) { TrainingData = false };
                content.ChangeBackgroud(new SolidColorBrush(Colors.Orange));
                string person = PersonPrediction == " - " ? e.name : PersonPrediction;
                content.Person = person;

                id = content.Id;
                li = new ListBoxItem();
                li.Content = content;
                li.Tag = TestSequence;
                _previousExpectedStringCache = new List<string>();
                _expectedNodeCache = new List<INode>();

                lsbPersonResults.Items.Insert(0, new ListBoxItem() { Content = new PersonResultBarControl(1, lsbAllSequemces.Items.Count, TestSequence, person, e.name) });

                UpgradePersonReport(person, e.name);

                int i = 1;
                foreach (var item in lsbPersonResults.Items)
                {
                    ((PersonResultBarControl)((ListBoxItem)item).Content).Order = i;
                    i++;
                }
            }

            if (_showPersonAbstract)
            {
                int index = 1;
                for (int i = 0; i < lsbAllSequemces.Items.Count; i++)
                {
                    if (!newSequenceBarAdded)
                        newSequenceBarAdded = addSequenceToListIfPersonAlreadyAdded(li, i);
                    index = ReOrderSequences(id, index, i);
                }

                if (TestSequence != null && li != null && e.beginNewSequence && !newSequenceBarAdded)
                    addSequenceAndSequencePerson(li, index);

                _selectedControl.TestCount++;
                _selectedPersonReportControl.TestCount++;
            }
            else
            {
                if (TestSequence != null && li != null && e.beginNewSequence)
                    lsbAllSequemces.Items.Insert(0, li);

                for (int i = 0; i < lsbAllSequemces.Items.Count; i++)
                {
                    ReOrderSequences(id, i + 1, i);
                }

                _selectedControl.TestCount++;
                _selectedPersonReportControl.TestCount++;
            }

            _incomingNodecache = e.Node;
            AnalyzeIncomingSequence(e.sequence, e.Node);
            drawGraph(e.graph, e.startNode, e.EndNode, e.GraphSequence);

            SetSummariesFromReports(_selectedControl, _selectedPersonReportControl);
        }


        private void Control_test(object sender, testEventArgs<INode> e)
        {
            if ((TestSequence != null || TestSequence?.Count >= 0))
            {

                var content = new sequenceBar(lsbAllSequemces.Items.Count, TestSequence, e.scenario, person: e.name, showPerson: true) { TrainingData = false };
                content.ChangeBackgroud(new SolidColorBrush(Colors.Orange));
                ListBoxItem li = new ListBoxItem();
                li.Content = content;
                li.Tag = TestSequence;
                lsbAllSequemces.Items.Insert(0, li);
                string person = PersonPrediction == " - " ? e.name : PersonPrediction;



                for (int i = 0; i < lsbAllSequemces.Items.Count; i++)
                {
                    ReOrderSequences(content.Id, i + 1, i);
                }

                GetReportControl().TestCount++;
                GetReportControl(1).TestCount++;

                GetReportControl().NewAddedDataCount++;
                GetReportControl(1).NewAddedDataCount++;

                lsbPersonResults.Items.Insert(0, new ListBoxItem() { Content = new PersonResultBarControl(1, lsbAllSequemces.Items.Count, TestSequence, person, e.name) });

                UpgradePersonReport(person, e.name);

                int index = 1;
                foreach (var item in lsbPersonResults.Items)
                {
                    ((PersonResultBarControl)((ListBoxItem)item).Content).Order = index;
                    index++;
                }
            }

            AnalyzeIncomingSequence(e.sequence);
            if (tabControl.SelectedIndex == 0) tab1.IsSelected = true;
            drawGraph(e.graph, e.startNode, e.EndNode, e.GraphSequence);
        }

        private void Control_showContentEvent(object sender, ShowContentEventArgs e)
        {
            FillList(e.ListToShow, ((PersonUserControl)sender).ID.ToString());
        }

        private void Control_OnMessageAdding(object sender, LogAddedEventArgs e)
        {
            AddLog(e.Message);
        }

        private void Control_OnClose(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                int index = pnlControlContainer.Children.IndexOf((UIElement)sender);
                pnlControlContainer.Children.RemoveAt(index);

                for (int i = 0; i < pnlControlContainer.Children.Count; i++)
                {
                    if (!(pnlControlContainer.Children[i] is PersonUserControl))
                        continue;

                    ((PersonUserControl)pnlControlContainer.Children[i]).Order = i + 1;
                }
            });
        }

        #endregion Control Events

        #region Window Events
        private void btnAddNewPersonController_Click(object sender, RoutedEventArgs e)
        {
            string personSegestion = Individuals.Count > pnlControlContainer.Children.Count ? Individuals[pnlControlContainer.Children.Count] : "";
            PersonUserControl control = new PersonUserControl(pnlControlContainer.Children.Count + 1, personSegestion);
            control.Margin = new Thickness(0, 1, 0, 2);
            control.OnClose += Control_OnClose;
            control.OnMessageAdding += Control_OnMessageAdding;
            control.showContentEvent += Control_showContentEvent;
            control.RunTest += Control_test;
            control.MoveNext += Control_MoveNext;
            control.MoveBack += Control_MoveBack;
            pnlControlContainer.Children.Add(control);
        }

        private void btnGraphSettings_Click(object sender, RoutedEventArgs e)
        {
            new MainWindowForGraph(false).ShowDialog();
        }

        private void btnRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            pnlControlContainer.Children.Clear();
        }

        private void btnRun_Click(object sender, RoutedEventArgs e)
        {
            UseTrainingSet = true;

            List<PersonUserControl> controlList = new List<PersonUserControl>();
            lsbAllSequemces.Items.Clear();
            lsbSuitableSequences.Items.Clear();
            TestSequence = null;

            foreach (var item in pnlControlContainer.Children)
            {
                if (!(item is PersonUserControl))
                    continue;

                if (!((PersonUserControl)item).FamilyMember)
                    continue;

                controlList.Add((PersonUserControl)item);
            }

            string sequenceAbstaract = "";
            int order = 1;

            foreach (var control in controlList)
            {
                if (!control.Run(_scenarioCount)) continue;

                if (control.Person != sequenceAbstaract && _showPersonAbstract)
                {
                    sequenceAbstaract = control.Person;
                    ListBoxItem li = new ListBoxItem();
                    li.Content = sequenceAbstaract;
                    li.Tag = null;

                    lsbAllSequemces.Items.Add(li);
                }

                int index = 0;
                foreach (var sequence in control.SequenceList)
                {
                    ListBoxItem li = new ListBoxItem();
                    li.Content = new sequenceBar(order, sequence, control.Scenarios[index], person: control.Person, showPerson: !_showPersonAbstract) { TrainingData = true, ShowData = true };
                    li.Tag = sequence;

                    lsbAllSequemces.Items.Add(li);
                    order++;
                    index++;
                }
            }

            AddReport(order);

        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            UseTrainingSet = true;
            TestSequence = null;
            PreviousExpectedNode = string.Empty;
            IncomingNode = string.Empty;
            NextExpectedNode = string.Empty;
            PersonPrediction = string.Empty;
            lsbAllSequemces.Items.Clear();
            lsbSuitableSequences.Items.Clear();
            _sequence = null;
            pnlNodeComparer.Background = new SolidColorBrush(Colors.WhiteSmoke);
            _incomingNodecache = null;
            lsbResults.Items.Clear();
            pnlReports.Children.Clear();

            foreach (var item in pnlControlContainer.Children)
            {
                if (!(item is PersonUserControl))
                    continue;
                ((PersonUserControl)item).Reset();
            }
        }

        private void MainWindowSequentialPattern_Loaded(object sender, RoutedEventArgs e)
        {
            btnAddNewPersonController_Click(null, null);
        }

        #endregion Window Events

        #region Settings Events
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow();
            window.setProperties(_scenarioCount, LCSThreshold);
            window.BeforeClosing += Window_BeforeClosing;
            window.ShowDialog();
            window.BeforeClosing -= Window_BeforeClosing;
        }

        private void Window_BeforeClosing(object sender, EventArgs e)
        {
            ScenarioCount = ((SettingsWindow)sender).ScenarioCount.ToString();
            LCSThreshold = ((SettingsWindow)sender).LCSThreshold;
            UseTrainingSet = ((SettingsWindow)sender).UseTrainingSet;
        }

        #endregion Settings Events

        #endregion Events

        #region Methods

        #region Analyze And Move Next Methods
        private void AnalyzeIncomingSequence(Sequence<INode> sequence, INode incomingNode = null)
        {
            TestSequence = sequence;
            lsbSuitableSequences.Items.Clear();

            if (lsbAllSequemces.Items.Count <= 0)
            {
                PersonPrediction = "";
                return;
            }

            List<SequenceBarDTO> allSequencesDTO = GetAllSequencesFromList();

            SequenceAnalyzer analyzer = getAnalyzer(allSequencesDTO);

            var lastprobabilityDistributionOfNodesInTheNextStep = analyzer.probabilityDistributionOfNodesInTheNextStep;
            if (!analyzer.Analyze(sequence)) return;

            addSuitableSequences(analyzer.currentSequenceAnalysisResult);

            var exceptedNode = _expectedNodeCache.Count <= 0 ? null : _expectedNodeCache[_expectedNodeCache.Count - 1];

            SetPreviousExpectedAndIncomingNodeValuesAndSetBackgroundWithComparingNodes(incomingNode, exceptedNode);

            AddToPredictionReport(incomingNode, exceptedNode, lastprobabilityDistributionOfNodesInTheNextStep);

            setPersonPrediction(analyzer.currentSequenceAnalysisResult);

            setNextExpectedNodeValue(incomingNode, analyzer.probabilityDistributionOfNodesInTheNextStep);

            UpgradeReport(incomingNode, exceptedNode, analyzer.lastProbabilityDistributionOfNodes);
        }

        private void AddToPredictionReport(INode incomingNode, INode exceptedNode, List<AnalyzeResult> data)
        {
            if (exceptedNode == null || incomingNode == null)
                return;

            string exceptedNodeName = exceptedNode.Name;
            string incomingNodeName = data.Find(x => x.value.Name == incomingNode.Name)?.ToString();

            SolidColorBrush brush = new SolidColorBrush(Colors.LightPink);

            if (data.Exists(x => x.value.Name == incomingNode.Name))
                brush = new SolidColorBrush(Colors.LightSeaGreen);

            if (exceptedNode.Name == incomingNode.Name)
                brush = new SolidColorBrush(Colors.LightGreen);

            if (!string.IsNullOrEmpty(incomingNodeName) && (incomingNode.Name != exceptedNode.Name))
            {
                exceptedNodeName = incomingNode.Name;
            }

            ResultBarControl resultBar = new ResultBarControl(-1, TestSequence, exceptedNodeName, string.IsNullOrEmpty(incomingNodeName) ? incomingNode.Name : incomingNodeName, brush);
            ListBoxItem li = new ListBoxItem();
            li.Content = resultBar;
            lsbResults.Items.Insert(0, li);

            int i = 1;
            foreach (var item in lsbResults.Items)
            {
                if (!(item is ListBoxItem)) continue;

                ((ResultBarControl)((ListBoxItem)item).Content).Order = i.ToString();
                i++;
            }
        }

        private List<SequenceBarDTO> GetAllSequencesFromList()
        {
            List<SequenceBarDTO> allSequencesDTO = new List<SequenceBarDTO>();

            foreach (var item in lsbAllSequemces.Items)
            {
                if (!(((ListBoxItem)item).Content is sequenceBar))
                    continue;

                allSequencesDTO.Add(((sequenceBar)((ListBoxItem)item).Content).DTO);
            }

            return allSequencesDTO;
        }

        private void addSuitableSequences(List<SequenceBarDTO> data)
        {
            var orderedData = UseTrainingSet ? data.OrderByDescending(x => x.SimilarityRatio).ToList() : data.Where(x => !x.TrainingData).OrderBy(x => x.SimilarityRatio).ToList();
            foreach (var result in orderedData)
            {
                ListBoxItem li = new ListBoxItem();
                li.Content = new sequenceBar(result, showPerson: true, showSimilarityRatio: true) { TrainingData = result.TrainingData };
                li.Tag = result.Sequence;
                lsbSuitableSequences.Items.Add(li);
            }
        }

        private void ReloadSuitableSequences()
        {
            List<SequenceBarDTO> data = new List<SequenceBarDTO>();
            foreach (var item in lsbSuitableSequences.Items)
            {
                var dto = ((sequenceBar)((ListBoxItem)item).Content).DTO;
                if ((dto.TrainingData && UseTrainingSet) || !dto.TrainingData)
                    data.Add(dto);
            }

            if (data.Count <= 0) return;

            lsbSuitableSequences.Items.Clear();

            var orderedData = data.OrderBy(x => x.SimilarityRatio).ToList();
            foreach (var result in orderedData)
            {
                ListBoxItem li = new ListBoxItem();
                li.Content = new sequenceBar(result, showPerson: true, showSimilarityRatio: true) { TrainingData = result.TrainingData };
                li.Tag = result.Sequence;

                lsbSuitableSequences.Items.Add(li);
            }
        }

        private void SetPreviousExpectedAndIncomingNodeValuesAndSetBackgroundWithComparingNodes(INode incomingNode, INode expectedNode)
        {
            AnalyzeResult foundIncomingNode = null;

            if (incomingNode != null)
                foundIncomingNode = getAnalyzer()?.lastProbabilityDistributionOfNodes?.Find(x => x.value.Name == incomingNode.Name);

            if (PreviousExpectedNode != " - ") _previousExpectedStringCache.Add(PreviousExpectedNode);

            PreviousExpectedNode = incomingNode == null ? "" : NextExpectedNode;
            IncomingNode = incomingNode == null ? "" : foundIncomingNode == null ? incomingNode.ToString() : foundIncomingNode.ToString();

            if (PreviousExpectedNode != " - " && expectedNode != null && incomingNode != null)
            {
                if (incomingNode.Name == expectedNode.Name)
                {
                    pnlNodeComparer.Background = new SolidColorBrush(Colors.LightGreen); ;
                    return;
                }

                if (foundIncomingNode != null)
                {
                    pnlNodeComparer.Background = new SolidColorBrush(Colors.LightSeaGreen);
                    return;
                }

                pnlNodeComparer.Background = new SolidColorBrush(Colors.LightPink);
            }

            pnlNodeComparer.Background = new SolidColorBrush(Colors.WhiteSmoke);
        }

        private void setPreviousExpectedAndIncomingNodeValues(INode incomingNode, INode ExpectedNode)
        {
            if (PreviousExpectedNode != " - ") _previousExpectedStringCache.Add(PreviousExpectedNode);

            PreviousExpectedNode = incomingNode == null ? "" : NextExpectedNode;
            IncomingNode = incomingNode == null ? "" : incomingNode.ToString();

        }

        private void compareAndSetBackgroud(INode incommingNode, INode exceptedNode)
        {
            if (PreviousExpectedNode != " - " && exceptedNode != null && incommingNode != null)
                pnlNodeComparer.Background = (incommingNode.Name == exceptedNode.Name) ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            else
                pnlNodeComparer.Background = new SolidColorBrush(Colors.WhiteSmoke);
        }

        private void UpgradeReport(INode incommingNode, INode exceptedNode, List<AnalyzeResult> data)
        {
            if (incommingNode == null || exceptedNode == null)
                return;

            var reporter = GetReportControl();

            if (reporter == null)
                return;

            if (incommingNode.Name == exceptedNode.Name)
            {
                reporter.information.IncreaseTruePositive();
                return;
            }

            if (data != null && data.Exists(x => x.value.Name == incommingNode.Name))
            {
                reporter.information.IncreaseFalsePositive();
                return;
            }

            reporter.information.IncreaseFalseNegative();
        }

        private void UpgradePersonReport(string incommingPerson, string exceptedPerson)
        {
            if (string.IsNullOrEmpty(incommingPerson) || string.IsNullOrEmpty(exceptedPerson))
                return;

            var reporter = GetReportControl(1);

            if (reporter == null)
                return;

            if (incommingPerson == exceptedPerson)
            {
                reporter.information.IncreaseTruePositive();
                return;
            }

            reporter.information.IncreaseFalseNegative();
        }
        private SequenceAnalyzer getAnalyzer(List<SequenceBarDTO> data = null)
        {
            bool newAnalyzer = false;
            if (analyzer == null)
            {
                analyzer = new SequenceAnalyzer();
                newAnalyzer = true;
            }

            if (data == null)
                return analyzer;

            analyzer.Data = data;

            IAnalysisRule similarityRule = newAnalyzer ? new SimilarityRule(1) : analyzer.Rules[0];
            IAnalysisRule lcsRule = newAnalyzer ? new LCSRule(2) : analyzer.Rules[1];

            similarityRule.setParams(new SoftmaxNormalizer(), new MinMaxNormalizer());
            lcsRule.setParams(LCSThreshold, new SoftmaxNormalizer(), new MinMaxNormalizer());

            if (newAnalyzer)
            {
                analyzer.Rules.Add(similarityRule);
                analyzer.Rules.Add(lcsRule);
            }

            return analyzer;
        }

        private void setPersonPrediction(List<SequenceBarDTO> data)
        {
            if (data == null || data.Count <= 0)
            {
                PersonPrediction = "";
                return;
            }

            var LCS_Data = data.OrderByDescending(x => x.LCS).FirstOrDefault();

            var sequenceDTOforPersonPrediction = LCS_Data;
            PersonPrediction = sequenceDTOforPersonPrediction != null ? sequenceDTOforPersonPrediction.Person : "";
        }

        private void setNextExpectedNodeValue(INode node, List<AnalyzeResult> data)
        {
            if (data == null || data.Count <= 0)
            {
                NextExpectedNode = "";
                lsbExpectedNodes.ItemsSource = null;
                return;
            }

            var probabilityData = data.OrderByDescending(x => x.probability).ToList();

            AnalyzeResult probabilityDistribution = probabilityData.FirstOrDefault();
            NextExpectedNode = node == null ? "" : probabilityDistribution.ToString();
            _expectedNodeCache.Add(probabilityDistribution.value);

            lsbExpectedNodes.ItemsSource = probabilityData;
        }

        #endregion

        #region Report Methods
        private void AddReport(int Total)
        {
            pnlReports.Children.Clear();

            AccuracyInformation information = new AccuracyInformation("Sonraki Adım Tahmini Raporu");
            information.Total = 0;
            ReportControl reportControl = new ReportControl(information);
            reportControl.TrainingSetCount = Total;
            reportControl.NewAddedDataCount = 0;
            reportControl.TestCount = 0;

            pnlReports.Children.Add(reportControl);

            AccuracyInformation personInformation = new AccuracyInformation("Kişi Tahmini Raporu");
            information.Total = 0;
            ReportControl personReportControl = new ReportControl(personInformation);
            reportControl.TrainingSetCount = Total;
            reportControl.NewAddedDataCount = 0;
            reportControl.TestCount = 0;
            pnlReports.Children.Add(personReportControl);
        }

        private ReportControl GetReportControl(int order = 0)
        {
            if (pnlReports.Children.Count <= 0)
                AddReport(0);

            if (!(pnlReports.Children[0] is ReportControl))
            {
                pnlReports.Children.Clear();
                AddReport(0);
            }

            return (ReportControl)pnlReports.Children[order];
        }

        private void SetSummariesFromReports(ReportControl _selectedControl, ReportControl _selectedPersonReportControl)
        {
            StepSummaryOfSequentialPredictionList = $"D:{_selectedControl.information.TruePositive + _selectedControl.information.FalsePositive} Y: {_selectedControl.information.FalseNegative}";

            PersonSummaryOfSequentialPredictionList = $"D:{_selectedPersonReportControl.information.TruePositive + _selectedPersonReportControl.information.FalsePositive} Y: {_selectedPersonReportControl.information.FalseNegative}";
        }

        #endregion Report Methods

        private void drawGraph(IGraph graph, INode startNode, INode endNode, List<INode> graphSequence)
        {
            try
            {
                if (graph == null || startNode == null || endNode == null || graphSequence == null) return;


                if (string.IsNullOrEmpty(_drawedGraphName) || _drawedGraphName != graph.Name)
                {
                    _drawedGraphName = graph.Name;
                    board.draw(graph, startNode, endNode);
                }
                else
                {
                    _coordinates = board.CurrentCoordinates;

                    board.Reload(_coordinates);
                }

                board.Dispatcher.Invoke(() =>
                {
                    board.selectNodes(graphSequence);
                }, System.Windows.Threading.DispatcherPriority.Render);

            }
            catch (Exception ex)
            {

            }
        }

        private void AddLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TextBlock log = new TextBlock();
                log.Text = message;
                log.TextWrapping = TextWrapping.Wrap;
                log.Margin = new Thickness(5);
                pnlLogs.Children.Insert(0, log);

                if (pnlLogs.Children.Count > 100)
                    pnlLogs.Children.RemoveAt(100);
            });
        }

        private void addSequenceAndSequencePerson(ListBoxItem li, int index)
        {
            lsbAllSequemces.Items.Add(new ListBoxItem() { Content = ((sequenceBar)li.Content).DTO.Person });
            ((sequenceBar)li.Content).Order = index.ToString();
            lsbAllSequemces.Items.Add(li);
        }

        private int ReOrderSequences(Guid id, int index, int i)
        {
            var item = ((ListBoxItem)lsbAllSequemces.Items[i]).Content;
            if (item is sequenceBar)
            {
                if (((sequenceBar)item).Id != id)
                    ((sequenceBar)item).ChangeBackgroud();

                ((sequenceBar)((ListBoxItem)lsbAllSequemces.Items[i]).Content).Order = (index).ToString();
                index++;
            }

            return index;
        }

        private bool addSequenceToListIfPersonAlreadyAdded(ListBoxItem li, int i)
        {
            bool newSequenceBarAdded = false;
            var item = ((ListBoxItem)lsbAllSequemces.Items[i]).Content;
            if (li != null)
            {
                if (((sequenceBar)li.Content).DTO.Person == item.ToString())
                {
                    newSequenceBarAdded = true;
                    lsbAllSequemces.Items.Insert(i + 1, li);
                }
            }

            return newSequenceBarAdded;
        }

        #region different sources

        private void FillList(IList list, string title = "")
        {
            if (list == null)
                return;

            ArrayList itemsList = new ArrayList();
            if (list?.Count <= 0)
            {
                itemsList.Add("Gösterilecek eleman yok.");
                new LCS.DataShowWindow(itemsList) { Title = title }.Show();
                return;
            }

            foreach (var item in list)
            {
                itemsList.Add(item.ToString());
            }

            new LCS.DataShowWindow(itemsList) { Title = title }.Show();
        }

        #endregion


        #endregion Methods

        #region INotifyPRopertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged([CallerMemberName] string propertName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertName));
        }

        #endregion INotifyPRopertyChanged Implementation
    }
}
using Adle.Analysis;
using Adle.Analysis.Rules;
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
using System.Windows.Shapes;

namespace GUI_Simulation.SequencePattern
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        #region Fields

        private int _scerioCount;

        private int _LCSThreshold;

        private bool _useSoftmax;

        private bool _useMinmaxSoftmax;

        private bool _useZscoreSoftmax;

        private bool _randomSelection;

        private bool _useTrainingSet;

        #endregion Fields

        #region Properties

        public int ScenarioCount
        {
            get
            {
                return _scerioCount;
            }

            set
            {
                _scerioCount = value;
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

        public bool UseSoftmax
        {
            get
            {
                return _useSoftmax;
            }

            set
            {
                _useSoftmax = value;
                onPropertyChanged();
            }
        }

        public bool UseMinmaxSoftmax
        {
            get
            {
                return _useMinmaxSoftmax;
            }

            set
            {
                _useMinmaxSoftmax = value;
                onPropertyChanged();
            }
        }

        public bool UseZscoreSoftmax
        {
            get
            {
                return _useZscoreSoftmax;
            }

            set
            {
                _useZscoreSoftmax = value;
                onPropertyChanged();
            }
        }

        public bool RandomSelection
        {
            get
            {
                return _randomSelection;
            }
            set
            {
                _randomSelection = value;
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
                _useTrainingSet = value;
                onPropertyChanged();
            }
        }

        #endregion Properties

        #region Public Methods

        public void setProperties(int ScenarioCount = 25, int LCSThreshold = 5, bool UseSoftmax = true, bool UseMinmaxSoftmax = true, bool UseZscoreSoftmax = false, bool RandomSelection = false, bool UseTraningSet = true)
        {
            this.ScenarioCount = ScenarioCount;
            this.LCSThreshold = LCSThreshold;
            this.UseSoftmax = UseSoftmax;
            this.UseMinmaxSoftmax = UseMinmaxSoftmax;
            this.UseZscoreSoftmax = UseZscoreSoftmax;
            this.RandomSelection = RandomSelection;
            this.UseTrainingSet = UseTraningSet;
        }

        #endregion Public Methods

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        private void onPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion    INotifyPropertyChanged Implementation

        #region Events

        public delegate void BeforeClosingHandler(object sender, EventArgs e);
        public event BeforeClosingHandler BeforeClosing;
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            BeforeClosing?.Invoke(this, null);
        }

        #endregion Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

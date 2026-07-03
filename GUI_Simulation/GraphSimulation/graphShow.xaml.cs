using AdleGraph.Interfaces;
using AdleGraph.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace GUI_Simulation.GraphSimulation
{
    /// <summary>
    /// Interaction logic for graphShow.xaml
    /// </summary>
    public partial class graphShow : Window
    {
        public graphShow()
        {
            InitializeComponent();
        }

        public void UpdateBoad(IGraph graph)
        {
            graphWindow.draw(graph);
        }
    }
}

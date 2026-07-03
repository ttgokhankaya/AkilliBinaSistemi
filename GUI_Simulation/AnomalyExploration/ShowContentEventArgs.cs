using Adle.Analysis;
using Adle.Analysis.Rules;
using AdleGraph.Interfaces;
using SequentialPattern;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GUI_Simulation.AnomalyExploration
{
    public class ShowContentEventArgs : RoutedEventArgs
    {
        public string ContentToShow { get; set; }

        public IList ListToShow { get; set; }

        public List<InputWindow> Data { get; set; }
    }

    public class ResultEventArgs : RoutedEventArgs
    {
        public List<string> Logs { get; set; }
    }

    public class LogAddedEventArgs : RoutedEventArgs
    {
        public string Message { get; set; }
    }

    public class testEventArgs<T> : RoutedEventArgs
    {
        public Sequence<T> sequence { get; set; }
        public string name { get; set; }

        public IGraph graph { get; set; }

        public INode startNode { get; set; }
        public INode EndNode { get; set; }
        public List<INode> GraphSequence { get; set; }
        public Scenario scenario { get; set; }

    }

    public class MoveTestEventArgs<T> : RoutedEventArgs
    {
        public Sequence<T> sequence { get; set; }

        public INode Node { get; set; }

        public string name { get; set; }

        public bool beginNewSequence { get; set; } = false;
        public Scenario scenario { get; set; }

        public IGraph graph { get; set; }

        public INode startNode { get; set; }
        public INode EndNode { get; set; }
        public List<INode> GraphSequence { get; set; }

    }
}

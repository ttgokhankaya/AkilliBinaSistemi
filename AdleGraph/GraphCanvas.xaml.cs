using AdleGraph.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AdleGraph
{
    public class X_Y_values
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class CoordinatesEventArgs : EventArgs
    {
        public Dictionary<string, X_Y_values> Coordinates { get; set; }
    }

    public partial class GraphCanvas : UserControl
    {
        public event EventHandler<CoordinatesEventArgs> coordinates;

        private const double NodeRadius = 20;
        private const double CanvasWidth = 600;
        private const double CanvasHeight = 400;
        private Dictionary<INode, Point> _nodePositions = new Dictionary<INode, Point>();
        private IGraph _currentGraph;
        private INode _currentStartNode;
        private INode _currentEndNode;

        public GraphCanvas()
        {
            InitializeComponent();
        }

        public Dictionary<string, X_Y_values> CurrentCoordinates
        {
            get
            {
                var result = new Dictionary<string, X_Y_values>();
                foreach (var kvp in _nodePositions)
                    if (kvp.Key.Name != null)
                        result[kvp.Key.Name] = new X_Y_values { X = kvp.Value.X, Y = kvp.Value.Y };
                return result;
            }
        }

        public void draw(IGraph graph) => draw(graph, null, null);

        public void draw(IGraph graph, INode startNode, INode endNode)
        {
            _currentGraph = graph;
            _currentStartNode = startNode;
            _currentEndNode = endNode;

            mainCanvas.Children.Clear();
            _nodePositions.Clear();

            if (graph == null) return;

            var nodes = new List<INode>(graph.NodeList);
            int count = nodes.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                double angle = 2 * Math.PI * i / count - Math.PI / 2;
                double cx = CanvasWidth / 2 + (CanvasWidth / 2 - NodeRadius - 10) * Math.Cos(angle);
                double cy = CanvasHeight / 2 + (CanvasHeight / 2 - NodeRadius - 10) * Math.Sin(angle);
                _nodePositions[nodes[i]] = new Point(cx, cy);
            }

            drawEdgesAndNodes(startNode, endNode);
        }

        public void Reload(Dictionary<string, X_Y_values> savedCoordinates)
        {
            if (_currentGraph == null) return;
            draw(_currentGraph, _currentStartNode, _currentEndNode);
        }

        public void selectNodes(List<INode> selectedNodes)
        {
            foreach (var child in mainCanvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag is INode node)
                {
                    if (selectedNodes != null && selectedNodes.Contains(node))
                        ellipse.Stroke = new SolidColorBrush(Colors.DodgerBlue) { };
                    else
                        ellipse.Stroke = Brushes.DimGray;
                }
            }
        }

        private void drawEdgesAndNodes(INode startNode, INode endNode)
        {
            foreach (var edge in _currentGraph.EdgeList)
            {
                if (_nodePositions.TryGetValue(edge.Node1, out var from) &&
                    _nodePositions.TryGetValue(edge.Node2, out var to))
                {
                    var line = new Line
                    {
                        X1 = from.X, Y1 = from.Y,
                        X2 = to.X, Y2 = to.Y,
                        Stroke = Brushes.DarkGray,
                        StrokeThickness = 1.5
                    };
                    mainCanvas.Children.Add(line);
                }
            }

            foreach (var node in _currentGraph.NodeList)
            {
                var pos = _nodePositions[node];
                Brush fill = Brushes.LightGray;
                if (node == startNode) fill = Brushes.LightGreen;
                else if (node == endNode) fill = Brushes.Salmon;

                var ellipse = new Ellipse
                {
                    Width = NodeRadius * 2,
                    Height = NodeRadius * 2,
                    Fill = fill,
                    Stroke = Brushes.DimGray,
                    StrokeThickness = 1.5,
                    Tag = node
                };
                Canvas.SetLeft(ellipse, pos.X - NodeRadius);
                Canvas.SetTop(ellipse, pos.Y - NodeRadius);
                mainCanvas.Children.Add(ellipse);

                var label = new TextBlock
                {
                    Text = node.Name ?? "",
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                Canvas.SetLeft(label, pos.X - NodeRadius);
                Canvas.SetTop(label, pos.Y - 8);
                mainCanvas.Children.Add(label);
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            coordinates?.Invoke(this, new CoordinatesEventArgs
            {
                Coordinates = CurrentCoordinates
            });
        }
    }
}

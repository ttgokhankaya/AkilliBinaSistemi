using AdleGraph.Interface;
using AdleGraph.Interfaces;
using IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdleGraph
{
    public class Graph : IGraph
    {
        #region Ctor
        public Graph()
        {
            Container.InitContainer();
            Container.Register<INode, Node>();
            Container.Register<IEdge, Edge>();
        }

        public Graph(IRScriptRunner rRunner = null, string name = "")
        {
            _rRunner = rRunner;
            Name = string.IsNullOrEmpty(name) ? Guid.NewGuid().ToString().Replace("-", "") : name;
            Container.InitContainer();
            Container.Register<INode, Node>();
            Container.Register<IEdge, Edge>();
        }

        #endregion Ctor

        #region Fields

        private readonly IRScriptRunner _rRunner;

        private List<INode> _nodeList;
        private List<IEdge> _edgeList;

        #endregion Fields

        #region Properties

        public bool ShowEdgesWeights { get; set; } = false;
        public string Name { get; set; }

        public List<INode> NodeList
        {
            get
            {
                if (_nodeList == null)
                {
                    _nodeList = new List<INode>();
                }

                return _nodeList;
            }

            set
            {
                _nodeList = value;
            }
        }

        public List<IEdge> EdgeList
        {
            get
            {
                if (_edgeList == null)
                {
                    _edgeList = new List<IEdge>();
                }
                return _edgeList;
            }

            set
            {
                _edgeList = value;
            }
        }

        #endregion    Properties

        #region Methods

        #region Node Base Methods

        public INode GetNodeWithName(string name, bool addNodeIfNotExist = false)
        {
            if (NodeList == null)
                return null;

            var foundNode = NodeList.Find(x => x.Name == name);
            if (foundNode == null && addNodeIfNotExist)
            {
                foundNode = Container.Resolve<INode>();
                foundNode.Name = name;
                AddNode(foundNode);
            }

            return foundNode;
        }

        public void AddNode(INode node)
        {
            CheckIfNodeIsNull(node, errorCode: "105");

            CheckNodeName(node.Name, errorCode: "106");

            CheckIfNodeAddedBefore(node, errorCode: "107");

            NodeList.Add(node);
        }

        public INode AddNode(string name)
        {
            CheckNodeName(name, errorCode: "108");
            CheckIfNodeAddedBefore(name, errorCode: "109");

            INode newNode = Container.Resolve<INode>(name);
            NodeList.Add(newNode);
            return newNode;
        }

        public bool NodeExits(INode node)
        {
            var foundNode = GetNodeWithName(node.Name, false);
            return foundNode != null;
        }

        public bool NodeExits(string nodeName)
        {
            var foundNode = GetNodeWithName(nodeName);
            return foundNode != null;
        }

        #endregion Node Base Methods

        #region Adge Base Methods

        public IEdge AddEdge(INode nodeFrom, INode nodeTo = null, Func<double> function = null, bool isdirected = false)
        {
            CheckIfNodeIsNull(nodeFrom, "the output node cannot be empty", errorCode: "101");

            if (nodeTo == null)
                nodeTo = nodeFrom;
            IEdge edge = Container.Resolve<IEdge>();

            edge.ShowWeight = ShowEdgesWeights;
            edge.IsDirected = isdirected;
            edge.FunctionOfEdge = function;
            edge.Node1 = nodeFrom;
            edge.Node2 = nodeTo;
            edge.Weight = -1;

            EdgeList.Add(edge);
            nodeFrom.EdgeCount++;
            nodeTo.EdgeCount++;

            return edge;
        }

        public IEdge AddEdge(string NodeNameFrom, string NodeNameTo, Func<double> function = null, bool isDirected = false)
        {
            CheckNodeName(NodeNameFrom, "the output node cannot be empty", errorCode: "110");

            INode foundNode = GetNodeWithName(NodeNameFrom);
            INode secondNode = null;

            CheckIfNodeIsNull(foundNode, "the output node cannot be empty", errorCode: "102");

            if (string.IsNullOrEmpty(NodeNameTo))
                secondNode = foundNode;
            else
                secondNode = GetNodeWithName(NodeNameTo);

            if (secondNode == null)
                secondNode = foundNode;

            return AddEdge(foundNode, secondNode, function, isDirected);
        }

        public IEdge AddEdge(string NodeNameFrom, string NodeNameTo, double weight = default(double), bool isDirected = false)
        {
            IEdge edge = AddEdge(NodeNameFrom, NodeNameTo, null, isDirected);
            edge.Weight = weight;
            edge.ShowWeight = true;
            return edge;
        }

        public IEdge AddEdge(INode nodeFrom, INode nodeTo = null, double weight = default(double), bool isdirected = false)
        {
            IEdge edge = AddEdge(nodeFrom, nodeTo, null, isdirected);
            edge.Weight = weight;
            edge.ShowWeight = true;
            return edge;
        }

        public bool UpdateEdge(string edgeName, IEdge newEdge)
        {
            if (string.IsNullOrEmpty(edgeName))
                return false;

            if (newEdge == null)
                return false;

            var foundEdge = EdgeList.Find(x => x.Name == edgeName);
            foundEdge.Node1 = newEdge.Node1;
            foundEdge.Node2 = newEdge.Node2;
            foundEdge.IsDirected = newEdge.IsDirected;
            foundEdge.Weight = newEdge.Weight;
            foundEdge.FunctionOfEdge = newEdge.FunctionOfEdge;
            return true;
        }

        #endregion Adge Base Methods

        #region Matrix

        public static IGraph CreateNewGraphFromMatrixString(string matrixString, string name = "")
        {
            string[] array = matrixString.Replace("\r\n", "&").TrimEnd('&').Split('&');
            string[] nodes = array[0].Substring(1, array[0].Length - 1).Replace("\t", "&").TrimEnd('&').Split('&');
            IGraph graph = Container.Resolve<IGraph>();
            graph.Name = name;

            foreach (var node in nodes)
            {
                graph.AddNode(node);
            }

            for (int i = 1; i < array.Length; i++)
            {
                string[] values = array[i].Replace("\t", "&").TrimEnd('&').Substring((array[i].Replace("\t", "&").TrimEnd('&')).IndexOf("&") + 1).Split('&');
                for (int j = 0; j < values.Length; j++)
                {
                    if (values[j] != "0")
                    {
                        double _weight = 0;
                        if (values[j] != "-1")
                        {
                            values[j] = values[j].Replace('.', ',');
                            if (double.TryParse(values[j], out _weight))
                            {
                                graph.AddEdge(graph.NodeList[i - 1], graph.NodeList[j], _weight, true);
                                continue;
                            }
                        }

                        graph.AddEdge(graph.NodeList[i - 1], graph.NodeList[j], null, true);
                    }
                }
            }

            return graph;
        }

        public double[,] GetMatrixOfGraph()
        {
            int length = NodeList.Count;
            double[,] data = new double[length, length];

            for (int i = 0; i < NodeList.Count; i++)
            {
                INode item = NodeList[i];
                var edgesOfNode = EdgeList.Where(x => x.Node1.Name == item.Name).ToList();
                for (int j = 0; j < NodeList.Count; j++)
                {
                    INode node2 = NodeList[j];
                    var foundEdge = edgesOfNode.Find(x => x.Node2.Name == node2.Name);
                    data[i, j] = foundEdge == null ? 0 : foundEdge.Weight;
                }
            }

            return data;
        }

        public string ToMatrisString()
        {
            if (NodeList == null || NodeList.Count < 0)
                return "the inserted node could not be found";

            var matris = GetMatrixOfGraph();
            StringBuilder builder = new StringBuilder();
            builder.Append("\t");

            for (int i = 0; i < NodeList.Count; i++)
            {
                builder.Append($"{NodeList[i].Name}\t");
            }

            builder.AppendLine();

            for (int i = 0; i < NodeList.Count; i++)
            {
                builder.Append($"{NodeList[i].Name}\t");
                for (int j = 0; j < NodeList.Count; j++)
                {
                    builder.Append($"{matris[i, j]}\t");
                }
                builder.AppendLine();
            }

            string result = builder.ToString();
#if debug
            debug.WriteLine(result);
#endif
            return result;
        }

        #endregion Matrix

        #region Run Methods
        public List<List<INode>> Run(INode StartNode, INode EndNode, bool useEgdeWeights = false, int maxIteration = 100, bool allowLoops = false, int maxTryCountForLoops = 10)
        {
            CheckIfNodeIsNull(StartNode, "Start node can not be null", errorCode: "103");
            CheckIfNodeIsNull(EndNode, "End node can not be null", errorCode: "104");

            List<List<INode>> pathCollection = new List<List<INode>>();


            bool continueIterations = true;
            int iteration = 1;

            while (continueIterations)
            {
                List<INode> path = new List<INode>();
                path.Add(StartNode);

                INode current = StartNode;
                int tryCountForLoops = 0;
                bool GraphPath = true;

                while (GraphPath)
                {
                    INode nextNodeCandidate = MoveNext(current, useEgdeWeights);
                    if (nextNodeCandidate == null)
                        break;

                    if (!allowLoops && path.Exists(x => x.Name == nextNodeCandidate.Name))
                    {
                        tryCountForLoops++;

                        if (maxTryCountForLoops == tryCountForLoops)
                            GraphPath = false;
                        else
                            continue;
                    }

                    tryCountForLoops = 0;

                    current = nextNodeCandidate;

                    path.Add(current);

                    if (current.Name == EndNode.Name)
                        GraphPath = false;
                }

                if (iteration == maxIteration)
                    continueIterations = false;

                iteration++;
                pathCollection.Add(path);
            }

            return pathCollection;
        }

        public INode MoveNext(INode resourceNode, bool useEgdeWeights = false)
        {
            CheckIfNodeIsNull(resourceNode, errorCode: "100");

            INode selectedNode = null;
            List<IEdge> resourceNodesEdges = EdgeList.FindAll(x => x.Node1.Name == resourceNode.Name);

            if (resourceNodesEdges?.Count == 0)
                return selectedNode;

            if (resourceNodesEdges?.Count == 1)
                selectedNode = resourceNodesEdges[0].Node2;

            if (useEgdeWeights && resourceNodesEdges?.Count > 1)
            {
                int[] Weights = new int[resourceNodesEdges.Count];
                for (int i = 0; i < resourceNodesEdges.Count; i++)
                {
                    if (resourceNodesEdges[i].Weight <= 0)
                    {
                        useEgdeWeights = false;
                        break;
                    }

                    Weights[i] = (int)(resourceNodesEdges[i].Weight * 100);
                }

                if (useEgdeWeights)
                {
                    int nextIndex = Utility.RandomWeighted(Weights);
                    selectedNode = resourceNodesEdges[nextIndex].Node2;
                }
            }

            if (!useEgdeWeights && resourceNodesEdges?.Count > 1)
            {
                int nextIndex = Utility.Between(0, resourceNodesEdges.Count, includeMaxValue: true);

                selectedNode = resourceNodesEdges[nextIndex].Node2;
            }

            if (selectedNode == null)
            {
                //Todo
            }

            return selectedNode;
        }

        #endregion Run Methods
        public override string ToString()
        {
            return $"{Name} Node Count {NodeList.Count} Edge Count {EdgeList.Count}";
        }

        #endregion

        #region Private Methods

        #region Check Methods - They Throws Exceptions

        private void CheckIfNodeIsNull(INode node, string message = "Node can not be null", string errorCode = "")
        {
            if (node == null)
                throw new ArgumentNullException($"{message}{(string.IsNullOrEmpty(errorCode) ? "" : $"Error Code:{errorCode}")}");
        }

        private void CheckIfNodeAddedBefore(INode node, string errorCode = "")
        {
            if (NodeExits(node))
                throw new Exception($"{node.Name} node is already added {(string.IsNullOrEmpty(errorCode) ? "" : $"Error Code:{errorCode}")}");
        }

        private void CheckIfNodeAddedBefore(string name, string errorCode = "")
        {
            if (NodeExits(name))
                throw new Exception($"{name} node is already added {(string.IsNullOrEmpty(errorCode) ? "" : $"Error Code:{errorCode}")}");
        }

        private void CheckNodeName(string name, string message = "Nodes must be named", string errorCode = "")
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{message}{(string.IsNullOrEmpty(errorCode) ? "" : $"Error Code:{errorCode}")}");
        }

        #endregion Check Methods - They Throws Exceptions

        #endregion Private Methods
    }
}

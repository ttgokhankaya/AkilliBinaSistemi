namespace Adle.Contracts;

/// <summary>A persisted graph. <paramref name="MatrixValue"/> is the TSV adjacency matrix.</summary>
public record GraphDto(int Id, string? Name, string? MatrixValue);

/// <summary>Create/update payload for a graph.</summary>
public record GraphWriteDto(string Name, string MatrixValue);

/// <summary>
/// Parameters for a simulation run over a persisted graph: random walks from
/// <paramref name="StartNode"/> to <paramref name="EndNode"/>.
/// </summary>
public record RunGraphRequest(
    string StartNode,
    string EndNode,
    int Iterations = 25,
    bool UseEdgeWeights = false,
    bool AllowLoops = false);

/// <summary>
/// Result of a graph run: <paramref name="Walks"/> is one node-name sequence per
/// iteration. Each walk can be fed straight into /api/analysis/predict.
/// </summary>
public record RunGraphResponse(int WalkCount, string[][] Walks);

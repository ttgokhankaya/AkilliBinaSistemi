namespace Adle.Contracts;

/// <summary>One labelled training sequence (ordered node/sensor names).</summary>
public record TrainingSequenceDto(string[] Tokens, string? Person);

/// <summary>
/// Request for next-step + person prediction over the thesis LCS+softmax pipeline.
/// </summary>
public record PredictRequest(
    TrainingSequenceDto[] Training,
    string[] Prefix,
    int LcsThreshold = 0);

/// <summary>A candidate next node with its predicted probability.</summary>
public record NextNodeProbability(string? Node, double Probability);

/// <summary>Result of a next-step prediction.</summary>
public record PredictResponse(
    string? PredictedNextNode,
    string? PredictedPerson,
    int MatchCount,
    string? RuleUsed,
    IReadOnlyList<NextNodeProbability> Distribution);

namespace Adle.Contracts;

// These DTOs are the single contract for the anomaly endpoints. They serialize
// as camelCase to the browser (default) and as snake_case to the Python
// ml_service (the MlServiceClient uses JsonNamingPolicy.SnakeCaseLower), so
// e.g. NComponents <-> n_components, AllObservations <-> all_observations.

/// <summary>t-SNE dimensionality-reduction request (proxied to ml_service /tsne).</summary>
public record TsneRequestDto(
    double[][] Observations,
    double Perplexity = 0.9,
    int NComponents = 2);

/// <summary>t-SNE result: the reduced coordinates.</summary>
public record TsneResponseDto(double[][] Output);

/// <summary>
/// Random-forest proximity request (proxied to ml_service /random-forest).
/// <c>S0</c>/<c>S1</c> are the two class samples; <c>AllObservations</c> is the
/// full set scored against the trained ensemble.
/// </summary>
public record RandomForestRequestDto(
    double[][] S0,
    double[][] S1,
    double[][] AllObservations,
    int NTrees = 100);

/// <summary>Random-forest result: proximity matrix + textual dumps of sample trees.</summary>
public record RandomForestResponseDto(
    double[][] ProximityMatrix,
    string[] TreeTexts);

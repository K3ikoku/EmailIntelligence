namespace EmailIntelligence.Functions.Contracts;

public sealed record PipelineRunResponse
{
    public required bool Success { get; init; }
}
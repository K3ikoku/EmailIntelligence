namespace EmailIntelligence.Infrastructure.Services;

public sealed record ConfigurationResult<T>
{
    public bool Succeeded { get; private init; }
    public T? Value { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = [];

    public static ConfigurationResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static ConfigurationResult<T> Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };
}

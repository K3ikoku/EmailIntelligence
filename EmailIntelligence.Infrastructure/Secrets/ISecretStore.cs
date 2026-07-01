namespace EmailIntelligence.Infrastructure.Secrets;

public interface ISecretStore
{
    Task<string> GetSecretAsync(string name, CancellationToken cancellationToken = default);
    Task<string?> TryGetSecretAsync(string name, CancellationToken cancellationToken = default);
    Task<string> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default);
    Task DeleteSecretAsync(string name, CancellationToken cancellationToken = default);
}

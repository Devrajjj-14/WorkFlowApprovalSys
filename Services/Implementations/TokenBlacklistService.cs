namespace WorkflowApprovalApi.Services.Implementations;

// Singleton service that holds a HashSet of revoked JWT tokens.
// Singleton means one instance for the entire application lifetime —
// so the blacklist persists across all requests until the server restarts.
public class TokenBlacklistService
{
    // HashSet gives O(1) lookup — instant check no matter how many tokens are blacklisted.
    // HashSet automatically ignores duplicate adds.
    private readonly HashSet<string> _blacklistedTokens = new();

    // Lock object to make the HashSet thread-safe.
    // Multiple requests can come in simultaneously — without a lock, two threads
    // modifying the HashSet at the same time could corrupt it.
    private readonly object _lock = new();

    // Called during logout — adds the token to the blacklist.
    public void Blacklist(string token)
    {
        lock (_lock)
        {
            _blacklistedTokens.Add(token);
        }
    }

    // Called on every request by the middleware — checks if the token is revoked.
    public bool IsBlacklisted(string token)
    {
        lock (_lock)
        {
            return _blacklistedTokens.Contains(token);
        }
    }
}

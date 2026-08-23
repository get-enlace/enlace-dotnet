namespace Enlace.AspNetCore;

/// <summary>
/// Holds the outcome of the startup spec <em>discovery</em> — which URL to use, or why
/// none could be found. The document itself is never cached here: per the adapter
/// contract (spec content is "not stored, read fresh each load"), the <c>/api/spec</c>
/// endpoint re-fetches <see cref="ResolvedUrl"/> on every request.
/// </summary>
internal sealed class EnlaceSpecCache
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string? ResolvedUrl { get; private set; }

    public Exception? Failure { get; private set; }

    /// <summary>Completes once startup discovery has finished, successfully or not.</summary>
    public Task Ready => _ready.Task;

    public void SetResolved(string url)
    {
        ResolvedUrl = url;
        _ready.TrySetResult();
    }

    public void SetFailed(Exception exception)
    {
        Failure = exception;
        _ready.TrySetResult();
    }
}

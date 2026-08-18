using DartsLeaderboard.Application.Abstractions;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class RecordingNotifier : IMatchNotifier
{
    public List<int> Notifications { get; } = new();

    public void NotifyChanged(int matchId) => Notifications.Add(matchId);

    public IDisposable Subscribe(int matchId, Func<Task> handler) => new Subscription();

    private sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

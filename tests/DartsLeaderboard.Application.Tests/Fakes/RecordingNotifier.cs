using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class RecordingNotifier : IMatchNotifier
{
    public List<int> Notifications { get; } = new();

    public List<MatchStateDto> States { get; } = new();

    public void NotifyChanged(int matchId, MatchStateDto state)
    {
        Notifications.Add(matchId);
        States.Add(state);
    }

    public IDisposable Subscribe(int matchId, Func<MatchStateDto, Task> handler) => new Subscription();

    private sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

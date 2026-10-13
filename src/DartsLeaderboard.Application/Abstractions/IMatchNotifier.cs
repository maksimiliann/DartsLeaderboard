using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Abstractions;

public interface IMatchNotifier
{
    void NotifyChanged(int matchId, MatchStateDto state);

    IDisposable Subscribe(int matchId, Func<MatchStateDto, Task> handler);
}

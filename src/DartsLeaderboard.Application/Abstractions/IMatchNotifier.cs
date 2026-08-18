namespace DartsLeaderboard.Application.Abstractions;

public interface IMatchNotifier
{
    void NotifyChanged(int matchId);

    IDisposable Subscribe(int matchId, Func<Task> handler);
}

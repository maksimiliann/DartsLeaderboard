using System.Collections.Concurrent;
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Infrastructure.Notifications;

public sealed class InMemoryMatchNotifier : IMatchNotifier
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Func<MatchStateDto, Task>>> _subscribers = new();

    public void NotifyChanged(int matchId, MatchStateDto state)
    {
        if (!_subscribers.TryGetValue(matchId, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Values)
        {
            var callback = handler;
            _ = Task.Run(() => callback(state));
        }
    }

    public IDisposable Subscribe(int matchId, Func<MatchStateDto, Task> handler)
    {
        var token = Guid.NewGuid();
        var handlers = _subscribers.GetOrAdd(matchId, _ => new ConcurrentDictionary<Guid, Func<MatchStateDto, Task>>());
        handlers[token] = handler;

        return new Subscription(() =>
        {
            if (_subscribers.TryGetValue(matchId, out var current))
            {
                current.TryRemove(token, out _);
            }
        });
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}

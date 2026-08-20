using DartsLeaderboard.Infrastructure.Notifications;

namespace DartsLeaderboard.Infrastructure.Tests;

public class InMemoryMatchNotifierTests
{
    [Fact]
    public async Task NotifyChanged_InvokesSubscribersOfThatMatchOnly()
    {
        var notifier = new InMemoryMatchNotifier();
        var matchCalls = 0;
        var otherCalls = 0;

        using var _ = notifier.Subscribe(1, () =>
        {
            Interlocked.Increment(ref matchCalls);
            return Task.CompletedTask;
        });

        using var __ = notifier.Subscribe(2, () =>
        {
            Interlocked.Increment(ref otherCalls);
            return Task.CompletedTask;
        });

        notifier.NotifyChanged(1);
        await Task.Delay(100);

        Assert.Equal(1, matchCalls);
        Assert.Equal(0, otherCalls);
    }

    [Fact]
    public async Task Dispose_StopsNotifications()
    {
        var notifier = new InMemoryMatchNotifier();
        var calls = 0;

        var subscription = notifier.Subscribe(1, () =>
        {
            Interlocked.Increment(ref calls);
            return Task.CompletedTask;
        });

        subscription.Dispose();
        notifier.NotifyChanged(1);
        await Task.Delay(100);

        Assert.Equal(0, calls);
    }
}

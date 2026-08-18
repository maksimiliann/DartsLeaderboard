using System.Reflection;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests;

internal static class TestMatchBuilder
{
    public static readonly DateTimeOffset Start = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    public static Match Create(MatchSettings settings, params string[] playerNames)
    {
        var match = Match.Start(settings, Enumerable.Range(1, playerNames.Length).ToArray(), Start).Value!;
        SetProperty(match, nameof(Match.Id), 12);

        var seat = 0;
        foreach (var participant in match.Participants)
        {
            SetProperty(participant, nameof(MatchParticipant.Id), seat + 1);
            SetProperty(participant, nameof(MatchParticipant.Player),
                Domain.Players.Player.Create(playerNames[seat], Start).Value!);
            seat++;
        }

        return match;
    }

    public static Match WithThrows(this Match match, params int[] points)
    {
        for (var i = 0; i < points.Length; i++)
        {
            var result = match.RecordThrow(points[i], Start.AddMinutes(i));
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Бросок {points[i]} отклонён: {result.Error}");
            }
        }

        return match;
    }

    private static void SetProperty(object target, string propertyName, object value) =>
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(target, new[] { value });
}

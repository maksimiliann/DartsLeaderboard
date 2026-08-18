using System.Reflection;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Domain.Tests;

internal static class MatchTestFactory
{
    private static readonly DateTimeOffset Start = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    /// <summary>Матч с заданными идентификаторами участников (в БД их присваивает EF Core).</summary>
    public static Match Create(MatchSettings settings, int participantCount)
    {
        var playerIds = Enumerable.Range(1, participantCount).ToArray();
        var match = Match.Start(settings, playerIds, Start).Value!;

        var seat = 0;
        foreach (var participant in match.Participants)
        {
            SetProperty(participant, nameof(MatchParticipant.Id), ++seat);
        }

        return match;
    }

    /// <summary>Добавляет броски по кругу в обход правил: нужно для тестов самих правил.</summary>
    public static Match WithRawThrows(this Match match, params int[] points)
    {
        var participants = match.Participants;
        var throws = (List<Throw>)typeof(Match)
            .GetField("_throws", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(match)!;

        for (var i = 0; i < points.Length; i++)
        {
            var participant = participants[i % participants.Count];
            var recorded = (Throw)Activator.CreateInstance(typeof(Throw), nonPublic: true)!;

            SetProperty(recorded, nameof(Throw.ParticipantId), participant.Id);
            SetProperty(recorded, nameof(Throw.RoundNumber), i / participants.Count + 1);
            SetProperty(recorded, nameof(Throw.Points), points[i]);
            SetProperty(recorded, nameof(Throw.RecordedAt), Start.AddMinutes(i));

            throws.Add(recorded);
        }

        return match;
    }

    private static void SetProperty(object target, string propertyName, object value) =>
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(target, new[] { value });
}

namespace DartsLeaderboard.Application.Contracts;

public sealed record ClubRecordDto(string Key, string Title, string? HolderName, int? Value);

public sealed record RecordCandidate(string HolderName, int Value, DateTimeOffset AchievedAt);

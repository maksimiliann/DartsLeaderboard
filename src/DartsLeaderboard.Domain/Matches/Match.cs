using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches;

public sealed class Match
{
    public const int MinParticipants = 2;

    private readonly List<MatchParticipant> _participants = new();
    private readonly List<Throw> _throws = new();

    private Match() { }

    public int Id { get; private set; }
    public GameMode Mode { get; private set; }
    public int? StartingScore { get; private set; }
    public int? RoundLimit { get; private set; }
    public MatchStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int? WinnerParticipantId { get; private set; }

    public IReadOnlyList<MatchParticipant> Participants =>
        _participants.OrderBy(p => p.SeatOrder).ToList();

    public IReadOnlyList<Throw> Throws =>
        _throws.OrderBy(t => t.RoundNumber).ThenBy(t => SeatOf(t.ParticipantId)).ToList();

    public int CurrentRoundNumber => _throws.Count / _participants.Count + 1;

    public MatchParticipant? CurrentParticipant =>
        Status == MatchStatus.InProgress
            ? Participants[_throws.Count % _participants.Count]
            : null;

    public static Result<Match> Start(MatchSettings settings, IReadOnlyList<int> playerIds, DateTimeOffset now)
    {
        var validation = settings.Validate();
        if (!validation.IsSuccess)
        {
            return Result<Match>.Failure(validation.Error!.Value);
        }

        if (playerIds.Count < MinParticipants)
        {
            return Result<Match>.Failure(DomainErrorCode.TooFewParticipants);
        }

        if (playerIds.Distinct().Count() != playerIds.Count)
        {
            return Result<Match>.Failure(DomainErrorCode.DuplicateParticipant);
        }

        var match = new Match
        {
            Mode = settings.Mode,
            StartingScore = settings.StartingScore,
            RoundLimit = settings.RoundLimit,
            Status = MatchStatus.InProgress,
            StartedAt = now
        };

        for (var seat = 0; seat < playerIds.Count; seat++)
        {
            match._participants.Add(new MatchParticipant(playerIds[seat], seat));
        }

        return Result<Match>.Success(match);
    }

    public int PointsOf(int participantId) =>
        _throws.Where(t => t.ParticipantId == participantId).Sum(t => t.Points);

    public int RoundCountOf(int participantId) =>
        _throws.Count(t => t.ParticipantId == participantId);

    internal int SeatOf(int participantId) =>
        _participants.FirstOrDefault(p => p.Id == participantId)?.SeatOrder ?? int.MaxValue;
}

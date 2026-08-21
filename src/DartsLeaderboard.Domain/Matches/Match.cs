using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches.Rules;

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

    /// <summary>Навигация для EF Core: содержимое приватного списка без сортировки.</summary>
    public IReadOnlyCollection<MatchParticipant> AllParticipants => _participants;

    /// <summary>Навигация для EF Core: содержимое приватного списка без сортировки.</summary>
    public IReadOnlyCollection<Throw> AllThrows => _throws;

    public int CurrentRoundNumber => _throws.Count / _participants.Count + 1;

    public MatchParticipant? CurrentParticipant =>
        Status == MatchStatus.InProgress
            ? Participants[_throws.Count % _participants.Count]
            : null;

    public IGameRules Rules => GameRules.For(this);

    public int CompletedRoundCount => _throws.Count == 0 ? 0 : _throws.Max(t => t.RoundNumber);

    public Result<Throw> RecordThrow(int points, DateTimeOffset now) =>
        RecordThrow([new VisitDart(points, false)], now);

    public Result<Throw> RecordThrow(IReadOnlyList<VisitDart> darts, DateTimeOffset now)
    {
        if (Status != MatchStatus.InProgress)
        {
            return Result<Throw>.Failure(DomainErrorCode.MatchNotInProgress);
        }

        var points = darts.Count == 0 ? 0 : darts.Sum(dart => dart.Points);
        if (points < 0 || points > Throw.MaxPoints || darts.Any(dart => dart.Points < 0 || dart.Points > Throw.MaxPoints))
        {
            return Result<Throw>.Failure(DomainErrorCode.PointsOutOfRange);
        }

        var participant = CurrentParticipant!;
        var rules = Rules;

        var validation = rules.ValidateThrow(this, participant, points);
        if (!validation.IsSuccess)
        {
            return Result<Throw>.Failure(validation.Error!.Value);
        }

        var recorded = new Throw(
            participant.Id,
            CurrentRoundNumber,
            rules.NormalizeVisit(this, participant, darts),
            now);
        _throws.Add(recorded);

        var outcome = rules.Evaluate(this);
        if (outcome.IsFinished)
        {
            Status = MatchStatus.Finished;
            FinishedAt = now;
            WinnerParticipantId = outcome.WinnerParticipantId;
        }

        return Result<Throw>.Success(recorded);
    }

    public Result UndoLastThrow()
    {
        if (Status == MatchStatus.Abandoned)
        {
            return Result.Failure(DomainErrorCode.MatchNotInProgress);
        }

        if (_throws.Count == 0)
        {
            return Result.Failure(DomainErrorCode.NoThrowsToUndo);
        }

        // Throws returns a sorted copy; remove the last recorded entry from _throws by identity.
        var last = _throws[^1];
        _throws.Remove(last);

        Status = MatchStatus.InProgress;
        FinishedAt = null;
        WinnerParticipantId = null;

        return Result.Success();
    }

    public Result Abandon(DateTimeOffset now)
    {
        if (Status != MatchStatus.InProgress)
        {
            return Result.Failure(DomainErrorCode.MatchNotInProgress);
        }

        Status = MatchStatus.Abandoned;
        FinishedAt = now;
        return Result.Success();
    }

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

namespace DartsLeaderboard.Domain.Common;

public enum DomainErrorCode
{
    PlayerNameEmpty,
    PlayerNameTooLong,
    PlayerNameTaken,
    PlayerNotFound,
    MatchNotFound,
    MatchNotInProgress,
    TooFewParticipants,
    DuplicateParticipant,
    InvalidStartingScore,
    InvalidRoundLimit,
    PointsOutOfRange,
    PointsExceedRemaining,
    NoThrowsToUndo,
    RoundAlreadyRecorded
}

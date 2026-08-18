namespace DartsLeaderboard.Application.Abstractions;

/// <summary>Бросок этого раунда уже записан другим устройством.</summary>
public sealed class MatchConflictException : Exception
{
    public MatchConflictException(Exception? innerException = null)
        : base("Раунд уже записан", innerException)
    {
    }
}

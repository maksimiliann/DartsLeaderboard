using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Players;

public sealed class Player
{
    public const int MaxNameLength = 50;

    private Player() => Name = string.Empty;

    public int Id { get; private set; }
    public string Name { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Player> Create(string name, DateTimeOffset now)
    {
        var validation = ValidateName(name);
        if (!validation.IsSuccess)
        {
            return Result<Player>.Failure(validation.Error!.Value);
        }

        return Result<Player>.Success(new Player { Name = name.Trim(), CreatedAt = now });
    }

    public Result Rename(string name)
    {
        var validation = ValidateName(name);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        Name = name.Trim();
        return Result.Success();
    }

    public void SetArchived(bool archived) => IsArchived = archived;

    private static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(DomainErrorCode.PlayerNameEmpty);
        }

        return name.Trim().Length > MaxNameLength
            ? Result.Failure(DomainErrorCode.PlayerNameTooLong)
            : Result.Success();
    }
}

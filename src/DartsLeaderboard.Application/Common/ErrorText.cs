using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Common;

public static class ErrorText
{
    public static string For(DomainErrorCode code) => code switch
    {
        DomainErrorCode.PlayerNameEmpty => "Имя игрока не может быть пустым",
        DomainErrorCode.PlayerNameTooLong => "Имя игрока слишком длинное",
        DomainErrorCode.PlayerNameTaken => "Игрок с таким именем уже есть",
        DomainErrorCode.PlayerNotFound => "Игрок не найден",
        DomainErrorCode.MatchNotFound => "Матч не найден",
        DomainErrorCode.MatchNotInProgress => "Матч уже не идёт",
        DomainErrorCode.TooFewParticipants => "Нужно выбрать хотя бы двух игроков",
        DomainErrorCode.DuplicateParticipant => "Игрок выбран дважды",
        DomainErrorCode.InvalidStartingScore => "Некорректный начальный счёт",
        DomainErrorCode.InvalidRoundLimit => "Некорректное количество раундов",
        DomainErrorCode.PointsOutOfRange => "Очки за раунд должны быть от 0 до 180",
        DomainErrorCode.PointsExceedRemaining => "Больше остатка: при переборе вводите 0",
        DomainErrorCode.NoThrowsToUndo => "Отменять нечего",
        DomainErrorCode.RoundAlreadyRecorded => "Раунд уже записан с другого устройства, состояние обновлено",
        _ => "Неизвестная ошибка"
    };
}

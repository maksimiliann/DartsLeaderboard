using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Web.Speech;

namespace DartsLeaderboard.Web.Tests;

public class ResultAnnouncementTests
{
    [Fact]
    public void Format_ReadsPlaceNameAndPoints()
    {
        var text = ResultAnnouncement.Format(
        [
            new StandingDto(1, "Иван", 120),
            new StandingDto(2, "Мария", 95)
        ]);

        Assert.Equal(
            "<speak>Первое место. Иван. 120 очков.<break time=\"400ms\"/>Второе место. Мария. 95 очков.</speak>",
            text);
    }

    [Theory]
    [InlineData(1, "очко")]
    [InlineData(2, "очка")]
    [InlineData(4, "очка")]
    [InlineData(5, "очков")]
    [InlineData(11, "очков")]
    [InlineData(21, "очко")]
    [InlineData(22, "очка")]
    [InlineData(100, "очков")]
    public void Format_DeclinesPoints(int total, string word)
    {
        var text = ResultAnnouncement.Format([new StandingDto(1, "Иван", total)]);

        Assert.Contains($"Иван. {total} {word}.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_KeepsTiedPlacesAndEscapesName()
    {
        var text = ResultAnnouncement.Format(
        [
            new StandingDto(1, "Иван & Ко", 80),
            new StandingDto(1, "Мария", 80),
            new StandingDto(3, "Пётр", 40)
        ]);

        Assert.Contains("Первое место. Иван &amp; Ко. 80 очков.", text, StringComparison.Ordinal);
        Assert.Contains("<break time=\"400ms\"/>Первое место. Мария. 80 очков.", text, StringComparison.Ordinal);
        Assert.Contains("<break time=\"400ms\"/>Третье место. Пётр. 40 очков.</speak>", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_UsesNumericPlaceAfterTenth()
    {
        var text = ResultAnnouncement.Format([new StandingDto(11, "Иван", 10)]);

        Assert.Contains("11 место. Иван. 10 очков.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CanSpeak_OnlyForFinishedMatchWithStandings()
    {
        Assert.True(ResultAnnouncement.CanSpeak(State("Завершён", inProgress: false, [new StandingDto(1, "Иван", 10)])));
        Assert.True(ResultAnnouncement.CanSpeak(State("Ничья", inProgress: false, [new StandingDto(1, "Иван", 10)])));
        Assert.False(ResultAnnouncement.CanSpeak(State("Идёт", inProgress: true, [new StandingDto(1, "Иван", 10)])));
        Assert.False(ResultAnnouncement.CanSpeak(State("Прерван", inProgress: false, [new StandingDto(1, "Иван", 10)])));
        Assert.False(ResultAnnouncement.CanSpeak(State("Завершён", inProgress: false, [])));
    }

    private static MatchStateDto State(string status, bool inProgress, IReadOnlyList<StandingDto> standings) =>
        new(
            1,
            "Наибольшая сумма",
            status,
            inProgress,
            1,
            null,
            null,
            null,
            [],
            [],
            [],
            false,
            [],
            standings,
            null);
}

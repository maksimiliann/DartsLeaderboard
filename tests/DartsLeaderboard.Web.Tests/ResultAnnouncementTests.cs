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
            "<speak><prosody rate=\"slow\"><s>Первое место. Иван. сто двадцать очков.</s><s>Второе место. Мария. девяносто пять очков.</s></prosody></speak>",
            text);
    }

    [Theory]
    [InlineData(0, "ноль очков")]
    [InlineData(1, "одно очко")]
    [InlineData(2, "два очка")]
    [InlineData(4, "четыре очка")]
    [InlineData(5, "пять очков")]
    [InlineData(11, "одиннадцать очков")]
    [InlineData(21, "двадцать одно очко")]
    [InlineData(22, "двадцать два очка")]
    [InlineData(80, "восемьдесят очков")]
    [InlineData(95, "девяносто пять очков")]
    [InlineData(100, "сто очков")]
    [InlineData(101, "сто одно очко")]
    [InlineData(111, "сто одиннадцать очков")]
    [InlineData(120, "сто двадцать очков")]
    [InlineData(266, "двести шестьдесят шесть очков")]
    [InlineData(1000, "одна тысяча очков")]
    [InlineData(2000, "две тысячи очков")]
    [InlineData(2345, "две тысячи триста сорок пять очков")]
    [InlineData(11000, "одиннадцать тысяч очков")]
    [InlineData(21000, "двадцать одна тысяча очков")]
    public void Format_SpeaksPointsAsWords(int total, string spoken)
    {
        var text = ResultAnnouncement.Format([new StandingDto(1, "Иван", total)]);

        Assert.Contains($"Иван. {spoken}.", text, StringComparison.Ordinal);
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

        Assert.Contains("<s>Первое место. Иван &amp; Ко. восемьдесят очков.</s>", text, StringComparison.Ordinal);
        Assert.Contains("<s>Первое место. Мария. восемьдесят очков.</s>", text, StringComparison.Ordinal);
        Assert.Contains("<s>Третье место. Пётр. сорок очков.</s></prosody></speak>", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_UsesNumericPlaceAfterTenth()
    {
        var text = ResultAnnouncement.Format([new StandingDto(11, "Иван", 10)]);

        Assert.Contains("11 место. Иван. десять очков.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CanSpeak_OnlyForFinishedMatchWithStandings()
    {
        Assert.Equal("Озвучить результат", ResultAnnouncement.ButtonText(SpeechPlayback.Idle));
        Assert.Equal("Пауза", ResultAnnouncement.ButtonText(SpeechPlayback.Playing));
        Assert.Equal("Продолжить", ResultAnnouncement.ButtonText(SpeechPlayback.Paused));
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

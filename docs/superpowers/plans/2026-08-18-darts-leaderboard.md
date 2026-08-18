# Darts Leaderboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Веб-приложение для подсчёта и хранения очков при игре в дартс: таблица со столбцами-игроками, два режима игры, живая статистика, график трендов и история результатов в PostgreSQL.

**Architecture:** Clean architecture из четырёх слоёв (Domain, Application, Infrastructure, Web) с зависимостями строго внутрь. Правила режимов и расчёт статистики живут в домене за стратегией `IGameRules`; Application — тонкие use case-сервисы; каждый бросок пишется в БД сразу (write-through), состояние матча восстанавливается проигрыванием бросков.

**Tech Stack:** .NET 10, Blazor Server (InteractiveServer), MudBlazor 9.x, PostgreSQL 16, EF Core 10 (Npgsql), xUnit, Testcontainers, bUnit, Docker Compose.

**Спека:** `docs/superpowers/specs/2026-08-17-darts-leaderboard-design.md`

## Global Constraints

- Target framework всех проектов — `net10.0`; `Nullable` и `ImplicitUsings` включены.
- MudBlazor версии не ниже 9.0.0 (единственная ветка с поддержкой .NET 10). Пакеты добавлять через `dotnet add package` без указания версии, чтобы взять актуальную.
- FluentAssertions не использовать; только штатные `Assert` из xUnit.
- MediatR и любые CQRS-библиотеки не использовать: use case — обычный класс с одним публичным методом `ExecuteAsync`.
- `DartsLeaderboard.Web` не ссылается на `DartsLeaderboard.Domain`. UI работает только с DTO из Application. Нарушение этого правила — повод отклонить задачу.
- `DartsLeaderboard.Domain` не имеет ссылок на пакеты, включая EF Core.
- Приложение не проверяет правила дартса (дабл-аут, перебор). Единственное исключение — в x01 очки броска не могут превышать остаток.
- Один бросок = три дротика одним числом, диапазон 0..180. Пустое поле ввода означает 0 (незачтённый раунд).
- Авторизации нет.
- Имена таблиц и столбцов в БД — snake_case, задаются явно в конфигурациях EF Core (без пакета конвенций).
- Тексты UI и сообщений об ошибках — на русском.
- Коммит после каждой задачи; сообщения в стиле Conventional Commits.

---

## File Structure

```
DartsLeaderboard.sln
Directory.Build.props
Dockerfile
docker-compose.yml
.env.example
README.md
src/
  DartsLeaderboard.Domain/
    Common/Result.cs                    — Result, Result<T>
    Common/DomainErrorCode.cs           — коды ошибок домена
    Players/Player.cs                   — игрок справочника
    Matches/GameMode.cs                 — режим игры
    Matches/MatchStatus.cs              — статус матча
    Matches/MatchSettings.cs            — настройки режима + валидация
    Matches/Match.cs                    — агрегат матча
    Matches/MatchParticipant.cs         — участник матча
    Matches/Throw.cs                    — запись раунда
    Matches/Rules/IGameRules.cs         — стратегия режима
    Matches/Rules/MatchOutcome.cs       — результат оценки матча
    Matches/Rules/StatisticItem.cs      — именованный показатель
    Matches/Rules/GameRules.cs          — фабрика стратегий
    Matches/Rules/X01Rules.cs           — режим на очки
    Matches/Rules/HighestTotalRules.cs  — режим на максимум
  DartsLeaderboard.Application/
    Abstractions/IPlayerRepository.cs
    Abstractions/IMatchRepository.cs
    Abstractions/ILeaderboardQueries.cs
    Abstractions/IMatchQueries.cs
    Abstractions/IMatchNotifier.cs
    Abstractions/IClock.cs
    Abstractions/MatchConflictException.cs
    Common/OperationResult.cs
    Common/ErrorText.cs                 — DomainErrorCode -> русский текст
    Contracts/PlayerDto.cs
    Contracts/MatchStateDto.cs          — MatchStateDto, MatchColumnDto, MatchRowDto, MatchCellDto, StatisticDto, ChartSeriesDto
    Contracts/MatchSetupRequest.cs      — GameModeOption, MatchSetupRequest
    Contracts/LeaderboardRowDto.cs
    Contracts/MatchListItemDto.cs
    Matches/MatchStateMapper.cs
    Matches/StartMatchService.cs
    Matches/RecordThrowService.cs
    Matches/UndoLastThrowService.cs
    Matches/GetMatchStateService.cs
    Matches/AbandonMatchService.cs
    Players/GetPlayersService.cs
    Players/AddPlayerService.cs
    Players/RenamePlayerService.cs
    Players/SetPlayerArchivedService.cs
    Reports/GetLeaderboardService.cs
    Reports/GetMatchListService.cs
    DependencyInjection.cs
  DartsLeaderboard.Infrastructure/
    Persistence/DartsDbContext.cs
    Persistence/Configurations/PlayerConfiguration.cs
    Persistence/Configurations/MatchConfiguration.cs
    Persistence/Configurations/MatchParticipantConfiguration.cs
    Persistence/Configurations/ThrowConfiguration.cs
    Persistence/Repositories/PlayerRepository.cs
    Persistence/Repositories/MatchRepository.cs
    Persistence/Queries/LeaderboardQueries.cs
    Persistence/Queries/MatchQueries.cs
    Persistence/DatabaseInitializer.cs  — hosted service, миграции с ретраями
    Notifications/InMemoryMatchNotifier.cs
    SystemClock.cs
    DependencyInjection.cs
  DartsLeaderboard.Web/
    Program.cs
    appsettings.json
    Components/App.razor, Routes.razor, _Imports.razor
    Components/Layout/MainLayout.razor, NavMenu.razor
    Components/Pages/Leaderboard.razor      — "/"
    Components/Pages/Players.razor          — "/players"
    Components/Pages/NewMatch.razor         — "/match/new"
    Components/Pages/MatchPage.razor        — "/match/{MatchId:int}"
    Components/Pages/History.razor          — "/history"
    Components/Match/MatchScoreGrid.razor
    Components/Match/RoundInput.razor
    Components/Match/MatchStatsPanel.razor
    Components/Match/ThrowTrendChart.razor
tests/
  DartsLeaderboard.Domain.Tests/
  DartsLeaderboard.Application.Tests/
  DartsLeaderboard.Infrastructure.Tests/
  DartsLeaderboard.Web.Tests/
```

---

### Task 1: Каркас решения

**Files:**
- Create: `Directory.Build.props`, `DartsLeaderboard.sln`
- Create: `src/DartsLeaderboard.Domain/DartsLeaderboard.Domain.csproj`
- Create: `src/DartsLeaderboard.Application/DartsLeaderboard.Application.csproj`
- Create: `src/DartsLeaderboard.Infrastructure/DartsLeaderboard.Infrastructure.csproj`
- Create: `src/DartsLeaderboard.Web/DartsLeaderboard.Web.csproj` (шаблон `blazor` с интерактивностью Server)
- Create: `tests/DartsLeaderboard.Domain.Tests`, `tests/DartsLeaderboard.Application.Tests`, `tests/DartsLeaderboard.Infrastructure.Tests`, `tests/DartsLeaderboard.Web.Tests`

**Interfaces:**
- Consumes: ничего.
- Produces: собирающееся решение с проектами и ссылками, на которые опираются все последующие задачи.

- [ ] **Step 1: Проверить SDK**

Run: `dotnet --list-sdks`
Expected: в списке есть строка, начинающаяся с `10.`. Если нет — установить: `winget install --id Microsoft.DotNet.SDK.10`.

- [ ] **Step 2: Создать `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Создать проекты и решение**

```bash
dotnet new sln -n DartsLeaderboard
dotnet new classlib -o src/DartsLeaderboard.Domain
dotnet new classlib -o src/DartsLeaderboard.Application
dotnet new classlib -o src/DartsLeaderboard.Infrastructure
dotnet new blazor -o src/DartsLeaderboard.Web --interactivity Server --all-interactive
dotnet new xunit -o tests/DartsLeaderboard.Domain.Tests
dotnet new xunit -o tests/DartsLeaderboard.Application.Tests
dotnet new xunit -o tests/DartsLeaderboard.Infrastructure.Tests
dotnet new xunit -o tests/DartsLeaderboard.Web.Tests
dotnet sln add (Get-ChildItem -Recurse -Filter *.csproj)
```

Удалить файлы-заготовки `Class1.cs` из трёх classlib-проектов.

- [ ] **Step 4: Прописать ссылки между проектами**

```bash
dotnet add src/DartsLeaderboard.Application reference src/DartsLeaderboard.Domain
dotnet add src/DartsLeaderboard.Infrastructure reference src/DartsLeaderboard.Application
dotnet add src/DartsLeaderboard.Web reference src/DartsLeaderboard.Application
dotnet add src/DartsLeaderboard.Web reference src/DartsLeaderboard.Infrastructure
dotnet add tests/DartsLeaderboard.Domain.Tests reference src/DartsLeaderboard.Domain
dotnet add tests/DartsLeaderboard.Application.Tests reference src/DartsLeaderboard.Application
dotnet add tests/DartsLeaderboard.Infrastructure.Tests reference src/DartsLeaderboard.Infrastructure
dotnet add tests/DartsLeaderboard.Web.Tests reference src/DartsLeaderboard.Web
```

`DartsLeaderboard.Web` ссылается на Infrastructure только ради регистрации служб в `Program.cs`; ссылки на Domain у Web быть не должно.

- [ ] **Step 5: Проверить сборку и прогон тестов**

Run: `dotnet build` затем `dotnet test`
Expected: сборка без ошибок; тесты проходят (в шаблонных проектах их нет или есть пустой файл — это нормально).

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "chore: scaffold solution with clean architecture projects"
```

---

### Task 2: Примитивы домена и игрок

**Files:**
- Create: `src/DartsLeaderboard.Domain/Common/DomainErrorCode.cs`
- Create: `src/DartsLeaderboard.Domain/Common/Result.cs`
- Create: `src/DartsLeaderboard.Domain/Players/Player.cs`
- Test: `tests/DartsLeaderboard.Domain.Tests/PlayerTests.cs`

**Interfaces:**
- Consumes: каркас из Task 1.
- Produces:
  - `enum DomainErrorCode` со значениями `PlayerNameEmpty, PlayerNameTooLong, PlayerNameTaken, PlayerNotFound, MatchNotFound, MatchNotInProgress, TooFewParticipants, DuplicateParticipant, InvalidStartingScore, InvalidRoundLimit, PointsOutOfRange, PointsExceedRemaining, NoThrowsToUndo, RoundAlreadyRecorded`
  - `Result` с `IsSuccess`, `Error`, `Result.Success()`, `Result.Failure(DomainErrorCode)`
  - `Result<T>` с `IsSuccess`, `Value`, `Error`, `Result<T>.Success(T)`, `Result<T>.Failure(DomainErrorCode)`
  - `Player` с `Id`, `Name`, `IsArchived`, `CreatedAt`, `Player.Create(string name, DateTimeOffset now) -> Result<Player>`, `Rename(string name) -> Result`, `SetArchived(bool archived)`

- [ ] **Step 1: Написать падающие тесты**

`tests/DartsLeaderboard.Domain.Tests/PlayerTests.cs`:

```csharp
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Domain.Tests;

public class PlayerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_TrimsName()
    {
        var result = Player.Create("  Максим  ", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Максим", result.Value!.Name);
        Assert.False(result.Value.IsArchived);
        Assert.Equal(Now, result.Value.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyName(string name)
    {
        var result = Player.Create(name, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCode.PlayerNameEmpty, result.Error);
    }

    [Fact]
    public void Create_RejectsTooLongName()
    {
        var result = Player.Create(new string('a', 51), Now);

        Assert.Equal(DomainErrorCode.PlayerNameTooLong, result.Error);
    }

    [Fact]
    public void Rename_ChangesName()
    {
        var player = Player.Create("Аня", Now).Value!;

        var result = player.Rename(" Анна ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Анна", player.Name);
    }

    [Fact]
    public void SetArchived_TogglesFlag()
    {
        var player = Player.Create("Пётр", Now).Value!;

        player.SetArchived(true);
        Assert.True(player.IsArchived);

        player.SetArchived(false);
        Assert.False(player.IsArchived);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: ошибки компиляции — типы `Player`, `Result`, `DomainErrorCode` не найдены.

- [ ] **Step 3: Реализовать примитивы и игрока**

`Common/DomainErrorCode.cs`:

```csharp
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
```

`Common/Result.cs`:

```csharp
namespace DartsLeaderboard.Domain.Common;

public sealed class Result
{
    private Result(bool isSuccess, DomainErrorCode? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public DomainErrorCode? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(DomainErrorCode error) => new(false, error);
}

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, DomainErrorCode? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public DomainErrorCode? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(DomainErrorCode error) => new(false, default, error);
}
```

`Players/Player.cs`:

```csharp
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
```

- [ ] **Step 4: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: PASS, 6 тестов.

- [ ] **Step 5: Commit**

```bash
git add src/DartsLeaderboard.Domain tests/DartsLeaderboard.Domain.Tests
git commit -m "feat(domain): add result primitives and player entity"
```

---

### Task 3: Агрегат матча и старт матча

**Files:**
- Create: `src/DartsLeaderboard.Domain/Matches/GameMode.cs`, `MatchStatus.cs`, `MatchSettings.cs`, `MatchParticipant.cs`, `Throw.cs`, `Match.cs`
- Test: `tests/DartsLeaderboard.Domain.Tests/MatchStartTests.cs`

**Interfaces:**
- Consumes: `Result`, `Result<T>`, `DomainErrorCode` (Task 2), `Player` (Task 2).
- Produces:
  - `enum GameMode { X01 = 1, HighestTotal = 2 }`
  - `enum MatchStatus { InProgress = 1, Finished = 2, Abandoned = 3 }`
  - `sealed record MatchSettings(GameMode Mode, int? StartingScore, int? RoundLimit)` с `MatchSettings.X01(int startingScore)`, `MatchSettings.HighestTotal(int roundLimit)`, `Validate() -> Result`
  - `MatchParticipant` с `Id`, `MatchId`, `PlayerId`, `SeatOrder`, `Player? Player`, `PlayerName`
  - `Throw` с `Id`, `MatchId`, `ParticipantId`, `RoundNumber`, `Points`, `RecordedAt`
  - `Match` с `Id`, `Mode`, `StartingScore`, `RoundLimit`, `Status`, `StartedAt`, `FinishedAt`, `WinnerParticipantId`, `Participants`, `Throws`, `CurrentRoundNumber`, `CurrentParticipant`, `Match.Start(MatchSettings, IReadOnlyList<int> playerIds, DateTimeOffset now) -> Result<Match>`
- Методы `RecordThrow`, `UndoLastThrow`, `Abandon` появятся в Task 6; в этой задаче их ещё нет.

- [ ] **Step 1: Написать падающие тесты**

`tests/DartsLeaderboard.Domain.Tests/MatchStartTests.cs`:

```csharp
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Domain.Tests;

public class MatchStartTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_X01_SetsSettingsAndSeats()
    {
        var result = Match.Start(MatchSettings.X01(501), new[] { 7, 3, 9 }, Now);

        Assert.True(result.IsSuccess);
        var match = result.Value!;
        Assert.Equal(GameMode.X01, match.Mode);
        Assert.Equal(501, match.StartingScore);
        Assert.Null(match.RoundLimit);
        Assert.Equal(MatchStatus.InProgress, match.Status);
        Assert.Equal(Now, match.StartedAt);
        Assert.Equal(new[] { 7, 3, 9 }, match.Participants.Select(p => p.PlayerId));
        Assert.Equal(new[] { 0, 1, 2 }, match.Participants.Select(p => p.SeatOrder));
        Assert.Equal(1, match.CurrentRoundNumber);
        Assert.Equal(7, match.CurrentParticipant!.PlayerId);
    }

    [Fact]
    public void Start_HighestTotal_SetsRoundLimit()
    {
        var match = Match.Start(MatchSettings.HighestTotal(5), new[] { 1, 2 }, Now).Value!;

        Assert.Equal(GameMode.HighestTotal, match.Mode);
        Assert.Equal(5, match.RoundLimit);
        Assert.Null(match.StartingScore);
    }

    [Fact]
    public void Start_RejectsSinglePlayer()
    {
        var result = Match.Start(MatchSettings.X01(501), new[] { 1 }, Now);

        Assert.Equal(DomainErrorCode.TooFewParticipants, result.Error);
    }

    [Fact]
    public void Start_RejectsDuplicatePlayer()
    {
        var result = Match.Start(MatchSettings.X01(501), new[] { 1, 1 }, Now);

        Assert.Equal(DomainErrorCode.DuplicateParticipant, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(2001)]
    public void Start_RejectsInvalidStartingScore(int startingScore)
    {
        var result = Match.Start(MatchSettings.X01(startingScore), new[] { 1, 2 }, Now);

        Assert.Equal(DomainErrorCode.InvalidStartingScore, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Start_RejectsInvalidRoundLimit(int roundLimit)
    {
        var result = Match.Start(MatchSettings.HighestTotal(roundLimit), new[] { 1, 2 }, Now);

        Assert.Equal(DomainErrorCode.InvalidRoundLimit, result.Error);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: ошибки компиляции — нет типов `Match`, `MatchSettings`, `GameMode`.

- [ ] **Step 3: Реализовать типы матча**

`Matches/GameMode.cs`:

```csharp
namespace DartsLeaderboard.Domain.Matches;

public enum GameMode
{
    X01 = 1,
    HighestTotal = 2
}
```

`Matches/MatchStatus.cs`:

```csharp
namespace DartsLeaderboard.Domain.Matches;

public enum MatchStatus
{
    InProgress = 1,
    Finished = 2,
    Abandoned = 3
}
```

`Matches/MatchSettings.cs`:

```csharp
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches;

public sealed record MatchSettings(GameMode Mode, int? StartingScore, int? RoundLimit)
{
    public const int MaxStartingScore = 2000;
    public const int MaxRoundLimit = 50;

    public static MatchSettings X01(int startingScore) => new(GameMode.X01, startingScore, null);

    public static MatchSettings HighestTotal(int roundLimit) => new(GameMode.HighestTotal, null, roundLimit);

    public Result Validate() => Mode switch
    {
        GameMode.X01 when StartingScore is null or < 1 or > MaxStartingScore
            => Result.Failure(DomainErrorCode.InvalidStartingScore),
        GameMode.HighestTotal when RoundLimit is null or < 1 or > MaxRoundLimit
            => Result.Failure(DomainErrorCode.InvalidRoundLimit),
        _ => Result.Success()
    };
}
```

`Matches/MatchParticipant.cs`:

```csharp
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Domain.Matches;

public sealed class MatchParticipant
{
    private MatchParticipant() { }

    internal MatchParticipant(int playerId, int seatOrder)
    {
        PlayerId = playerId;
        SeatOrder = seatOrder;
    }

    public int Id { get; private set; }
    public int MatchId { get; private set; }
    public int PlayerId { get; private set; }
    public int SeatOrder { get; private set; }
    public Player? Player { get; private set; }

    public string PlayerName => Player?.Name ?? $"Игрок {SeatOrder + 1}";
}
```

`Matches/Throw.cs`:

```csharp
namespace DartsLeaderboard.Domain.Matches;

public sealed class Throw
{
    private Throw() { }

    internal Throw(int participantId, int roundNumber, int points, DateTimeOffset recordedAt)
    {
        ParticipantId = participantId;
        RoundNumber = roundNumber;
        Points = points;
        RecordedAt = recordedAt;
    }

    public const int MaxPoints = 180;

    public int Id { get; private set; }
    public int MatchId { get; private set; }
    public int ParticipantId { get; private set; }
    public int RoundNumber { get; private set; }
    public int Points { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
}
```

`Matches/Match.cs` (в этой задаче — только состояние и старт):

```csharp
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
```

- [ ] **Step 4: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: PASS, все тесты Task 2 и Task 3.

- [ ] **Step 5: Commit**

```bash
git add src/DartsLeaderboard.Domain tests/DartsLeaderboard.Domain.Tests
git commit -m "feat(domain): add match aggregate with participants and start rules"
```

---

### Task 4: Стратегия режимов и правила x01

**Files:**
- Create: `src/DartsLeaderboard.Domain/Matches/Rules/IGameRules.cs`, `MatchOutcome.cs`, `StatisticItem.cs`, `GameRules.cs`, `X01Rules.cs`
- Modify: `src/DartsLeaderboard.Domain/Matches/Match.cs` (добавить свойство `Rules`)
- Test: `tests/DartsLeaderboard.Domain.Tests/X01RulesTests.cs`

**Interfaces:**
- Consumes: `Match`, `MatchParticipant`, `Throw` (Task 3).
- Produces:
  - `sealed record MatchOutcome(bool IsFinished, int? WinnerParticipantId)` с `MatchOutcome.NotFinished`
  - `sealed record StatisticItem(string Name, string Value)`
  - `interface IGameRules` с членами: `Result ValidateThrow(Match match, MatchParticipant participant, int points)`, `MatchOutcome Evaluate(Match match)`, `int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber)`, `IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant)`, `IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match)`, `bool SupportsTrendChart { get; }`, `string Title { get; }`
  - `static class GameRules` с `GameRules.For(Match match) -> IGameRules`
  - `sealed class X01Rules(int startingScore) : IGameRules`
  - `Match.Rules -> IGameRules`
- Реализация `BuildPlayerStatistics` и `BuildMatchStatistics` для x01 появится в Task 7; здесь они возвращают пустой список.

- [ ] **Step 1: Написать падающие тесты**

`tests/DartsLeaderboard.Domain.Tests/X01RulesTests.cs`:

```csharp
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;

namespace DartsLeaderboard.Domain.Tests;

public class X01RulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    private static Match NewMatch() =>
        Match.Start(MatchSettings.X01(301), new[] { 1, 2 }, Now).Value!;

    [Fact]
    public void For_ReturnsX01Rules()
    {
        Assert.IsType<X01Rules>(GameRules.For(NewMatch()));
    }

    [Fact]
    public void ValidateThrow_AllowsPointsUpToRemaining()
    {
        var match = NewMatch();
        var rules = match.Rules;

        Assert.True(rules.ValidateThrow(match, match.Participants[0], 180).IsSuccess);
    }

    [Fact]
    public void ValidateThrow_RejectsPointsAboveRemaining()
    {
        var match = NewMatch();
        var rules = new X01Rules(50);

        var result = rules.ValidateThrow(match, match.Participants[0], 60);

        Assert.Equal(DomainErrorCode.PointsExceedRemaining, result.Error);
    }

    [Fact]
    public void Evaluate_NotFinishedWhileEveryoneHasRemainder()
    {
        var match = NewMatch();

        Assert.False(match.Rules.Evaluate(match).IsFinished);
    }

    [Fact]
    public void SupportsTrendChart_IsFalse()
    {
        Assert.False(NewMatch().Rules.SupportsTrendChart);
    }

    [Fact]
    public void Title_MentionsStartingScore()
    {
        Assert.Equal("301 на очки", NewMatch().Rules.Title);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests --filter X01RulesTests`
Expected: ошибки компиляции — нет `IGameRules`, `GameRules`, `X01Rules`.

- [ ] **Step 3: Реализовать стратегию и x01**

`Matches/Rules/MatchOutcome.cs`:

```csharp
namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed record MatchOutcome(bool IsFinished, int? WinnerParticipantId)
{
    public static readonly MatchOutcome NotFinished = new(false, null);
}
```

`Matches/Rules/StatisticItem.cs`:

```csharp
namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed record StatisticItem(string Name, string Value);
```

`Matches/Rules/IGameRules.cs`:

```csharp
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public interface IGameRules
{
    string Title { get; }

    bool SupportsTrendChart { get; }

    Result ValidateThrow(Match match, MatchParticipant participant, int points);

    MatchOutcome Evaluate(Match match);

    int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber);

    IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant);

    IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match);
}
```

`Matches/Rules/GameRules.cs`:

```csharp
namespace DartsLeaderboard.Domain.Matches.Rules;

public static class GameRules
{
    public static IGameRules For(Match match) => match.Mode switch
    {
        GameMode.X01 => new X01Rules(match.StartingScore!.Value),
        GameMode.HighestTotal => new HighestTotalRules(match.RoundLimit!.Value),
        _ => throw new NotSupportedException($"Режим {match.Mode} не поддерживается")
    };
}
```

`Matches/Rules/X01Rules.cs`:

```csharp
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class X01Rules : IGameRules
{
    private readonly int _startingScore;

    public X01Rules(int startingScore) => _startingScore = startingScore;

    public string Title => $"{_startingScore} на очки";

    public bool SupportsTrendChart => false;

    public Result ValidateThrow(Match match, MatchParticipant participant, int points) =>
        points > RemainingFor(match, participant)
            ? Result.Failure(DomainErrorCode.PointsExceedRemaining)
            : Result.Success();

    public MatchOutcome Evaluate(Match match)
    {
        var winner = match.Participants.FirstOrDefault(p => RemainingFor(match, p) == 0);
        return winner is null ? MatchOutcome.NotFinished : new MatchOutcome(true, winner.Id);
    }

    public int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber) =>
        _startingScore - match.Throws
            .Where(t => t.ParticipantId == participant.Id && t.RoundNumber <= roundNumber)
            .Sum(t => t.Points);

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant) =>
        Array.Empty<StatisticItem>();

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match) =>
        Array.Empty<StatisticItem>();

    internal int RemainingFor(Match match, MatchParticipant participant) =>
        _startingScore - match.PointsOf(participant.Id);
}
```

Заглушки статистики заменяются в Task 7.

`Matches/Match.cs` — добавить свойство рядом с `CurrentParticipant`:

```csharp
    public Rules.IGameRules Rules => Rules_Factory();

    private Rules.IGameRules Rules_Factory() => Rules.GameRules.For(this);
```

Проще и читаемее: `public IGameRules Rules => GameRules.For(this);` с `using DartsLeaderboard.Domain.Matches.Rules;` в начале файла. Используй этот вариант.

- [ ] **Step 4: Реализовать заглушку `HighestTotalRules`, чтобы фабрика компилировалась**

`Matches/Rules/HighestTotalRules.cs` (полная реализация — Task 5):

```csharp
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Domain.Matches.Rules;

public sealed class HighestTotalRules : IGameRules
{
    private readonly int _roundLimit;

    public HighestTotalRules(int roundLimit) => _roundLimit = roundLimit;

    public string Title => $"Максимум за {_roundLimit} раундов";

    public bool SupportsTrendChart => true;

    public Result ValidateThrow(Match match, MatchParticipant participant, int points) => Result.Success();

    public MatchOutcome Evaluate(Match match) => MatchOutcome.NotFinished;

    public int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber) => null;

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant) =>
        Array.Empty<StatisticItem>();

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match) =>
        Array.Empty<StatisticItem>();
}
```

- [ ] **Step 5: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/DartsLeaderboard.Domain tests/DartsLeaderboard.Domain.Tests
git commit -m "feat(domain): add game rules strategy with x01 implementation"
```

---

### Task 5: Правила режима «максимум за N раундов»

**Files:**
- Modify: `src/DartsLeaderboard.Domain/Matches/Rules/HighestTotalRules.cs`
- Test: `tests/DartsLeaderboard.Domain.Tests/HighestTotalRulesTests.cs`

**Interfaces:**
- Consumes: `IGameRules`, `MatchOutcome` (Task 4), `Match` (Task 3).
- Produces: рабочие `Evaluate` и `RunningValueAfterRound` в `HighestTotalRules`; статистика по-прежнему пустая до Task 7.

Тесты требуют записанных бросков, а публичный `Match.RecordThrow` появится только в Task 6. Поэтому здесь создаётся тестовый хелпер `MatchTestFactory`, который добавляет броски напрямую в приватный список агрегата через рефлексию: правила при этом не срабатывают, что для тестов `Evaluate` и `RunningValueAfterRound` как раз и нужно. В Task 6 хелпер дополняется методом, работающим через публичный API.

- [ ] **Step 1: Написать хелпер и падающие тесты**

`tests/DartsLeaderboard.Domain.Tests/MatchTestFactory.cs`:

```csharp
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
```

`tests/DartsLeaderboard.Domain.Tests/HighestTotalRulesTests.cs`:

```csharp
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Domain.Tests;

public class HighestTotalRulesTests
{
    private static Match NewMatch(int roundLimit = 2, int participants = 2) =>
        MatchTestFactory.Create(MatchSettings.HighestTotal(roundLimit), participants);

    [Fact]
    public void Evaluate_NotFinishedUntilRoundLimitReachedByEveryone()
    {
        var match = NewMatch().WithRawThrows(60, 40, 20);

        Assert.False(match.Rules.Evaluate(match).IsFinished);
    }

    [Fact]
    public void Evaluate_WinnerIsHighestTotal()
    {
        var match = NewMatch().WithRawThrows(60, 40, 20, 30);

        var outcome = match.Rules.Evaluate(match);

        Assert.True(outcome.IsFinished);
        Assert.Equal(match.Participants[0].Id, outcome.WinnerParticipantId);
    }

    [Fact]
    public void Evaluate_TieHasNoWinner()
    {
        var match = NewMatch().WithRawThrows(60, 60, 20, 20);

        var outcome = match.Rules.Evaluate(match);

        Assert.True(outcome.IsFinished);
        Assert.Null(outcome.WinnerParticipantId);
    }

    [Fact]
    public void RunningValueAfterRound_IsCumulativeSum()
    {
        var match = NewMatch().WithRawThrows(60, 40, 25, 30);
        var first = match.Participants[0];

        Assert.Equal(60, match.Rules.RunningValueAfterRound(match, first, 1));
        Assert.Equal(85, match.Rules.RunningValueAfterRound(match, first, 2));
    }

    [Fact]
    public void Title_MentionsRoundLimit()
    {
        Assert.Equal("Максимум за 5 раундов", MatchTestFactory
            .Create(MatchSettings.HighestTotal(5), 2)
            .Rules.Title);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests --filter HighestTotalRulesTests`
Expected: FAIL — `Evaluate` возвращает `MatchOutcome.NotFinished`, а `RunningValueAfterRound` возвращает `null` (заглушки из Task 4).

- [ ] **Step 3: Реализовать `Evaluate` и `RunningValueAfterRound`**

```csharp
    public MatchOutcome Evaluate(Match match)
    {
        var completedByEveryone = match.Participants.All(p => match.RoundCountOf(p.Id) >= _roundLimit);
        if (!completedByEveryone)
        {
            return MatchOutcome.NotFinished;
        }

        var totals = match.Participants
            .Select(p => new { Participant = p, Total = match.PointsOf(p.Id) })
            .ToList();

        var best = totals.Max(t => t.Total);
        var winners = totals.Where(t => t.Total == best).ToList();

        return winners.Count == 1
            ? new MatchOutcome(true, winners[0].Participant.Id)
            : new MatchOutcome(true, null);
    }

    public int? RunningValueAfterRound(Match match, MatchParticipant participant, int roundNumber) =>
        match.Throws
            .Where(t => t.ParticipantId == participant.Id && t.RoundNumber <= roundNumber)
            .Sum(t => t.Points);
```

- [ ] **Step 4: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/DartsLeaderboard.Domain tests/DartsLeaderboard.Domain.Tests
git commit -m "feat(domain): implement highest-total mode rules"
```

---

### Task 6: Запись броска, отмена и прерывание матча

**Files:**
- Modify: `src/DartsLeaderboard.Domain/Matches/Match.cs`
- Test: `tests/DartsLeaderboard.Domain.Tests/MatchThrowTests.cs`

**Interfaces:**
- Consumes: `Match` (Task 3), `IGameRules`, `MatchOutcome` (Task 4, 5).
- Produces:
  - `Match.RecordThrow(int points, DateTimeOffset now) -> Result<Throw>`
  - `Match.UndoLastThrow() -> Result`
  - `Match.Abandon(DateTimeOffset now) -> Result`
  - `Match.CompletedRoundCount -> int` (максимальный номер раунда среди записанных бросков)
  - тестовый хелпер `MatchTestFactory.WithThrows(this Match, params int[])`, которым пользуются задачи 7 и далее

- [ ] **Step 1: Написать падающие тесты**

`tests/DartsLeaderboard.Domain.Tests/MatchThrowTests.cs`:

```csharp
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Domain.Tests;

public class MatchThrowTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    private static Match X01(int startingScore = 301, int participants = 2) =>
        MatchTestFactory.Create(MatchSettings.X01(startingScore), participants);

    [Fact]
    public void RecordThrow_AssignsRoundAndParticipantInTurnOrder()
    {
        var match = X01();

        var first = match.RecordThrow(60, Now).Value!;
        var second = match.RecordThrow(45, Now).Value!;
        var third = match.RecordThrow(20, Now).Value!;

        Assert.Equal(match.Participants[0].Id, first.ParticipantId);
        Assert.Equal(1, first.RoundNumber);
        Assert.Equal(match.Participants[1].Id, second.ParticipantId);
        Assert.Equal(1, second.RoundNumber);
        Assert.Equal(match.Participants[0].Id, third.ParticipantId);
        Assert.Equal(2, third.RoundNumber);
        Assert.Equal(2, match.CurrentRoundNumber);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(181)]
    public void RecordThrow_RejectsPointsOutOfRange(int points)
    {
        var result = X01().RecordThrow(points, Now);

        Assert.Equal(DomainErrorCode.PointsOutOfRange, result.Error);
    }

    [Fact]
    public void RecordThrow_ZeroIsAllowed()
    {
        var match = X01();

        Assert.True(match.RecordThrow(0, Now).IsSuccess);
        Assert.Equal(301, match.Rules.RunningValueAfterRound(match, match.Participants[0], 1));
    }

    [Fact]
    public void RecordThrow_RejectsPointsAboveRemaining()
    {
        var match = X01(101);
        match.RecordThrow(100, Now);
        match.RecordThrow(50, Now);

        var result = match.RecordThrow(20, Now);

        Assert.Equal(DomainErrorCode.PointsExceedRemaining, result.Error);
    }

    [Fact]
    public void RecordThrow_FinishesMatchWhenRemainderIsZero()
    {
        var match = X01(101);
        match.RecordThrow(101, Now);

        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal(Now, match.FinishedAt);
        Assert.Equal(match.Participants[0].Id, match.WinnerParticipantId);
        Assert.Null(match.CurrentParticipant);
    }

    [Fact]
    public void RecordThrow_RejectedAfterMatchFinished()
    {
        var match = X01(101);
        match.RecordThrow(101, Now);

        var result = match.RecordThrow(20, Now);

        Assert.Equal(DomainErrorCode.MatchNotInProgress, result.Error);
    }

    [Fact]
    public void UndoLastThrow_RemovesThrowAndReopensMatch()
    {
        var match = X01(101);
        match.RecordThrow(101, Now);

        var result = match.UndoLastThrow();

        Assert.True(result.IsSuccess);
        Assert.Empty(match.Throws);
        Assert.Equal(MatchStatus.InProgress, match.Status);
        Assert.Null(match.FinishedAt);
        Assert.Null(match.WinnerParticipantId);
        Assert.Equal(match.Participants[0].Id, match.CurrentParticipant!.Id);
    }

    [Fact]
    public void UndoLastThrow_FailsWhenNoThrows()
    {
        Assert.Equal(DomainErrorCode.NoThrowsToUndo, X01().UndoLastThrow().Error);
    }

    [Fact]
    public void Abandon_MarksMatchAbandoned()
    {
        var match = X01();

        var result = match.Abandon(Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Abandoned, match.Status);
        Assert.Equal(Now, match.FinishedAt);
        Assert.Equal(DomainErrorCode.MatchNotInProgress, match.RecordThrow(20, Now).Error);
    }

    [Fact]
    public void CompletedRoundCount_CountsRecordedRounds()
    {
        var match = X01().WithThrows(60, 40, 20);

        Assert.Equal(2, match.CompletedRoundCount);
    }
}
```

- [ ] **Step 2: Дополнить тестовый хелпер методом через публичный API**

В `tests/DartsLeaderboard.Domain.Tests/MatchTestFactory.cs` добавить рядом с `WithRawThrows`:

```csharp
    /// <summary>Записывает броски по кругу через публичный API агрегата, с проверкой правил.</summary>
    public static Match WithThrows(this Match match, params int[] points)
    {
        for (var i = 0; i < points.Length; i++)
        {
            var result = match.RecordThrow(points[i], Start.AddMinutes(i));
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Бросок {points[i]} отклонён: {result.Error}");
            }
        }

        return match;
    }
```

- [ ] **Step 3: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests --filter MatchThrowTests`
Expected: ошибки компиляции — нет методов `RecordThrow`, `UndoLastThrow`, `Abandon`, `CompletedRoundCount`.

- [ ] **Step 4: Реализовать методы в `Match`**

```csharp
    public int CompletedRoundCount => _throws.Count == 0 ? 0 : _throws.Max(t => t.RoundNumber);

    public Result<Throw> RecordThrow(int points, DateTimeOffset now)
    {
        if (Status != MatchStatus.InProgress)
        {
            return Result<Throw>.Failure(DomainErrorCode.MatchNotInProgress);
        }

        if (points < 0 || points > Throw.MaxPoints)
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

        var recorded = new Throw(participant.Id, CurrentRoundNumber, points, now);
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

        var last = Throws[^1];
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
```

- [ ] **Step 5: Прогнать все тесты домена**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: PASS, включая тесты Task 5.

- [ ] **Step 6: Commit**

```bash
git add src/DartsLeaderboard.Domain tests/DartsLeaderboard.Domain.Tests
git commit -m "feat(domain): record, undo and abandon throws in match aggregate"
```

---

### Task 7: Статистика по режимам

**Files:**
- Modify: `src/DartsLeaderboard.Domain/Matches/Rules/X01Rules.cs`, `HighestTotalRules.cs`
- Test: `tests/DartsLeaderboard.Domain.Tests/StatisticsTests.cs`

**Interfaces:**
- Consumes: `IGameRules`, `StatisticItem` (Task 4), `Match.RecordThrow` (Task 6).
- Produces: заполненные `BuildPlayerStatistics` и `BuildMatchStatistics` для обоих режимов. Названия показателей фиксированы и используются в тестах UI:
  - x01, по игроку: `Осталось`, `Раундов`, плюс `Закрыл за` у победителя.
  - x01, по матчу: `Раундов сыграно`, `Ближе всех к финишу`.
  - Максимум, по игроку: `Сумма`, `Максимум`, `Минимум`, `Среднее`, `Раундов`.
  - Максимум, по матчу: `Лучший бросок`, `Худший бросок`, `Средний раунд`, `Осталось раундов`.
  - Значение отсутствующего показателя — строка `—`.

- [ ] **Step 1: Написать падающие тесты**

`tests/DartsLeaderboard.Domain.Tests/StatisticsTests.cs`:

```csharp
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;

namespace DartsLeaderboard.Domain.Tests;

public class StatisticsTests
{
    private static string Value(IReadOnlyList<StatisticItem> items, string name) =>
        items.Single(i => i.Name == name).Value;

    [Fact]
    public void X01_PlayerStatistics_ShowRemainderAndRounds()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(301), 2).WithThrows(60, 40, 100);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.Equal("141", Value(stats, "Осталось"));
        Assert.Equal("2", Value(stats, "Раундов"));
        Assert.DoesNotContain(stats, i => i.Name == "Закрыл за");
    }

    [Fact]
    public void X01_PlayerStatistics_ShowClosingRoundsForWinner()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(101), 2).WithThrows(60, 20, 41);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.Equal("0", Value(stats, "Осталось"));
        Assert.Equal("2", Value(stats, "Закрыл за"));
    }

    [Fact]
    public void X01_PlayerStatistics_HaveNoThrowMetrics()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(301), 2).WithThrows(60, 40);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.DoesNotContain(stats, i => i.Name is "Максимум" or "Минимум" or "Среднее");
    }

    [Fact]
    public void X01_MatchStatistics_ShowRoundsAndLeader()
    {
        var match = MatchTestFactory.Create(MatchSettings.X01(301), 2).WithThrows(60, 40, 100);
        var stats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("2", Value(stats, "Раундов сыграно"));
        Assert.Equal("Игрок 1 (141)", Value(stats, "Ближе всех к финишу"));
    }

    [Fact]
    public void HighestTotal_PlayerStatistics_CountZerosInEveryMetric()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(3), 2)
            .WithThrows(60, 10, 0, 20, 30, 30);
        var stats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);

        Assert.Equal("90", Value(stats, "Сумма"));
        Assert.Equal("60", Value(stats, "Максимум"));
        Assert.Equal("0", Value(stats, "Минимум"));
        Assert.Equal("30,0", Value(stats, "Среднее"));
        Assert.Equal("3", Value(stats, "Раундов"));
    }

    [Fact]
    public void HighestTotal_MatchStatistics_ShowBestAndWorstThrow()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(2), 2)
            .WithThrows(60, 10, 0, 20);
        var stats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("60 · Игрок 1", Value(stats, "Лучший бросок"));
        Assert.Equal("0 · Игрок 1", Value(stats, "Худший бросок"));
        Assert.Equal("22,5", Value(stats, "Средний раунд"));
        Assert.Equal("0", Value(stats, "Осталось раундов"));
    }

    [Fact]
    public void HighestTotal_Statistics_HandleMatchWithoutThrows()
    {
        var match = MatchTestFactory.Create(MatchSettings.HighestTotal(5), 2);
        var playerStats = match.Rules.BuildPlayerStatistics(match, match.Participants[0]);
        var matchStats = match.Rules.BuildMatchStatistics(match);

        Assert.Equal("0", Value(playerStats, "Сумма"));
        Assert.Equal("—", Value(playerStats, "Среднее"));
        Assert.Equal("—", Value(matchStats, "Лучший бросок"));
        Assert.Equal("5", Value(matchStats, "Осталось раундов"));
    }
}
```

Форматирование чисел с запятой как десятичным разделителем: `InvariantGlobalization` даёт точку, поэтому в реализации используй явную культуру `ru-RU` (см. шаг 3).

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests --filter StatisticsTests`
Expected: FAIL — статистика возвращает пустые списки, `Single` бросает исключение.

- [ ] **Step 3: Реализовать статистику x01**

В `X01Rules` заменить заглушки. Вверху файла добавить `using System.Globalization;` и общий формат:

```csharp
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant)
    {
        var items = new List<StatisticItem>
        {
            new("Осталось", RemainingFor(match, participant).ToString(Ru)),
            new("Раундов", match.RoundCountOf(participant.Id).ToString(Ru))
        };

        if (match.WinnerParticipantId == participant.Id)
        {
            items.Add(new StatisticItem("Закрыл за", match.RoundCountOf(participant.Id).ToString(Ru)));
        }

        return items;
    }

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match)
    {
        var leader = match.Participants
            .OrderBy(p => RemainingFor(match, p))
            .ThenBy(p => p.SeatOrder)
            .First();

        return new List<StatisticItem>
        {
            new("Раундов сыграно", match.CompletedRoundCount.ToString(Ru)),
            new("Ближе всех к финишу", $"{leader.PlayerName} ({RemainingFor(match, leader)})")
        };
    }
```

- [ ] **Step 4: Реализовать статистику режима на максимум**

В `HighestTotalRules` (также `using System.Globalization;`):

```csharp
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private const string NoValue = "—";

    public IReadOnlyList<StatisticItem> BuildPlayerStatistics(Match match, MatchParticipant participant)
    {
        var points = match.Throws
            .Where(t => t.ParticipantId == participant.Id)
            .Select(t => t.Points)
            .ToList();

        return new List<StatisticItem>
        {
            new("Сумма", points.Sum().ToString(Ru)),
            new("Максимум", points.Count == 0 ? NoValue : points.Max().ToString(Ru)),
            new("Минимум", points.Count == 0 ? NoValue : points.Min().ToString(Ru)),
            new("Среднее", points.Count == 0 ? NoValue : points.Average().ToString("F1", Ru)),
            new("Раундов", points.Count.ToString(Ru))
        };
    }

    public IReadOnlyList<StatisticItem> BuildMatchStatistics(Match match)
    {
        var throws = match.Throws;
        var remainingRounds = Math.Max(0, _roundLimit - match.CompletedRoundCount);

        if (throws.Count == 0)
        {
            return new List<StatisticItem>
            {
                new("Лучший бросок", NoValue),
                new("Худший бросок", NoValue),
                new("Средний раунд", NoValue),
                new("Осталось раундов", remainingRounds.ToString(Ru))
            };
        }

        var best = throws.OrderByDescending(t => t.Points).First();
        var worst = throws.OrderBy(t => t.Points).First();

        return new List<StatisticItem>
        {
            new("Лучший бросок", Describe(match, best)),
            new("Худший бросок", Describe(match, worst)),
            new("Средний раунд", throws.Average(t => t.Points).ToString("F1", Ru)),
            new("Осталось раундов", remainingRounds.ToString(Ru))
        };
    }

    private static string Describe(Match match, Throw recorded)
    {
        var participant = match.Participants.First(p => p.Id == recorded.ParticipantId);
        return $"{recorded.Points} · {participant.PlayerName}";
    }
```

- [ ] **Step 5: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Domain.Tests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/DartsLeaderboard.Domain tests/DartsLeaderboard.Domain.Tests
git commit -m "feat(domain): add mode-specific match statistics"
```

---

### Task 8: Контракты Application и маппинг состояния матча

**Files:**
- Create: `src/DartsLeaderboard.Application/Abstractions/IClock.cs`, `IPlayerRepository.cs`, `IMatchRepository.cs`, `ILeaderboardQueries.cs`, `IMatchQueries.cs`, `IMatchNotifier.cs`, `MatchConflictException.cs`
- Create: `src/DartsLeaderboard.Application/Common/OperationResult.cs`, `ErrorText.cs`
- Create: `src/DartsLeaderboard.Application/Contracts/PlayerDto.cs`, `MatchStateDto.cs`, `MatchSetupRequest.cs`, `LeaderboardRowDto.cs`, `MatchListItemDto.cs`
- Create: `src/DartsLeaderboard.Application/Matches/MatchStateMapper.cs`
- Test: `tests/DartsLeaderboard.Application.Tests/TestMatchBuilder.cs`, `MatchStateMapperTests.cs`

**Interfaces:**
- Consumes: `Match`, `MatchSettings`, `IGameRules`, `StatisticItem`, `DomainErrorCode` (задачи 2-7).
- Produces:
  - `IClock { DateTimeOffset UtcNow { get; } }`
  - `IPlayerRepository`: `ListAsync(bool includeArchived, CancellationToken)`, `GetAsync(int playerId, CancellationToken)`, `ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken)`, `AddAsync(Player, CancellationToken)`, `SaveChangesAsync(CancellationToken)`
  - `IMatchRepository`: `GetAsync(int matchId, CancellationToken)`, `AddAsync(Match, CancellationToken)`, `SaveChangesAsync(CancellationToken)`, `AllPlayersExistAsync(IReadOnlyList<int> playerIds, CancellationToken)`
  - `ILeaderboardQueries.GetAsync(CancellationToken) -> IReadOnlyList<LeaderboardRowDto>`
  - `IMatchQueries.ListAsync(MatchListFilter, CancellationToken) -> IReadOnlyList<MatchListItemDto>`
  - `IMatchNotifier`: `NotifyChanged(int matchId)`, `Subscribe(int matchId, Func<Task> handler) -> IDisposable`
  - `MatchConflictException : Exception`
  - `OperationResult`, `OperationResult<T>` с `Success`, `Fail(string)`, `Fail(DomainErrorCode)`
  - `ErrorText.For(DomainErrorCode) -> string`
  - DTO: `PlayerDto`, `StatisticDto`, `MatchCellDto`, `MatchRowDto`, `MatchColumnDto`, `ChartSeriesDto`, `MatchStateDto`, `GameModeOption`, `MatchSetupRequest`, `LeaderboardRowDto`, `MatchListFilter`, `MatchListItemDto`
  - `MatchStateMapper.ToDto(Match match) -> MatchStateDto`

- [ ] **Step 1: Написать падающие тесты маппера**

`tests/DartsLeaderboard.Application.Tests/TestMatchBuilder.cs`:

```csharp
using System.Reflection;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests;

internal static class TestMatchBuilder
{
    public static readonly DateTimeOffset Start = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    public static Match Create(MatchSettings settings, params string[] playerNames)
    {
        var match = Match.Start(settings, Enumerable.Range(1, playerNames.Length).ToArray(), Start).Value!;
        SetProperty(match, nameof(Match.Id), 12);

        var seat = 0;
        foreach (var participant in match.Participants)
        {
            SetProperty(participant, nameof(MatchParticipant.Id), seat + 1);
            SetProperty(participant, nameof(MatchParticipant.Player),
                Domain.Players.Player.Create(playerNames[seat], Start).Value!);
            seat++;
        }

        return match;
    }

    public static Match WithThrows(this Match match, params int[] points)
    {
        for (var i = 0; i < points.Length; i++)
        {
            var result = match.RecordThrow(points[i], Start.AddMinutes(i));
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Бросок {points[i]} отклонён: {result.Error}");
            }
        }

        return match;
    }

    private static void SetProperty(object target, string propertyName, object value) =>
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(target, new[] { value });
}
```

`tests/DartsLeaderboard.Application.Tests/MatchStateMapperTests.cs`:

```csharp
using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests;

public class MatchStateMapperTests
{
    [Fact]
    public void ToDto_X01_BuildsColumnsRowsAndRemainders()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.X01(301), "Максим", "Аня")
            .WithThrows(60, 45, 100);

        var dto = MatchStateMapper.ToDto(match);

        Assert.Equal(12, dto.MatchId);
        Assert.Equal("301 на очки", dto.ModeTitle);
        Assert.Equal("Идёт", dto.StatusTitle);
        Assert.True(dto.IsInProgress);
        Assert.Equal(new[] { "Максим", "Аня" }, dto.Columns.Select(c => c.PlayerName));
        Assert.Equal(2, dto.Rows.Count);
        Assert.Equal(60, dto.Rows[0].Cells[0].Points);
        Assert.Equal(241, dto.Rows[0].Cells[0].RunningValue);
        Assert.Equal(100, dto.Rows[1].Cells[0].Points);
        Assert.Equal(141, dto.Rows[1].Cells[0].RunningValue);
        Assert.Null(dto.Rows[1].Cells[1].Points);
        Assert.Equal("Аня", dto.CurrentPlayerName);
        Assert.False(dto.ShowTrendChart);
        Assert.Empty(dto.ChartSeries);
    }

    [Fact]
    public void ToDto_X01_ShowsWinnerAndFinishedStatus()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.X01(101), "Максим", "Аня")
            .WithThrows(101);

        var dto = MatchStateMapper.ToDto(match);

        Assert.False(dto.IsInProgress);
        Assert.Equal("Завершён", dto.StatusTitle);
        Assert.Equal("Максим", dto.WinnerPlayerName);
        Assert.Null(dto.CurrentParticipantId);
    }

    [Fact]
    public void ToDto_HighestTotal_BuildsChartSeries()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.HighestTotal(3), "Максим", "Аня")
            .WithThrows(60, 20, 40, 10);

        var dto = MatchStateMapper.ToDto(match);

        Assert.True(dto.ShowTrendChart);
        Assert.Equal(2, dto.ChartSeries.Count);
        Assert.Equal("Максим", dto.ChartSeries[0].PlayerName);
        Assert.Equal(new double[] { 60, 40 }, dto.ChartSeries[0].RoundPoints);
        Assert.Equal(new double[] { 60, 100 }, dto.ChartSeries[0].CumulativePoints);
        Assert.Contains(dto.MatchStatistics, s => s.Name == "Лучший бросок" && s.Value == "60 · Максим");
    }

    [Fact]
    public void ToDto_HighestTotal_TieShowsDrawStatus()
    {
        var match = TestMatchBuilder
            .Create(MatchSettings.HighestTotal(1), "Максим", "Аня")
            .WithThrows(60, 60);

        var dto = MatchStateMapper.ToDto(match);

        Assert.Equal("Ничья", dto.StatusTitle);
        Assert.Null(dto.WinnerPlayerName);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests`
Expected: ошибки компиляции — нет `MatchStateMapper` и DTO.

- [ ] **Step 3: Создать абстракции и общие типы**

`Abstractions/IClock.cs`:

```csharp
namespace DartsLeaderboard.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

`Abstractions/IPlayerRepository.cs`:

```csharp
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Application.Abstractions;

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> ListAsync(bool includeArchived, CancellationToken cancellationToken);

    Task<Player?> GetAsync(int playerId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken cancellationToken);

    Task AddAsync(Player player, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

`Abstractions/IMatchRepository.cs`:

```csharp
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Abstractions;

public interface IMatchRepository
{
    Task<Match?> GetAsync(int matchId, CancellationToken cancellationToken);

    Task AddAsync(Match match, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> AllPlayersExistAsync(IReadOnlyList<int> playerIds, CancellationToken cancellationToken);
}
```

`Abstractions/MatchConflictException.cs`:

```csharp
namespace DartsLeaderboard.Application.Abstractions;

/// <summary>Бросок этого раунда уже записан другим устройством.</summary>
public sealed class MatchConflictException : Exception
{
    public MatchConflictException(Exception? innerException = null)
        : base("Раунд уже записан", innerException)
    {
    }
}
```

`Abstractions/IMatchNotifier.cs`:

```csharp
namespace DartsLeaderboard.Application.Abstractions;

public interface IMatchNotifier
{
    void NotifyChanged(int matchId);

    IDisposable Subscribe(int matchId, Func<Task> handler);
}
```

`Abstractions/ILeaderboardQueries.cs` и `IMatchQueries.cs`:

```csharp
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Abstractions;

public interface ILeaderboardQueries
{
    Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken);
}

public interface IMatchQueries
{
    Task<IReadOnlyList<MatchListItemDto>> ListAsync(MatchListFilter filter, CancellationToken cancellationToken);
}
```

`Common/ErrorText.cs`:

```csharp
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
```

`Common/OperationResult.cs`:

```csharp
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Common;

public sealed record OperationResult(bool IsSuccess, string? ErrorMessage)
{
    public static OperationResult Success() => new(true, null);

    public static OperationResult Fail(string message) => new(false, message);

    public static OperationResult Fail(DomainErrorCode code) => new(false, ErrorText.For(code));
}

public sealed record OperationResult<T>(bool IsSuccess, T? Value, string? ErrorMessage)
{
    public static OperationResult<T> Success(T value) => new(true, value, null);

    public static OperationResult<T> Fail(string message) => new(false, default, message);

    public static OperationResult<T> Fail(DomainErrorCode code) => new(false, default, ErrorText.For(code));
}
```

- [ ] **Step 4: Создать DTO**

`Contracts/PlayerDto.cs`:

```csharp
namespace DartsLeaderboard.Application.Contracts;

public sealed record PlayerDto(int Id, string Name, bool IsArchived);
```

`Contracts/MatchStateDto.cs`:

```csharp
namespace DartsLeaderboard.Application.Contracts;

public sealed record StatisticDto(string Name, string Value);

public sealed record MatchCellDto(int? Points, int? RunningValue);

public sealed record MatchRowDto(int RoundNumber, IReadOnlyList<MatchCellDto> Cells);

public sealed record MatchColumnDto(int ParticipantId, string PlayerName, IReadOnlyList<StatisticDto> Statistics);

public sealed record ChartSeriesDto(
    string PlayerName,
    IReadOnlyList<double> RoundPoints,
    IReadOnlyList<double> CumulativePoints);

public sealed record MatchStateDto(
    int MatchId,
    string ModeTitle,
    string StatusTitle,
    bool IsInProgress,
    int CurrentRoundNumber,
    int? CurrentParticipantId,
    string? CurrentPlayerName,
    string? WinnerPlayerName,
    IReadOnlyList<MatchColumnDto> Columns,
    IReadOnlyList<MatchRowDto> Rows,
    IReadOnlyList<StatisticDto> MatchStatistics,
    bool ShowTrendChart,
    IReadOnlyList<ChartSeriesDto> ChartSeries);
```

`Contracts/MatchSetupRequest.cs`:

```csharp
namespace DartsLeaderboard.Application.Contracts;

public enum GameModeOption
{
    X01 = 1,
    HighestTotal = 2
}

public sealed record MatchSetupRequest(
    GameModeOption Mode,
    int? StartingScore,
    int? RoundLimit,
    IReadOnlyList<int> PlayerIds);
```

`Contracts/LeaderboardRowDto.cs`:

```csharp
namespace DartsLeaderboard.Application.Contracts;

public sealed record LeaderboardRowDto(int PlayerId, string PlayerName, int Wins, int MatchesPlayed)
{
    public double WinRate => MatchesPlayed == 0 ? 0 : (double)Wins / MatchesPlayed;
}
```

`Contracts/MatchListItemDto.cs`:

```csharp
namespace DartsLeaderboard.Application.Contracts;

public enum MatchListFilter
{
    InProgress = 1,
    Finished = 2
}

public sealed record MatchListItemDto(
    int MatchId,
    string ModeTitle,
    string Participants,
    string? WinnerName,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt);
```

- [ ] **Step 5: Реализовать маппер**

`Matches/MatchStateMapper.cs`:

```csharp
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;

namespace DartsLeaderboard.Application.Matches;

public static class MatchStateMapper
{
    public static MatchStateDto ToDto(Match match)
    {
        var rules = match.Rules;
        var participants = match.Participants;

        var rowCount = match.Status == MatchStatus.InProgress
            ? Math.Max(match.CompletedRoundCount, match.CurrentRoundNumber)
            : match.CompletedRoundCount;

        var throwsByCell = match.Throws.ToDictionary(t => (t.ParticipantId, t.RoundNumber));

        var rows = new List<MatchRowDto>();
        for (var round = 1; round <= rowCount; round++)
        {
            var cells = participants
                .Select(p => throwsByCell.TryGetValue((p.Id, round), out var recorded)
                    ? new MatchCellDto(recorded.Points, rules.RunningValueAfterRound(match, p, round))
                    : new MatchCellDto(null, null))
                .ToList();

            rows.Add(new MatchRowDto(round, cells));
        }

        var columns = participants
            .Select(p => new MatchColumnDto(p.Id, p.PlayerName, Map(rules.BuildPlayerStatistics(match, p))))
            .ToList();

        var winner = participants.FirstOrDefault(p => p.Id == match.WinnerParticipantId);

        return new MatchStateDto(
            match.Id,
            rules.Title,
            StatusTitle(match),
            match.Status == MatchStatus.InProgress,
            match.CurrentRoundNumber,
            match.CurrentParticipant?.Id,
            match.CurrentParticipant?.PlayerName,
            winner?.PlayerName,
            columns,
            rows,
            Map(rules.BuildMatchStatistics(match)),
            rules.SupportsTrendChart,
            rules.SupportsTrendChart ? BuildSeries(match) : Array.Empty<ChartSeriesDto>());
    }

    private static IReadOnlyList<StatisticDto> Map(IReadOnlyList<StatisticItem> items) =>
        items.Select(i => new StatisticDto(i.Name, i.Value)).ToList();

    private static string StatusTitle(Match match) => match.Status switch
    {
        MatchStatus.InProgress => "Идёт",
        MatchStatus.Abandoned => "Прерван",
        MatchStatus.Finished when match.WinnerParticipantId is null => "Ничья",
        _ => "Завершён"
    };

    private static IReadOnlyList<ChartSeriesDto> BuildSeries(Match match) =>
        match.Participants
            .Select(p =>
            {
                var points = match.Throws
                    .Where(t => t.ParticipantId == p.Id)
                    .OrderBy(t => t.RoundNumber)
                    .Select(t => (double)t.Points)
                    .ToList();

                var cumulative = new List<double>(points.Count);
                var running = 0d;
                foreach (var value in points)
                {
                    running += value;
                    cumulative.Add(running);
                }

                return new ChartSeriesDto(p.PlayerName, points, cumulative);
            })
            .ToList();
}
```

- [ ] **Step 6: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests`
Expected: PASS, 4 теста.

- [ ] **Step 7: Commit**

```bash
git add src/DartsLeaderboard.Application tests/DartsLeaderboard.Application.Tests
git commit -m "feat(app): add contracts, abstractions and match state mapper"
```

---

### Task 9: Use case-сервисы справочника игроков

**Files:**
- Create: `src/DartsLeaderboard.Application/Players/GetPlayersService.cs`, `AddPlayerService.cs`, `RenamePlayerService.cs`, `SetPlayerArchivedService.cs`
- Test: `tests/DartsLeaderboard.Application.Tests/Fakes/FakePlayerRepository.cs`, `Fakes/FixedClock.cs`, `PlayerServicesTests.cs`

**Interfaces:**
- Consumes: `IPlayerRepository`, `IClock`, `OperationResult`, `PlayerDto` (Task 8), `Player` (Task 2).
- Produces:
  - `GetPlayersService.ExecuteAsync(bool includeArchived, CancellationToken = default) -> Task<IReadOnlyList<PlayerDto>>`
  - `AddPlayerService.ExecuteAsync(string name, CancellationToken = default) -> Task<OperationResult<PlayerDto>>`
  - `RenamePlayerService.ExecuteAsync(int playerId, string name, CancellationToken = default) -> Task<OperationResult>`
  - `SetPlayerArchivedService.ExecuteAsync(int playerId, bool archived, CancellationToken = default) -> Task<OperationResult>`
  - тестовые двойники `FakePlayerRepository`, `FixedClock`, используемые в задачах 9-11

- [ ] **Step 1: Написать фейки и падающие тесты**

`tests/DartsLeaderboard.Application.Tests/Fakes/FixedClock.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class FixedClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);
}
```

`tests/DartsLeaderboard.Application.Tests/Fakes/FakePlayerRepository.cs`:

```csharp
using System.Reflection;
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class FakePlayerRepository : IPlayerRepository
{
    private readonly List<Player> _players = new();
    private int _nextId;

    public int SaveCount { get; private set; }

    public Player Seed(string name, bool archived = false)
    {
        var player = Player.Create(name, new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)).Value!;
        AssignId(player);
        player.SetArchived(archived);
        _players.Add(player);
        return player;
    }

    public Task<IReadOnlyList<Player>> ListAsync(bool includeArchived, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Player>>(_players
            .Where(p => includeArchived || !p.IsArchived)
            .OrderBy(p => p.Name)
            .ToList());

    public Task<Player?> GetAsync(int playerId, CancellationToken cancellationToken) =>
        Task.FromResult(_players.FirstOrDefault(p => p.Id == playerId));

    public Task<bool> ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken cancellationToken) =>
        Task.FromResult(_players.Any(p =>
            p.Id != excludePlayerId &&
            string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Player player, CancellationToken cancellationToken)
    {
        AssignId(player);
        _players.Add(player);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private void AssignId(Player player) =>
        typeof(Player)
            .GetProperty(nameof(Player.Id), BindingFlags.Public | BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true)!
            .Invoke(player, new object[] { ++_nextId });
}
```

`tests/DartsLeaderboard.Application.Tests/PlayerServicesTests.cs`:

```csharp
using DartsLeaderboard.Application.Players;
using DartsLeaderboard.Application.Tests.Fakes;

namespace DartsLeaderboard.Application.Tests;

public class PlayerServicesTests
{
    private readonly FakePlayerRepository _repository = new();
    private readonly FixedClock _clock = new();

    [Fact]
    public async Task AddPlayer_SavesTrimmedName()
    {
        var service = new AddPlayerService(_repository, _clock);

        var result = await service.ExecuteAsync("  Максим ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Максим", result.Value!.Name);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task AddPlayer_RejectsDuplicateNameIgnoringCase()
    {
        _repository.Seed("Максим");
        var service = new AddPlayerService(_repository, _clock);

        var result = await service.ExecuteAsync("максим");

        Assert.False(result.IsSuccess);
        Assert.Equal("Игрок с таким именем уже есть", result.ErrorMessage);
    }

    [Fact]
    public async Task AddPlayer_RejectsEmptyName()
    {
        var service = new AddPlayerService(_repository, _clock);

        var result = await service.ExecuteAsync("   ");

        Assert.Equal("Имя игрока не может быть пустым", result.ErrorMessage);
    }

    [Fact]
    public async Task GetPlayers_HidesArchivedByDefault()
    {
        _repository.Seed("Максим");
        _repository.Seed("Пётр", archived: true);
        var service = new GetPlayersService(_repository);

        var visible = await service.ExecuteAsync(includeArchived: false);
        var all = await service.ExecuteAsync(includeArchived: true);

        Assert.Single(visible);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task RenamePlayer_UpdatesName()
    {
        var player = _repository.Seed("Аня");
        var service = new RenamePlayerService(_repository);

        var result = await service.ExecuteAsync(player.Id, "Анна");

        Assert.True(result.IsSuccess);
        Assert.Equal("Анна", player.Name);
    }

    [Fact]
    public async Task RenamePlayer_FailsForUnknownPlayer()
    {
        var service = new RenamePlayerService(_repository);

        var result = await service.ExecuteAsync(42, "Анна");

        Assert.Equal("Игрок не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task SetArchived_TogglesFlag()
    {
        var player = _repository.Seed("Пётр");
        var service = new SetPlayerArchivedService(_repository);

        await service.ExecuteAsync(player.Id, archived: true);

        Assert.True(player.IsArchived);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests --filter PlayerServicesTests`
Expected: ошибки компиляции — нет сервисов игроков.

- [ ] **Step 3: Реализовать сервисы**

`Players/GetPlayersService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Players;

public sealed class GetPlayersService(IPlayerRepository repository)
{
    public async Task<IReadOnlyList<PlayerDto>> ExecuteAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var players = await repository.ListAsync(includeArchived, cancellationToken);
        return players.Select(p => new PlayerDto(p.Id, p.Name, p.IsArchived)).ToList();
    }
}
```

`Players/AddPlayerService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Players;

namespace DartsLeaderboard.Application.Players;

public sealed class AddPlayerService(IPlayerRepository repository, IClock clock)
{
    public async Task<OperationResult<PlayerDto>> ExecuteAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var created = Player.Create(name, clock.UtcNow);
        if (!created.IsSuccess)
        {
            return OperationResult<PlayerDto>.Fail(created.Error!.Value);
        }

        if (await repository.ExistsWithNameAsync(created.Value!.Name, null, cancellationToken))
        {
            return OperationResult<PlayerDto>.Fail(DomainErrorCode.PlayerNameTaken);
        }

        await repository.AddAsync(created.Value, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var player = created.Value;
        return OperationResult<PlayerDto>.Success(new PlayerDto(player.Id, player.Name, player.IsArchived));
    }
}
```

`Players/RenamePlayerService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Players;

public sealed class RenamePlayerService(IPlayerRepository repository)
{
    public async Task<OperationResult> ExecuteAsync(
        int playerId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var player = await repository.GetAsync(playerId, cancellationToken);
        if (player is null)
        {
            return OperationResult.Fail(DomainErrorCode.PlayerNotFound);
        }

        if (await repository.ExistsWithNameAsync(name, playerId, cancellationToken))
        {
            return OperationResult.Fail(DomainErrorCode.PlayerNameTaken);
        }

        var renamed = player.Rename(name);
        if (!renamed.IsSuccess)
        {
            return OperationResult.Fail(renamed.Error!.Value);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
```

`Players/SetPlayerArchivedService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Players;

public sealed class SetPlayerArchivedService(IPlayerRepository repository)
{
    public async Task<OperationResult> ExecuteAsync(
        int playerId,
        bool archived,
        CancellationToken cancellationToken = default)
    {
        var player = await repository.GetAsync(playerId, cancellationToken);
        if (player is null)
        {
            return OperationResult.Fail(DomainErrorCode.PlayerNotFound);
        }

        player.SetArchived(archived);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
```

- [ ] **Step 4: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/DartsLeaderboard.Application tests/DartsLeaderboard.Application.Tests
git commit -m "feat(app): add player directory use cases"
```

---

### Task 10: Use case-сервисы матча

**Files:**
- Create: `src/DartsLeaderboard.Application/Matches/StartMatchService.cs`, `GetMatchStateService.cs`, `RecordThrowService.cs`, `UndoLastThrowService.cs`, `AbandonMatchService.cs`
- Test: `tests/DartsLeaderboard.Application.Tests/Fakes/FakeMatchRepository.cs`, `Fakes/RecordingNotifier.cs`, `MatchServicesTests.cs`

**Interfaces:**
- Consumes: `IMatchRepository`, `IPlayerRepository`, `IClock`, `IMatchNotifier`, `MatchConflictException`, `MatchStateMapper`, `MatchSetupRequest` (Task 8), агрегат `Match` (задачи 3-7).
- Produces:
  - `StartMatchService.ExecuteAsync(MatchSetupRequest request, CancellationToken = default) -> Task<OperationResult<int>>` (возвращает Id созданного матча)
  - `GetMatchStateService.ExecuteAsync(int matchId, CancellationToken = default) -> Task<OperationResult<MatchStateDto>>`
  - `RecordThrowService.ExecuteAsync(int matchId, int points, CancellationToken = default) -> Task<OperationResult<MatchStateDto>>`
  - `UndoLastThrowService.ExecuteAsync(int matchId, CancellationToken = default) -> Task<OperationResult<MatchStateDto>>`
  - `AbandonMatchService.ExecuteAsync(int matchId, CancellationToken = default) -> Task<OperationResult>`

- [ ] **Step 1: Написать фейки и падающие тесты**

`tests/DartsLeaderboard.Application.Tests/Fakes/FakeMatchRepository.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class FakeMatchRepository : IMatchRepository
{
    private readonly Dictionary<int, Match> _matches = new();

    public bool ThrowConflictOnSave { get; set; }

    public int SaveCount { get; private set; }

    public void Seed(Match match) => _matches[match.Id] = match;

    public Task<Match?> GetAsync(int matchId, CancellationToken cancellationToken) =>
        Task.FromResult(_matches.TryGetValue(matchId, out var match) ? match : null);

    public Task AddAsync(Match match, CancellationToken cancellationToken)
    {
        _matches[match.Id] = match;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ThrowConflictOnSave)
        {
            throw new MatchConflictException();
        }

        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<bool> AllPlayersExistAsync(IReadOnlyList<int> playerIds, CancellationToken cancellationToken) =>
        Task.FromResult(MissingPlayerIds.Intersect(playerIds).Any() == false);

    public IReadOnlyCollection<int> MissingPlayerIds { get; set; } = Array.Empty<int>();
}
```

`tests/DartsLeaderboard.Application.Tests/Fakes/RecordingNotifier.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class RecordingNotifier : IMatchNotifier
{
    public List<int> Notifications { get; } = new();

    public void NotifyChanged(int matchId) => Notifications.Add(matchId);

    public IDisposable Subscribe(int matchId, Func<Task> handler) => new Subscription();

    private sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
```

`tests/DartsLeaderboard.Application.Tests/MatchServicesTests.cs`:

```csharp
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Application.Tests.Fakes;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Tests;

public class MatchServicesTests
{
    private readonly FakeMatchRepository _matches = new();
    private readonly FakePlayerRepository _players = new();
    private readonly RecordingNotifier _notifier = new();
    private readonly FixedClock _clock = new();

    [Fact]
    public async Task StartMatch_X01_CreatesMatch()
    {
        var first = _players.Seed("Максим");
        var second = _players.Seed("Аня");
        var service = new StartMatchService(_matches, _clock);

        var result = await service.ExecuteAsync(new MatchSetupRequest(
            GameModeOption.X01, 501, null, new[] { first.Id, second.Id }));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _matches.SaveCount);
    }

    [Fact]
    public async Task StartMatch_RejectsSingleParticipant()
    {
        var service = new StartMatchService(_matches, _clock);

        var result = await service.ExecuteAsync(new MatchSetupRequest(
            GameModeOption.HighestTotal, null, 5, new[] { 1 }));

        Assert.Equal("Нужно выбрать хотя бы двух игроков", result.ErrorMessage);
    }

    [Fact]
    public async Task StartMatch_RejectsUnknownPlayer()
    {
        _matches.MissingPlayerIds = new[] { 99 };
        var service = new StartMatchService(_matches, _clock);

        var result = await service.ExecuteAsync(new MatchSetupRequest(
            GameModeOption.X01, 501, null, new[] { 1, 99 }));

        Assert.Equal("Игрок не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task RecordThrow_ReturnsUpdatedStateAndNotifies()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня");
        _matches.Seed(match);
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id, 60);

        Assert.True(result.IsSuccess);
        Assert.Equal(60, result.Value!.Rows[0].Cells[0].Points);
        Assert.Equal(new[] { match.Id }, _notifier.Notifications);
        Assert.Equal(1, _matches.SaveCount);
    }

    [Fact]
    public async Task RecordThrow_ReturnsErrorForUnknownMatch()
    {
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(777, 60);

        Assert.Equal("Матч не найден", result.ErrorMessage);
    }

    [Fact]
    public async Task RecordThrow_TranslatesConflictIntoUserMessage()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня");
        _matches.Seed(match);
        _matches.ThrowConflictOnSave = true;
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id, 60);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Раунд уже записан с другого устройства, состояние обновлено",
            result.ErrorMessage);
    }

    [Fact]
    public async Task RecordThrow_PropagatesDomainError()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(101), "Максим", "Аня");
        _matches.Seed(match);
        var service = new RecordThrowService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id, 180);

        Assert.Equal("Больше остатка: при переборе вводите 0", result.ErrorMessage);
    }

    [Fact]
    public async Task UndoLastThrow_RemovesThrow()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня").WithThrows(60);
        _matches.Seed(match);
        var service = new UndoLastThrowService(_matches, _notifier);

        var result = await service.ExecuteAsync(match.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(match.Throws);
        Assert.Equal(new[] { match.Id }, _notifier.Notifications);
    }

    [Fact]
    public async Task AbandonMatch_MarksMatchAbandoned()
    {
        var match = TestMatchBuilder.Create(MatchSettings.X01(301), "Максим", "Аня");
        _matches.Seed(match);
        var service = new AbandonMatchService(_matches, _clock, _notifier);

        var result = await service.ExecuteAsync(match.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Abandoned, match.Status);
    }

    [Fact]
    public async Task GetMatchState_ReturnsStateForKnownMatch()
    {
        var match = TestMatchBuilder.Create(MatchSettings.HighestTotal(5), "Максим", "Аня");
        _matches.Seed(match);
        var service = new GetMatchStateService(_matches);

        var result = await service.ExecuteAsync(match.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Максимум за 5 раундов", result.Value!.ModeTitle);
    }
}
```

Существование выбранных игроков проверяется через `IMatchRepository.AllPlayersExistAsync`, поэтому `StartMatchService` зависит только от `IMatchRepository` и `IClock`. Поле `_players` в тестах нужно лишь для того, чтобы завести игроков с идентификаторами.

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests --filter MatchServicesTests`
Expected: ошибки компиляции — нет сервисов матча.

- [ ] **Step 3: Реализовать сервисы**

`Matches/StartMatchService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;
using DartsLeaderboard.Domain.Matches;

namespace DartsLeaderboard.Application.Matches;

public sealed class StartMatchService(IMatchRepository matches, IClock clock)
{
    public async Task<OperationResult<int>> ExecuteAsync(
        MatchSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = request.Mode switch
        {
            GameModeOption.X01 => MatchSettings.X01(request.StartingScore ?? 0),
            GameModeOption.HighestTotal => MatchSettings.HighestTotal(request.RoundLimit ?? 0),
            _ => null
        };

        if (settings is null)
        {
            return OperationResult<int>.Fail("Неизвестный режим игры");
        }

        var started = Match.Start(settings, request.PlayerIds, clock.UtcNow);
        if (!started.IsSuccess)
        {
            return OperationResult<int>.Fail(started.Error!.Value);
        }

        if (!await matches.AllPlayersExistAsync(request.PlayerIds, cancellationToken))
        {
            return OperationResult<int>.Fail(DomainErrorCode.PlayerNotFound);
        }

        await matches.AddAsync(started.Value!, cancellationToken);
        await matches.SaveChangesAsync(cancellationToken);

        return OperationResult<int>.Success(started.Value!.Id);
    }
}
```

`Matches/GetMatchStateService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class GetMatchStateService(IMatchRepository matches)
{
    public async Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        return match is null
            ? OperationResult<MatchStateDto>.Fail(DomainErrorCode.MatchNotFound)
            : OperationResult<MatchStateDto>.Success(MatchStateMapper.ToDto(match));
    }
}
```

`Matches/RecordThrowService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class RecordThrowService(IMatchRepository matches, IClock clock, IMatchNotifier notifier)
{
    public async Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        int points,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        if (match is null)
        {
            return OperationResult<MatchStateDto>.Fail(DomainErrorCode.MatchNotFound);
        }

        var recorded = match.RecordThrow(points, clock.UtcNow);
        if (!recorded.IsSuccess)
        {
            return OperationResult<MatchStateDto>.Fail(recorded.Error!.Value);
        }

        try
        {
            await matches.SaveChangesAsync(cancellationToken);
        }
        catch (MatchConflictException)
        {
            return OperationResult<MatchStateDto>.Fail(DomainErrorCode.RoundAlreadyRecorded);
        }

        notifier.NotifyChanged(matchId);
        return OperationResult<MatchStateDto>.Success(MatchStateMapper.ToDto(match));
    }
}
```

`Matches/UndoLastThrowService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class UndoLastThrowService(IMatchRepository matches, IMatchNotifier notifier)
{
    public async Task<OperationResult<MatchStateDto>> ExecuteAsync(
        int matchId,
        CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        if (match is null)
        {
            return OperationResult<MatchStateDto>.Fail(DomainErrorCode.MatchNotFound);
        }

        var undone = match.UndoLastThrow();
        if (!undone.IsSuccess)
        {
            return OperationResult<MatchStateDto>.Fail(undone.Error!.Value);
        }

        await matches.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged(matchId);

        return OperationResult<MatchStateDto>.Success(MatchStateMapper.ToDto(match));
    }
}
```

`Matches/AbandonMatchService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Common;
using DartsLeaderboard.Domain.Common;

namespace DartsLeaderboard.Application.Matches;

public sealed class AbandonMatchService(IMatchRepository matches, IClock clock, IMatchNotifier notifier)
{
    public async Task<OperationResult> ExecuteAsync(int matchId, CancellationToken cancellationToken = default)
    {
        var match = await matches.GetAsync(matchId, cancellationToken);
        if (match is null)
        {
            return OperationResult.Fail(DomainErrorCode.MatchNotFound);
        }

        var abandoned = match.Abandon(clock.UtcNow);
        if (!abandoned.IsSuccess)
        {
            return OperationResult.Fail(abandoned.Error!.Value);
        }

        await matches.SaveChangesAsync(cancellationToken);
        notifier.NotifyChanged(matchId);

        return OperationResult.Success();
    }
}
```

- [ ] **Step 4: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/DartsLeaderboard.Application tests/DartsLeaderboard.Application.Tests
git commit -m "feat(app): add match use cases with write-through and conflict handling"
```

---

### Task 11: Отчёты и регистрация служб Application

**Files:**
- Create: `src/DartsLeaderboard.Application/Reports/GetLeaderboardService.cs`, `GetMatchListService.cs`
- Create: `src/DartsLeaderboard.Application/DependencyInjection.cs`
- Test: `tests/DartsLeaderboard.Application.Tests/DependencyInjectionTests.cs`

**Interfaces:**
- Consumes: `ILeaderboardQueries`, `IMatchQueries` (Task 8), сервисы задач 9-10.
- Produces:
  - `GetLeaderboardService.ExecuteAsync(CancellationToken = default) -> Task<IReadOnlyList<LeaderboardRowDto>>`
  - `GetMatchListService.ExecuteAsync(MatchListFilter filter, CancellationToken = default) -> Task<IReadOnlyList<MatchListItemDto>>`
  - `DependencyInjection.AddApplication(this IServiceCollection services) -> IServiceCollection`

- [ ] **Step 1: Добавить пакет DI в Application**

```bash
dotnet add src/DartsLeaderboard.Application package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add tests/DartsLeaderboard.Application.Tests package Microsoft.Extensions.DependencyInjection
```

- [ ] **Step 2: Написать падающий тест регистрации**

`tests/DartsLeaderboard.Application.Tests/DependencyInjectionTests.cs`:

```csharp
using DartsLeaderboard.Application;
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Application.Players;
using DartsLeaderboard.Application.Reports;
using DartsLeaderboard.Application.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace DartsLeaderboard.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ResolvesEveryUseCase()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IPlayerRepository, FakePlayerRepository>();
        services.AddSingleton<IMatchRepository, FakeMatchRepository>();
        services.AddSingleton<IMatchNotifier, RecordingNotifier>();
        services.AddSingleton<IClock, FixedClock>();
        services.AddSingleton<ILeaderboardQueries, StubLeaderboardQueries>();
        services.AddSingleton<IMatchQueries, StubMatchQueries>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AddPlayerService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetPlayersService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RenamePlayerService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SetPlayerArchivedService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<StartMatchService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<RecordThrowService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<UndoLastThrowService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AbandonMatchService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetMatchStateService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetLeaderboardService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetMatchListService>());
    }
}
```

Добавить в `tests/DartsLeaderboard.Application.Tests/Fakes/StubQueries.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Tests.Fakes;

internal sealed class StubLeaderboardQueries : ILeaderboardQueries
{
    public Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaderboardRowDto>>(Array.Empty<LeaderboardRowDto>());
}

internal sealed class StubMatchQueries : IMatchQueries
{
    public Task<IReadOnlyList<MatchListItemDto>> ListAsync(MatchListFilter filter, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MatchListItemDto>>(Array.Empty<MatchListItemDto>());
}
```

- [ ] **Step 3: Убедиться, что тест падает**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests --filter DependencyInjectionTests`
Expected: ошибка компиляции — нет `AddApplication` и сервисов отчётов.

- [ ] **Step 4: Реализовать сервисы отчётов и регистрацию**

`Reports/GetLeaderboardService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public sealed class GetLeaderboardService(ILeaderboardQueries queries)
{
    public Task<IReadOnlyList<LeaderboardRowDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        queries.GetAsync(cancellationToken);
}
```

`Reports/GetMatchListService.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;

namespace DartsLeaderboard.Application.Reports;

public sealed class GetMatchListService(IMatchQueries queries)
{
    public Task<IReadOnlyList<MatchListItemDto>> ExecuteAsync(
        MatchListFilter filter,
        CancellationToken cancellationToken = default) =>
        queries.ListAsync(filter, cancellationToken);
}
```

`DependencyInjection.cs`:

```csharp
using DartsLeaderboard.Application.Matches;
using DartsLeaderboard.Application.Players;
using DartsLeaderboard.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace DartsLeaderboard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetPlayersService>();
        services.AddScoped<AddPlayerService>();
        services.AddScoped<RenamePlayerService>();
        services.AddScoped<SetPlayerArchivedService>();

        services.AddScoped<StartMatchService>();
        services.AddScoped<GetMatchStateService>();
        services.AddScoped<RecordThrowService>();
        services.AddScoped<UndoLastThrowService>();
        services.AddScoped<AbandonMatchService>();

        services.AddScoped<GetLeaderboardService>();
        services.AddScoped<GetMatchListService>();

        return services;
    }
}
```

- [ ] **Step 5: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Application.Tests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/DartsLeaderboard.Application tests/DartsLeaderboard.Application.Tests
git commit -m "feat(app): add report services and DI registration"
```

---

### Task 12: Персистентность на EF Core и PostgreSQL

**Files:**
- Modify: `src/DartsLeaderboard.Domain/Matches/Match.cs` (добавить коллекции для маппинга)
- Create: `src/DartsLeaderboard.Infrastructure/Persistence/DartsDbContext.cs`
- Create: `src/DartsLeaderboard.Infrastructure/Persistence/Configurations/PlayerConfiguration.cs`, `MatchConfiguration.cs`, `MatchParticipantConfiguration.cs`, `ThrowConfiguration.cs`
- Create: `src/DartsLeaderboard.Infrastructure/Persistence/Repositories/PlayerRepository.cs`, `MatchRepository.cs`
- Create: `src/DartsLeaderboard.Infrastructure/SystemClock.cs`
- Create: миграция `src/DartsLeaderboard.Infrastructure/Persistence/Migrations/*_InitialCreate.cs`
- Test: `tests/DartsLeaderboard.Infrastructure.Tests/PostgresFixture.cs`, `PersistenceTests.cs`

**Interfaces:**
- Consumes: `IPlayerRepository`, `IMatchRepository`, `IClock`, `MatchConflictException` (Task 8), сущности домена.
- Produces:
  - `Match.AllParticipants -> IReadOnlyCollection<MatchParticipant>` и `Match.AllThrows -> IReadOnlyCollection<Throw>` — навигации для EF Core, отдающие содержимое приватных списков без сортировки
  - `DartsDbContext` с `DbSet<Player> Players`, `DbSet<Match> Matches`
  - `PlayerRepository : IPlayerRepository`, `MatchRepository : IMatchRepository` (транслирует нарушение уникальности в `MatchConflictException`)
  - `SystemClock : IClock`
  - миграция `InitialCreate`, создающая таблицы `players`, `matches`, `match_participants`, `throws` и уникальные индексы

- [ ] **Step 1: Добавить пакеты**

```bash
dotnet add src/DartsLeaderboard.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/DartsLeaderboard.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/DartsLeaderboard.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/DartsLeaderboard.Infrastructure package Microsoft.Extensions.Hosting.Abstractions
dotnet add src/DartsLeaderboard.Infrastructure package Microsoft.Extensions.Configuration.Abstractions
dotnet add src/DartsLeaderboard.Infrastructure package Microsoft.Extensions.Logging.Abstractions
dotnet add tests/DartsLeaderboard.Infrastructure.Tests package Testcontainers.PostgreSql
dotnet tool install --global dotnet-ef
```

- [ ] **Step 2: Открыть коллекции агрегата для маппинга**

В `src/DartsLeaderboard.Domain/Matches/Match.cs` добавить рядом с `Participants` и `Throws`:

```csharp
    /// <summary>Навигация для EF Core: содержимое приватного списка без сортировки.</summary>
    public IReadOnlyCollection<MatchParticipant> AllParticipants => _participants;

    /// <summary>Навигация для EF Core: содержимое приватного списка без сортировки.</summary>
    public IReadOnlyCollection<Throw> AllThrows => _throws;
```

- [ ] **Step 3: Написать падающие интеграционные тесты**

`tests/DartsLeaderboard.Infrastructure.Tests/PostgresFixture.cs`:

```csharp
using DartsLeaderboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DartsLeaderboard.Infrastructure.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    public DartsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DartsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
```

Если версия xUnit в проекте использует синхронный `IAsyncLifetime` из `Xunit` с `Task InitializeAsync()`, приведи сигнатуры к тому, что требует установленный пакет: смысл шагов не меняется.

`tests/DartsLeaderboard.Infrastructure.Tests/PersistenceTests.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Tests;

[Collection(nameof(PostgresCollection))]
public class PersistenceTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Migrations_CreateSchema()
    {
        await using var context = fixture.CreateContext();

        Assert.Empty(await context.Players.Where(p => p.Name == "нет такого").ToListAsync());
    }

    [Fact]
    public async Task PlayerRepository_AddsAndFindsByNameIgnoringCase()
    {
        await using var context = fixture.CreateContext();
        var repository = new PlayerRepository(context);

        await repository.AddAsync(Player.Create("Максим-" + Guid.NewGuid().ToString("N")[..6], Now).Value!, default);
        await repository.SaveChangesAsync(default);

        var stored = (await repository.ListAsync(includeArchived: true, default)).Last();
        Assert.True(await repository.ExistsWithNameAsync(stored.Name.ToUpperInvariant(), null, default));
        Assert.False(await repository.ExistsWithNameAsync(stored.Name, stored.Id, default));
    }

    [Fact]
    public async Task MatchRepository_RoundTripsAggregateWithThrows()
    {
        int matchId;

        await using (var context = fixture.CreateContext())
        {
            var players = new PlayerRepository(context);
            var first = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
            var second = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
            await players.AddAsync(first, default);
            await players.AddAsync(second, default);
            await players.SaveChangesAsync(default);

            var matches = new MatchRepository(context);
            var match = Match.Start(MatchSettings.X01(301), new[] { first.Id, second.Id }, Now).Value!;
            await matches.AddAsync(match, default);
            await matches.SaveChangesAsync(default);

            match.RecordThrow(60, Now);
            match.RecordThrow(45, Now);
            await matches.SaveChangesAsync(default);

            matchId = match.Id;
        }

        await using var verifyContext = fixture.CreateContext();
        var restored = await new MatchRepository(verifyContext).GetAsync(matchId, default);

        Assert.NotNull(restored);
        Assert.Equal(2, restored!.Participants.Count);
        Assert.Equal(2, restored.Throws.Count);
        Assert.Equal(60, restored.Throws[0].Points);
        Assert.Equal(2, restored.CurrentRoundNumber);
        Assert.NotNull(restored.Participants[0].Player);
        Assert.Equal(241, restored.Rules.RunningValueAfterRound(restored, restored.Participants[0], 1));
    }

    [Fact]
    public async Task MatchRepository_TranslatesDuplicateRoundIntoConflict()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var first = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
        var second = Player.Create("Игрок-" + Guid.NewGuid().ToString("N")[..6], Now).Value!;
        await players.AddAsync(first, default);
        await players.AddAsync(second, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var match = Match.Start(MatchSettings.X01(301), new[] { first.Id, second.Id }, Now).Value!;
        await matches.AddAsync(match, default);
        await matches.SaveChangesAsync(default);

        match.RecordThrow(60, Now);
        await matches.SaveChangesAsync(default);

        // Второе устройство прочитало матч до записи и пишет тот же раунд заново.
        await using var secondContext = fixture.CreateContext();
        var secondMatches = new MatchRepository(secondContext);
        var sameMatch = (await secondMatches.GetAsync(match.Id, default))!;
        sameMatch.UndoLastThrow();
        sameMatch.RecordThrow(20, Now);
        secondContext.Entry(sameMatch.Throws[0]).State = EntityState.Added;

        await Assert.ThrowsAsync<MatchConflictException>(() => secondMatches.SaveChangesAsync(default));
    }
}
```

Последний тест воспроизводит реальный сценарий двух устройств: второе пишет раунд 1, уже записанный первым, и нарушение уникального индекса `ix_throws_match_participant_round` должно превратиться в `MatchConflictException`.

- [ ] **Step 4: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Infrastructure.Tests`
Expected: ошибки компиляции — нет `DartsDbContext` и репозиториев.

- [ ] **Step 5: Реализовать контекст и конфигурации**

`Persistence/DartsDbContext.cs`:

```csharp
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence;

public sealed class DartsDbContext(DbContextOptions<DartsDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DartsDbContext).Assembly);
}
```

`Persistence/Configurations/PlayerConfiguration.cs`:

```csharp
using DartsLeaderboard.Domain.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(Player.MaxNameLength).IsRequired();
        builder.Property(p => p.IsArchived).HasColumnName("is_archived");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
    }
}
```

`Persistence/Configurations/MatchConfiguration.cs`:

```csharp
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.Mode).HasColumnName("mode").HasConversion<int>();
        builder.Property(m => m.StartingScore).HasColumnName("starting_score");
        builder.Property(m => m.RoundLimit).HasColumnName("round_limit");
        builder.Property(m => m.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(m => m.StartedAt).HasColumnName("started_at");
        builder.Property(m => m.FinishedAt).HasColumnName("finished_at");
        builder.Property(m => m.WinnerParticipantId).HasColumnName("winner_participant_id");

        builder.HasMany(m => m.AllParticipants)
            .WithOne()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.AllThrows)
            .WithOne()
            .HasForeignKey(t => t.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.AllParticipants)
            .HasField("_participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(m => m.AllThrows)
            .HasField("_throws")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(m => m.Participants);
        builder.Ignore(m => m.Throws);
    }
}
```

`Persistence/Configurations/MatchParticipantConfiguration.cs`:

```csharp
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.ToTable("match_participants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.MatchId).HasColumnName("match_id");
        builder.Property(p => p.PlayerId).HasColumnName("player_id");
        builder.Property(p => p.SeatOrder).HasColumnName("seat_order");

        builder.HasOne(p => p.Player)
            .WithMany()
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.MatchId, p.PlayerId }).IsUnique();
        builder.HasIndex(p => new { p.MatchId, p.SeatOrder }).IsUnique();
        builder.Ignore(p => p.PlayerName);
    }
}
```

`Persistence/Configurations/ThrowConfiguration.cs`:

```csharp
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartsLeaderboard.Infrastructure.Persistence.Configurations;

public sealed class ThrowConfiguration : IEntityTypeConfiguration<Throw>
{
    public void Configure(EntityTypeBuilder<Throw> builder)
    {
        builder.ToTable("throws");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.MatchId).HasColumnName("match_id");
        builder.Property(t => t.ParticipantId).HasColumnName("participant_id");
        builder.Property(t => t.RoundNumber).HasColumnName("round_number");
        builder.Property(t => t.Points).HasColumnName("points");
        builder.Property(t => t.RecordedAt).HasColumnName("recorded_at");

        builder.HasOne<MatchParticipant>()
            .WithMany()
            .HasForeignKey(t => t.ParticipantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.MatchId, t.ParticipantId, t.RoundNumber })
            .IsUnique()
            .HasDatabaseName("ix_throws_match_participant_round");
    }
}
```

- [ ] **Step 6: Реализовать репозитории и часы**

`SystemClock.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;

namespace DartsLeaderboard.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

`Persistence/Repositories/PlayerRepository.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Players;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Repositories;

public sealed class PlayerRepository(DartsDbContext context) : IPlayerRepository
{
    public async Task<IReadOnlyList<Player>> ListAsync(bool includeArchived, CancellationToken cancellationToken) =>
        await context.Players
            .Where(p => includeArchived || !p.IsArchived)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<Player?> GetAsync(int playerId, CancellationToken cancellationToken) =>
        context.Players.FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

    public Task<bool> ExistsWithNameAsync(string name, int? excludePlayerId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return context.Players.AnyAsync(
            p => p.Name.ToLower() == normalized && (excludePlayerId == null || p.Id != excludePlayerId),
            cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken) =>
        await context.Players.AddAsync(player, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
```

`Persistence/Repositories/MatchRepository.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DartsLeaderboard.Infrastructure.Persistence.Repositories;

public sealed class MatchRepository(DartsDbContext context) : IMatchRepository
{
    private const string UniqueViolation = "23505";

    public Task<Match?> GetAsync(int matchId, CancellationToken cancellationToken) =>
        context.Matches
            .Include(m => m.AllParticipants)
            .ThenInclude(p => p.Player)
            .Include(m => m.AllThrows)
            .FirstOrDefaultAsync(m => m.Id == matchId, cancellationToken);

    public async Task AddAsync(Match match, CancellationToken cancellationToken) =>
        await context.Matches.AddAsync(match, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new MatchConflictException(exception);
        }
    }

    public async Task<bool> AllPlayersExistAsync(
        IReadOnlyList<int> playerIds,
        CancellationToken cancellationToken)
    {
        var found = await context.Players.CountAsync(p => playerIds.Contains(p.Id), cancellationToken);
        return found == playerIds.Distinct().Count();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}
```

- [ ] **Step 7: Создать миграцию с уникальным индексом имени без учёта регистра**

Регистрация контекста в веб-хосте появится только в Task 14, поэтому миграция создаётся через design-time фабрику. Добавь `src/DartsLeaderboard.Infrastructure/Persistence/DesignTimeDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DartsLeaderboard.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DartsDbContext>
{
    public DartsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<DartsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=darts;Username=darts;Password=darts")
            .Options);
}
```

Затем выполни:

```bash
dotnet ef migrations add InitialCreate --project src/DartsLeaderboard.Infrastructure --output-dir Persistence/Migrations
```

В сгенерированный файл миграции, в конец метода `Up`, добавить регистронезависимый уникальный индекс имени игрока:

```csharp
            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_players_name_ci ON players (lower(name));");
```

и в начало метода `Down`:

```csharp
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_players_name_ci;");
```

- [ ] **Step 8: Прогнать интеграционные тесты**

Run: `dotnet test tests/DartsLeaderboard.Infrastructure.Tests`
Expected: PASS, 4 теста. Docker должен быть запущен — Testcontainers поднимает `postgres:16-alpine`.

- [ ] **Step 9: Commit**

```bash
git add src tests
git commit -m "feat(infra): add EF Core persistence with postgres migrations"
```

---

### Task 13: Запросы отчётов, уведомления и регистрация инфраструктуры

**Files:**
- Modify: `src/DartsLeaderboard.Domain/Matches/Rules/GameRules.cs` (добавить `TitleFor`), `X01Rules.cs`, `HighestTotalRules.cs` (использовать `TitleFor`)
- Create: `src/DartsLeaderboard.Infrastructure/Persistence/Queries/LeaderboardQueries.cs`, `MatchQueries.cs`
- Create: `src/DartsLeaderboard.Infrastructure/Notifications/InMemoryMatchNotifier.cs`
- Create: `src/DartsLeaderboard.Infrastructure/Persistence/DatabaseInitializer.cs`
- Create: `src/DartsLeaderboard.Infrastructure/DependencyInjection.cs`
- Test: `tests/DartsLeaderboard.Infrastructure.Tests/QueriesTests.cs`, `InMemoryMatchNotifierTests.cs`

**Interfaces:**
- Consumes: `DartsDbContext`, репозитории (Task 12), `ILeaderboardQueries`, `IMatchQueries`, `IMatchNotifier` (Task 8).
- Produces:
  - `GameRules.TitleFor(GameMode mode, int? startingScore, int? roundLimit) -> string`
  - `LeaderboardQueries : ILeaderboardQueries`, `MatchQueries : IMatchQueries`
  - `InMemoryMatchNotifier : IMatchNotifier` (регистрируется синглтоном)
  - `DatabaseInitializer : IHostedService` — применяет миграции с 10 попытками и паузой 3 секунды
  - `DependencyInjection.AddInfrastructure(this IServiceCollection services, string connectionString) -> IServiceCollection`

- [ ] **Step 1: Написать падающие тесты**

`tests/DartsLeaderboard.Infrastructure.Tests/InMemoryMatchNotifierTests.cs`:

```csharp
using DartsLeaderboard.Infrastructure.Notifications;

namespace DartsLeaderboard.Infrastructure.Tests;

public class InMemoryMatchNotifierTests
{
    [Fact]
    public async Task NotifyChanged_InvokesSubscribersOfThatMatchOnly()
    {
        var notifier = new InMemoryMatchNotifier();
        var matchCalls = 0;
        var otherCalls = 0;

        using var _ = notifier.Subscribe(1, () =>
        {
            Interlocked.Increment(ref matchCalls);
            return Task.CompletedTask;
        });

        using var __ = notifier.Subscribe(2, () =>
        {
            Interlocked.Increment(ref otherCalls);
            return Task.CompletedTask;
        });

        notifier.NotifyChanged(1);
        await Task.Delay(100);

        Assert.Equal(1, matchCalls);
        Assert.Equal(0, otherCalls);
    }

    [Fact]
    public async Task Dispose_StopsNotifications()
    {
        var notifier = new InMemoryMatchNotifier();
        var calls = 0;

        var subscription = notifier.Subscribe(1, () =>
        {
            Interlocked.Increment(ref calls);
            return Task.CompletedTask;
        });

        subscription.Dispose();
        notifier.NotifyChanged(1);
        await Task.Delay(100);

        Assert.Equal(0, calls);
    }
}
```

`tests/DartsLeaderboard.Infrastructure.Tests/QueriesTests.cs`:

```csharp
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Players;
using DartsLeaderboard.Infrastructure.Persistence.Queries;
using DartsLeaderboard.Infrastructure.Persistence.Repositories;

namespace DartsLeaderboard.Infrastructure.Tests;

[Collection(nameof(PostgresCollection))]
public class QueriesTests(PostgresFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 20, 0, 0, TimeSpan.Zero);

    private static string UniqueName() => "Игрок-" + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public async Task Leaderboard_CountsWinsAndMatches()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var winner = Player.Create(UniqueName(), Now).Value!;
        var loser = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(winner, default);
        await players.AddAsync(loser, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var match = Match.Start(MatchSettings.X01(101), new[] { winner.Id, loser.Id }, Now).Value!;
        await matches.AddAsync(match, default);
        await matches.SaveChangesAsync(default);
        match.RecordThrow(101, Now);
        await matches.SaveChangesAsync(default);

        var rows = await new LeaderboardQueries(context).GetAsync(default);

        var winnerRow = rows.Single(r => r.PlayerId == winner.Id);
        var loserRow = rows.Single(r => r.PlayerId == loser.Id);
        Assert.Equal(1, winnerRow.Wins);
        Assert.Equal(1, winnerRow.MatchesPlayed);
        Assert.Equal(0, loserRow.Wins);
        Assert.Equal(1, loserRow.MatchesPlayed);
    }

    [Fact]
    public async Task MatchList_SplitsInProgressAndFinished()
    {
        await using var context = fixture.CreateContext();
        var players = new PlayerRepository(context);
        var first = Player.Create(UniqueName(), Now).Value!;
        var second = Player.Create(UniqueName(), Now).Value!;
        await players.AddAsync(first, default);
        await players.AddAsync(second, default);
        await players.SaveChangesAsync(default);

        var matches = new MatchRepository(context);
        var finished = Match.Start(MatchSettings.X01(101), new[] { first.Id, second.Id }, Now).Value!;
        var running = Match.Start(MatchSettings.HighestTotal(5), new[] { first.Id, second.Id }, Now).Value!;
        await matches.AddAsync(finished, default);
        await matches.AddAsync(running, default);
        await matches.SaveChangesAsync(default);
        finished.RecordThrow(101, Now);
        await matches.SaveChangesAsync(default);

        var queries = new MatchQueries(context);
        var finishedList = await queries.ListAsync(MatchListFilter.Finished, default);
        var runningList = await queries.ListAsync(MatchListFilter.InProgress, default);

        var finishedItem = finishedList.Single(m => m.MatchId == finished.Id);
        Assert.Equal("101 на очки", finishedItem.ModeTitle);
        Assert.Equal(first.Name, finishedItem.WinnerName);
        Assert.Contains(first.Name, finishedItem.Participants);

        var runningItem = runningList.Single(m => m.MatchId == running.Id);
        Assert.Equal("Максимум за 5 раундов", runningItem.ModeTitle);
        Assert.Null(runningItem.WinnerName);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Infrastructure.Tests`
Expected: ошибки компиляции — нет `LeaderboardQueries`, `MatchQueries`, `InMemoryMatchNotifier`.

- [ ] **Step 3: Вынести формирование названия режима в домен**

В `Matches/Rules/GameRules.cs` добавить:

```csharp
    public static string TitleFor(GameMode mode, int? startingScore, int? roundLimit) => mode switch
    {
        GameMode.X01 => $"{startingScore} на очки",
        GameMode.HighestTotal => $"Максимум за {roundLimit} раундов",
        _ => "Неизвестный режим"
    };
```

В `X01Rules` заменить свойство на `public string Title => GameRules.TitleFor(GameMode.X01, _startingScore, null);`
В `HighestTotalRules` — на `public string Title => GameRules.TitleFor(GameMode.HighestTotal, null, _roundLimit);`

- [ ] **Step 4: Реализовать запросы**

`Persistence/Queries/LeaderboardQueries.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Queries;

public sealed class LeaderboardQueries(DartsDbContext context) : ILeaderboardQueries
{
    public async Task<IReadOnlyList<LeaderboardRowDto>> GetAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Players
            .Select(player => new LeaderboardRowDto(
                player.Id,
                player.Name,
                context.Matches.Count(m =>
                    m.Status == MatchStatus.Finished &&
                    m.AllParticipants.Any(p => p.PlayerId == player.Id && p.Id == m.WinnerParticipantId)),
                context.Matches.Count(m =>
                    m.Status == MatchStatus.Finished &&
                    m.AllParticipants.Any(p => p.PlayerId == player.Id))))
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(r => r.Wins)
            .ThenByDescending(r => r.MatchesPlayed)
            .ThenBy(r => r.PlayerName)
            .ToList();
    }
}
```

`Persistence/Queries/MatchQueries.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Application.Contracts;
using DartsLeaderboard.Domain.Matches;
using DartsLeaderboard.Domain.Matches.Rules;
using Microsoft.EntityFrameworkCore;

namespace DartsLeaderboard.Infrastructure.Persistence.Queries;

public sealed class MatchQueries(DartsDbContext context) : IMatchQueries
{
    public async Task<IReadOnlyList<MatchListItemDto>> ListAsync(
        MatchListFilter filter,
        CancellationToken cancellationToken)
    {
        var status = filter == MatchListFilter.InProgress ? MatchStatus.InProgress : MatchStatus.Finished;

        var raw = await context.Matches
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.StartedAt)
            .Select(m => new
            {
                m.Id,
                m.Mode,
                m.StartingScore,
                m.RoundLimit,
                m.StartedAt,
                m.FinishedAt,
                m.WinnerParticipantId,
                Participants = m.AllParticipants
                    .OrderBy(p => p.SeatOrder)
                    .Select(p => new { p.Id, Name = p.Player!.Name })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return raw
            .Select(m => new MatchListItemDto(
                m.Id,
                GameRules.TitleFor(m.Mode, m.StartingScore, m.RoundLimit),
                string.Join(", ", m.Participants.Select(p => p.Name)),
                m.Participants.FirstOrDefault(p => p.Id == m.WinnerParticipantId)?.Name,
                m.StartedAt,
                m.FinishedAt))
            .ToList();
    }
}
```

- [ ] **Step 5: Реализовать уведомления, инициализацию БД и регистрацию**

`Notifications/InMemoryMatchNotifier.cs`:

```csharp
using System.Collections.Concurrent;
using DartsLeaderboard.Application.Abstractions;

namespace DartsLeaderboard.Infrastructure.Notifications;

public sealed class InMemoryMatchNotifier : IMatchNotifier
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Func<Task>>> _subscribers = new();

    public void NotifyChanged(int matchId)
    {
        if (!_subscribers.TryGetValue(matchId, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Values)
        {
            _ = Task.Run(handler);
        }
    }

    public IDisposable Subscribe(int matchId, Func<Task> handler)
    {
        var token = Guid.NewGuid();
        var handlers = _subscribers.GetOrAdd(matchId, _ => new ConcurrentDictionary<Guid, Func<Task>>());
        handlers[token] = handler;

        return new Subscription(() =>
        {
            if (_subscribers.TryGetValue(matchId, out var current))
            {
                current.TryRemove(token, out _);
            }
        });
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}
```

`Persistence/DatabaseInitializer.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DartsLeaderboard.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DartsDbContext>();
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Миграции применены с попытки {Attempt}", attempt);
                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts)
            {
                logger.LogWarning(exception, "База недоступна, попытка {Attempt} из {Max}", attempt, MaxAttempts);
                await Task.Delay(Delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Не удалось применить миграции: база недоступна");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

`DependencyInjection.cs`:

```csharp
using DartsLeaderboard.Application.Abstractions;
using DartsLeaderboard.Infrastructure.Notifications;
using DartsLeaderboard.Infrastructure.Persistence;
using DartsLeaderboard.Infrastructure.Persistence.Queries;
using DartsLeaderboard.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DartsLeaderboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DartsDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ILeaderboardQueries, LeaderboardQueries>();
        services.AddScoped<IMatchQueries, MatchQueries>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMatchNotifier, InMemoryMatchNotifier>();
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}
```

- [ ] **Step 6: Прогнать тесты**

Run: `dotnet test tests/DartsLeaderboard.Infrastructure.Tests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src tests
git commit -m "feat(infra): add report queries, match notifier and DI wiring"
```

---

### Task 14: Хост Blazor, оболочка MudBlazor и лидерборд

**Files:**
- Modify: `src/DartsLeaderboard.Web/Program.cs`, `appsettings.json`, `Components/App.razor`, `Components/_Imports.razor`, `Components/Layout/MainLayout.razor`, `Components/Layout/NavMenu.razor`
- Create: `src/DartsLeaderboard.Web/Components/Pages/Leaderboard.razor`
- Delete: шаблонные страницы `Components/Pages/Counter.razor`, `Weather.razor`, `Home.razor` и шаблонный `Components/Layout/NavMenu.razor.css` при необходимости

**Interfaces:**
- Consumes: `AddApplication` (Task 11), `AddInfrastructure` (Task 13), `GetLeaderboardService`, `GetMatchListService`, `LeaderboardRowDto`, `MatchListItemDto`, `MatchListFilter` (задачи 8, 11).
- Produces: работающий сайт с тёмной темой MudBlazor, навигацией по разделам и страницей `/` со сводной таблицей побед и списком незавершённых матчей.

- [ ] **Step 1: Добавить MudBlazor**

```bash
dotnet add src/DartsLeaderboard.Web package MudBlazor
```

- [ ] **Step 2: Настроить `Program.cs`**

```csharp
using DartsLeaderboard.Application;
using DartsLeaderboard.Infrastructure;
using DartsLeaderboard.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

var connectionString = builder.Configuration.GetConnectionString("Darts")
    ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Darts");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
```

`appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Darts": "Host=localhost;Port=5432;Database=darts;Username=darts;Password=darts"
  }
}
```

- [ ] **Step 3: Подключить стили и шрифты MudBlazor**

В `Components/App.razor` в `<head>` добавить:

```html
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
```

и перед закрывающим `</body>`:

```html
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

В `Components/_Imports.razor` добавить:

```razor
@using MudBlazor
@using DartsLeaderboard.Application.Contracts
```

- [ ] **Step 4: Сверстать оболочку с тёмной темой**

`Components/Layout/MainLayout.razor`:

```razor
@inherits LayoutComponentBase

<MudThemeProvider IsDarkMode="true" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start"
                       OnClick="@(() => _drawerOpen = !_drawerOpen)" />
        <MudText Typo="Typo.h6" Class="ml-2">Дартс</MudText>
    </MudAppBar>
    <MudDrawer @bind-Open="_drawerOpen" Elevation="1">
        <NavMenu />
    </MudDrawer>
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="py-6">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
}
```

`Components/Layout/NavMenu.razor`:

```razor
<MudNavMenu>
    <MudNavLink Href="/" Match="NavLinkMatch.All" Icon="@Icons.Material.Filled.EmojiEvents">Лидерборд</MudNavLink>
    <MudNavLink Href="/match/new" Icon="@Icons.Material.Filled.PlayArrow">Новый матч</MudNavLink>
    <MudNavLink Href="/players" Icon="@Icons.Material.Filled.People">Игроки</MudNavLink>
    <MudNavLink Href="/history" Icon="@Icons.Material.Filled.History">История</MudNavLink>
</MudNavMenu>
```

- [ ] **Step 5: Сверстать страницу лидерборда**

`Components/Pages/Leaderboard.razor`:

```razor
@page "/"
@using DartsLeaderboard.Application.Reports
@inject GetLeaderboardService LeaderboardService
@inject GetMatchListService MatchListService
@inject NavigationManager Navigation

<PageTitle>Лидерборд</PageTitle>

<MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-4">
    <MudText Typo="Typo.h5">Лидерборд</MudText>
    <MudSpacer />
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               StartIcon="@Icons.Material.Filled.Add"
               OnClick="@(() => Navigation.NavigateTo("/match/new"))">
        Новый матч
    </MudButton>
</MudStack>

<MudPaper Class="pa-2 mb-6">
    <MudTable Items="_rows" Dense="true" Hover="true" Loading="_loading">
        <HeaderContent>
            <MudTh>Игрок</MudTh>
            <MudTh>Победы</MudTh>
            <MudTh>Матчей</MudTh>
            <MudTh>Доля побед</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Игрок">@context.PlayerName</MudTd>
            <MudTd DataLabel="Победы">@context.Wins</MudTd>
            <MudTd DataLabel="Матчей">@context.MatchesPlayed</MudTd>
            <MudTd DataLabel="Доля побед">@context.WinRate.ToString("P0")</MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudText Typo="Typo.body2">Матчей пока нет — начни первый.</MudText>
        </NoRecordsContent>
    </MudTable>
</MudPaper>

@if (_unfinished.Count > 0)
{
    <MudText Typo="Typo.h6" Class="mb-2">Незавершённые матчи</MudText>
    <MudPaper Class="pa-2">
        <MudTable Items="_unfinished" Dense="true" Hover="true">
            <HeaderContent>
                <MudTh>Матч</MudTh>
                <MudTh>Режим</MudTh>
                <MudTh>Игроки</MudTh>
                <MudTh>Начат</MudTh>
                <MudTh />
            </HeaderContent>
            <RowTemplate>
                <MudTd DataLabel="Матч">#@context.MatchId</MudTd>
                <MudTd DataLabel="Режим">@context.ModeTitle</MudTd>
                <MudTd DataLabel="Игроки">@context.Participants</MudTd>
                <MudTd DataLabel="Начат">@context.StartedAt.LocalDateTime.ToString("dd.MM HH:mm")</MudTd>
                <MudTd>
                    <MudButton Size="Size.Small" Variant="Variant.Text" Color="Color.Primary"
                               OnClick="@(() => Navigation.NavigateTo($"/match/{context.MatchId}"))">
                        Продолжить
                    </MudButton>
                </MudTd>
            </RowTemplate>
        </MudTable>
    </MudPaper>
}

@code {
    private IReadOnlyList<LeaderboardRowDto> _rows = Array.Empty<LeaderboardRowDto>();
    private IReadOnlyList<MatchListItemDto> _unfinished = Array.Empty<MatchListItemDto>();
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _rows = await LeaderboardService.ExecuteAsync();
        _unfinished = await MatchListService.ExecuteAsync(MatchListFilter.InProgress);
        _loading = false;
    }
}
```

Удалить шаблонные страницы `Home.razor`, `Counter.razor`, `Weather.razor` и ссылки на них.

- [ ] **Step 6: Проверить запуск**

Compose появится только в Task 18, поэтому подними Postgres одиночным контейнером:

```bash
docker run --rm -d --name darts-db -e POSTGRES_DB=darts -e POSTGRES_USER=darts -e POSTGRES_PASSWORD=darts -p 5432:5432 postgres:16-alpine
dotnet run --project src/DartsLeaderboard.Web
```

Expected: в логах сообщение «Миграции применены с попытки 1», страница `http://localhost:5000` открывается, показывает пустой лидерборд с кнопкой «Новый матч» и не выдаёт ошибок в консоли браузера.

- [ ] **Step 7: Commit**

```bash
git add src/DartsLeaderboard.Web
git commit -m "feat(web): add mudblazor shell and leaderboard page"
```

---

### Task 15: Страницы справочника игроков и создания матча

**Files:**
- Create: `src/DartsLeaderboard.Web/Components/Pages/Players.razor`, `NewMatch.razor`

**Interfaces:**
- Consumes: `GetPlayersService`, `AddPlayerService`, `RenamePlayerService`, `SetPlayerArchivedService`, `StartMatchService`, `MatchSetupRequest`, `GameModeOption` (задачи 9-11).
- Produces: страницы `/players` и `/match/new`; после успешного старта матча переход на `/match/{id}`.

- [ ] **Step 1: Сверстать страницу игроков**

`Components/Pages/Players.razor`:

```razor
@page "/players"
@using DartsLeaderboard.Application.Players
@inject GetPlayersService GetPlayers
@inject AddPlayerService AddPlayer
@inject RenamePlayerService RenamePlayer
@inject SetPlayerArchivedService SetArchived
@inject ISnackbar Snackbar

<PageTitle>Игроки</PageTitle>

<MudText Typo="Typo.h5" Class="mb-4">Игроки</MudText>

<MudPaper Class="pa-4 mb-4">
    <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
        <MudTextField @bind-Value="_newName" Label="Имя игрока" Immediate="true"
                      OnKeyDown="@OnNewNameKeyDown" MaxLength="50" Style="max-width: 280px" />
        <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Add">Добавить</MudButton>
        <MudSpacer />
        <MudSwitch T="bool" Value="_showArchived" ValueChanged="ToggleArchived" Color="Color.Primary">
            Показывать архивных
        </MudSwitch>
    </MudStack>
</MudPaper>

<MudPaper Class="pa-2">
    <MudTable Items="_players" Dense="true" Hover="true" Loading="_loading">
        <HeaderContent>
            <MudTh>Имя</MudTh>
            <MudTh>Статус</MudTh>
            <MudTh />
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Имя">
                <MudTextField T="string" Value="@context.Name" Immediate="false"
                              ValueChanged="@(name => Rename(context.Id, name))" Underline="false" />
            </MudTd>
            <MudTd DataLabel="Статус">@(context.IsArchived ? "В архиве" : "Активен")</MudTd>
            <MudTd>
                <MudButton Size="Size.Small" Variant="Variant.Text"
                           OnClick="@(() => Archive(context.Id, !context.IsArchived))">
                    @(context.IsArchived ? "Вернуть" : "В архив")
                </MudButton>
            </MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudText Typo="Typo.body2">Пока никого нет — добавь первого игрока.</MudText>
        </NoRecordsContent>
    </MudTable>
</MudPaper>

@code {
    private IReadOnlyList<PlayerDto> _players = Array.Empty<PlayerDto>();
    private string _newName = string.Empty;
    private bool _showArchived;
    private bool _loading = true;

    protected override Task OnInitializedAsync() => Reload();

    private async Task Reload()
    {
        _loading = true;
        _players = await GetPlayers.ExecuteAsync(_showArchived);
        _loading = false;
    }

    private async Task ToggleArchived(bool value)
    {
        _showArchived = value;
        await Reload();
    }

    private async Task OnNewNameKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await Add();
        }
    }

    private async Task Add()
    {
        var result = await AddPlayer.ExecuteAsync(_newName);
        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
            return;
        }

        _newName = string.Empty;
        await Reload();
    }

    private async Task Rename(int playerId, string name)
    {
        var result = await RenamePlayer.ExecuteAsync(playerId, name);
        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
        }

        await Reload();
    }

    private async Task Archive(int playerId, bool archived)
    {
        var result = await SetArchived.ExecuteAsync(playerId, archived);
        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
        }

        await Reload();
    }
}
```

- [ ] **Step 2: Сверстать страницу создания матча**

`Components/Pages/NewMatch.razor`:

```razor
@page "/match/new"
@using DartsLeaderboard.Application.Matches
@using DartsLeaderboard.Application.Players
@inject GetPlayersService GetPlayers
@inject StartMatchService StartMatch
@inject ISnackbar Snackbar
@inject NavigationManager Navigation

<PageTitle>Новый матч</PageTitle>

<MudText Typo="Typo.h5" Class="mb-4">Новый матч</MudText>

<MudGrid>
    <MudItem xs="12" md="6">
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.subtitle1" Class="mb-2">Режим</MudText>
            <MudToggleGroup T="GameModeOption" Value="_mode" ValueChanged="OnModeChanged" Color="Color.Primary">
                <MudToggleItem Value="GameModeOption.X01" Text="На очки" />
                <MudToggleItem Value="GameModeOption.HighestTotal" Text="Максимум" />
            </MudToggleGroup>

            @if (_mode == GameModeOption.X01)
            {
                <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Center" Class="mt-4">
                    <MudSelect T="int" Label="Начальный счёт" Value="_startingScore"
                               ValueChanged="@(value => _startingScore = value)" Style="max-width: 180px">
                        <MudSelectItem T="int" Value="301">301</MudSelectItem>
                        <MudSelectItem T="int" Value="501">501</MudSelectItem>
                        <MudSelectItem T="int" Value="701">701</MudSelectItem>
                    </MudSelect>
                    <MudNumericField T="int" @bind-Value="_startingScore" Label="Свой счёт"
                                     Min="1" Max="2000" Style="max-width: 160px" />
                </MudStack>
            }
            else
            {
                <MudNumericField T="int" @bind-Value="_roundLimit" Label="Количество раундов"
                                 Min="1" Max="50" Class="mt-4" Style="max-width: 200px" />
            }
        </MudPaper>
    </MudItem>

    <MudItem xs="12" md="6">
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.subtitle1" Class="mb-2">Участники и порядок</MudText>

            <MudList T="PlayerDto" Dense="true">
                @foreach (var player in _players)
                {
                    <MudListItem T="PlayerDto">
                        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1">
                            <MudCheckBox T="bool" Value="_selected.Contains(player.Id)"
                                         ValueChanged="@(value => Toggle(player.Id, value))"
                                         Label="@player.Name" />
                            <MudSpacer />
                            @if (_selected.Contains(player.Id))
                            {
                                <MudText Typo="Typo.caption">@(_selected.IndexOf(player.Id) + 1)</MudText>
                                <MudIconButton Icon="@Icons.Material.Filled.ArrowUpward" Size="Size.Small"
                                               OnClick="@(() => Move(player.Id, -1))" />
                                <MudIconButton Icon="@Icons.Material.Filled.ArrowDownward" Size="Size.Small"
                                               OnClick="@(() => Move(player.Id, 1))" />
                            }
                        </MudStack>
                    </MudListItem>
                }
            </MudList>

            @if (_players.Count == 0)
            {
                <MudAlert Severity="Severity.Info" Class="mt-2">
                    Сначала добавь игроков в справочнике.
                </MudAlert>
            }
        </MudPaper>
    </MudItem>
</MudGrid>

<MudButton Variant="Variant.Filled" Color="Color.Primary" Class="mt-4"
           Disabled="_selected.Count < 2" OnClick="Start">
    Начать
</MudButton>

@code {
    private IReadOnlyList<PlayerDto> _players = Array.Empty<PlayerDto>();
    private readonly List<int> _selected = new();
    private GameModeOption _mode = GameModeOption.X01;
    private int _startingScore = 501;
    private int _roundLimit = 5;

    protected override async Task OnInitializedAsync() =>
        _players = await GetPlayers.ExecuteAsync(includeArchived: false);

    private void OnModeChanged(GameModeOption mode) => _mode = mode;

    private void Toggle(int playerId, bool selected)
    {
        if (selected && !_selected.Contains(playerId))
        {
            _selected.Add(playerId);
        }
        else if (!selected)
        {
            _selected.Remove(playerId);
        }
    }

    private void Move(int playerId, int offset)
    {
        var index = _selected.IndexOf(playerId);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _selected.Count)
        {
            return;
        }

        (_selected[index], _selected[target]) = (_selected[target], _selected[index]);
    }

    private async Task Start()
    {
        var request = new MatchSetupRequest(
            _mode,
            _mode == GameModeOption.X01 ? _startingScore : null,
            _mode == GameModeOption.HighestTotal ? _roundLimit : null,
            _selected.ToList());

        var result = await StartMatch.ExecuteAsync(request);
        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
            return;
        }

        Navigation.NavigateTo($"/match/{result.Value}");
    }
}
```

- [ ] **Step 3: Проверить вручную**

Run: `dotnet run --project src/DartsLeaderboard.Web`
Expected: на `/players` добавляется игрок, повторное имя даёт красное сообщение, архивирование скрывает игрока из списка; на `/match/new` кнопка «Начать» активна только при двух и более выбранных игроках, после нажатия происходит переход на `/match/{id}` (страница пока не существует — ожидается пустой экран, это нормально до Task 16).

- [ ] **Step 4: Commit**

```bash
git add src/DartsLeaderboard.Web
git commit -m "feat(web): add players directory and new match pages"
```

---

### Task 16: Экран матча с таблицей, вводом и панелью статистики

**Files:**
- Create: `src/DartsLeaderboard.Web/Components/Match/RoundInput.razor`, `MatchScoreGrid.razor`, `MatchStatsPanel.razor`
- Create: `src/DartsLeaderboard.Web/Components/Pages/MatchPage.razor`
- Test: `tests/DartsLeaderboard.Web.Tests/RoundInputTests.cs`

**Interfaces:**
- Consumes: `GetMatchStateService`, `RecordThrowService`, `UndoLastThrowService`, `AbandonMatchService`, `MatchStateDto` (задачи 8, 10), `IMatchNotifier` (Task 13).
- Produces:
  - `RoundInput` с параметрами `Disabled` (bool), `PlayerName` (string?), `OnSubmit` (`EventCallback<int>`): пустое значение отправляет 0, Enter вызывает `OnSubmit` и очищает поле.
  - `MatchScoreGrid` с параметром `State` (`MatchStateDto`) и `Input` (RenderFragment) — рисует столбцы игроков, строки раундов, подвал с показателями.
  - `MatchStatsPanel` с параметром `Statistics` (`IReadOnlyList<StatisticDto>`).
  - Страница `/match/{MatchId:int}`, подписанная на уведомления об изменении матча.

- [ ] **Step 1: Подключить bUnit и написать падающие тесты компонента ввода**

```bash
dotnet add tests/DartsLeaderboard.Web.Tests package bunit
dotnet add tests/DartsLeaderboard.Web.Tests package MudBlazor
```

`tests/DartsLeaderboard.Web.Tests/RoundInputTests.cs`:

```csharp
using Bunit;
using DartsLeaderboard.Web.Components.Match;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace DartsLeaderboard.Web.Tests;

public class RoundInputTests : TestContext
{
    public RoundInputTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void EmptyValue_SubmitsZero()
    {
        int? submitted = null;
        var component = RenderComponent<RoundInput>(parameters => parameters
            .Add(p => p.OnSubmit, points => submitted = points));

        component.Find("input").KeyDown(key: "Enter");

        Assert.Equal(0, submitted);
    }

    [Fact]
    public void EnteredValue_SubmitsPointsAndClearsField()
    {
        int? submitted = null;
        var component = RenderComponent<RoundInput>(parameters => parameters
            .Add(p => p.OnSubmit, points => submitted = points));

        var input = component.Find("input");
        input.Change("60");
        input.KeyDown(key: "Enter");

        Assert.Equal(60, submitted);
        Assert.Equal(string.Empty, component.Find("input").GetAttribute("value") ?? string.Empty);
    }
}
```

- [ ] **Step 2: Убедиться, что тесты падают**

Run: `dotnet test tests/DartsLeaderboard.Web.Tests`
Expected: ошибка компиляции — нет `RoundInput`.

- [ ] **Step 3: Реализовать компонент ввода**

`Components/Match/RoundInput.razor`:

```razor
<MudNumericField T="int?" @bind-Value="_points" Label="@Label" Disabled="Disabled"
                 Min="0" Max="180" HideSpinButtons="true" Immediate="true"
                 OnKeyDown="OnKeyDown" Style="max-width: 160px" />

@code {
    private int? _points;

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public string? PlayerName { get; set; }

    [Parameter] public EventCallback<int> OnSubmit { get; set; }

    private string Label => PlayerName is null ? "Очки" : $"Очки: {PlayerName}";

    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key != "Enter" || Disabled)
        {
            return;
        }

        var points = _points ?? 0;
        _points = null;
        await OnSubmit.InvokeAsync(points);
    }
}
```

- [ ] **Step 4: Реализовать таблицу и панель статистики**

`Components/Match/MatchScoreGrid.razor`:

```razor
<MudSimpleTable Dense="true" Bordered="true" Striped="false" Style="overflow-x: auto">
    <thead>
        <tr>
            <th style="width: 96px">Раунд</th>
            @foreach (var column in State.Columns)
            {
                <th class="@(column.ParticipantId == State.CurrentParticipantId ? "mud-primary-text" : null)">
                    @column.PlayerName
                </th>
            }
        </tr>
    </thead>
    <tbody>
        @foreach (var row in State.Rows)
        {
            <tr>
                <td>@row.RoundNumber</td>
                @for (var index = 0; index < row.Cells.Count; index++)
                {
                    var cell = row.Cells[index];
                    var isCurrent = State.IsInProgress
                        && row.RoundNumber == State.CurrentRoundNumber
                        && State.Columns[index].ParticipantId == State.CurrentParticipantId;

                    <td class="@(isCurrent ? "mud-theme-primary" : null)" style="text-align: center">
                        @if (cell.Points is not null)
                        {
                            <MudText Typo="Typo.body1"><b>@cell.Points</b></MudText>
                            @if (cell.RunningValue is not null)
                            {
                                <MudText Typo="Typo.caption">@cell.RunningValue</MudText>
                            }
                        }
                        else if (isCurrent)
                        {
                            <MudText Typo="Typo.caption">ходит</MudText>
                        }
                        else
                        {
                            <MudText Typo="Typo.caption">—</MudText>
                        }
                    </td>
                }
            </tr>
        }
    </tbody>
    <tfoot>
        @foreach (var name in StatisticNames)
        {
            <tr>
                <td>@name</td>
                @foreach (var column in State.Columns)
                {
                    <td style="text-align: center">
                        @(column.Statistics.FirstOrDefault(s => s.Name == name)?.Value ?? "—")
                    </td>
                }
            </tr>
        }
    </tfoot>
</MudSimpleTable>

@code {
    [Parameter, EditorRequired] public MatchStateDto State { get; set; } = default!;

    private IEnumerable<string> StatisticNames =>
        State.Columns.SelectMany(c => c.Statistics.Select(s => s.Name)).Distinct();
}
```

`Components/Match/MatchStatsPanel.razor`:

```razor
<MudPaper Class="pa-4">
    <MudText Typo="Typo.overline" Class="mb-2">Статистика матча</MudText>
    @foreach (var item in Statistics)
    {
        <MudStack Row="true" Class="py-1">
            <MudText Typo="Typo.body2" Color="Color.Secondary">@item.Name</MudText>
            <MudSpacer />
            <MudText Typo="Typo.body2"><b>@item.Value</b></MudText>
        </MudStack>
    }
    @if (Statistics.Count == 0)
    {
        <MudText Typo="Typo.body2">Пока нет данных.</MudText>
    }
</MudPaper>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<StatisticDto> Statistics { get; set; } = Array.Empty<StatisticDto>();
}
```

- [ ] **Step 5: Реализовать страницу матча с живым обновлением**

`Components/Pages/MatchPage.razor`:

```razor
@page "/match/{MatchId:int}"
@using DartsLeaderboard.Application.Abstractions
@using DartsLeaderboard.Application.Matches
@using DartsLeaderboard.Web.Components.Match
@implements IDisposable
@inject GetMatchStateService GetState
@inject RecordThrowService RecordThrow
@inject UndoLastThrowService UndoThrow
@inject AbandonMatchService AbandonMatch
@inject IMatchNotifier Notifier
@inject ISnackbar Snackbar
@inject NavigationManager Navigation

<PageTitle>Матч #@MatchId</PageTitle>

@if (_state is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-4" Spacing="2">
        <MudText Typo="Typo.h5">@_state.ModeTitle</MudText>
        <MudChip T="string" Size="Size.Small">Матч #@_state.MatchId</MudChip>
        <MudChip T="string" Size="Size.Small">Раунд @_state.CurrentRoundNumber</MudChip>
        @if (_state.IsInProgress)
        {
            <MudChip T="string" Size="Size.Small" Color="Color.Primary">Бросает: @_state.CurrentPlayerName</MudChip>
        }
        else
        {
            <MudChip T="string" Size="Size.Small" Color="Color.Success">
                @_state.StatusTitle@(_state.WinnerPlayerName is null ? "" : $": {_state.WinnerPlayerName}")
            </MudChip>
        }
        <MudSpacer />
        @if (_state.IsInProgress)
        {
            <MudButton Variant="Variant.Text" Color="Color.Error" OnClick="Abandon">Прервать матч</MudButton>
        }
    </MudStack>

    <MudGrid>
        <MudItem xs="12" md="8">
            <MudPaper Class="pa-2">
                <MatchScoreGrid State="_state" />
            </MudPaper>

            <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Center" Class="mt-3">
                <RoundInput Disabled="@(!_state.IsInProgress)" PlayerName="@_state.CurrentPlayerName"
                            OnSubmit="Record" />
                <MudButton Variant="Variant.Outlined" OnClick="Undo">Отменить бросок</MudButton>
            </MudStack>
        </MudItem>
        <MudItem xs="12" md="4">
            <MatchStatsPanel Statistics="_state.MatchStatistics" />
        </MudItem>
    </MudGrid>
}

@code {
    private MatchStateDto? _state;
    private IDisposable? _subscription;

    [Parameter] public int MatchId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await Reload();
        _subscription = Notifier.Subscribe(MatchId, () => InvokeAsync(async () =>
        {
            await Reload();
            StateHasChanged();
        }));
    }

    private async Task Reload()
    {
        var result = await GetState.ExecuteAsync(MatchId);
        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
            Navigation.NavigateTo("/");
            return;
        }

        _state = result.Value;
    }

    private async Task Record(int points)
    {
        var result = await RecordThrow.ExecuteAsync(MatchId, points);
        if (result.IsSuccess)
        {
            _state = result.Value;
            return;
        }

        Snackbar.Add(result.ErrorMessage!, Severity.Error);
        await Reload();
    }

    private async Task Undo()
    {
        var result = await UndoThrow.ExecuteAsync(MatchId);
        if (result.IsSuccess)
        {
            _state = result.Value;
            return;
        }

        Snackbar.Add(result.ErrorMessage!, Severity.Warning);
    }

    private async Task Abandon()
    {
        var result = await AbandonMatch.ExecuteAsync(MatchId);
        if (!result.IsSuccess)
        {
            Snackbar.Add(result.ErrorMessage!, Severity.Error);
            return;
        }

        Navigation.NavigateTo("/");
    }

    public void Dispose() => _subscription?.Dispose();
}
```

- [ ] **Step 6: Прогнать тесты и проверить вручную**

Run: `dotnet test tests/DartsLeaderboard.Web.Tests` затем `dotnet run --project src/DartsLeaderboard.Web`
Expected: тесты PASS. Вручную: матч 501 на двух игроков — ввод числа и Enter записывает бросок, остаток уменьшается, ход переходит следующему; пустой Enter записывает 0; ввод больше остатка даёт сообщение «Больше остатка: при переборе вводите 0»; открытие той же страницы во второй вкладке показывает те же данные и обновляется после записи в первой.

- [ ] **Step 7: Commit**

```bash
git add src/DartsLeaderboard.Web tests/DartsLeaderboard.Web.Tests
git commit -m "feat(web): add match screen with score grid, input and live stats"
```

---

### Task 17: График трендов и история матчей

**Files:**
- Create: `src/DartsLeaderboard.Web/Components/Match/ThrowTrendChart.razor`
- Create: `src/DartsLeaderboard.Web/Components/Pages/History.razor`
- Modify: `src/DartsLeaderboard.Web/Components/Pages/MatchPage.razor` (вставить график)

**Interfaces:**
- Consumes: `MatchStateDto.ShowTrendChart`, `MatchStateDto.ChartSeries` (Task 8), `GetMatchListService` (Task 11).
- Produces: компонент `ThrowTrendChart` с параметром `Series` (`IReadOnlyList<ChartSeriesDto>`) и страница `/history`.

- [ ] **Step 1: Реализовать компонент графика**

`Components/Match/ThrowTrendChart.razor`:

```razor
<MudPaper Class="pa-4 mt-3">
    <MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-2">
        <MudText Typo="Typo.overline">Тренд по раундам</MudText>
        <MudSpacer />
        <MudToggleGroup T="bool" Value="_cumulative" ValueChanged="@(value => _cumulative = value)"
                        Size="Size.Small" Color="Color.Primary">
            <MudToggleItem Value="false" Text="Очки за раунд" />
            <MudToggleItem Value="true" Text="Накопительно" />
        </MudToggleGroup>
    </MudStack>

    @if (RoundCount == 0)
    {
        <MudText Typo="Typo.body2">Броски ещё не записаны.</MudText>
    }
    else
    {
        <MudChart ChartType="ChartType.Line" ChartSeries="ChartData" XAxisLabels="Labels" Height="260px" />
    }
</MudPaper>

@code {
    private bool _cumulative;

    [Parameter, EditorRequired] public IReadOnlyList<ChartSeriesDto> Series { get; set; } = Array.Empty<ChartSeriesDto>();

    private int RoundCount => Series.Count == 0 ? 0 : Series.Max(s => s.RoundPoints.Count);

    private string[] Labels => Enumerable.Range(1, RoundCount).Select(i => i.ToString()).ToArray();

    private List<MudBlazor.ChartSeries> ChartData => Series
        .Select(s => new MudBlazor.ChartSeries
        {
            Name = s.PlayerName,
            Data = (_cumulative ? s.CumulativePoints : s.RoundPoints).ToArray()
        })
        .ToList();
}
```

- [ ] **Step 2: Вставить график на страницу матча**

В `Components/Pages/MatchPage.razor`, внутри `<MudItem xs="12" md="8">`, сразу после блока с `RoundInput` и кнопкой отмены, добавить:

```razor
            @if (_state.ShowTrendChart)
            {
                <ThrowTrendChart Series="_state.ChartSeries" />
            }
```

Условие гарантирует, что в режиме x01 графика нет вовсе (`ShowTrendChart` там `false`).

- [ ] **Step 3: Сверстать страницу истории**

`Components/Pages/History.razor`:

```razor
@page "/history"
@using DartsLeaderboard.Application.Reports
@inject GetMatchListService MatchListService
@inject NavigationManager Navigation

<PageTitle>История</PageTitle>

<MudText Typo="Typo.h5" Class="mb-4">История матчей</MudText>

<MudPaper Class="pa-2">
    <MudTable Items="_matches" Dense="true" Hover="true" Loading="_loading"
              OnRowClick="@((TableRowClickEventArgs<MatchListItemDto> args) =>
                  Navigation.NavigateTo($"/match/{args.Item!.MatchId}"))">
        <HeaderContent>
            <MudTh>Матч</MudTh>
            <MudTh>Режим</MudTh>
            <MudTh>Игроки</MudTh>
            <MudTh>Победитель</MudTh>
            <MudTh>Завершён</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Матч">#@context.MatchId</MudTd>
            <MudTd DataLabel="Режим">@context.ModeTitle</MudTd>
            <MudTd DataLabel="Игроки">@context.Participants</MudTd>
            <MudTd DataLabel="Победитель">@(context.WinnerName ?? "Ничья")</MudTd>
            <MudTd DataLabel="Завершён">
                @(context.FinishedAt?.LocalDateTime.ToString("dd.MM.yyyy HH:mm") ?? "—")
            </MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudText Typo="Typo.body2">Завершённых матчей пока нет.</MudText>
        </NoRecordsContent>
    </MudTable>
</MudPaper>

@code {
    private IReadOnlyList<MatchListItemDto> _matches = Array.Empty<MatchListItemDto>();
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _matches = await MatchListService.ExecuteAsync(MatchListFilter.Finished);
        _loading = false;
    }
}
```

Экран матча уже работает как режим только для чтения: при завершённом матче поле ввода отключено, а кнопки прерывания нет.

- [ ] **Step 4: Проверить вручную**

Run: `dotnet run --project src/DartsLeaderboard.Web`
Expected: матч «Максимум за 5 раундов» показывает график, линии обновляются после каждого броска, переключатель меняет метрику; матч 501 графика не показывает; на `/history` завершённый матч открывается по клику и отображается без возможности ввода.

- [ ] **Step 5: Commit**

```bash
git add src/DartsLeaderboard.Web
git commit -m "feat(web): add throw trend chart and match history page"
```

---

### Task 18: Docker, документация и финальная проверка

**Files:**
- Create: `Dockerfile`, `docker-compose.yml`, `.env.example`, `README.md`
- Modify: `.gitignore` (уже игнорирует `.env`)

**Interfaces:**
- Consumes: всё приложение (задачи 1-17).
- Produces: запуск командой `docker compose up -d` с автоматическим применением миграций и доступом из локальной сети.

- [ ] **Step 1: Написать `Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY DartsLeaderboard.sln ./
COPY src/ ./src/
RUN dotnet restore src/DartsLeaderboard.Web/DartsLeaderboard.Web.csproj
RUN dotnet publish src/DartsLeaderboard.Web/DartsLeaderboard.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN adduser --disabled-password --no-create-home --uid 10001 darts
COPY --from=build /app/publish ./
USER darts
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DartsLeaderboard.Web.dll"]
```

- [ ] **Step 2: Написать `docker-compose.yml` и `.env.example`**

`docker-compose.yml`:

```yaml
services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - darts-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 5s
      retries: 10
    restart: unless-stopped

  web:
    build: .
    depends_on:
      db:
        condition: service_healthy
    environment:
      ConnectionStrings__Darts: "Host=db;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
    ports:
      - "8080:8080"
    restart: unless-stopped

volumes:
  darts-data:
```

`.env.example`:

```dotenv
POSTGRES_DB=darts
POSTGRES_USER=darts
POSTGRES_PASSWORD=change-me
```

- [ ] **Step 3: Написать `README.md`**

~~~markdown
# Darts Leaderboard

Домашнее приложение для подсчёта очков в дартс: таблица со столбцами-игроками, два режима игры,
живая статистика, график трендов и история матчей в PostgreSQL.

## Запуск

```bash
cp .env.example .env   # поменяй пароль
docker compose up -d
```

Приложение будет на `http://localhost:8080`. С телефона или планшета в домашней сети —
`http://<адрес-компьютера>:8080`, узнать адрес можно командой `ipconfig`.

## Режимы игры

- **x01** — начальный счёт 301, 501, 701 или свой; очки вычитаются, победа при остатке ровно 0.
- **Максимум за N раундов** — по умолчанию 5 раундов, побеждает наибольшая сумма.

Один бросок в таблице — это три дротика, записанные одним числом (0..180). Приложение не проверяет
правила дартса: перебор, недобор и закрытие даблом игроки определяют сами, незачтённый раунд
записывается нулём.

## Разработка

```bash
dotnet test
dotnet run --project src/DartsLeaderboard.Web
```

Интеграционные тесты поднимают PostgreSQL через Testcontainers, поэтому нужен запущенный Docker.

## Документы

- Дизайн: `docs/superpowers/specs/2026-08-17-darts-leaderboard-design.md`
- План реализации: `docs/superpowers/plans/2026-08-18-darts-leaderboard.md`
~~~

- [ ] **Step 4: Проверить сборку образа и запуск**

```bash
cp .env.example .env
docker compose up -d --build
docker compose logs web
```

Expected: в логах «Миграции применены с попытки N», `http://localhost:8080` открывается, матч создаётся и броски записываются. Затем `docker compose restart web` — незавершённый матч остаётся на месте с теми же бросками (проверка write-through).

- [ ] **Step 5: Прогнать все тесты**

Run: `dotnet test`
Expected: PASS во всех четырёх тестовых проектах.

- [ ] **Step 6: Commit**

```bash
git add Dockerfile docker-compose.yml .env.example README.md
git commit -m "chore: add docker compose setup and readme"
```

---

## Self-Review

Проверка плана против спеки выполнена: каждая секция спеки закрыта задачами — стек и структура решения (задачи 1, 14), домен с правилами и инвариантами (2-7), режимно-зависимая статистика (7), Application с тонкими use case-ами и write-through (8-11), схема БД со snake_case и уникальными индексами (12), запросы отчётов и уведомления (13), интерфейс варианта C с таблицей, панелью статистики и вводом как в Excel (16), график только в режиме на максимум (17), история и лидерборд (14, 17), тестирование на четырёх уровнях (2-16), Docker и README (18).

Известные допущения, которые исполнитель должен проверить на месте и поправить локально, не меняя смысла шагов:

- Точные сигнатуры `IAsyncLifetime` и `TestContext` зависят от установленных версий xUnit и bUnit.
- Имена параметров компонентов MudBlazor 9.x (`MudToggleGroup`, `MudChip`, `MudList`) могли измениться между минорными версиями; при ошибке компиляции сверяйся с документацией компонента, сохраняя описанное поведение.


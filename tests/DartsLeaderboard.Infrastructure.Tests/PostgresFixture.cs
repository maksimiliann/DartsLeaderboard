using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using DartsLeaderboard.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DartsLeaderboard.Infrastructure.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private LocalPostgresCluster? _ephemeral;
    private string? _adminConnectionString;
    private string? _databaseName;

    public string ConnectionString { get; private set; } = "";

    public string PostgresSource { get; private set; } = "";

    public async Task InitializeAsync()
    {
        if (await TryStartTestcontainersAsync())
        {
            PostgresSource = "testcontainers";
        }
        else if (await TryStartLocalServiceAsync())
        {
            PostgresSource = "local-service";
        }
        else
        {
            await StartEphemeralClusterAsync();
            PostgresSource = "ephemeral-binaries";
        }

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        if (_adminConnectionString is not null && _databaseName is not null)
        {
            try
            {
                await using var admin = new NpgsqlConnection(_adminConnectionString);
                await admin.OpenAsync();
                await using var drop = admin.CreateCommand();
                drop.CommandText = $"DROP DATABASE IF EXISTS {_databaseName} WITH (FORCE);";
                await drop.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                // Best-effort cleanup; ephemeral cluster shutdown still runs below.
            }
        }

        if (_ephemeral is not null)
        {
            await _ephemeral.DisposeAsync();
        }
    }

    public DartsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DartsDbContext>()
            .UseNpgsql(ConnectionString)
            .Options);

    private async Task<bool> TryStartTestcontainersAsync()
    {
        try
        {
            var container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await container.StartAsync();
            _container = container;
            ConnectionString = container.GetConnectionString();
            return true;
        }
        catch (Exception exception) when (IsDockerUnavailable(exception))
        {
            return false;
        }
    }

    private async Task<bool> TryStartLocalServiceAsync()
    {
        foreach (var candidate in LocalAdminCandidates())
        {
            if (await TryOpenAsync(candidate))
            {
                await CreateUniqueDatabaseAsync(candidate);
                return true;
            }
        }

        return false;
    }

    private async Task StartEphemeralClusterAsync()
    {
        _ephemeral = await LocalPostgresCluster.StartAsync();
        await CreateUniqueDatabaseAsync(_ephemeral.AdminConnectionString);
    }

    private async Task CreateUniqueDatabaseAsync(string adminConnectionString)
    {
        _adminConnectionString = adminConnectionString;
        _databaseName = "darts_test_" + Guid.NewGuid().ToString("N");

        await using var admin = new NpgsqlConnection(adminConnectionString);
        await admin.OpenAsync();
        await using var create = admin.CreateCommand();
        create.CommandText = $"CREATE DATABASE {_databaseName};";
        await create.ExecuteNonQueryAsync();

        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = _databaseName
        };
        ConnectionString = builder.ConnectionString;
    }

    private static IEnumerable<string> LocalAdminCandidates()
    {
        var fromEnv = Environment.GetEnvironmentVariable("DARTS_TEST_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Darts");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            yield return fromEnv;
        }

        var host = Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("PGPASSWORD");

        if (!string.IsNullOrEmpty(password))
        {
            yield return $"Host={host};Port={port};Database=postgres;Username={user};Password={password}";
        }

        yield return $"Host={host};Port={port};Database=postgres;Username={user}";
        yield return "Host=localhost;Port=5432;Database=postgres;Username=postgres";
        yield return "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
        yield return "Host=localhost;Port=5432;Database=postgres;Username=darts;Password=darts";
        yield return "Host=localhost;Port=5432;Database=darts;Username=darts;Password=darts";
    }

    private static async Task<bool> TryOpenAsync(string connectionString)
    {
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsDockerUnavailable(Exception exception) =>
        exception is DockerUnavailableException
        || exception.InnerException is DockerUnavailableException
        || exception.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase);
}

internal sealed class LocalPostgresCluster : IAsyncDisposable
{
    private readonly string _dataDirectory;
    private readonly string _binDirectory;
    private readonly int _port;

    private LocalPostgresCluster(string dataDirectory, string binDirectory, int port)
    {
        _dataDirectory = dataDirectory;
        _binDirectory = binDirectory;
        _port = port;
    }

    public string AdminConnectionString =>
        $"Host=127.0.0.1;Port={_port};Database=postgres;Username=postgres";

    public static async Task<LocalPostgresCluster> StartAsync()
    {
        var binDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PostgreSQL", "18", "bin");
        if (!File.Exists(Path.Combine(binDirectory, "initdb.exe")))
        {
            throw new InvalidOperationException(
                "Docker is unavailable and local PostgreSQL binaries were not found at Program Files\\PostgreSQL\\18\\bin.");
        }

        var dataDirectory = Path.Combine(Path.GetTempPath(), "darts-pg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDirectory);

        var port = GetFreeTcpPort();
        var cluster = new LocalPostgresCluster(dataDirectory, binDirectory, port);

        try
        {
            await cluster.RunAsync(
                "initdb.exe",
                $"-D \"{dataDirectory}\" -U postgres --auth=trust --encoding=UTF8 --locale=C --no-sync");

            var logFile = Path.Combine(dataDirectory, "pg.log");
            await cluster.RunAsync(
                "pg_ctl.exe",
                $"-D \"{dataDirectory}\" -l \"{logFile}\" -o \"-p {port} -c listen_addresses=127.0.0.1\" -w start");
        }
        catch
        {
            await cluster.DisposeAsync();
            throw;
        }

        return cluster;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await RunAsync("pg_ctl.exe", $"-D \"{_dataDirectory}\" -m fast -w stop");
        }
        catch (Exception)
        {
            // Ignore shutdown errors during cleanup.
        }

        try
        {
            if (Directory.Exists(_dataDirectory))
            {
                Directory.Delete(_dataDirectory, recursive: true);
            }
        }
        catch (Exception)
        {
            // Temp directory cleanup is best-effort.
        }
    }

    private async Task RunAsync(string executable, string arguments)
    {
        var start = new ProcessStartInfo
        {
            FileName = Path.Combine(_binDirectory, executable),
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Failed to start {executable}.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{executable} failed ({process.ExitCode}): {stderr}{stdout}");
        }
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;


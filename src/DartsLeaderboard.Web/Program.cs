using DartsLeaderboard.Application;
using DartsLeaderboard.Infrastructure;
using DartsLeaderboard.Web.Components;
using DartsLeaderboard.Web.Speech;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = builder.Environment.IsDevelopment());
builder.Services.AddMudServices();
builder.Services.AddMemoryCache();
builder.Services.Configure<SpeechOptions>(builder.Configuration.GetSection(SpeechOptions.SectionName));
builder.Services.AddHttpClient<SileroSpeechClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<SpeechOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(60);
});

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
app.MapPost("/api/speech", async (
    SpeechTextRequest request,
    SileroSpeechClient speech,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 4000)
    {
        return Results.BadRequest();
    }

    try
    {
        var wav = await speech.SynthesizeAsync(request.Text, cancellationToken);
        return Results.File(wav, "audio/wav");
    }
    catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
    {
        return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Сервис озвучивания недоступен");
    }
}).DisableAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode(options =>
    options.DisableWebSocketCompression = true);

app.Run();

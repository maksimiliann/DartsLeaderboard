using DartsLeaderboard.Application;
using DartsLeaderboard.Infrastructure;
using DartsLeaderboard.Web.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = builder.Environment.IsDevelopment());
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
app.MapRazorComponents<App>().AddInteractiveServerRenderMode(options =>
    options.DisableWebSocketCompression = true);

app.Run();

using kechap.Components;
using kechap.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Scoped = one instance per browser tab (no database for now)
builder.Services.AddScoped<TimerSettingsService>();
builder.Services.AddScoped<PomodoroTimerService>();

// Singleton = one shared instance for everyone, so all visitors see the same
// ratings and comments (saved to App_Data/feedback.json).
builder.Services.AddSingleton<FeedbackService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
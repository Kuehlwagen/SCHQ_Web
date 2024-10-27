using Microsoft.EntityFrameworkCore;
using SCHQ_Blazor.Components;
using SCHQ_Blazor.Models;
using SCHQ_Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();
builder.Services.AddControllers();
builder.Services.AddMemoryCache(x => x.SizeLimit = builder.Configuration.GetSection("MemoryCache").GetValue<int>("SizeLimit"));

// Add services to the container.
builder.Services.AddRazorComponents()
  .AddInteractiveServerComponents()
  .AddInteractiveWebAssemblyComponents();

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<SCHQ_Service>();

// Create / migrate SQLite database
RelationsContext context = new();
if (context.Database.GetPendingMigrations().Any()) {
  /*
    Developer-PowerShell:
    dotnet add package Microsoft.EntityFrameworkCore.Design
    dotnet tool update --global dotnet-ef
    dotnet ef migrations add MigrationName
    dotnet ef migrations remove
  */
  await context.Database.MigrateAsync();
}
await context.Database.EnsureCreatedAsync();

// Remove channels without password
try {
  Channel?[] channels = [.. context.Channels.Where(c => string.IsNullOrEmpty(c.Password))];
  if (channels?.Length > 0) {
    context.RemoveRange(channels!);
    context.SaveChanges();
  }
} catch (Exception) { }

// Culture / Localization
string[] supportedCultures = ["de-DE", "en-US"];
var localizationOptions = new RequestLocalizationOptions()
  .SetDefaultCulture(supportedCultures[0])
  .AddSupportedCultures(supportedCultures)
  .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
  app.UseWebAssemblyDebugging();
} else {
  app.UseExceptionHandler("/Error", createScopeForErrors: true);
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();

app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode()
  .AddInteractiveWebAssemblyRenderMode();

app.Run();

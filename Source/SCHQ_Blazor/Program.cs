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

// Use MySQL database
builder.Services.AddDbContext<RelationsContext>(options =>
  options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")!));

builder.Services.AddTransient<SCHQ_Service>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<SCHQ_Service>();

// Culture / Localization
string[] supportedCultures = ["de-DE", "en-US"];
var localizationOptions = new RequestLocalizationOptions()
  .SetDefaultCulture(supportedCultures[0])
  .AddSupportedCultures(supportedCultures)
  .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

// Add / Update Database
using var scope = app.Services.CreateScope();
using var relationsContext = scope.ServiceProvider.GetService<RelationsContext>();
if (relationsContext!.Database.GetPendingMigrations().Any()) {
  /*
    Developer-PowerShell:
    dotnet add package Microsoft.EntityFrameworkCore.Design
    dotnet tool update --global dotnet-ef
    dotnet ef migrations add MigrationName
    dotnet ef migrations remove
    dotnet ef database update <previous-migration-name>
    dotnet ef database update 0
  */
  await relationsContext.Database.MigrateAsync();
}
await relationsContext.Database.EnsureCreatedAsync();

// Remove channels without password
try {
  Channel?[] channels = [.. relationsContext.Channels!.Where(c => string.IsNullOrEmpty(c.Password))];
  if (channels?.Length > 0) {
    relationsContext.RemoveRange(channels!);
    relationsContext.SaveChanges();
  }
} catch (Exception) { }

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

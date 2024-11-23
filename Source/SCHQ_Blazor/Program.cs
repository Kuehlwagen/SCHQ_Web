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

/*
server {
        server_name     sctools.de;
        location / {
                proxy_pass              http://127.0.0.1:5555;
                proxy_http_version      1.1;
                proxy_set_header        Upgrade $http_upgrade;
                proxy_set_header        Connection keep-alive;
                proxy_set_header        Host $host;
                proxy_cache_bypass      $http_upgrade;
                proxy_set_header        X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header        X-Forwarded-Proto $scheme;
                proxy_redirect          off;
        }
        location /schq.SCHQ_Relations {
                grpc_pass               grpc://127.0.0.1:5556;
        }
        client_header_timeout   3h;
        client_body_timeout     3h;
        grpc_read_timeout       3h;
        grpc_send_timeout       3h;
        proxy_read_timeout      3h;
        listen 443 http2 ssl; # managed by Certbot
        ssl_certificate /etc/letsencrypt/live/sctools.de-0001/fullchain.pem; # managed by Certbot
        ssl_certificate_key /etc/letsencrypt/live/sctools.de-0001/privkey.pem; # managed by Certbot
        include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
        ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot
}

server {
        if ($host = sctools.de) {
                return 301 https://$host$request_uri;
        } # managed by Certbot
        listen          80;
        server_name     sctools.de;
        return 404; # managed by Certbot
}
*/
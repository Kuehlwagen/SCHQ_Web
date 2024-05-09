using Grpc.Net.Client;
using SCHQ_Blazor.Classes;
using SCHQ_Blazor.Components;
using SCHQ_Protos;
using static SCHQ_Protos.SCHQ_Relations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization();
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

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

app.MapPost("/relations", async (RestSetRelationsRequest request) => {
  IResult result = Results.Empty;
  try {
    string? gRPC_Url = builder.Configuration.GetValue<string>("gRPC_Url");
    if (gRPC_Url != null) {
      SCHQ_RelationsClient gRPC_Client = new(GrpcChannel.ForAddress(gRPC_Url,
        new() {
          HttpHandler = new SocketsHttpHandler() {
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
          }
        }));
      var rpc_request = new SetRelationsRequest() {
        Channel = request.Channel,
        Password = request.Password
      };
      rpc_request.Relations.AddRange(request.Relations!);
      SuccessReply rtnVal = await gRPC_Client.SetRelationsAsync(rpc_request);
      if (rtnVal.Success) {
        result = Results.Ok(rtnVal);
      } else {
        result = Results.BadRequest(rtnVal);
      }
    }
  } catch (Exception ex) {
    result = Results.Conflict(ex);
  }
  return result;
});

app.Run();

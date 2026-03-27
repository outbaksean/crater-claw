using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CraterClaw.Blazor;
using CraterClaw.Blazor.Api;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient<CraterClawClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

await builder.Build().RunAsync();

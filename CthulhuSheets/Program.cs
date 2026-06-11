using CthulhuSheets;
using CthulhuSheets.Services;
using CthulhuSheets.Services.Storage;
using Magic.IndexedDb;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMagicBlazorDB(BlazorInteropMode.WASM, builder.HostEnvironment.IsDevelopment());

builder.Services.AddMudServices();
builder.Services.AddScoped<DiceRollService>();
builder.Services.AddScoped<IndexedDbCharacterStore>();
builder.Services.AddScoped<LocalStorageCharacterStore>();
builder.Services.AddScoped<InvestigatorService>();

await builder.Build().RunAsync();

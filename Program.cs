using GaGSemanticMap.Components;
using DotNetEnv;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using GaGSemanticMap;
using Azure.AI.OpenAI;
using Azure;
using GaGSemanticMap.Services;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

//add configs
builder.Configuration.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true, true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(); 


// register services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(provider =>
{
  
    OpenAIClient client = Environment.GetEnvironmentVariable("TYPE") == "azure"
        ? new OpenAIClient(new Uri(Environment.GetEnvironmentVariable("ENDPOINT")!), new AzureKeyCredential(Environment.GetEnvironmentVariable("KEY")))
        : new OpenAIClient(Environment.GetEnvironmentVariable("KEY"));

    return client;
});

builder.Services.AddSingleton<ISemanticSearchService, SemanticSearchService>();


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

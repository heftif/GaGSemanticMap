using GaGSemanticMap.Components;
using DotNetEnv;
using Azure.AI.OpenAI;
using Azure;
using GaGSemanticMap.Services;
using GaGSemanticMap.Skills;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

string key = Environment.GetEnvironmentVariable("KEY");
string endPoint = Environment.GetEnvironmentVariable("ENDPOINT");
string model = Environment.GetEnvironmentVariable("MODEL");
string embeddingModel = Environment.GetEnvironmentVariable("EMBEDDING");

//todo: implement a logger

//add configs
/*builder.Configuration.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true, true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(); */

// register services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLogging();
builder.Logging.SetMinimumLevel(LogLevel.Warning);


var client = new OpenAIClient(new Uri(endPoint!), new AzureKeyCredential(key));

//initiate kernel
var kernelBuilder = new KernelBuilder();
kernelBuilder.WithAzureOpenAIChatCompletionService(model, client);
kernelBuilder.WithAzureOpenAITextEmbeddingGenerationService(embeddingModel, endPoint!, key);
IKernel kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);


builder.Services.AddSingleton<ISemanticSearchService, SemanticSearchService>();
builder.Services.AddSingleton<IKernelService, KernelService>();
builder.Services.AddSingleton<IOutputSkill, OutputSkill>();
builder.Services.AddSingleton<ICheckInputFunction, CheckInputFunction>();


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

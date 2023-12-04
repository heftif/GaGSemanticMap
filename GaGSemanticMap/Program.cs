using GaGSemanticMap.Components;
using DotNetEnv;
using Azure.AI.OpenAI;
using Azure;
using GaGSemanticMap.Services;
using GaGSemanticMap.Skills;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Plugins.Memory;

var builder = WebApplication.CreateBuilder(args);

//initialise environment and variables
Env.Load();

string key = Environment.GetEnvironmentVariable("KEY");
string endPoint = Environment.GetEnvironmentVariable("ENDPOINT");
string model = Environment.GetEnvironmentVariable("MODEL");
string embeddingModel = Environment.GetEnvironmentVariable("EMBEDDING");

// register services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//todo: implement a logger
builder.Services.AddLogging();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

//get client
var client = new OpenAIClient(new Uri(endPoint!), new AzureKeyCredential(key));

//initiate kernel
var kernelBuilder = new KernelBuilder();
kernelBuilder.WithAzureOpenAIChatCompletionService(model, client);
//kernelBuilder.WithAzureOpenAITextEmbeddingGenerationService(embeddingModel, endPoint!, key);
IKernel kernel = kernelBuilder.Build();

//configure plugins (folders mainly)
var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "plugins");
kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "OrchestratorPlugin");
kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, "ChatPlugin");
builder.Services.AddSingleton(kernel);

//create memory for embeddings
var memoryBuilder = new MemoryBuilder();
memoryBuilder.WithAzureOpenAITextEmbeddingGenerationService(embeddingModel, endPoint!, key);
memoryBuilder.WithMemoryStore(new VolatileMemoryStore());
var memory = memoryBuilder.Build();
builder.Services.AddSingleton(memory);

//adding services
builder.Services.AddSingleton<ISemanticSearchService, SemanticSearchService>();
builder.Services.AddSingleton<IKernelService, KernelService>();
builder.Services.AddSingleton<IChatConversationFunction, ChatConversationFunction>();


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

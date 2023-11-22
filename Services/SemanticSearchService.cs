using Azure;
using Azure.AI.OpenAI;
using GaGSemanticMap.Models;

namespace GaGSemanticMap.Services;

public class SemanticSearchService : ISemanticSearchService
{
    string key = Environment.GetEnvironmentVariable("KEY");
    string endPoint = Environment.GetEnvironmentVariable("ENDPOINT");
	string model = Environment.GetEnvironmentVariable("MODEL");

    List<EventPoint> eventPoints = new List<EventPoint>();

    public SemanticSearchService() 
    {
        //load the eventpoints from csv file 
        //use a db in the future
        eventPoints = File.ReadAllLines("GaGData_181123.csv").Skip(1).Select(x => EventPoint.FromCsv(x)).ToList();


    }

	public async Task<string> GetOpenAIResponse(string userInput)
    {
        OpenAIClient client = new OpenAIClient(new Uri(endPoint!), new AzureKeyCredential(key));

        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                new ChatMessage(ChatRole.System, "You are an unhelpful assistant, getting sassy when you have to answer a question"),
                new ChatMessage(ChatRole.User, userInput)
            },
            MaxTokens = 400,
            DeploymentName = model
           
        };

        Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionsOptions);

        var botResponse = response.Value.Choices.First().Message.Content;
        
        return botResponse;
    }
}

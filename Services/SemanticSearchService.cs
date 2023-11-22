using Azure;
using Azure.AI.OpenAI;
using GaGSemanticMap.Models;
using Vector = Pgvector.Vector;

namespace GaGSemanticMap.Services;

public class SemanticSearchService : ISemanticSearchService
{
    string key = Environment.GetEnvironmentVariable("KEY");
    string endPoint = Environment.GetEnvironmentVariable("ENDPOINT");
	string model = Environment.GetEnvironmentVariable("MODEL");
	string embeddingModel = Environment.GetEnvironmentVariable("EMBEDDING");

	List<EventPoint> eventPoints = new List<EventPoint>();


    public SemanticSearchService() 
    {
        //load the eventpoints from csv file 
        //use a db in the future
        eventPoints = File.ReadAllLines("GaGData_181123.csv").Skip(1).Select(x => EventPoint.FromCsv(x)).ToList();

		Console.WriteLine("Initialized Event Points");

	}

	public async Task<string> GetOpenAIResponseAsync(string userInput)
    {
		//initialise client
		var client = new OpenAIClient(new Uri(endPoint!), new AzureKeyCredential(key));

		var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            Messages =
            {
                //new ChatMessage(ChatRole.System, "You are an unhelpful assistant, getting sassy when you have to answer a question"),
                new ChatMessage(ChatRole.System, "You speak like Yoda and give wise advice"),
                new ChatMessage(ChatRole.User, userInput)
            },
            MaxTokens = 400,
            DeploymentName = model
           
        };

        Response<ChatCompletions> response = await client.GetChatCompletionsAsync(chatCompletionsOptions);

        var botResponse = response.Value.Choices.First().Message.Content;
        
        return botResponse;
    }

    private async Task<Vector> GetEmbeddingsAsync(string userInput, int retry)
    {
        //initialise client
		var client = new OpenAIClient(new Uri(endPoint!), new AzureKeyCredential(key));

		Console.WriteLine($"Getting embedding for {userInput}");

		try
		{
			EmbeddingsOptions options = new EmbeddingsOptions(embeddingModel, new string[] { userInput });

			var result = (await client.GetEmbeddingsAsync(options)).Value.Data[0].Embedding;

			return new Vector(result);

		}
		catch (Exception ex)
		{
			if (retry < 5)
			{
				retry++;

				Console.WriteLine(ex.Message);
				Console.WriteLine("Retry");

				return await GetEmbeddingsAsync(userInput, retry);
			}

			return null;
		}

	}

    public async Task GetEventsBySemanticRelevanceAsync(string userInput)
    {
		int retry = 0;

		// Create an embedding for the input search
		var vector = await GetEmbeddingsAsync(userInput, retry);


		var eventsWithDistance = eventPoints
				.Select(c => new { Item = c, Distance = GetCosineSimilarity(vector.ToArray(), c.Embedding.ToArray())})
				.OrderBy(c => c.Distance)
				.ToList();


		//it should be this, but we can only do this with database backing, so needs to wait
		/*var eventsWithDistance = eventPoints
		        .Select(c => new { Item = c, Distance = c.Embedding.CosineDistance(vector) })
		        .OrderBy(c => c.Distance)
		        .ToList();*/

		//print the 10 closest items
		for(int i = 0; i < 10; i++)
		{
			var e = eventsWithDistance[i];
			Console.WriteLine($"Event: {e.Item.EpisodeName}, Description: {e.Item.Description}");
		}

	}

	public double GetCosineSimilarity(float[] V1, float[] V2)
	{
		int N = 0;

		N = ((V2.Count() < V1.Count()) ? V2.Count() : V1.Count());

		double dot = 0.0d;
		double mag1 = 0.0d;
		double mag2 = 0.0d;
		for (int n = 0; n < N; n++)
		{
			dot += V1[n] * V2[n];
			mag1 += Math.Pow(V1[n], 2);
			mag2 += Math.Pow(V2[n], 2);
		}

		return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
	}
}

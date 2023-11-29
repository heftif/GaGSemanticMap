using Azure;
using Azure.AI.OpenAI;
using GaGSemanticMap.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using System.Collections;
using Vector = Pgvector.Vector;


namespace GaGSemanticMap.Services;

public class SemanticSearchService : ISemanticSearchService
{

	List<EventPoint> eventPoints = new List<EventPoint>();
	ISemanticTextMemory memory;


	public SemanticSearchService(ISemanticTextMemory memory) 
    {
        //load the eventpoints from csv file 
        eventPoints = File.ReadAllLines("GaGData_181123.csv").Skip(1).Select(x => EventPoint.FromCsv(x)).ToList();

		Console.WriteLine("Initialized Event Points");


		//it would be nice to do this with a memory store, but to do this, we need to store
		//the embeddings vectors as they take forever
		//to load otherwise - also, these many accesses might be costly.
		//We could do this using sqlite (see connector)

		//fill memory with info from podcast
		/*string collectionName = "GaGEpisodes";
		foreach(var ep in eventPoints)
		{
			memory.SaveInformationAsync(collectionName, ep.Description, ep.EpisodeName).GetAwaiter().GetResult();
		}

		this.memory = memory;*/
		
		//TEST(collectionName).GetAwaiter().GetResult();

	}

	/*private async Task TEST(string collectionName)
	{
		//test
		var questions = new[]
		{
			"what about things in london",
			"are there events in asia",
			"do you know things about vikings?"
		};

		foreach (var q in questions)
		{
			var responses = memory.SearchAsync(collectionName, q, limit: 5).ToBlockingEnumerable();

			foreach(var response in responses)
				Console.WriteLine(q + " " + response?.Metadata.Text);
		}
	}*/

	//in a next step, this should be changed to move the data I read into the semantic memory and than retrieve from there.
	[SKFunction, SKName(nameof(GetEventsBySemanticRelevanceAsync))]
	public async Task<string> GetEventsBySemanticRelevanceAsync(string botInput)
    {
		// Create an embedding for the input search
		var vector = await GetEmbeddingsAsync(botInput);

		var eventsWithDistance = eventPoints
				.Select(c => new { Item = c, Distance = GetCosineDistance(vector.ToArray(), c.Embedding.ToArray())})
				.OrderBy(c => c.Distance)
				.ToList();

		//it should be this, but we can only do this with database backing, so needs to wait
		/*var eventsWithDistance = eventPoints
		        .Select(c => new { Item = c, Distance = c.Embedding.CosineDistance(vector) })
		        .OrderBy(c => c.Distance)
		        .ToList();*/

		var maxDistance = eventsWithDistance.Select(x => x.Distance).Max();
		var minDistance = eventsWithDistance.Select(x => x.Distance).Min();

		//stretch events to range from 0 to 1, to make it easier to find a cutoff
		var normalizedEvents = eventsWithDistance
			.Select(c => new EventPointWithDistance{ 
					EventPoint = c.Item, 
					Distance =		c.Distance, 
					NormDistance = NormalizeDistance(c.Distance, minDistance, maxDistance) 
			})
			.ToList();

		//string assembly
		string reply = "";

		//print the 20 closest items
		for (int i = 0; i < 10; i++)
		{
			var e = normalizedEvents[i];
			Console.WriteLine($" {i}. Event: {e.EventPoint.EpisodeName}, Distance: {e.Distance}");
			e.Rank = i;
		}

		reply = JSONHelper.ConvertToJson<EventPointWithDistance>(normalizedEvents.Take(5).ToList());

		Console.WriteLine($"Finished with results from embedding");

		return reply;

	}

	private async Task<Vector> GetEmbeddingsAsync(string botInput)
	{

		string key = Environment.GetEnvironmentVariable("KEY");
		string endPoint = Environment.GetEnvironmentVariable("ENDPOINT");
		string embeddingModel = Environment.GetEnvironmentVariable("EMBEDDING");

		//initialise client
		var client = new OpenAIClient(new Uri(endPoint!), new AzureKeyCredential(key));

		Console.WriteLine($"Getting embedding for {botInput}");

		try
		{
			EmbeddingsOptions options = new EmbeddingsOptions(new string[] { botInput });

			var result = (await client.GetEmbeddingsAsync(embeddingModel, options)).Value.Data[0].Embedding;

			return new Vector(result as float[] ?? [.. result]);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);

			return null;
		}

	}

	private double NormalizeDistance(double value, double min, double max)
	{
		//linearly extrapolate to range between 0 to 1
		return (value- min) / (max - min);
	}

	private double GetCosineDistance(float[] V1, float[] V2)
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

		return (1 - (dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2))));
	}
}

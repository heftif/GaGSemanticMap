using Azure;
using Azure.AI.OpenAI;
using GaGSemanticMap.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Memory;
using System.Collections;
using System.Formats.Asn1;
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
	}

	//give back an event point that fits the episode description.
	public async Task<EventPoint> GetEventPointAsync(string episodeName)
	{
		return eventPoints.Where(x => episodeName.Contains(x.EpisodeName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
	}

	//in a next step, this should be changed to move the data I read into the semantic memory and than retrieve from there.
	[SKFunction, SKName(nameof(GetEventsBySemanticRelevanceAsync))]
	public async Task<string> GetEventsBySemanticRelevanceAsync(string botInput)
    {
		// Create an embedding for the input search
		var vector = await GetEmbeddingsAsync(botInput);

		//calculate cosine distance of event to my given points
		var eventsWithDistance = eventPoints
				.Select(c => new EventPointWithDistance
				{	EventPoint = c, 
					Distance = GetCosineDistance(vector.ToArray(), c.Embedding.ToArray())
				})
				.OrderBy(c => c.Distance)
				.ToList();

		//string assembly
		string reply = "";

		//print the 20 closest items
		for (int i = 0; i < 10; i++)
		{
			var e = eventsWithDistance[i];
			Console.WriteLine($" {i}. Event: {e.EventPoint.EpisodeName}, Distance: {e.Distance}");
			e.Rank = i;
		}

		reply = JSONHelper.ConvertToJson<EventPointWithDistance>(eventsWithDistance.Take(5).ToList());

		Console.WriteLine($"Finished with results from embedding");

		return reply;

	}

	//get the embedding vector of the given input
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

	//calculate cosine distance between two floats
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

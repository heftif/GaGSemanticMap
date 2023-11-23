using Azure.AI.OpenAI;
using Azure;
using DotNetEnv;
using Pgvector;
using OfficeOpenXml;
using System.Runtime.ConstrainedExecution;
using System.Net.Http.Headers;
using CreateDataEmbeddings;

public class Programm
{
	public static async Task Main()
	{
		Env.Load();

		string key = Environment.GetEnvironmentVariable("KEY");
		string endPoint = Environment.GetEnvironmentVariable("ENDPOINT");
		string embeddingModel = Environment.GetEnvironmentVariable("EMBEDDING"); 

		string filePath = "GaGData_181123.xlsx";

		ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

		//initialise client
		var client = new OpenAIClient(new Uri(endPoint!), new AzureKeyCredential(key));

		//read data from excel, create embedding and save to excel file
		using (var package = new ExcelPackage(new FileInfo(filePath)))
		{
			var worksheet = package.Workbook.Worksheets[0];

			for (int row = 2; row <= worksheet.Dimension.Rows; row++)
			{
				string description = worksheet.Cells[row, 4].Text;
				bool isVectorized = !string.IsNullOrEmpty(worksheet.Cells[row, 5].Text);

				if (!string.IsNullOrWhiteSpace(description) && !isVectorized)
				{
					// Get the episode name from column 1
					string episodeName = worksheet.Cells[row, 2].Text;

					int retry = 0;
					// Process the content and remove the episode name
					var vec = await CreateEmbeddingAsync(client, description, embeddingModel, episodeName, retry);

					if (vec != null)
					{
						// Add the processed content to the fourth column
						worksheet.Cells[row, 5].Value = vec;

						//save the changes
						package.Save();
					}
				}
			}

		}

		List<EventPoint> eventPoints = new List<EventPoint>();

		//create objects and check the embeddings
		using (var package = new ExcelPackage(new FileInfo(filePath)))
		{
			var worksheet = package.Workbook.Worksheets[0];

			

			for (int row = 2; row <= worksheet.Dimension.Rows; row++)
			{
				var ep = new EventPoint
				{
					EpisodeName = worksheet.Cells[row, 2].Text,
					Embedding = new Vector(worksheet.Cells[row, 5].Text)
				};

				eventPoints.Add(ep);
			}
		}

		//check a random data point to see how the embeddings are distributed and if everything's correct
		var baseEvent = eventPoints[10];

		var vector = baseEvent.Embedding;

		Console.WriteLine($"Selected Event to Check: {baseEvent.EpisodeName}");
		Console.WriteLine($"-----");
		Console.WriteLine($"Closest Events:");

		//calculate cosine distance between all event and the given random event point
		var eventsWithDistance = eventPoints
				.Select(c => new { Item = c, Distance = GetCosineSimilarity(vector.ToArray(), c.Embedding.ToArray()) })
				.OrderByDescending(c => c.Distance)
				.ToList();


		//print the 10 closest items
		for (int i = 0; i < 20; i++)
		{
			var e = eventsWithDistance[i];
			Console.WriteLine($" {i}. Event: {e.Item.EpisodeName}, Distance {e.Distance}");
		}

	}


	static async Task<Vector> CreateEmbeddingAsync(OpenAIClient client, string description, string embeddingModel, string episodeName, int retry)
	{

		Console.WriteLine($"Getting embedding for {episodeName}, Length: {description.Length}");

		try
		{
			EmbeddingsOptions options = new EmbeddingsOptions(embeddingModel, new string[] { description });

			var result = (await client.GetEmbeddingsAsync(options)).Value.Data[0].Embedding;

			return new Vector(result);
		}
		catch (Exception ex)
		{
			if (retry < 10)
			{
				retry++;

				Console.WriteLine(ex.Message);
				Console.WriteLine("Retry");

				return await CreateEmbeddingAsync(client, description, embeddingModel, episodeName, retry);
			}

			return null;
		}
		
	}

	private static double GetCosineSimilarity(float[] V1, float[] V2)
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

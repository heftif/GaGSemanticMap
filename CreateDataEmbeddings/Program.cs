using Azure.AI.OpenAI;
using Azure;
using DotNetEnv;
using Pgvector;
using OfficeOpenXml;
using System.Runtime.ConstrainedExecution;
using System.Net.Http.Headers;
using CreateDataEmbeddings;
using UMAP;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Drawing;
using MessagePack;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;

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

			string pattern = @"(GAG\d{3}:|ZS\d{2,3}:)";

			for (int row = 2; row <= worksheet.Dimension.Rows; row++)
			{
				var epsiodeNumber = "";

				var match = Regex.Matches(worksheet.Cells[row, 2].Text, pattern).FirstOrDefault();
				if(match != null)
				{
					epsiodeNumber = match.Value;
				}

				var ep = new EventPoint
				{
					EpisodeName = worksheet.Cells[row, 2].Text,
					Embedding = new Vector(worksheet.Cells[row, 5].Text),
					EpisodeNumber = epsiodeNumber,
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


		//visualise embeddings with UMap
		VisualiseEmbeddings(eventPoints);
	}

	static void VisualiseEmbeddings(List<EventPoint> eventPoints)
	{
		//see https://github.com/curiosity-ai/umap-sharp for reference

		// Define Vectors
		float[][] vectors = eventPoints.Select(x => x.Embedding.ToArray()).ToArray();

		// Calculate embedding vectors using the default configuration
		var umap = new Umap(distance: Umap.DistanceFunctions.Cosine);
		var numberOfEpochs = umap.InitializeFit(vectors);
		for (var i = 0; i < numberOfEpochs; i++)
			umap.Step();

		// This will be a float[][] where each nested array has two elements
		var embeddings = umap.GetEmbedding()
			.Select(vector => new { X = vector[0], Y = vector[1] })
			.ToArray();

		// Fit the vectors to a 0-1 range (this isn't necessary if feeding these values down from a server to a browser to draw with Plotly because ronend because Plotly scales the axes to the data)
		var minX = embeddings.Min(vector => vector.X);
		var rangeX = embeddings.Max(vector => vector.X) - minX;
		var minY = embeddings.Min(vector => vector.Y);
		var rangeY = embeddings.Max(vector => vector.Y) - minY;


		var scaledEmbeddings = embeddings
			.Select(vector => new { X = (vector.X - minX) / rangeX, Y = (vector.Y - minY) / rangeY })
			.ToArray();

		const int width = 2400;
		const int height = 1800;
		using (var bitmap = new Bitmap(width, height))
		{
			using (var g = Graphics.FromImage(bitmap))
			{
				g.FillRectangle(Brushes.LightBlue, 0, 0, width, height);
				g.SmoothingMode = SmoothingMode.HighQuality;
				g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				using (var font = new Font("Tahoma", 10))
				{
					foreach (var (vector, uid) in scaledEmbeddings.Zip(eventPoints.Select(x => x.EpisodeName), (vector, entry) => (vector, entry)))
					{
						if (uid != "GAG325: Der Große Smog von 1952" 
							&& uid !="GAG09: Wer den englischen Parlamentsbrand auf dem Kerbholz hat"
							&& uid != "GAG355: Der Englische Schweiß"
							&& uid != "GAG94: Wer zittert nicht vor den Mohocks?")
						{
							g.DrawString(uid, font, Brushes.Black, vector.X * width, vector.Y * height);
						}
						else
						{
							g.DrawString(uid, font, Brushes.Red, vector.X * width, vector.Y * height);
						}
					}
				}
			}
			bitmap.Save("Output-Label.png");
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



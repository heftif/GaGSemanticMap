using Newtonsoft.Json;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vector = Pgvector.Vector;

namespace GaGSemanticMap.Models;

public class EventPoint
{
	public string EpisodeName;

	public string EpsiodeLink;

	public string Description;

	public Location Location;

	[JsonIgnore]
	public Vector Embedding;

	public static EventPoint FromCsv(string csvLine)
	{
		string[] values = csvLine.Split(';');

		if(values.Length > 5 ) 
		{
			Console.WriteLine("Warning: found more than 4 cells for given row!");

		}

		var ep = new EventPoint();
		ep.Location = Location.FromCsv(values[0]);
		ep.EpisodeName = values[1];
		ep.EpsiodeLink = values[2];
		ep.Description = values[3];
		ep.Embedding = new Vector(values[4]);

		return ep;
	}
}

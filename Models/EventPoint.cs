using System.Runtime.CompilerServices;

namespace GaGSemanticMap.Models;

public class EventPoint
{
	public string EpisodeName;

	public string EpsiodeLink;

	public string Description;

	public Location Location;

	public static EventPoint FromCsv(string csvLine)
	{
		//todo: implement a warning if we have more than 4 entries given back
		string[] values = csvLine.Split(';');

		var ep = new EventPoint();
		ep.Location = Location.FromCsv(values[0]);
		ep.EpisodeName = values[1];
		ep.EpsiodeLink = values[2];
		ep.Description = values[3];

		return ep;
	}
}

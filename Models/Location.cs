using System.Text.RegularExpressions;

namespace GaGSemanticMap.Models;

public class Location
{
	public double Latitude { get; set; }

	public double Longitude { get; set; }

	public static Location FromCsv(string csv)
	{
		var loc = new Location();
		Tuple<double, double> coordinates = ExtractCoordinates(csv);

		loc.Latitude = coordinates.Item1;
		loc.Longitude = coordinates.Item2;

		return loc;
	}

	static Tuple<double, double> ExtractCoordinates(string input)
	{
		// Define a regular expression pattern to match the latitude and longitude
		string pattern = @"POINT\s*\(\s*(-?\d+\.\d+)\s+(-?\d+\.\d+)\s*\)";

		// Use Regex.Match to find the matches in the input text
		Match match = Regex.Match(input, pattern);

		// Check if the match was successful
		if (match.Success)
		{
			// Extract latitude and longitude from the matched groups
			double longitude = double.Parse(match.Groups[1].Value);
			double latitude = double.Parse(match.Groups[2].Value);

			// Return the coordinates as a Tuple
			return new Tuple<double, double>(latitude, longitude);
		}
		else
		{
			// Return null or throw an exception, depending on your needs
			throw new ArgumentException("Input does not match the expected format.");
		}
	}
}


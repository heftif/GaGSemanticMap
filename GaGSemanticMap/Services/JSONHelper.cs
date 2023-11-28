using Newtonsoft.Json;

namespace GaGSemanticMap.Services
{
	public static class JSONHelper
	{
		public static string ConvertToJson<T>(List<T> objects)
		{
			// Serialize the list of objects to JSON
			return JsonConvert.SerializeObject(objects, Formatting.Indented);
		}

	}
}

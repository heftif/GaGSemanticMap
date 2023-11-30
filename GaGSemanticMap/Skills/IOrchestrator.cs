namespace GaGSemanticMap.Skills
{
	public interface IOrchestrator
	{

		public Task<string> RouteRequestAsync(string input);
	}
}

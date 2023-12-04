namespace GaGSemanticMap.Skills
{
	public interface IOrchestrator
	{
		/// <summary>
		/// Route user request to a pipeline
		/// </summary>
		public Task<string> RouteRequestAsync(string input);
	}
}

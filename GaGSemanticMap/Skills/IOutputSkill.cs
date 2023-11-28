using Microsoft.SemanticKernel.Orchestration;

namespace GaGSemanticMap.Skills
{
	public interface IOutputSkill
	{
		/// <summary>
		/// Responds to the user
		/// </summary>
		public Task<string> RespondAsync(string message, SKContext context);
	}
}

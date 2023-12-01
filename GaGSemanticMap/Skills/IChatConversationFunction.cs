using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace GaGSemanticMap.Skills
{
	public interface IChatConversationFunction
	{
		/// <summary>
		/// Checks validity of the input
		/// </summary>
		public Task<string> TranslateInputAsync(string message, SKContext context);

		/// <summary>
		/// Evalute the response
		/// </summary>
		public Task<string> EvaluateResponseAsync(string message);

		/// <summary>
		/// AskForClarification
		/// </summary>
		public Task<string> AskForClarificationAsync();
	}
}

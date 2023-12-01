using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
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

		/// <summary>
		/// delivers more information about the desired topic
		/// </summary>
		public Task<string> GetMoreInformationAsync(string input);

		/// <summary>
		/// find the episode that should be added to the queue
		/// </summary>
		public Task<string> GetEpisodeForQueue(string input);

		/// <summary>
		/// add the bot message to the history
		/// </summary>
		public Task AddMessageToHistoryAsync(string message, AuthorRole role);
	}
}

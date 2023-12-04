using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;

namespace GaGSemanticMap.Skills
{
	public interface IChatConversationFunction
	{
		/// <summary>
		/// Translates input and condenses meaning
		/// </summary>
		public Task<string> TranslateInputAsync(string message, SKContext context);

		/// <summary>
		/// Evaluate the response
		/// </summary>
		public Task<string> EvaluateResponseAsync(string message);

		/// <summary>
		/// Ask for clarification if user input has not been fully understood
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
		/// add the message to the chat history
		/// </summary>
		public Task AddMessageToHistoryAsync(string message, AuthorRole role);
	}
}

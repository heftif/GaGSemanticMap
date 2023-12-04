using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;


namespace GaGSemanticMap.Skills
{
	public class ChatConversationFunction : IChatConversationFunction
	{
		private readonly OpenAIRequestSettings chatRequestSettings;
		private readonly IChatCompletion chatCompletion;
		private readonly IKernel kernel;
		private ChatHistory chatHistory;

		public ChatConversationFunction(IKernel kernel)
		{
			this.kernel = kernel;

			//for this service to be registered, the kernel must be registered with AzureOpenAI (with the corresponding endpoint and key)
			chatCompletion = kernel.GetService<IChatCompletion>();

		}

		[SKFunction, SKName(nameof(TranslateInputAsync))]
		public async Task<string> TranslateInputAsync(string input, SKContext context)
		{
			//to help with tokens and to not mix up various query, we reset the chat when asking for new episodes
			ResetChat();

			//add the user message to the full chat history
			chatHistory.AddUserMessage(input);

			var getTranslation = kernel.Functions.GetFunction("ChatPlugin", "GetTranslation");

			var getTranslationVariable = new ContextVariables
			{
				["input"] = input,
				["language"] = "german"
			};

			string translation = (await kernel.RunAsync(getTranslationVariable, getTranslation)).GetValue<string>()!.Trim();

			if (string.IsNullOrEmpty(translation.Trim()))
			{
				translation = input;
				Console.WriteLine($"Warning, translation didn't work, handing input down instead: {translation}");
			}

			Console.WriteLine($"Translation and Condensed: {translation}");
			//return the translation
			return translation;
		}

		[SKFunction, SKName(nameof(EvaluateResponseAsync))]
		public async Task<string> EvaluateResponseAsync(string input)
		{
			Console.WriteLine($"Formulate reply for answer");

			var getEpisodeAnswer = kernel.Functions.GetFunction("ChatPlugin", "GetEpisodeAnswer");

			string episodeAnswer = (await kernel.RunAsync(input, getEpisodeAnswer)).GetValue<string>()!.Trim();

			chatHistory.AddAssistantMessage(episodeAnswer);

			Console.WriteLine($"Got the following reply: {episodeAnswer}");

			//return the bot answer
			return episodeAnswer;
		}

		[SKFunction, SKName(nameof(AskForClarificationAsync))]
		public async Task<string> AskForClarificationAsync()
		{
			var getClarification = kernel.Functions.GetFunction("ChatPlugin", "GetClarification");

			string clarification = (await kernel.RunAsync(getClarification)).GetValue<string>()!.Trim();

			chatHistory.AddAssistantMessage(clarification);
			//return the clarification
			return clarification;
		}

		[SKFunction, SKName(nameof(GetMoreInformationAsync))]
		public async Task<string> GetMoreInformationAsync(string input)
		{
			var getMoreInformation = kernel.Functions.GetFunction("ChatPlugin", "GetMoreInformation");

			var getMoreInformationVariable = new ContextVariables
			{
				["input"] = input,
				["history"] = TransformChatHistory()
			};

			string moreInformation = (await kernel.RunAsync(getMoreInformationVariable, getMoreInformation)).GetValue<string>()!.Trim();

			chatHistory.AddUserMessage(input);
			Console.WriteLine("Retrieved more information");
			chatHistory.AddAssistantMessage(moreInformation);

			//return the clarification
			return moreInformation;
		}

		[SKFunction, SKName(nameof(GetEpisodeForQueue))]
		public async Task<string> GetEpisodeForQueue(string input)
		{
			Console.WriteLine("Getting Episode from Context");
			var getEpisode = kernel.Functions.GetFunction("ChatPlugin", "GetEpisode");

			var getEpisodeVariable = new ContextVariables
			{
				["input"] = input,
				["history"] = TransformChatHistory()
			};

			string episodeName = (await kernel.RunAsync(getEpisodeVariable, getEpisode)).GetValue<string>()!.Trim();

			chatHistory.AddUserMessage(input);

			Console.WriteLine($"Episode Name: {episodeName}");
			//return the episode name
			return episodeName;
		}

		public async Task AddMessageToHistoryAsync(string message, AuthorRole role)
		{
			if (chatHistory == null)
				StartChat();

			if(role == AuthorRole.Assistant)
			{
				chatHistory.AddAssistantMessage(message);
			}
			else if(role == AuthorRole.User)
			{
				chatHistory.AddUserMessage(message);
			}

			Console.WriteLine($"Added message to history: {message}");
			
		}

		private string TransformChatHistory()
		{
			string history = "";
			
			if (chatHistory != null && chatHistory.Count > 0)
			{
				foreach (var chat in chatHistory)
				{
					if (chat.Role == AuthorRole.User)
					{
						history += "User: " + chat.Content;
					}
					else if (chat.Role == AuthorRole.Assistant)
					{
						history += "Bot: " + chat.Content;
					}

					history += "\n";
				}
			}
			else
			{
				StartChat();
			}

			return history;
		}

		private void ResetChat()
		{
			if (chatHistory != null && chatHistory.Any())
				chatHistory.Clear();

			StartChat();
		}

		private void StartChat()
		{
			chatHistory = chatCompletion.CreateNewChat();
		}
	}
}

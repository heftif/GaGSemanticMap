using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Diagnostics;


namespace GaGSemanticMap.Skills
{
	public class CheckInputFunction : ICheckInputFunction
	{
		private readonly OpenAIRequestSettings chatRequestSettings;
		private readonly IChatCompletion chatCompletion;
		private ChatHistory chatHistory;

		public CheckInputFunction(IKernel kernel)
		{
			chatRequestSettings = new()
			{
				MaxTokens = 600,
				Temperature = 0.4f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				TopP = 0.0f
			};

			//for this service to be registered, the kernel must be registered with AzureOpenAI (with the corresponding endpoint and key)
			chatCompletion = kernel.GetService<IChatCompletion>();

		}

		[SKFunction, SKName(nameof(ValidateInputAsync))]
		public async Task<string> ValidateInputAsync(string input, SKContext context)
		{
			SetupChat();

			Console.WriteLine($"translating {input}");

			//localchat for this query only
			var prompt = "Take the query or command and translate it to german, without adding anything to it.";

			var localChatHistory = chatCompletion.CreateNewChat(prompt);

			string reply = string.Empty;
			try
			{

				//add the user input to the chat history
				localChatHistory.AddUserMessage(input);

				IReadOnlyList<IChatResult> completion = null;

				try
				{
					//hand the history to the chatmodel to get the response
					completion = await chatCompletion.GetChatCompletionsAsync(localChatHistory, chatRequestSettings);
				}
				catch (Exception ex)
				{
					Console.Write(ex.Message);
				}

				if (!completion.Any())
					throw new SKException("No completion results returned from OpenAI.");

				foreach (IChatResult result in completion)
				{
					// Add the german question to the chat history as first question.
					ChatMessage message = await result.GetChatMessageAsync();
					chatHistory.AddUserMessage(message.Content);

					reply += Environment.NewLine + message.Content;
				}
			}
			catch (SKException aiex)
			{
				// Reply with the error message if there is one
				reply = $"OpenAI returned an error ({aiex.Message}). Please try again.";
			}

			Console.WriteLine($"Got Reply {reply}");

			if (string.IsNullOrEmpty(reply.Trim()))
			{
				reply = input;
				Console.WriteLine($"Warning, translation didn't work, handing input down instead: {reply}");
			}
				

			return reply;
		}

		[SKFunction, SKName(nameof(EvaluateResponseAsync))]
		public async Task<string> EvaluateResponseAsync(string input)
		{
			Console.WriteLine($"Formulate reply for answer");

			string reply = "";

			try
			{
				chatHistory.AddAssistantMessage($"Here are the top 5 podcast episodes relating to the question you asked {input}");

				IReadOnlyList<IChatResult> completion = null;

				try
				{
					//hand the history to the chatmodel to get the response
					completion = await chatCompletion.GetChatCompletionsAsync(chatHistory, chatRequestSettings);
				}
				catch (SKException ex)
				{
					Console.Write(ex.Message);
				}

				if (!completion.Any())
					throw new SKException("No completion results returned from OpenAI.");

				foreach (IChatResult result in completion)
				{
					// Add the completion result as an assistant message to the chat history.
					ChatMessage message = await result.GetChatMessageAsync();
					chatHistory.AddAssistantMessage(message.Content);

					reply += Environment.NewLine + message.Content;
				}


			}
			catch(SKException aiex)
			{
				// Reply with the error message if there is one
				reply = $"OpenAI returned an error ({aiex.Message}). Please try again.";
			}

			Console.WriteLine($"Got the following reply: {reply}");


			return reply;
		}


		private void SetupChat()
		{
			if (chatHistory != null && chatHistory.Any())
				chatHistory.Clear();

			//overall chat
			var prompt = "Check the answers and give back a numbered bullet point list with the 5 top results" +
			"The structure of the bullet points should be the following: episode name (including the number of the podcast i.e. GAG303 etc.)" +
			"followed by a short summary and a link to the episode with an emoji. The episode number and name should be in german, the short summary in english. " +
			"make a linebreak between episode name, description and link" +
			"Also be nice about it and sound a fun guy.";

			chatHistory = chatCompletion.CreateNewChat(prompt);
		}

	}


}

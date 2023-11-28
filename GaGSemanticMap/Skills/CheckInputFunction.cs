using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Diagnostics;
using System.Collections.Generic;
using Sprache;

namespace GaGSemanticMap.Skills
{
	public class CheckInputFunction : ICheckInputFunction
	{
		private readonly OpenAIRequestSettings chatRequestSettings;
		private readonly IChatCompletion chatCompletion;
		private readonly ChatHistory chatHistory;

		public CheckInputFunction(IKernel kernel)
		{
			chatRequestSettings = new()
			{
				MaxTokens = 500,
				Temperature = 0.2f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				TopP = 0.0f
			};

			//for this service to be registered, the kernel must be registered with AzureOpenAI (with the corresponding endpoint and key)
			chatCompletion = kernel.GetService<IChatCompletion>();

			var prompt = "You take the question of the user and reformulate it in such a way that is optimal to find related semantic embeddings in history podcasts episodes. The response " +
				"has to be between 10 and 50 words. You speak german and give the answer in german.";

			chatHistory = chatCompletion.CreateNewChat(prompt);

		}

		[SKFunction, SKName(nameof(ValidateInputAsync))]
		public async Task<string> ValidateInputAsync(string input, SKContext context)
		{
			string reply = string.Empty;
			try
			{
				//add the user input to the chat history
				chatHistory.AddUserMessage(input);

				IReadOnlyList<IChatResult> completion = null;

				try
				{
					//hand the history to the chatmodel to get the response
					completion = await chatCompletion.GetChatCompletionsAsync(chatHistory, chatRequestSettings);
				}
				catch (Exception ex)
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
			catch (SKException aiex)
			{
				// Reply with the error message if there is one
				reply = $"OpenAI returned an error ({aiex.Message}). Please try again.";
			}

			return reply;
		}

		[SKFunction, SKName(nameof(EvaluateResponseAsync))]
		public async Task<string> EvaluateResponseAsync(string input)
		{
			string reply = "";

			try
			{
				chatHistory.AddAssistantMessage($"Here are the top 10 podcast episodes relating to the question you asked {input}");
				chatHistory.AddSystemMessage("Check the answers and give back a list containing at minimum 5 of these 10 episodes as episode title" +
												"in bullet point form. Also be nice about it and sound natural.");


				IReadOnlyList<IChatResult> completion = null;

				try
				{
					//hand the history to the chatmodel to get the response
					completion = await chatCompletion.GetChatCompletionsAsync(chatHistory, chatRequestSettings);
				}
				catch (Exception ex)
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

			return reply;
		}
	}


}

﻿using Microsoft.SemanticKernel.Orchestration;
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
				Temperature = 0.0f,
				FrequencyPenalty = 0.0f,
				PresencePenalty = 0.0f,
				TopP = 0.0f
			};

			//for this service to be registered, the kernel must be registered with AzureOpenAI (with the corresponding endpoint and key)
			chatCompletion = kernel.GetService<IChatCompletion>();

			var prompt = "You are an expert on the podcast 'Geschichten aus der Geschichte', which is a history podcast, telling short stories about events that happened in history. " +
				"		You check, if the user is asking a question related to history or a place existing on earth. If you are not sure, ask the user to be more precise. " +
				"If the question is not related to history, tell the user that you are an expert for this history podcast only and you can't answer the question. You answer in english " +
				"and are kind and professional.";

			chatHistory = chatCompletion.CreateNewChat(prompt);

		}

		[SKFunction, SKName(nameof(ValidateInputAsync))]
		public async Task<string> ValidateInputAsync(string input, SKContext context)
		{

			string reply = string.Empty;
			try
			{
				// Add the question as a user message to the chat history, then send everything to OpenAI.
				// The chat history is used as context for the prompt
				chatHistory.AddUserMessage(input);

				IReadOnlyList<IChatResult> completion = null;

				try
				{
					completion = await chatCompletion.GetChatCompletionsAsync(chatHistory, chatRequestSettings);
				}
				catch(Exception ex)
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

	}
}
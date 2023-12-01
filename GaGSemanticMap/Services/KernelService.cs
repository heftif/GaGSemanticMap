using GaGSemanticMap.Models;
using GaGSemanticMap.Skills;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using System.Text.RegularExpressions;
using System.Threading;


namespace GaGSemanticMap.Services
{
	public class KernelService : IKernelService
	{
		private readonly IKernel? kernel;
		private readonly ISemanticSearchService semanticSearchService;
		private readonly IChatConversationFunction chatService;
		private readonly IDictionary<string, ISKFunction> chatFunctions;
		private readonly IDictionary<string, ISKFunction> semanticSearchFunctions;
		private readonly IDictionary<string, ISKFunction> orchestrationFunctions;

		public KernelService(IKernel kernel, IChatConversationFunction chatFunction, ISemanticSearchService semanticSearchService)
		{
			this.kernel = kernel;
			this.semanticSearchService = semanticSearchService;
			this.chatService = chatFunction;

			if(kernel != null)
			{
				chatFunctions = kernel.ImportFunctions(chatFunction, "ChatPlugin");
				semanticSearchFunctions = kernel.ImportFunctions(semanticSearchService);
				orchestrationFunctions = kernel.ImportFunctions(new Orchestrator(kernel), "OrchestratorPlugin");
			}
			else
			{
				throw new Exception("Could not implement kernel!");
			}
			
		}

		public async Task<string> FindEpisodes(string input)
		{
			//get intent
			var intent = await kernel.RunAsync(input, orchestrationFunctions[nameof(IOrchestrator.RouteRequestAsync)]);

			//now we could use a planner, which decides for the best pipeline itself
			//this could be an upgrade for the next version
			var action = intent.GetValue<string>();

			Console.WriteLine($"Performing the following action: {action}");

			switch (action)
			{
				//search corresponding episodes
				case "search":
					return await SearchEpisodes(input);
				//add epsiode to listen list
				case "add":
					return await AddEpisodeToQueue(input);
				//respond with more in depth knowledge about the input
				case "deepen":
					return await GetMoreInformation(input);
				//ask for clarification
				case "?":
					return await AskForClarification();
				default:
					return "no intent found";
			}
			
		}

		private async Task<string> SearchEpisodes(string input)
		{
			//create pipeLine
			ISKFunction[] pipeline = {
							chatFunctions[nameof(IChatConversationFunction.TranslateInputAsync)],
							semanticSearchFunctions[nameof(ISemanticSearchService.GetEventsBySemanticRelevanceAsync)],
							chatFunctions[nameof(IChatConversationFunction.EvaluateResponseAsync)]
					};

			var result = await kernel.RunAsync(input, pipeline);
			//reformulate the question and translate to german
			var botResponse = result.GetValue<string>();
			//ensure formatting
			return EnsureFormatting(botResponse);
		}

		private async Task<string> AddEpisodeToQueue(string input)
		{
			//I need to find out which episode(s) out of the context and then just add a simple function
			var result = await kernel.RunAsync(input, chatFunctions[nameof(IChatConversationFunction.GetEpisodeForQueue)]);

			var episodeName = result.GetValue<string>().Trim();

			if(!string.IsNullOrEmpty(episodeName))
			{
				//find the corresponding episode from the eventpoints
				var eventPoint = await semanticSearchService.GetEventPoint(episodeName);

				if(eventPoint != null)
				{
					await chatService.AddMessageToHistoryAsync("Added the episode to the queue!", AuthorRole.Assistant);

					return $"AddToQueue, {eventPoint.EpisodeName}, {eventPoint.EpsiodeLink}";
				}
			}
			
			//apologize and ask for clarification
			return await AskForClarification();
		}

		private async Task<string> GetMoreInformation(string input)
		{
			//this maybe requires history, because the user could say: give me information about the first choice etc. 
			var result = await kernel.RunAsync(input, chatFunctions[nameof(IChatConversationFunction.GetMoreInformationAsync)]);

			return result.GetValue<string>();
		}

		private async Task<string> AskForClarification()
		{
			var result = await kernel.RunAsync(chatFunctions[nameof(IChatConversationFunction.AskForClarificationAsync)]);

			return result.GetValue<string>();
		}

		private string EnsureFormatting(string botResponse)
		{
			//make sure there is a new line before the link
			return Regex.Replace(botResponse, @"(?<!\n)\[", "\n[");
		}
	}
}

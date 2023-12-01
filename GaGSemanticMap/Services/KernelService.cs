using GaGSemanticMap.Skills;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;
using System.Threading;


namespace GaGSemanticMap.Services
{
	public class KernelService : IKernelService
	{
		private readonly IKernel? kernel;
		private readonly IDictionary<string, ISKFunction> chatFunctions;
		private readonly IDictionary<string, ISKFunction> semanticSearchFunctions;
		private readonly IDictionary<string, ISKFunction> orchestrationFunctions;

		public KernelService(IKernel kernel, IChatConversationFunction chatFunction, ISemanticSearchService semanticSearchService)
		{
			this.kernel = kernel;

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
			return "";
		}

		private async Task<string> GetMoreInformation(string input)
		{
			return "";
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

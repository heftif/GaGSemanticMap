using GaGSemanticMap.Skills;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
//using Microsoft.SemanticKernel.SkillDefinition;
//using Microsoft.SemanticKernel.Skills.Core;

namespace GaGSemanticMap.Services
{
	public class KernelService : IKernelService
	{
		private readonly IKernel? kernel;
		private readonly IDictionary<string, ISKFunction> checkInputFunctions;
		private readonly IDictionary<string, ISKFunction> semanticSearchFunctions;
		private readonly IDictionary<string, ISKFunction> orchestrationFunctions;

		public KernelService(IKernel kernel, IOutputSkill outputSkill, ICheckInputFunction checkInputFunction, ISemanticSearchService semanticSearchService)
		{
			this.kernel = kernel;

			if(kernel != null)
			{
				checkInputFunctions = kernel.ImportFunctions(checkInputFunction);
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

			//create pipeLine
			ISKFunction[] pipeline = {
				checkInputFunctions[nameof(ICheckInputFunction.TranslateInputAsync)],
				semanticSearchFunctions[nameof(ISemanticSearchService.GetEventsBySemanticRelevanceAsync)],
				checkInputFunctions[nameof(ICheckInputFunction.EvaluateResponseAsync)]
			};

			var result = await kernel.RunAsync(input, pipeline);
			
			//reformulate the question and translate to german
			var botResponse = result.GetValue<string>();

			//ensure formatting
			return EnsureFormatting(botResponse);
		}

		private string EnsureFormatting(string botResponse)
		{
			//make sure there is a new line before the link
			return Regex.Replace(botResponse, @"(?<!\n)\[", "\n[");
		}
	}
}

using GaGSemanticMap.Skills;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using System.Runtime.CompilerServices;
using System.Threading;
//using Microsoft.SemanticKernel.SkillDefinition;
//using Microsoft.SemanticKernel.Skills.Core;

namespace GaGSemanticMap.Services
{
	public class KernelService : IKernelService
	{
		private readonly IKernel? kernel;
		private readonly IDictionary<string, ISKFunction> outputSkillFunctions;
		private readonly IDictionary<string, ISKFunction> checkInputFunctions;
		private readonly IDictionary<string, ISKFunction> semanticSearchFunctions;

		public KernelService(IKernel kernel, IOutputSkill outputSkill, ICheckInputFunction checkInputFunction, ISemanticSearchService semanticSearchService)
		{
			this.kernel = kernel;

			if(kernel != null)
			{
				outputSkillFunctions = kernel.ImportFunctions(outputSkill);
				checkInputFunctions = kernel.ImportFunctions(checkInputFunction);
				semanticSearchFunctions = kernel.ImportFunctions(semanticSearchService);
			}
			else
			{
				throw new Exception("Could not implement kernel!");
			}
			
		}

		public async Task FindEpisodes(string input)
		{
			//reformulate the question and translate to german
			var result = await kernel.RunAsync(
						input,
						checkInputFunctions[nameof(ICheckInputFunction.ValidateInputAsync)]
				);

			var botResponse = result.GetValue<string>();

			ISKFunction[] pipeline = {
				semanticSearchFunctions[nameof(ISemanticSearchService.GetEventsBySemanticRelevanceAsync)],

			};

			await kernel.RunAsync(botResponse, pipeline);
		}
	}
}

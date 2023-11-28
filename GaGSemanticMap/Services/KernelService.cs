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

		public KernelService(IKernel kernel, IOutputSkill outputSkill, ICheckInputFunction checkInputFunction)
		{
			this.kernel = kernel;

			if(kernel != null)
			{
				outputSkillFunctions = kernel.ImportFunctions(outputSkill);
				checkInputFunctions = kernel.ImportFunctions(checkInputFunction);
			}
			else
			{
				throw new Exception("Could not implement kernel!");
			}
			
		}

		public async Task FindEpisodes(string input)
		{
			await kernel.RunAsync(
					input,
					checkInputFunctions[nameof(ICheckInputFunction.ValidateInputAsync)]
			);
		}
	}
}

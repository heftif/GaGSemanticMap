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

		public KernelService(IOutputSkill outputSkill)
		{
			kernel = new KernelBuilder().Build();

			if(kernel != null)
			{
				outputSkillFunctions = kernel.ImportFunctions(outputSkill);
			}
			else
			{
				throw new Exception("Could not implement kernel!");
			}
			

		}

		public async Task StartAsync(string input)
		{
			await kernel.RunAsync(
					"Beep, boop, I'm .DotNetBot and I'm here to help. If you're done say goodbye.",
					outputSkillFunctions[nameof(IOutputSkill.RespondAsync)]
			);
		}
	}
}

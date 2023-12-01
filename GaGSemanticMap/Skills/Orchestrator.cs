//see also examples at https://github.com/microsoft/semantic-kernel
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace GaGSemanticMap.Skills
{
	public class Orchestrator
	{
		private readonly IKernel kernel;

		public Orchestrator(IKernel kernel) 
		{
			this.kernel = kernel;
		}

		[SKFunction, SKName(nameof(RouteRequestAsync))]
		public async Task<string> RouteRequestAsync(string input)
		{
			
			var getIntent = kernel.Functions.GetFunction("OrchestratorPlugin", "GetIntent");
			var getIntentVariables = new ContextVariables
			{
				["input"] = input,
				["options"] = "search, add, deepen, ?" //option from which to choose from
			};

			string intent = (await kernel.RunAsync(getIntentVariables, getIntent)).GetValue<string>()!.Trim();

			//return the intent
			return intent;
		}
	}
}

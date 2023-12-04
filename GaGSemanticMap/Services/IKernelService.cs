namespace GaGSemanticMap.Services
{
	public interface IKernelService
	{
		/// <summary>
		/// Process the user input and do the desired action
		/// </summary>
		Task<string> ProcessInput(string input);
	}
}

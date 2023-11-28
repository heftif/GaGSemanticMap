namespace GaGSemanticMap.Services
{
	public interface IKernelService
	{
		Task<string> FindEpisodes(string input);
	}
}

using Vector = Pgvector.Vector;

namespace GaGSemanticMap.Services;

public interface ISemanticSearchService
{
    Task<string> GetOpenAIResponseAsync(string userInput);
	Task GetEventsBySemanticRelevanceAsync(string userInput);
}

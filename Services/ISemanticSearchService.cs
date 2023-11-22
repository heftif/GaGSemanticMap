namespace GaGSemanticMap.Services;

public interface ISemanticSearchService
{
    Task<string> GetOpenAIResponse(string userInput);
}

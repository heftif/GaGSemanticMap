using GaGSemanticMap.Models;
using Vector = Pgvector.Vector;

namespace GaGSemanticMap.Services;

public interface ISemanticSearchService
{
	Task<string> GetEventsBySemanticRelevanceAsync(string botInput);
}

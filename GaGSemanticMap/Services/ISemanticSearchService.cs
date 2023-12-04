using GaGSemanticMap.Models;
using Vector = Pgvector.Vector;

namespace GaGSemanticMap.Services;

public interface ISemanticSearchService
{
	/// <summary>
	/// Get the most semantic relevant events from the events given an input
	/// </summary>
	Task<string> GetEventsBySemanticRelevanceAsync(string botInput);

	/// <summary>
	/// Get corresponding event point from epsiodename input
	/// </summary>
	Task<EventPoint> GetEventPointAsync(string epsiodeName);
}

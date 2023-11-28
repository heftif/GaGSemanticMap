namespace GaGSemanticMap.Models
{
	public class EventPointWithDistance
	{
		public EventPoint EventPoint { get; set; }
		public double Distance { get; set; }
		public double NormDistance { get; set; }
		public int Rank { get; set; }
	}
}

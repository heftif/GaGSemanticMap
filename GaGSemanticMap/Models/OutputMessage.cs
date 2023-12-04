namespace GaGSemanticMap.Models
{
	public class OutputMessage
	{
		public string From { get; set; }
		public string Text { get; set; }
		public bool IsBulletPoint { get; set; }
		public string BulletPointString { get; set; }
		public string Description { get; set; }
		public string Link { get; set; }

		public OutputMessage(string from, string text, bool isBulletPoint = false)
		{
			From = from;
			Text = text;
			IsBulletPoint = isBulletPoint;
			BulletPointString = string.Empty;
			Description = string.Empty;
			Link = string.Empty;
		}

		public OutputMessage( string bulletPointString, string description, string link, bool isBulletPoint = true)
		{
			From = string.Empty; 
			Text = string.Empty; 
			IsBulletPoint = isBulletPoint;
			BulletPointString = bulletPointString;
			Description = description;
			Link = link;
		}
	}
}

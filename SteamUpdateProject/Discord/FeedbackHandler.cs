using System.IO;

namespace SteamUpdateProject.Discord
{
	/// <summary>
	/// Handles writing feedback to the bot.
	/// </summary>
	internal static class FeedbackHandler
	{
		public static string FeedbackDir = Directory.GetCurrentDirectory() + "\\feedback\\";

		public static void AddFeedback(string feedback, string discordID)
		{
			if (!Directory.Exists(FeedbackDir))
			{
				Directory.CreateDirectory(FeedbackDir);
			}

			using (StreamWriter fw = new(FeedbackDir + $"{discordID.Replace(":", "").Replace(" ", "")}.txt"))
			{
				fw.WriteLine(feedback);
			}
		}
	}
}

using System.IO;

namespace SteamUpdateProject.Discord
{
	/// <summary>
	/// Handles writing feedback to the bot.
	/// This is just so I can browse feedback.
	/// </summary>
	internal static class FeedbackHandler
	{
		public static string FeedbackDir = Directory.GetCurrentDirectory() + "\\feedback\\";

		public static void AddFeedback(string feedback, string discordID)
		{
			if (!string.IsNullOrWhiteSpace(feedback))
				return;

			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			feedback = System.Text.RegularExpressions.Regex.Replace(feedback, invalidRegStr, "_");

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

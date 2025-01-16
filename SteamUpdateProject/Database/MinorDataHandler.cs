using System.IO;
using System.Text;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json;
using SharpCompress.Writers;
using SteamKit2;
using static SteamUpdateProject.Discord.ConfigHandler;

namespace SteamUpdateProject
{
	/// <summary>
	/// This handles small data like <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> so we can keep track of those small things.
	/// </summary>
	internal class MinorDataHandler
	{
		private const string fileName = "SteamData.json";

		private readonly string _operatingFile = Directory.GetCurrentDirectory() + "//" + fileName;

		public StatJson BotStats;

		/// <summary>
		/// Writes <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> into <see cref="SteamData.data"/>
		/// </summary>
		public void WriteData()
		{
			BotStats = new StatJson(LoggingAndErrorHandler.Updates, LoggingAndErrorHandler.ContentUpdates, LoggingAndErrorHandler.Exceptions, LoggingAndErrorHandler.MinutesRunning);

			string seralizedObject = JsonConvert.SerializeObject(BotStats, Formatting.Indented);

			using StreamWriter config = new StreamWriter(_operatingFile);
			config.Write(seralizedObject);
		}

		/// <summary>
		/// Read contents of SteamData.data and updates <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/>
		/// </summary>
		public void ReadData()
		{
			if(!File.Exists(_operatingFile))
			{
				File.Create(_operatingFile);
				return;
			}

			string rawConfigJson = "";

			using (FileStream fs = File.OpenRead(_operatingFile))

			using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
				rawConfigJson = sr.ReadToEnd();

			BotStats = JsonConvert.DeserializeObject<StatJson>(rawConfigJson);

			LoggingAndErrorHandler.Updates = BotStats.Updates;
			LoggingAndErrorHandler.Exceptions = BotStats.Exceptions;
			LoggingAndErrorHandler.ContentUpdates = BotStats.ContentUpdates;
			LoggingAndErrorHandler.MinutesRunning = BotStats.MinutesRunning;
		}

		public struct StatJson
		{
			public StatJson(long updates, long contentUpdates, long exceptions, long minutesRunning)
			{
				Updates = updates;
				ContentUpdates = contentUpdates;
				Exceptions = exceptions;
				MinutesRunning = minutesRunning;
			}

			[JsonProperty("Updates")]
			public long Updates { get; private set; }

			[JsonProperty("ContentUpdates")]
			public long ContentUpdates { get; private set; }

			[JsonProperty("Exceptions")]
			public long Exceptions { get; private set; }

			[JsonProperty("MinutesRunning")]
			public long MinutesRunning { get; private set; }
		}
	}
}

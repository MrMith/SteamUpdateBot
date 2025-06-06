using System.Collections.Generic;
using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json;
using SharpCompress.Writers;
using SteamKit2;
using SteamUpdateProject.Entities;

namespace SteamUpdateProject.Database
{
	/// <summary>
	/// This handles small data like <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> so we can keep track of those small things.
	/// </summary>
	internal class MinorDataHandler
	{

#if DEBUG
		private const string FileName = "DebugSteamData.json";
#else
		private const string FileName = "SteamData.json";
#endif

		private readonly string _operatingFile = Directory.GetCurrentDirectory() + "//" + FileName;

		public StatJson BotStats;

		/// <summary>
		/// Writes <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> into <see cref="SteamData.data"/>
		/// </summary>
		public void WriteData()
		{
			IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			FilterDefinition<AppInfo> emptyFilter = Builders<AppInfo>.Filter.Empty;
			FilterDefinition<AppInfo> contentFilter = Builders<AppInfo>.Filter.Ne("DepoName", (BsonNull) null);

			IMongoCollection<AppInfo> aI_Collection = db.GetCollection<AppInfo>(AppInfo.DBName);

			BotStats = new StatJson(aI_Collection.CountDocuments(emptyFilter), aI_Collection.CountDocuments(contentFilter), LoggingAndErrorHandler.Exceptions, LoggingAndErrorHandler.MinutesRunning);

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

		public struct StatJson(long updates, long contentUpdates, long exceptions, long minutesRunning)
		{
			[JsonProperty(nameof(Updates))]
			public long Updates { get; private set; } = updates;

			[JsonProperty(nameof(ContentUpdates))]
			public long ContentUpdates { get; private set; } = contentUpdates;

			[JsonProperty(nameof(Exceptions))]
			public long Exceptions { get; private set; } = exceptions;

			[JsonProperty(nameof(MinutesRunning))]
			public long MinutesRunning { get; private set; } = minutesRunning;
		}
	}
}

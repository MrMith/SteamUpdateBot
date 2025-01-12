using Newtonsoft.Json;
using SteamUpdateProject.Steam;
using System;
using System.IO;
using System.Text;

namespace SteamUpdateProject.Discord
{
	/// <summary>
	/// This is used for verifying the config and getting the data related to the <see cref="DiscordBot"/> token and <see cref="SteamBot"/> login information.
	/// <br></br>
	/// Config contains everything needed to run the bot.
	/// </summary>
	public class ConfigHandler
	{
		public ConfigJson Config;
		
		public ConfigHandler()
		{
			if (!File.Exists("config.json"))
			{
				Console.WriteLine("No config.json found, input your config options here to be saved:\n");
				Console.WriteLine("Input Bot Token: ");
				var BotToken = Console.ReadLine();

				Console.WriteLine("Input Bot Prefix: ");
				var BotPrefix = Console.ReadLine();

				Console.WriteLine("Input Steam Username: ");
				var SteamName = Console.ReadLine();

				Console.WriteLine("Input Steam Password: ");
				var SteamPW = Console.ReadLine();

				Console.WriteLine("Input MongoDB connection string: ");
				var ConnectionString = Console.ReadLine();

				Console.WriteLine("Input Discord Dev Override ID (Optional): ");
				var optionalDevOverride = Console.ReadLine();
				var DiscordID = string.IsNullOrEmpty(optionalDevOverride) ? "0" : optionalDevOverride;

				Config = new ConfigJson(BotToken, BotPrefix, SteamName, SteamPW, ConnectionString, DiscordID);

				string seralizedObject = JsonConvert.SerializeObject(Config, Formatting.Indented);

				using StreamWriter config = new StreamWriter("config.json");
				config.Write(seralizedObject);
				return;
			}

			string rawConfigJson = "";

			using (FileStream fs = File.OpenRead("config.json"))

			using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
				rawConfigJson = sr.ReadToEnd();

			Config = JsonConvert.DeserializeObject<ConfigJson>(rawConfigJson);
		}

		#region Json Struct
		public struct ConfigJson
		{
			public ConfigJson(string token, string prefix, string steamName, string steamPW, string connectionString, string devOverride)
			{
				Token = token;
				CommandPrefix = prefix;
				SteamName = steamName;
				SteamPassword = steamPW;
				DBConnectionString = connectionString;
				DevOverride = devOverride;
			}

			[JsonProperty("token")]
			public string Token { get; private set; }

			[JsonProperty("prefix")]
			public string CommandPrefix { get; private set; }

			[JsonProperty("steamname")]
			public string SteamName { get; private set; }

			[JsonProperty("steampw")]
			public string SteamPassword { get; private set; }

			[JsonProperty("override")]
			public string DevOverride { get; set; }

			[JsonProperty("DBConnectionString")]
			public string DBConnectionString { get; set; }
		}
		#endregion
	}
}

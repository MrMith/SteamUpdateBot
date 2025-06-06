using Newtonsoft.Json;
using SteamUpdateProject.Discord;
using SteamUpdateProject.Steam;
using System;
using System.IO;
using System.Text;

namespace SteamUpdateProject
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
				string botToken = Console.ReadLine();

				Console.WriteLine("Input Debug Bot Token (Optional): ");
				string optionalBotToken= Console.ReadLine();

				Console.WriteLine("Input Bot Prefix: ");
				string botPrefix = Console.ReadLine();

				Console.WriteLine("Input Steam Username: ");
				string steamName = Console.ReadLine();

				Console.WriteLine("Input Steam Password: ");
				string steamPW = Console.ReadLine();

				Console.WriteLine("Input MongoDB connection string: ");
				string connectionString = Console.ReadLine();

				Console.WriteLine("Input Discord Dev Override ID (Optional): ");
				string optionalDevOverride = Console.ReadLine();
				string discordID = string.IsNullOrEmpty(optionalDevOverride) ? "0" : optionalDevOverride;

				Config = new ConfigJson(token: botToken, prefix: botPrefix, steamName: steamName, steamPW: steamPW, connectionString: connectionString, devOverride: discordID, debugToken: optionalBotToken);

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
		public struct ConfigJson(string token, string prefix, string steamName, string steamPW, string connectionString, string devOverride, string debugToken)
		{
			[JsonProperty("token")]
			public string Token { get; private set; } = token;

			[JsonProperty("debugtoken")]
			public string DebugToken { get; private set; } = debugToken;

			[JsonProperty("prefix")]
			public string CommandPrefix { get; private set; } = prefix;

			[JsonProperty("steamname")]
			public string SteamName { get; private set; } = steamName;

			[JsonProperty("steampw")]
			public string SteamPassword { get; private set; } = steamPW;

			[JsonProperty("override")]
			public string DevOverride { get; set; } = devOverride;

			[JsonProperty("DBConnectionString")]
			public string DBConnectionString { get; set; } = connectionString;
		}
		#endregion
	}
}

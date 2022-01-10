using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamUpdateProject.Discord;
using SteamUpdateProject.Steam;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft;

namespace SteamUpdateProject.Discord
{
	/// <summary>
	/// This is used for verifying the config and getting the data related to the <see cref="DiscordBot"/> token and <see cref="SteamBot"/> login information.
	/// <br></br>
	/// Config contains Bot Token, Bot Prefix and Override discord ID. 
	/// </summary>
	public class ConfigHandler
	{
		public string BotToken = "";
		public string BotPrefix = "";
		public string DiscordID = "";
		public string SteamPW = "";
		public string SteamName = "";

		public ConfigHandler()
		{
			if(!File.Exists("config.json"))
			{
				Console.WriteLine("No config.json found, input your config options here to be saved:\n");
				Console.WriteLine("Input Bot Token: ");
				var token = Console.ReadLine();
				BotToken = token;

				Console.WriteLine("Input Bot Prefix: ");
				var prefix = Console.ReadLine();
				BotPrefix = prefix;

				Console.WriteLine("Input Steam Username: ");
				var steamName = Console.ReadLine();
				SteamName = steamName;

				Console.WriteLine("Input Steam Password: ");
				var steamPW = Console.ReadLine();
				SteamPW = steamPW;

				Console.WriteLine("Input Discord Dev Override ID (Optional): ");
				var discordID = Console.ReadLine();
				discordID = discordID == null ? "0" : discordID;
				DiscordID = discordID;

				ConfigJson json = new ConfigJson(token, prefix, steamName, steamPW, discordID);

				var seralizedObject = JsonConvert.SerializeObject(json, Formatting.Indented);

				using StreamWriter config = new StreamWriter("config.json");
				config.Write(seralizedObject);
				return;
			}

			var RawConfigJson = "";

			using (var fs = File.OpenRead("config.json"))

			using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
				RawConfigJson = sr.ReadToEnd();

			ConfigJson cfgjson = JsonConvert.DeserializeObject<ConfigJson>(RawConfigJson);

			if (cfgjson.DevOverride == null)
				cfgjson.DevOverride = "0";

			BotToken = cfgjson.Token;
			BotPrefix = cfgjson.CommandPrefix;
			DiscordID = cfgjson.DevOverride;
			SteamName = cfgjson.SteamName;
			SteamPW = cfgjson.SteamPassword;
		}

		public struct ConfigJson
		{
			public ConfigJson(string _token, string _prefix, string _steamName, string _steamPW, string _devOverride)
			{
				Token = _token;
				CommandPrefix = _prefix;
				SteamName = _steamName;
				SteamPassword = _steamPW;
				DevOverride = _devOverride;
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
		}
	}
}

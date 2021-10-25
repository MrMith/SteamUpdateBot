using System;
using SteamUpdateProject.DiscordLogic;
using SteamUpdateProject.Steam;
using System.IO;
using System.Runtime.ExceptionServices;

namespace SteamUpdateProject
{
	/*
	 * To-Do
	 * 1. Add json based config system so I don't have to fuck about with args.
	 * 2. Queue system for Steam ratelimiting
	 * 3. Queue system for Discord ratelimiting (Might be included in DSharpPlus?)
	 * 4. Select comp seems pog
	 */

	/// <summary>
	/// Main logic that handles the steam bot, discord bot and managing the database.
	/// </summary>
	class SteamUpdateBot
	{
		public static DiscordBot DiscordClient;
		public static SteamBot SteamClient;
		public static SMOHandler SMOHandler;
		public static MinorDataHandler MinorDataHandler;
		public static LoggingAndErrorHandler LAEH;

		private static SQLDataBase _dataBase;
		public static bool FirstStartUp = true;

		public static string LogPath = Directory.GetCurrentDirectory() + "\\logs\\";
		public static string ConnectionString = $"Integrated Security=true;";
		public static string DatabaseDirectory = $"{Directory.GetCurrentDirectory()}\\database";
		public static ulong OverrideDiscordID = 0;

		/// <summary>
		/// arguments are the following based on index:
		/// 0 = Steam account username
		/// 1 = Steam account password
		/// 2 = Discord bot token
		/// 3 = Override discord user ID (Not required)
		/// </summary>
		/// <param name="args"></param>
		public static void Main(string[] args)
		{
			#region Database start
			SMOHandler = new SMOHandler();

			ConnectionString += $"Database={SMOHandler.SMODatabase.Name}";

			_dataBase = new SQLDataBase(ConnectionString);

			if (!File.Exists($"{DatabaseDirectory}\\SteamInformation.mdf"))
			{
				_dataBase.Database.CreateIfNotExists();
			}

			#endregion

			#region Bot Starts, Logging and Main While thread.
			LAEH = new LoggingAndErrorHandler();
			AppDomain.CurrentDomain.FirstChanceException += LAEH.FirstChanceHandler;

			MinorDataHandler = new MinorDataHandler();
			MinorDataHandler.ReadData();

			if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);

			DiscordClient = new DiscordBot();
			DiscordClient.StartDiscordBot(args[2]).GetAwaiter().GetResult();

			if (args.Length > 2 && ulong.TryParse(args[3], out var discordID))
				OverrideDiscordID = discordID;

			SteamClient = new SteamBot(args, DiscordClient);

			while (SteamClient.IsRunning)
			{
				SteamClient.Manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
			}
			#endregion
		}
	}
}
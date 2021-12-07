using SteamUpdateProject.Discord;
using SteamUpdateProject.Steam;
using System;
using System.IO;

namespace SteamUpdateProject
{
	/*
	 * To-Do
	 * 2. Queue system for Steam ratelimiting
	 * 3. Queue system for Discord ratelimiting (Might be included in DSharpPlus?)
	 */

	/// <summary>
	/// Main logic that handles the steam bot, discord bot and managing the database.
	/// </summary>
	internal class SteamUpdateBot
	{
		/// <summary>
		/// Main discord bot that we can talk to.
		/// </summary>
		public static DiscordBot DiscordClient;

		/// <summary>
		/// Main Steam bot that we talk to and listen for any updates on.
		/// </summary>
		public static SteamBot SteamClient;

		/// <summary>
		/// SQL Server Management Objects Handler.
		/// </summary>
		public static SMOHandler SMOHandler;

		/// <summary>
		/// Handles minor data for the bot overall like total updates, time running ect.
		/// </summary>
		public static MinorDataHandler MinorDataHandler;

		/// <summary>
		/// Logging and Error Handler.
		/// </summary>
		public static LoggingAndErrorHandler LAEH;

		/// <summary>
		/// Main path to logs.
		/// </summary>
		public static string LogPath = Directory.GetCurrentDirectory() + "\\logs\\";

		/// <summary>
		/// Connection String for our database.
		/// </summary>
		public static string ConnectionString = $"Integrated Security=true;";

		/// <summary>
		/// Where the Database is located at on the drive.
		/// </summary>
		public static string DatabaseDirectory = $"{Directory.GetCurrentDirectory()}\\database";

		/// <summary>
		/// Discord User ID that we use to override any channel permissions, needs <see cref="DiscordBot.DevOverride"/> to be true before it will check this.
		/// </summary>
		public static ulong OverrideDiscordID = 0;

		private static SQLDataBase _dataBase;

		/// <summary>
        /// Main entry for the program. It all goes downhill.
		/// </summary>
		public static void Main()
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

			var Config = MinorDataHandler.GetConfig();

			if (!Directory.Exists(LogPath))
				Directory.CreateDirectory(LogPath);

			DiscordClient = new DiscordBot();
			DiscordClient.StartDiscordBot(Config.Token).GetAwaiter().GetResult();

            OverrideDiscordID = ulong.Parse(Config.DevOverride);

			SteamClient = new SteamBot(Config.SteamName, Config.SteamPassword, DiscordClient);

			while (SteamClient.IsRunning)
			{
				SteamClient.Manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
			}
			#endregion
		}
	}
}
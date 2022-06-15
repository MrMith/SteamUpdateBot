﻿using SteamUpdateProject.Discord;
using SteamUpdateProject.Steam;
using System;
using System.IO;

namespace SteamUpdateProject
{
    /* To-Do
     * 
	 *  Long Term
	 *  1. Queue system for Steam ratelimiting
	 *  2. Queue system for Discord ratelimiting (Might be included in DSharpPlus?)
	 * 
	 *  Short Term
	 *  
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
		/// Handles minor data for the bot overall like total updates, time running ect.
		/// </summary>
		public static MinorDataHandler MinorDataHandler;

		/// <summary>
		/// Logging and Error Handler.
		/// </summary>
		public static LoggingAndErrorHandler LAEH;

		/// <summary>
		/// Name of mongoDB database. This is so I can't mispell the database name.
		/// </summary>
		public static string DatabaseName = "SteamInformation";

		/// <summary>
		/// Main path to logs.
		/// </summary>
		public static string LogPath = Directory.GetCurrentDirectory() + "\\logs\\";

		/// <summary>
		/// Discord User ID that we use to override any channel permissions, needs <see cref="DiscordBot.DevOverride"/> to be true before it will check this.
		/// </summary>
		public static ulong OverrideDiscordID = 0;

		/// <summary>
		/// This contains the client we connect to the database with.
		/// </summary>
		public static DataBaseHandler DB;

		/// <summary>
        /// Main entry for the program. It all goes downhill.
		/// </summary>
		public static void Main()
		{
			#region Database start

			DB = new DataBaseHandler();
			#endregion

			#region Bot Starts, Logging and Main While thread.

			LAEH = new LoggingAndErrorHandler();
			AppDomain.CurrentDomain.FirstChanceException += LAEH.FirstChanceHandler;

			MinorDataHandler = new MinorDataHandler();
			MinorDataHandler.ReadData();

			ConfigHandler Config = new ConfigHandler();

			if (!Directory.Exists(LogPath))
				Directory.CreateDirectory(LogPath);

			DiscordClient = new DiscordBot();
			DiscordClient.StartDiscordBot(Config).GetAwaiter().GetResult();

            OverrideDiscordID = ulong.Parse(Config.DiscordID);

			SteamClient = new SteamBot(Config.SteamName, Config.SteamPW, DiscordClient);

			while (SteamClient.IsRunning)
			{
				SteamClient.Manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
			}
			#endregion
		}
	}
}
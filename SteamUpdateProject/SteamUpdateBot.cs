using System;
using SteamUpdateProject.DiscordLogic;
using SteamUpdateProject.Steam;
using System.IO;
using System.Runtime.ExceptionServices;

namespace SteamUpdateProject
{
	/*
	 * To-Do
	 * 1. Fucking Shards, How Do They Work? 
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
		public static MinorDataHandler INIHandler;

		/// Total number of exceptions :)
		public static long Exceptions = 0;
		/// Total number of apps with detectable content filled updates (Basically if its not a store tag change and its public.)
		public static long ContentUpdates = 0;
		/// Total number of app updates
		public static long Updates = 0;
		/// Total number of minutes this program as been 
		public static long MinutesRunning = 0;

		private static SQLDataBase _database;
		public static bool FirstStartUp = true;

		public static string LogPath = Directory.GetCurrentDirectory() + "\\logs\\";
		public static string ConnectionString = $"Integrated Security=true;";
		public static string DatabaseDirectory = $"{Directory.GetCurrentDirectory()}\\database";
		public static ulong OverrideDiscordID;

		/// <summary>
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

			_database = new SQLDataBase(ConnectionString);

			if (!File.Exists($"{DatabaseDirectory}\\SteamInformation.mdf"))
			{
				_database.Database.CreateIfNotExists();
			}

			#endregion

			#region Bot Starts, Logging and Main While thread.
			AppDomain.CurrentDomain.FirstChanceException += FirstChanceHandler;

			INIHandler = new MinorDataHandler();
			INIHandler.ReadData();

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

		private static void FirstChanceHandler(object sender, FirstChanceExceptionEventArgs e)
		{
			Exceptions++;
			LogCancer(e.Exception);
		}

		/// <summary>
		/// This is my custom debug to console so when we have an error I can do look at [Code 0.0] and then go debugger it.
		/// </summary>
		/// <param name="Code">Which debug code</param>
		/// <param name="Type">Steam or Discord</param>
		public static void CustomError(string Code, string Type, Exception e = null)
		{
			/* 1.0 Discord -> DM app update
			 * 1.1 Discord -> Server app uppate
			 * 0.0 Steam -> Get app's product info for every update.
			 * 0.1 Steam -> Get app's access token (for product info)
			 * 1.0 Steam -> product request without access token
			 * 2.0 Steam -> product request with access token
			 */

			Console.WriteLine($"Error {Type} is down or your need to check your connection. Code: {Code}");

			if(e != null)
			{
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// This is my primary logging function to try and understand what breaks and when it does so.
		/// </summary>
		/// <param name="e">Exception that makes me cry</param>
		public static void LogCancer(Exception e)
		{
			if (e is System.Threading.Tasks.TaskCanceledException || e is System.Net.WebSockets.WebSocketException || e is DSharpPlus.Exceptions.ServerErrorException)
				return;

			if (e is IOException)
			{
				Console.WriteLine(e.InnerException);
				Console.WriteLine(e.Message);
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine(e.StackTrace);
				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine(e.Source?.ToString());
				Console.WriteLine(e.TargetSite?.ToString());
				Console.WriteLine();
				Console.WriteLine();
				return;
			}

			try
			{
				if (!Directory.Exists(Directory.GetCurrentDirectory() + $"//logs//")) Directory.CreateDirectory(Directory.GetCurrentDirectory() + $"//logs//");

				using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + $"//logs//log{GetFormattedDate()}.txt"))
				{
					sw.WriteLine(e.InnerException);
					sw.WriteLine(e.Message);
					sw.WriteLine();
					sw.WriteLine();
					sw.WriteLine(e.StackTrace);
					sw.WriteLine();
					sw.WriteLine(e.GetBaseException());
					sw.WriteLine();
					sw.WriteLine(e.ToString());
					sw.WriteLine();
					sw.WriteLine(e.Source?.ToString());
					sw.WriteLine(e.TargetSite?.ToString());
					sw.WriteLine();
					sw.WriteLine();
				}

			}
			catch
			{
				// You Get Nothing! You Lose! Good Day, Sir! 
			}
		}

		/// <summary>
		/// Formatted date so windows can have the filename include the date.
		/// </summary>
		/// <returns>Windows formatted string</returns>
		public static string GetFormattedDate()
		{
			return DateTime.UtcNow.ToString("yyyy-dd-M--HH-mm-ss");
		}
	}
}
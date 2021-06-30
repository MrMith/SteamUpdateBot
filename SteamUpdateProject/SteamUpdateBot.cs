using System;
using SteamUpdateProject.DiscordLogic;
using SteamUpdateProject.Steam;
using System.IO;
using System.Runtime.ExceptionServices;
using DSharpPlus;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace SteamUpdateProject
{
	//To-do

	//Move backdoor to being a runtime argument

	class SteamUpdateBot
	{
		public static DiscordBot DiscordClient;
		public static SteamBot SteamClient;
		public static SMOHandler SMOHandler;
		public static INIHandler INIHandler;

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

			INIHandler = new INIHandler();

			INIHandler.ReadData();

			if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
			DiscordClient = new DiscordBot();

			//DiscordClient.StartDiscordBot("").GetAwaiter().GetResult();
			DiscordClient.StartDiscordBot(args[2]).GetAwaiter().GetResult();

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
			}

		}

		/// <summary>
		/// Replaces the : character for a - character and replaces the / character for a _ character so it can be used to represent file names in Windows.
		/// </summary>
		/// <returns>Windows formatted string</returns>
		public static string GetFormattedDate()
		{
			return DateTime.UtcNow.ToString("yyyy-dd-M--HH-mm-ss");
		}
	}
}
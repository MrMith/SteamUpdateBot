using System;
using SteamUpdateProject.DiscordLogic;
using SteamUpdateProject.Steam;
using System.IO;
using System.Runtime.ExceptionServices;
using DSharpPlus;
using System.Linq;

namespace SteamUpdateProject
{
	class SteamUpdateBot
	{
		public static DiscordBot DiscordClient;
		public static SteamBot SteamClient;

		public static long Exceptions = 0; // Total number of exceptions :)
		public static long ContentUpdates = 0; // Total number of apps with detectable content filled updates (Basically if its not a store tag change and its public.)
		public static long Updates = 0; // Total number of app updates

		private static SQLDataBase Database;

		public static string LogPath = Directory.GetCurrentDirectory() + "\\logs\\";
		public static string ConnectionString = @"Data Source=(localdb)\mssqllocaldb;Initial Catalog=SteamUpdateProjectTest1.SQLDataBase;Integrated Security=True;MultipleActiveResultSets=True";

		public static void Main(string[] args)
		{
			Database = new SQLDataBase(ConnectionString);
			Database.Database.CreateIfNotExists();

			AppDomain.CurrentDomain.FirstChanceException += FirstChanceHandler;

			if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
			DiscordClient = new DiscordBot();

			DiscordClient.StartDiscordBot("NjM0MjUxMTU4NjE3MDYzNDI0.XpS8oA.URkcwaHa8l098vaNDSo42V-qm7A").GetAwaiter().GetResult();
			//DiscordClient.StartDiscordBot(args[2]).GetAwaiter().GetResult();

			using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				Updates = context.AppInfoData.ToList().Last().Key;
			}

			SteamClient = new SteamBot(args, DiscordClient);

			while (SteamClient.isRunning)
			{
				SteamClient.manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
			}
		}

		private static void FirstChanceHandler(object sender, FirstChanceExceptionEventArgs e)
		{
			Exceptions++;
			LogCancer(e.Exception);
		}

		public static void LogCancer(Exception e)
		{
			if (e is System.Threading.Tasks.TaskCanceledException || e is System.Net.WebSockets.WebSocketException || e is DSharpPlus.Exceptions.ServerErrorException)
				return;

			if(e is IOException)
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

		public static string GetFormattedDate()
		{
			return DateTime.UtcNow.ToString().Replace("/","_").Replace(":","-");
		}

	}
}
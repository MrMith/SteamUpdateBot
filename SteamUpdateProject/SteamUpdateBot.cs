using System;
using SteamUpdateProject.Discord;
using SteamUpdateProject.Steam;
using System.IO;
using System.Runtime.ExceptionServices;

namespace SteamUpdateProject
{
	class SteamUpdateBot
	{
		public static DiscordBot DiscordClient;
		public static SteamBot SteamClient;

		private static SQLDataBase Database;

		public static string LogPath = Directory.GetCurrentDirectory() + "\\logs\\";
		public static string ConnectionString = @"Data Source=(localdb)\mssqllocaldb;Initial Catalog=SteamUpdateProjectAids4.SQLDataBase;Integrated Security=True;MultipleActiveResultSets=True";
		public static void Main(string[] args)
		{
			Database = new SQLDataBase(ConnectionString);
			Database.Database.CreateIfNotExists();

			AppDomain.CurrentDomain.FirstChanceException += FirstChanceHandler;

			if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
			DiscordClient = new DiscordBot();
			DiscordClient.MainAsync("NjM0MjUxMTU4NjE3MDYzNDI0.XpS8oA.URkcwaHa8l098vaNDSo42V-qm7A").GetAwaiter().GetResult();
			//DiscordClient.MainAsync(args[2]).GetAwaiter().GetResult();

			SteamClient = new SteamBot(args, DiscordClient);

			while (SteamClient.isRunning)
			{
				SteamClient.manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
			}
		}

		private static void FirstChanceHandler(object sender, FirstChanceExceptionEventArgs e)
		{
			LogCancer(e.Exception);
		}

		public static void LogCancer(Exception e)
		{
			//using (StreamWriter sw z= File.CreateText(LogPath + DateTime.UtcNow.ToShortTimeString() + e.Message + ".txt"))
			//	{
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
		//}
	}
}
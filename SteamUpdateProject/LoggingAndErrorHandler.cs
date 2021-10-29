using System;
using SteamUpdateProject.DiscordLogic;
using SteamUpdateProject.Steam;
using System.IO;
using System.Runtime.ExceptionServices;

namespace SteamUpdateProject
{
	/// <summary>
	/// This class handles any errors and then logging said errors to give a easier time when debugging.
	/// </summary>
	class LoggingAndErrorHandler
	{
		/// Total number of exceptions :)
		public static long Exceptions = 0;
		/// Total number of apps with detectable content filled updates (Basically if its not a store tag change and its public.)
		public static long ContentUpdates = 0;
		/// Total number of app updates
		public static long Updates = 0;
		/// Total number of minutes this program as been 
		public static long MinutesRunning = 0;

		public void FirstChanceHandler(object sender, FirstChanceExceptionEventArgs e)
		{
			Exceptions++;
			LogCancer(e.Exception);
		}

		/// <summary>
		/// This is my custom debug to console so when we have an error I can do look at [Code 0.0] and then go debugger it.
		/// </summary>
		/// <param name="Code">Which debug code</param>
		/// <param name="Type">Steam or Discord</param>
		public void CustomError(CustomErrorType Code, Platform Type, Exception e = null)
		{
			Console.WriteLine($"Error {Type} is down or your need to check your connection. Code: {Code}");

			if (e != null)
			{
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// This is my primary logging function to try and understand what breaks and when it does so.
		/// </summary>
		/// <param name="e">Exception that makes me cry</param>
		public void LogCancer(Exception e)
		{
			if (e is System.Threading.Tasks.TaskCanceledException || e is System.Net.WebSockets.WebSocketException || e is DSharpPlus.Exceptions.ServerErrorException || e is SteamKit2.AsyncJobFailedException)
				return;

			if (e is IOException)
			{
				Console.WriteLine(e.StackTrace);
				Console.WriteLine();
				return;
			}

			try
			{
				if (!Directory.Exists(Directory.GetCurrentDirectory() + $"//logs//")) Directory.CreateDirectory(Directory.GetCurrentDirectory() + $"//logs//");

				using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + $"//logs//log{GetFormattedDate()}.txt"))
				{
					sw.WriteLine(e.StackTrace);
					sw.WriteLine();
					sw.WriteLine();
					sw.WriteLine(e.InnerException);
					sw.WriteLine();
					sw.WriteLine();
					sw.WriteLine(e.GetBaseException());
					sw.WriteLine();
					sw.WriteLine();
					sw.WriteLine(e.Message);
					sw.WriteLine();
					sw.WriteLine();
					sw.WriteLine(e.Source);
					sw.WriteLine();
					sw.WriteLine();
					sw.WriteLine(e.TargetSite);
					sw.WriteLine();
					sw.WriteLine((e is DSharpPlus.Exceptions.UnauthorizedException) ? (e as DSharpPlus.Exceptions.UnauthorizedException).JsonMessage : "");
					sw.WriteLine();
				}
			}
			catch
			{
				// You Get Nothing! You Lose! Good Day, Sir! 
			}
		}

		/// <summary>
		/// Error enum for my code so I don't have to use magic strings
		/// </summary>
		public enum CustomErrorType
		{
			Discord_DM = 0,               //DM app update
			Discord_AppUpdate = 1,        //Server app update
			Steam_AppInfoGet = 2,         // Get app's product info for every update.
			Steam_AppInfoToken = 3,       //Get app's access token (for product info)
			Steam_ProductReqNoToken = 4,  //product request without access token
			Steam_ProductReqYesToken = 5, //product request with access token
		}

		/// <summary>
		/// Which platform we're reporting an error on.
		/// </summary>
		public enum Platform
		{
			Steam = 0,
			Discord = 1,
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

using System;
using System.IO;
using System.Runtime.ExceptionServices;

namespace SteamUpdateProject
{
    //To-do -> use microsoft's logging system and not this garbo.
	/// <summary>
	/// This class handles any errors and then logging said errors to give a easier time when debugging.
	/// </summary>
	internal class LoggingAndErrorHandler
	{
		/// Total number of exceptions
		public static long Exceptions = 0;
		/// Total number of apps with detectable content changes.
		public static long ContentUpdates = 0;
		/// Total number of app updates.
		public static long Updates = 0;
		/// Total number of minutes this program as been running.
		public static long MinutesRunning = 0;

        /// <summary>
        /// Intercepts any exceptions that arise in the program during runtime.
        /// </summary>
		public void FirstChanceHandler(object sender, FirstChanceExceptionEventArgs e)
		{
			Exceptions++;
			BadlyFormattedFunction(e.Exception);
		}

		/// <summary>
		/// This is my custom debug to console so when we have an error I can do look at [Code 0.0] and then go debugger it.
		/// </summary>
		/// <param name="code">Which debug code</param>
		/// <param name="type">Steam or Discord</param>
		public void CustomError(CustomErrorType code, Platform type, Exception e = null)
		{
			Console.WriteLine($"Error {type} is down or your need to check your connection. Code: {code}");

			if (e != null)
			{
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// This is my primary logging function to try and understand what breaks and when it does so.
		/// </summary>
		/// <param name="e">Exception that makes me cry</param>
		public void BadlyFormattedFunction(Exception e)
		{
			if (e is System.Net.Sockets.SocketException || e is System.IO.IOException)
				return; //What the hell even is this, no line numbers or anything. Just a meaningless error.

            Console.WriteLine(e);
		    //To-do: Implement Microsoft's logging system.
		}

		/// <summary>
		/// Error enum for my code so I don't have to use strings
		/// </summary>
		public enum CustomErrorType
		{
			/// <summary>
			/// Direct Message a user on Discord about a Steam Application Update.
			/// </summary>
			Discord_DM = 0,
			/// <summary>
			/// Sending a message in a Discord guild's text channel about a Steam Application Update.
			/// </summary>
			Discord_AppUpdate = 1,
			/// <summary>
			/// Getting a Steam Application product information.
			/// </summary>
			Steam_AppInfoGet = 2,
			/// <summary>
			/// Getting a Steam Application's access token for a Update.
			/// </summary>
			Steam_AppInfoToken = 3,
			/// <summary>
			/// Getting a Steam Application product information WITH-<b>OUT</b> a token.
			/// </summary>
			Steam_ProductReqNoToken = 4,
			/// <summary>
			/// Getting a Steam Application product information <b>WITH</b> a token.
			/// </summary>
			Steam_ProductReqYesToken = 5,
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

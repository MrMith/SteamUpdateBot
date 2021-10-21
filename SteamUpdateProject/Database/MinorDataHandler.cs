using System.IO;

namespace SteamUpdateProject
{
	/// <summary>
	/// This handles small data like <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> so we can keep track of those small things.
	/// </summary>
	class MinorDataHandler
	{
		private string _operatingFile = Directory.GetCurrentDirectory() + "//SteamData.data";

		private enum DataEnum
		{
			Updates = 0,
			Content = 1,
			Exceptions = 2,
			MinutesRunning = 3
		}

		/// <summary>
		/// Writes <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> into <see cref="SteamData.data"/>
		/// </summary>
		public void WriteData()
		{
			using (StreamWriter writer = new StreamWriter(_operatingFile))
			{
				writer.WriteLine(LoggingAndErrorHandler.Updates);
				writer.WriteLine(LoggingAndErrorHandler.ContentUpdates);
				writer.WriteLine(LoggingAndErrorHandler.Exceptions);
				writer.WriteLine(LoggingAndErrorHandler.MinutesRunning);
			}
		}

		/// <summary>
		/// Read contents of SteamData.data and updates <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/>
		/// </summary>
		public void ReadData()
		{
			if (!File.Exists(_operatingFile))
				return;

			using StreamReader reader = new StreamReader(_operatingFile);

			string[] dataByLine = reader.ReadToEnd().Split("\n");
			for (int i = 0; i <= dataByLine.Length - 1; i++)
			{
				if (!long.TryParse(dataByLine[i], out long number))
					return;

				switch ((DataEnum)i)
				{
					case DataEnum.Updates:
						{
							LoggingAndErrorHandler.Updates = number;
							break;
						}
					case DataEnum.Content:
						{
							LoggingAndErrorHandler.ContentUpdates = number;
							break;
						}
					case DataEnum.Exceptions:
						{
							LoggingAndErrorHandler.Exceptions = number;
							break;
						}
					case DataEnum.MinutesRunning:
						{
							LoggingAndErrorHandler.MinutesRunning = number;
							break;
						}
				}
			}
		}
	}
}

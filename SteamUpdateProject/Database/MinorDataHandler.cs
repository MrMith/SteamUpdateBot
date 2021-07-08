using System.IO;

namespace SteamUpdateProject
{
	/// <summary>
	/// This handles small data like <see cref="SteamUpdateBot.Updates"/>, <see cref="SteamUpdateBot.ContentUpdates"/>, <see cref="SteamUpdateBot.Exceptions"/> and finally <see cref="SteamUpdateBot.MinutesRunning"/> so we can keep track of those small things.
	/// </summary>
	class MinorDataHandler
	{
		private string _operatingFile = Directory.GetCurrentDirectory() + "//SteamData.data";

		/// <summary>
		/// Writes <see cref="SteamUpdateBot.Updates"/>, <see cref="SteamUpdateBot.ContentUpdates"/>, <see cref="SteamUpdateBot.Exceptions"/> and finally <see cref="SteamUpdateBot.MinutesRunning"/> into <see cref="SteamData.data"/>
		/// </summary>
		public void WriteData()
		{
			using (StreamWriter writer = new StreamWriter(_operatingFile))
			{
				writer.WriteLine(SteamUpdateBot.Updates);
				writer.WriteLine(SteamUpdateBot.ContentUpdates);
				writer.WriteLine(SteamUpdateBot.Exceptions);
				writer.WriteLine(SteamUpdateBot.MinutesRunning);
			}
		}

		/// <summary>
		/// Read contents of SteamData.data and updates <see cref="SteamUpdateBot.Updates"/>, <see cref="SteamUpdateBot.ContentUpdates"/>, <see cref="SteamUpdateBot.Exceptions"/> and finally <see cref="SteamUpdateBot.MinutesRunning"/>
		/// </summary>
		public void ReadData()
		{
			if (!File.Exists(_operatingFile))
				return;

			using (StreamReader reader = new StreamReader(_operatingFile))
			{
				string[] dataByLine = reader.ReadToEnd().Split("\n");
				for (int i = 0; i <= dataByLine.Length - 1; i++)
				{
					if (long.TryParse(dataByLine[i], out long number))
					{
						switch(i)
						{
							case 0: //Updates
								{
									SteamUpdateBot.Updates = number;
									break;
								}
							case 1: //Content Updates
								{
									SteamUpdateBot.ContentUpdates = number;
									break;
								}
							case 2: //Exceptions
								{
									SteamUpdateBot.Exceptions = number;
									break;
								}
							case 3: //MinutesRunning
								{
									SteamUpdateBot.MinutesRunning = number;
									break;
								}
						}
					}
				}
			}
		}
	}
}

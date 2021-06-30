﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SteamUpdateProject
{
	/// <summary>
	/// This handles small data like <see cref="SteamUpdateBot.Updates"/>, <see cref="SteamUpdateBot.ContentUpdates"/>, <see cref="SteamUpdateBot.Exceptions"/> and finally <see cref="SteamUpdateBot.MinutesRunning"/> so we can keep track of those small things.
	/// </summary>
	class INIHandler
	{
		private string _operatingPath = Directory.GetCurrentDirectory();
		private string _operatingFile = Directory.GetCurrentDirectory() + "//SteamData.ini";

		/// <summary>
		/// Writes <see cref="SteamUpdateBot.Updates"/>, <see cref="SteamUpdateBot.ContentUpdates"/>, <see cref="SteamUpdateBot.Exceptions"/> and finally <see cref="SteamUpdateBot.MinutesRunning"/> into <see cref="SteamData.ini"/>
		/// </summary>
		public void WriteData()
		{
			MakeSureFileExists();

			using (StreamWriter writer = new StreamWriter(_operatingFile))
			{
				writer.WriteLine(SteamUpdateBot.Updates);
				writer.WriteLine(SteamUpdateBot.ContentUpdates);
				writer.WriteLine(SteamUpdateBot.Exceptions);
				writer.WriteLine(SteamUpdateBot.MinutesRunning);
			}
		}

		/// <summary>
		/// Read contents of SteamData.ini and updates <see cref="SteamUpdateBot.Updates"/>, <see cref="SteamUpdateBot.ContentUpdates"/>, <see cref="SteamUpdateBot.Exceptions"/> and finally <see cref="SteamUpdateBot.MinutesRunning"/>
		/// </summary>
		public void ReadData()
		{
			MakeSureFileExists();

			using (StreamReader reader = new StreamReader(_operatingFile))
			{
				string[] INIDataByLine = reader.ReadToEnd().Split("\n");
				for (int i = 0; i <= INIDataByLine.Length - 1; i++)
				{
					if (long.TryParse(INIDataByLine[i], out long number))
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

		private void MakeSureFileExists()
		{
			if(!File.Exists(_operatingFile))
			{
				File.Create(_operatingFile);
			}
		}
	}
}

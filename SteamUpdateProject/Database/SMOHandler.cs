using System;
using Microsoft.SqlServer.Management.Smo;

namespace SteamUpdateProject
{
	/// <summary>
	/// This handles everything related to SMO aka SQL Server Management Objects.
	/// See this for more reading -> https://docs.microsoft.com/en-us/sql/relational-databases/server-management-objects-smo/overview-smo?view=sql-server-ver15
	/// </summary>
	class SMOHandler
	{
		public Database SMODatabase;
		public Server SMOServer;

		public SMOHandler()
		{
			SMOServer = new Server();

			foreach (object dataBase in SMOServer.Databases)
			{
				if (dataBase.ToString().Contains("STEAMINFORMATION", StringComparison.OrdinalIgnoreCase))
				{
					SMODatabase = dataBase as Database;
				}
			}

			Console.WriteLine(SMODatabase.Name);
		}
	}
}

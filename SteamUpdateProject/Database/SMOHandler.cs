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
			SMOServer.ConnectionContext.Connect();

			SMODatabase = GetDatabase();

			if(!SMODatabase.IsAccessible)
			{
				SMODatabase.Drop();
				SMODatabase = new Database(SMOServer, "STEAMINFORMATION");
				SMODatabase.Create(false);
			}
		}

		private Database GetDatabase()
		{
			foreach (object dataBase in SMOServer.Databases)
			{
				if (dataBase.ToString().Contains("STEAMINFORMATION", StringComparison.OrdinalIgnoreCase))
				{
					return dataBase as Database;
				}
			}

			Database NewDB = new Database(SMOServer, "STEAMINFORMATION");
			
			NewDB.Create(false);

			return NewDB;
		}

	}
}

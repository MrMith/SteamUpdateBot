using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

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

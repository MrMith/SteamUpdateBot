using SteamUpdateProject.Entities;
using System.Collections.Generic;
using MongoDB;
using MongoDB.Driver;

namespace SteamUpdateProject
{
	/// <summary>
	/// The main <see cref="DbContext"/> class that we get/set any information related to <see cref="AppInfo"/> or <see cref="GuildInfo"/>.
	/// We connect to the database with a connectionString then it will convert our C# LINQ lambda expressions into SQL Queries.
	/// </summary>
	public class DataBaseHandler
    {
		public MongoClient Client;

		public DataBaseHandler()
		{
			Client = new MongoClient();
		}
	}
}

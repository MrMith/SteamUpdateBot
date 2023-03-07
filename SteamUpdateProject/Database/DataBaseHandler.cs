using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SteamUpdateProject.Entities;

namespace SteamUpdateProject
{
	/// <summary>
	/// The main <see cref="DbContext"/> class that we get/set any information related to <see cref="AppInfo"/> or <see cref="GuildInfo"/>.
	/// </summary>
	public class DataBaseHandler
	{
		/// <summary>
		/// The client for MongoDB, this is where the data goes to die.
		/// </summary>
		public MongoClient Client;

		public DataBaseHandler()
		{
			var connectionString = "mongodb://localhost";
			var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
			clientSettings.LinqProvider = LinqProvider.V2;
			Client = new MongoClient(clientSettings);
		}
	}
}

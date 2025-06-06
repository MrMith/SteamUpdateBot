using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SteamUpdateProject.Entities;

namespace SteamUpdateProject.Database
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
			string connectionString = SteamUpdateBot.ConfigHandler.Config.DBConnectionString;
			MongoClientSettings clientSettings = MongoClientSettings.FromConnectionString(connectionString);
			Client = new MongoClient(clientSettings);
		}
	}
}

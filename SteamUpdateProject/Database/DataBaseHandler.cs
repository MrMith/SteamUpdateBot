using MongoDB.Driver;
using SteamUpdateProject.Entities;

namespace SteamUpdateProject
{
	/// <summary>
	/// The main <see cref="DbContext"/> class that we get/set any information related to <see cref="AppInfo"/> or <see cref="GuildInfo"/>.
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

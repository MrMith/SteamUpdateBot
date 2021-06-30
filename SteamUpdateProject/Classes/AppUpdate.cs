using System;

namespace SteamUpdateProject
{
	/// <summary>
	/// Dummy class that is stored in the SQL database that contains information like <see cref="Name"/>, <see cref="AppID"/>, global steam <see cref="ChangeNumber"/>, Is there any meaningful <see cref="Content"/> updates,
	/// what time (UTC) this app last updated at (that we know of) <see cref="LastUpdated"/> and DepoName that updated <see cref="DepoName"/>.
	/// </summary>
	public class AppUpdate
	{
		public string Name { get; set; }
		public uint AppID { get; set; }
		public uint ChangeNumber { get; set; }
		public bool Content { get; set; }
		public DateTime LastUpdated { get; set; }
		public string DepoName { get; set; }
	}
}

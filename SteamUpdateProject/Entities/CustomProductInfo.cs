using SteamKit2;
using System.Collections.ObjectModel;

namespace SteamUpdateProject
{
	class CustomProductInfo
	{
		public ReadOnlyCollection<SteamApps.PICSProductInfoCallback> ProductInfo { get; set; }
		public bool IsPublic { get; set; }
	}
}

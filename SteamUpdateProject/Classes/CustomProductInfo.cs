using SteamKit2;

namespace SteamUpdateProject
{
	class CustomProductInfo
	{
		public AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet ProductInfo { get; set; }
		public bool IsPublic { get; set; }
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using SteamKit2;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using SteamUpdateProject.DiscordLogic;
using System.Diagnostics;
using System.Linq;

namespace SteamUpdateProject
{
	class CustomProductInfo
	{
		public AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet ProductInfo { get; set; }
		public bool IsPublic { get; set; }
	}
}

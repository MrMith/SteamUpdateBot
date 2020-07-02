using System;
using System.Collections.Generic;
using System.Text;

namespace SteamUpdateProject
{
	public class AppUpdate
	{
		public string Name { get; set; }
		public uint AppID { get; set; }
		public uint ChangeNumber { get; set; }
		public bool Content { get; set; }
		public DateTime LastUpdated { get; set; }
	}
}

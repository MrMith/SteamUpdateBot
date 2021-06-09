using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SteamUpdateProject
{
	public class SubbedApp : IEquatable<SubbedApp>
	{
		public SubbedApp()
		{
		}

		public SubbedApp(long appid)
		{
			AppID = appid;
		}

		[Key]
		public int Key { get; set; }
		public long AppID { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as SubbedApp);
		}

		public bool Equals(SubbedApp other)
		{
			return other != null &&
				   Key == other.Key &&
				   AppID == other.AppID;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Key, AppID);
		}

		public static bool operator ==(SubbedApp left, SubbedApp right)
		{
			return EqualityComparer<SubbedApp>.Default.Equals(left, right);
		}

		public static bool operator !=(SubbedApp left, SubbedApp right)
		{
			return !(left == right);
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SteamUpdateProject
{
	public class SubedApp : IEquatable<SubedApp>
	{
		public SubedApp()
		{
		}

		public SubedApp(long appid)
		{
			AppID = appid;
		}

		[Key]
		public int Key { get; set; }
		public long AppID { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as SubedApp);
		}

		public bool Equals(SubedApp other)
		{
			return other != null &&
				   Key == other.Key &&
				   AppID == other.AppID;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Key, AppID);
		}

		public static bool operator ==(SubedApp left, SubedApp right)
		{
			return EqualityComparer<SubedApp>.Default.Equals(left, right);
		}

		public static bool operator !=(SubedApp left, SubedApp right)
		{
			return !(left == right);
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SteamUpdateProject
{
	public class AppInfo : IEquatable<AppInfo>
	{
		[Key]
		public int Key { get; set; }
		public long AppID { get; set; }
		public string Name { get; set; }
		public DateTime? LastUpdated { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as AppInfo);
		}

		public bool Equals(AppInfo other)
		{
			return other != null &&
				   AppID == other.AppID;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(AppID);
		}

		public static bool operator ==(AppInfo left, AppInfo right)
		{
			return EqualityComparer<AppInfo>.Default.Equals(left, right);
		}

		public static bool operator !=(AppInfo left, AppInfo right)
		{
			return !(left == right);
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SteamUpdateProject
{
	public class GlobalData : IEquatable<GlobalData>
	{
		public GlobalData()
		{
		}

		public GlobalData(long updates, long contentUpdates)
		{
			Updates = updates;
			ContentUpdates = contentUpdates;
		}

		[Key]
		public int Key { get; set; }
		public long Updates { get; set; }
		public long ContentUpdates { get; set; }
		public long Exceptions { get; set; }

		public override bool Equals(object obj)
		{
			return Equals(obj as GlobalData);
		}

		public bool Equals(GlobalData other)
		{
			return other != null &&
					Key == other.Key &&
					Updates == other.Updates &&
					ContentUpdates == other.ContentUpdates;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Key, Updates, Updates);
		}

		public static bool operator ==(GlobalData left, GlobalData right)
		{
			return EqualityComparer<GlobalData>.Default.Equals(left, right);
		}

		public static bool operator !=(GlobalData left, GlobalData right)
		{
			return !(left == right);
		}
	}
}

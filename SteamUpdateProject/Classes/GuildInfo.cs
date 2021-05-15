using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace SteamUpdateProject
{
	public class GuildInfo : IEquatable<GuildInfo>
	{
		[Key]
		public int Key { get; set; }
		public long GuildID { get; set; }
		public long ChannelID { get; set; }
		public bool ShowContent { get; set; }
		public bool DebugMode { get; set; }
		public bool PublicDepoOnly { get; set; }

		public virtual List<SubedApp> SubscribedApps { get; set; } = new List<SubedApp>(); //Why in the living fuck can't I just make a int list? It never populates the list but if I make this shitty class it will load from the database properly. If you know why please yell at me because I don't know what these cocaine fueled coders are doing with this shit

		public override bool Equals(object obj)
		{
			return Equals(obj as GuildInfo);
		}

		public bool Equals(GuildInfo other)
		{
			return other != null &&
				   GuildID == other.GuildID && ChannelID == other.ChannelID;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(GuildID, ChannelID);
		}

		public static bool operator ==(GuildInfo left, GuildInfo right)
		{
			return EqualityComparer<GuildInfo>.Default.Equals(left, right);
		}

		public static bool operator !=(GuildInfo left, GuildInfo right)
		{
			return !(left == right);
		}
	}
}

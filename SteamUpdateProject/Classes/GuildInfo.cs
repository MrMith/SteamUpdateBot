using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SteamUpdateProject
{
	/// <summary>
	/// This is the dummy class that is stored in the database.
	/// </summary>
	public class GuildInfo : IEquatable<GuildInfo>
	{
		[Key]
		public int Key { get; set; }

		/// <summary>
		/// Relevant GuildID
		/// </summary>
		public long GuildID { get; set; }

		/// <summary>
		/// Text Channel ID to push updates to.
		/// </summary>
		public long ChannelID { get; set; }

		/// <summary>
		/// Only show meaningful updates or not.
		/// </summary>
		public bool ShowContent { get; set; }

		/// <summary>
		/// Debug mode that just spams any update that comes through the pipeline.
		/// </summary>
		public bool DebugMode { get; set; }

		/// <summary>
		/// Only pipe through updates that have changes from the default public branch of a steam app.
		/// </summary>
		public bool PublicDepoOnly { get; set; }

		/// <summary>
		/// List of apps this certain channel is subscribed to.
		/// </summary>
		public virtual List<SubbedApp> SubscribedApps { get; set; } = new List<SubbedApp>(); //Why in the living fuck can't I just make a int list? It never populates the list but if I make this shitty class it will load from the database properly. If you know why please yell at me because I don't know what these cocaine fueled coders are doing with this shit

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
